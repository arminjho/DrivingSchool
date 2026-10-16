import 'dart:async';
import 'dart:convert';

import 'package:driving_school_desktop/core/app_theme.dart';
import 'package:driving_school_desktop/core/auth_session.dart';
import 'package:driving_school_desktop/core/base_provider.dart';
import 'package:driving_school_desktop/core/notification_hub.dart';
import 'package:driving_school_desktop/core/token_store.dart';
import 'package:driving_school_desktop/models/notification_item.dart';
import 'package:http/http.dart' as http;
import 'package:flutter_test/flutter_test.dart';
import 'package:http/testing.dart';
import 'package:material_ui/material_ui.dart';

/// A signed-looking JWT carrying the same claims the API issues. The signature is not checked
/// client-side, so a placeholder is enough.
String makeJwt({
  String role = 'Admin',
  int userId = 1,
  String email = 'desktop',
  String firstName = 'Admin',
  String lastName = 'Korisnik',
  Duration validFor = const Duration(hours: 1),
}) {
  String encode(Map<String, dynamic> json) => base64Url.encode(utf8.encode(jsonEncode(json))).replaceAll('=', '');

  final payload = {
    'jti': 'test-jti',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '$userId',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': email,
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname': firstName,
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname': lastName,
    'exp': DateTime.now().toUtc().add(validFor).millisecondsSinceEpoch ~/ 1000,
  };
  return '${encode({'alg': 'HS256', 'typ': 'JWT'})}.${encode(payload)}.signature';
}

class MemoryTokenStore implements TokenStore {
  String? token;

  MemoryTokenStore([this.token]);

  @override
  Future<String?> read() async => token;

  @override
  Future<void> write(String value) async => token = value;

  @override
  Future<void> delete() async => token = null;
}

class FakeHub implements NotificationHub {
  final _notifications = StreamController<NotificationItem>.broadcast();
  final _connection = StreamController<bool>.broadcast();
  bool _connected = false;
  int startCount = 0;
  int stopCount = 0;
  String? lastToken;

  @override
  Stream<NotificationItem> get notifications => _notifications.stream;

  @override
  Stream<bool> get connection => _connection.stream;

  @override
  bool get isConnected => _connected;

  void push(NotificationItem item) => _notifications.add(item);

  void setConnected(bool value) {
    _connected = value;
    _connection.add(value);
  }

  @override
  Future<void> start(String accessToken) async {
    startCount++;
    lastToken = accessToken;
  }

  @override
  Future<void> stop() async {
    stopCount++;
    _connected = false;
  }

  @override
  Future<void> dispose() async {
    await _notifications.close();
    await _connection.close();
  }
}

NotificationItem notification(int id, {bool isRead = false, String title = 'Naslov'}) => NotificationItem(
      id: id,
      title: title,
      message: 'Poruka $id',
      type: 'LessonConfirmed',
      isRead: isRead,
      createdAt: DateTime.utc(2026, 10, 16, 12),
    );

Map<String, dynamic> notificationJson(int id, {bool isRead = false}) => {
      'id': id,
      'title': 'Naslov',
      'message': 'Poruka $id',
      'type': 'LessonConfirmed',
      'isRead': isRead,
      'createdAt': '2026-10-16T12:00:00',
    };

http.Response jsonResponse(Object body, [int status = 200]) =>
    http.Response(jsonEncode(body), status, headers: {'content-type': 'application/json; charset=utf-8'});

/// Routes every API call of a test to [handler] for the duration of the test.
final List<VoidCallback> _cleanups = [];

/// Registers something to dispose when the current [testApp] ends.
void onTestEnd(VoidCallback callback) => _cleanups.add(callback);

/// `testWidgets` that unmounts the app and runs the registered cleanups *before* the test ends:
/// Flutter fails a test that still has a timer pending, and a signed-in `AuthSession` holds one
/// (its token-expiry timer) until it is disposed.
void testApp(String description, Future<void> Function(WidgetTester tester) body) {
  testWidgets(description, (tester) async {
    try {
      await body(tester);
    } finally {
      await tester.pumpWidget(const SizedBox.shrink());
      for (final cleanup in _cleanups.reversed) {
        cleanup();
      }
      _cleanups.clear();
    }
  });
}

MockClient installClient(Future<http.Response> Function(http.Request request) handler) {
  final client = MockClient(handler);
  BaseProvider.client = client;
  return client;
}

/// A bare app around [child] with the real theme. [wrap] puts providers above the whole app, the
/// way `main.dart` does.
Widget themed(Widget child, {Widget Function(Widget app)? wrap}) {
  final app = MaterialApp(
    theme: AppTheme.dark(),
    home: Scaffold(body: child),
  );

  return wrap == null ? app : wrap(app);
}

AuthSession signedInSession({String role = 'Admin'}) {
  final session = AuthSession(MemoryTokenStore());
  session.signIn(makeJwt(role: role));
  return session;
}
