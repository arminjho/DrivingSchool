import 'dart:async';

import 'package:driving_school_mobile/core/auth_session.dart';
import 'package:driving_school_mobile/core/notification_center.dart';
import 'package:driving_school_mobile/providers/notification_provider.dart';
import 'package:fake_async/fake_async.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import '../support/test_support.dart';

/// A center wired to a fake hub and a fake API, created inside `fakeAsync` so its timers run on
/// the test clock.
class _Harness {
  final hub = FakeHub();
  final session = AuthSession(MemoryTokenStore());
  final requests = <String>[];
  int unread = 3;
  bool serverDown = false;
  List<Map<String, dynamic>> list = [notificationJson(2), notificationJson(1, isRead: true)];
  late final NotificationCenter center;

  _Harness() {
    installClient((request) async {
      requests.add('${request.method} ${request.url.path}');
      if (serverDown) throw http.ClientException('down');

      final path = request.url.path;
      if (path.endsWith('unread-count')) return jsonResponse(unread);
      if (path.endsWith('mark-read') || path.endsWith('mark-all-read')) return http.Response('', 204);
      return jsonResponse({'resultList': list, 'count': list.length});
    });

    center = NotificationCenter(session: session, provider: NotificationProvider(session), hub: hub);
  }

  int count(String path) => requests.where((r) => r.endsWith(path)).length;

  void signIn(FakeAsync async) {
    unawaited(session.signIn(makeJwt(role: 'Student')));
    async.flushMicrotasks();
  }

  void dispose() {
    center.dispose();
    session.dispose();
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('on sign-in it connects the hub with the token and loads the unread count', () {
    fakeAsync((async) {
      final h = _Harness()..signIn(async);

      expect(h.hub.startCount, 1);
      expect(h.hub.lastToken, h.session.token);
      expect(h.center.unreadCount, 3);
      h.dispose();
    });
  });

  test('a pushed notification raises the badge at once and appears at the top of the open list', () {
    fakeAsync((async) {
      final h = _Harness()..signIn(async);
      h.center.beginWatchingList();
      async.flushMicrotasks();
      expect(h.center.items!.map((n) => n.id), [2, 1]);

      h.hub.push(notification(9));
      async.flushMicrotasks();

      expect(h.center.unreadCount, 4);
      expect(h.center.items!.map((n) => n.id), [9, 2, 1]);

      h.hub.push(notification(9));
      async.flushMicrotasks();
      expect(h.center.unreadCount, 4, reason: 'a notification already shown is not counted twice');
      h.dispose();
    });
  });

  test('polling is the fallback: without a live hub it polls every 20 seconds', () {
    fakeAsync((async) {
      final h = _Harness()..signIn(async);
      expect(h.count('unread-count'), 1);

      async.elapse(const Duration(seconds: 19));
      expect(h.count('unread-count'), 1);

      async.elapse(const Duration(seconds: 2));
      expect(h.count('unread-count'), 2);

      async.elapse(const Duration(seconds: 20));
      expect(h.count('unread-count'), 3);
      h.dispose();
    });
  });

  test('once the hub is live polling drops to a slow safety net', () {
    fakeAsync((async) {
      final h = _Harness()..signIn(async);
      h.hub.setConnected(true);
      async.flushMicrotasks();
      final afterConnect = h.count('unread-count');
      expect(afterConnect, 2, reason: 'connecting triggers a catch-up refresh');

      async.elapse(const Duration(seconds: 60));
      expect(h.count('unread-count'), afterConnect, reason: 'no 20s polling while push is working');

      async.elapse(NotificationCenter.connectedPollInterval);
      expect(h.count('unread-count'), afterConnect + 1);
      h.dispose();
    });
  });

  test('when the hub drops, polling resumes at the short interval', () {
    fakeAsync((async) {
      final h = _Harness()..signIn(async);
      h.hub.setConnected(true);
      async.flushMicrotasks();
      final connected = h.count('unread-count');

      h.hub.setConnected(false);
      async.flushMicrotasks();
      async.elapse(const Duration(seconds: 21));

      expect(h.count('unread-count'), greaterThan(connected + 1));
      h.dispose();
    });
  });

  test('an unreachable server is retried with growing delays up to a ceiling', () {
    fakeAsync((async) {
      final h = _Harness()..serverDown = true;
      h.signIn(async);
      final attemptTimes = <int>[];
      var seen = h.count('unread-count');
      attemptTimes.add(0);

      for (var second = 1; second <= 700; second++) {
        async.elapse(const Duration(seconds: 1));
        if (h.count('unread-count') != seen) {
          seen = h.count('unread-count');
          attemptTimes.add(second);
        }
      }

      final gaps = [for (var i = 1; i < attemptTimes.length; i++) attemptTimes[i] - attemptTimes[i - 1]];
      expect(gaps.take(4), [40, 80, 160, 300], reason: 'doubles from the first failure, capped at 5 minutes');
      h.dispose();
    });
  });

  test('the list is only fetched while a screen is watching it, and in parallel with the count', () {
    fakeAsync((async) {
      final h = _Harness()..signIn(async);
      expect(h.center.items, isNull);
      expect(h.count('/api/notifications'), 0);

      h.center.beginWatchingList();
      async.flushMicrotasks();

      expect(h.center.items, hasLength(2));

      h.center.endWatchingList();
      final listCalls = h.requests.where((r) => r == 'GET /api/notifications').length;
      async.elapse(const Duration(seconds: 25));
      expect(h.requests.where((r) => r == 'GET /api/notifications').length, listCalls);
      h.dispose();
    });
  });

  test('marking one read updates the row and badge before the server answers', () {
    fakeAsync((async) {
      final h = _Harness()..signIn(async);
      h.center.beginWatchingList();
      async.flushMicrotasks();
      final unreadItem = h.center.items!.firstWhere((n) => !n.isRead);

      unawaited(h.center.markAsRead(unreadItem));

      expect(h.center.items!.firstWhere((n) => n.id == unreadItem.id).isRead, isTrue);
      expect(h.center.unreadCount, 2);
      async.flushMicrotasks();
      expect(h.count('mark-read'), 1);
      h.dispose();
    });
  });

  test('marking all read zeroes the badge and calls the server once', () {
    fakeAsync((async) {
      final h = _Harness()..signIn(async);
      h.center.beginWatchingList();
      async.flushMicrotasks();

      unawaited(h.center.markAllAsRead());

      expect(h.center.unreadCount, 0);
      expect(h.center.items!.every((n) => n.isRead), isTrue);
      async.flushMicrotasks();
      expect(h.count('mark-all-read'), 1);
      h.dispose();
    });
  });

  test('signing out stops the hub and clears the previous user\'s notifications', () {
    fakeAsync((async) {
      final h = _Harness()..signIn(async);
      h.center.beginWatchingList();
      async.flushMicrotasks();
      final stopsBefore = h.hub.stopCount;

      h.session.clear();
      async.flushMicrotasks();

      expect(h.hub.stopCount, greaterThan(stopsBefore));
      expect(h.center.unreadCount, 0);
      expect(h.center.items, isNull);

      final callsAfterLogout = h.requests.length;
      async.elapse(const Duration(minutes: 3));
      expect(h.requests.length, callsAfterLogout, reason: 'no polling while signed out');
      h.dispose();
    });
  });
}
