import 'package:driving_school_mobile/core/auth_api.dart';
import 'package:driving_school_mobile/core/auth_session.dart';
import 'package:driving_school_mobile/core/base_provider.dart';
import 'package:driving_school_mobile/core/notification_center.dart';
import 'package:driving_school_mobile/core/paged_controller.dart';
import 'package:driving_school_mobile/core/roles.dart';
import 'package:driving_school_mobile/l10n/app_strings.dart';
import 'package:driving_school_mobile/main.dart';
import 'package:driving_school_mobile/models/paged_result.dart';
import 'package:driving_school_mobile/providers/notification_provider.dart';
import 'package:driving_school_mobile/screens/login_screen.dart';
import 'package:driving_school_mobile/screens/notifications/notifications_screen.dart';
import 'package:driving_school_mobile/widgets/notifications_bell.dart';
import 'package:driving_school_mobile/widgets/ui/app_list_card.dart';
import 'package:driving_school_mobile/widgets/ui/paged_list_view.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../support/test_support.dart';

Map<String, dynamic> _authBody(String token, {String role = 'Student'}) => {
      'token': token,
      'expiresAtUtc': '2026-10-16T13:00:00Z',
      'userId': 1,
      'email': 'mobile',
      'firstName': 'Adnan',
      'lastName': 'Zukić',
      'role': role,
    };

/// The whole app against a fake API and a fake hub.
Future<({AuthSession session, FakeHub hub})> _pumpApp(
  WidgetTester tester,
  Future<http.Response> Function(http.Request request) api, {
  String? storedToken,
}) async {
  installClient(api);
  final session = AuthSession(MemoryTokenStore(storedToken));
  await session.restore();
  final hub = FakeHub();
  onTestEnd(session.dispose);

  await tester.pumpWidget(DrivingSchoolMobileApp(session: session, hub: hub));
  await tester.pump();
  return (session: session, hub: hub);
}

Future<http.Response> _quietApi(http.Request request) async {
  final path = request.url.path;
  if (path.endsWith('unread-count')) return jsonResponse(2);
  if (path.endsWith('logout')) return http.Response('', 204);
  return jsonResponse({'resultList': [notificationJson(2), notificationJson(1, isRead: true)], 'count': 2});
}

void main() {
  group('LoginScreen', () {
    Future<AuthSession> pumpLogin(WidgetTester tester, Future<http.Response> Function(http.Request) api) async {
      installClient(api);
      final session = AuthSession(MemoryTokenStore());
      onTestEnd(session.dispose);
      await tester.pumpWidget(themed(const LoginScreen(), wrap: (app) => MultiProvider(
            providers: [
              ChangeNotifierProvider.value(value: session),
              Provider(create: (_) => AuthApi(session: session, allowedRoles: const {Roles.student, Roles.instructor}, roleNotAllowedMessage: AppStrings.roleNotAllowed)),
            ],
            child: app,
          )));
      return session;
    }

    testApp('empty fields are reported under each field, in Bosnian', (tester) async {
      await pumpLogin(tester, _quietApi);

      await tester.tap(find.text(AppStrings.loginAction));
      await tester.pump();

      expect(find.text(AppStrings.required), findsNWidgets(2));
    });

    testApp('a successful sign-in starts the session', (tester) async {
      final session = await pumpLogin(tester, (request) async => jsonResponse(_authBody(makeJwt())));

      await tester.enterText(find.byType(TextField).first, 'mobile');
      await tester.enterText(find.byType(TextField).last, 'test');
      await tester.tap(find.text(AppStrings.loginAction));
      await tester.pumpAndSettle();

      expect(session.isLoggedIn, isTrue);
    });

    testApp('wrong credentials show the backend message in a banner and stay on the form', (tester) async {
      final session = await pumpLogin(tester, (request) async => jsonResponse({'message': 'Pogrešan e-mail ili lozinka.', 'errors': null}, 401));

      await tester.enterText(find.byType(TextField).first, 'mobile');
      await tester.enterText(find.byType(TextField).last, 'pogresna');
      await tester.tap(find.text(AppStrings.loginAction));
      await tester.pumpAndSettle();

      expect(find.text('Pogrešan e-mail ili lozinka.'), findsOneWidget);
      expect(session.isLoggedIn, isFalse);
    });

    testApp('an admin account is turned away by the mobile app', (tester) async {
      final session = await pumpLogin(tester, (request) async {
        return request.url.path.endsWith('login') ? jsonResponse(_authBody(makeJwt(role: 'Admin'), role: 'Admin')) : http.Response('', 204);
      });

      await tester.enterText(find.byType(TextField).first, 'desktop');
      await tester.enterText(find.byType(TextField).last, 'test');
      await tester.tap(find.text(AppStrings.loginAction));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.roleNotAllowed), findsOneWidget);
      expect(session.isLoggedIn, isFalse);
    });

    testApp('the password is hidden until the user asks to see it', (tester) async {
      await pumpLogin(tester, _quietApi);

      expect(tester.widget<EditableText>(find.byType(EditableText).last).obscureText, isTrue);
      await tester.tap(find.byTooltip(AppStrings.showPassword));
      await tester.pump();

      expect(tester.widget<EditableText>(find.byType(EditableText).last).obscureText, isFalse);
    });
  });

  group('the app', () {
    testApp('shows the login screen when nobody is signed in', (tester) async {
      await _pumpApp(tester, _quietApi);

      expect(find.byType(LoginScreen), findsOneWidget);
    });

    testApp('a returning user with a valid stored token goes straight to the shell', (tester) async {
      await _pumpApp(tester, _quietApi, storedToken: makeJwt(firstName: 'Armin', lastName: 'M'));
      await tester.pump();

      expect(find.byType(LoginScreen), findsNothing);
      expect(find.text(AppStrings.welcome('Armin M')), findsOneWidget);
    });

    testApp('the bell shows the unread count and opens the notifications', (tester) async {
      await _pumpApp(tester, _quietApi, storedToken: makeJwt());
      await tester.pump();

      expect(find.descendant(of: find.byType(NotificationsBell), matching: find.text('2')), findsOneWidget);

      await tester.tap(find.byType(NotificationsBell));
      await tester.pumpAndSettle();

      expect(find.byType(NotificationsScreen), findsOneWidget);
      expect(find.text('Poruka 2'), findsOneWidget);
    });

    testApp('an HTTP 401 anywhere sends the user back to the login screen', (tester) async {
      var expired = false;
      final app = await _pumpApp(tester, (request) async {
        if (expired && request.url.path.endsWith('unread-count')) return jsonResponse({'message': 'Token je opozvan.', 'errors': null}, 401);
        return _quietApi(request);
      }, storedToken: makeJwt());
      await tester.pump();
      expect(find.byType(LoginScreen), findsNothing);

      expired = true;
      final center = tester.element(find.byType(NotificationsBell)).read<NotificationCenter>();
      await center.refresh();
      await tester.pumpAndSettle();

      expect(app.session.isLoggedIn, isFalse);
      expect(find.byType(LoginScreen), findsOneWidget);
    });

    testApp('logging out revokes the token and shows the login screen', (tester) async {
      final calls = <String>[];
      await _pumpApp(tester, (request) async {
        calls.add(request.url.path);
        return _quietApi(request);
      }, storedToken: makeJwt());
      await tester.pump();

      await tester.tap(find.byType(PopupMenuButton<void>));
      await tester.pumpAndSettle();
      await tester.tap(find.text(AppStrings.logout));
      await tester.pumpAndSettle();

      expect(calls, contains('/api/auth/logout'));
      expect(find.byType(LoginScreen), findsOneWidget);
    });

    testApp('the bottom navigation bar switches between Home and Notifications', (tester) async {
      await _pumpApp(tester, _quietApi, storedToken: makeJwt());
      await tester.pump();

      expect(find.byType(NavigationBar), findsOneWidget);
      await tester.tap(find.widgetWithText(NavigationDestination, AppStrings.notificationsTitle));
      await tester.pumpAndSettle();

      expect(find.byType(NotificationsScreen), findsOneWidget);
    });
  });

  group('NotificationsScreen', () {
    Future<List<String>> pumpScreen(WidgetTester tester, {List<Map<String, dynamic>>? items}) async {
      final calls = <String>[];
      final list = items ?? [notificationJson(2), notificationJson(1, isRead: true)];
      installClient((request) async {
        calls.add('${request.method} ${request.url.path}');
        final path = request.url.path;
        if (path.endsWith('unread-count')) return jsonResponse(list.where((n) => n['isRead'] == false).length);
        if (path.endsWith('mark-all-read')) {
          for (final n in list) {
            n['isRead'] = true;
          }
          return http.Response('', 204);
        }
        if (path.endsWith('mark-read')) {
          list.firstWhere((n) => '${n['id']}' == request.url.pathSegments[2])['isRead'] = true;
          return http.Response('', 204);
        }
        return jsonResponse({'resultList': list, 'count': list.length});
      });

      final session = signedInSession();
      final center = NotificationCenter(session: session, provider: NotificationProvider(session), hub: FakeHub());
      onTestEnd(() {
        center.dispose();
        session.dispose();
      });

      await tester.pumpWidget(themed(const NotificationsScreen(), wrap: (app) => ChangeNotifierProvider.value(value: center, child: app)));
      await tester.pumpAndSettle();
      return calls;
    }

    testApp('lists notifications, unread ones marked, newest first as the API returns them', (tester) async {
      await pumpScreen(tester);

      expect(find.text('Poruka 2'), findsOneWidget);
      expect(find.text('Poruka 1'), findsOneWidget);
      expect(find.byIcon(Icons.mark_email_unread_rounded), findsOneWidget);
      expect(find.byIcon(Icons.mail_outline_rounded), findsOneWidget);
    });

    testApp('tapping an unread notification marks it read', (tester) async {
      final calls = await pumpScreen(tester);

      await tester.tap(find.text('Poruka 2'));
      await tester.pumpAndSettle();

      expect(calls, contains('POST /api/notifications/2/mark-read'));
    });

    testApp('"mark all read" works, and is disabled when everything is already read', (tester) async {
      final calls = await pumpScreen(tester);

      await tester.tap(find.text(AppStrings.markAllRead));
      await tester.pumpAndSettle();
      expect(calls, contains('POST /api/notifications/mark-all-read'));

      final button = tester.widget<TextButton>(find.widgetWithText(TextButton, AppStrings.markAllRead));
      expect(button.onPressed, isNull);
    });

    testApp('an empty feed says so', (tester) async {
      await pumpScreen(tester, items: []);

      expect(find.text(AppStrings.notificationsEmpty), findsOneWidget);
    });
  });

  group('PagedListView', () {
    PagedController<String> controllerWith(Future<PagedResult<String>> Function(PagedQuery) fetch, {bool accumulate = true}) =>
        PagedController<String>(fetch: fetch, accumulate: accumulate, searchParam: 'name', debounce: const Duration(milliseconds: 10));

    Widget list(PagedController<String> controller) => SizedBox(
          height: 600,
          child: PagedListView<String>(
            controller: controller,
            title: 'Gradovi',
            itemBuilder: (context, row) => AppListCard(title: row),
          ),
        );

    testApp('shows rows and searches on the server', (tester) async {
      final queries = <PagedQuery>[];
      final controller = controllerWith((query) async {
        queries.add(query);
        return PagedResult(resultList: ['Sarajevo', 'Mostar'], count: 2);
      });
      onTestEnd(controller.dispose);
      await tester.pumpWidget(themed(list(controller)));
      await controller.load();
      await tester.pumpAndSettle();

      expect(find.text('Sarajevo'), findsOneWidget);
      expect(find.text('Mostar'), findsOneWidget);

      await tester.enterText(find.byType(TextField), 'Mos');
      await tester.pump(const Duration(milliseconds: 50));
      await tester.pumpAndSettle();

      expect(queries.last.filters, {'name': 'Mos'});
    });

    testApp('an empty result for a search explains it instead of showing a bare list', (tester) async {
      final controller = controllerWith((query) async => const PagedResult(resultList: [], count: 0));
      onTestEnd(controller.dispose);
      await tester.pumpWidget(themed(list(controller)));
      await controller.load();
      controller.setSearch('xyz');
      await tester.pump(const Duration(milliseconds: 50));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.emptyForSearch('xyz')), findsOneWidget);
    });

    testApp('a failure is shown with a retry', (tester) async {
      var fail = true;
      final controller = controllerWith((query) async => fail ? throw http.ClientException('x') : const PagedResult(resultList: ['Tuzla'], count: 1));
      onTestEnd(controller.dispose);
      await tester.pumpWidget(themed(list(controller)));
      await controller.load();
      await tester.pumpAndSettle();
      expect(find.text(AppStrings.retry), findsOneWidget);

      fail = false;
      await tester.tap(find.text(AppStrings.retry));
      await tester.pumpAndSettle();

      expect(find.text('Tuzla'), findsOneWidget);
    });

    testApp('scrolling near the bottom loads the next page in accumulate mode', (tester) async {
      final queries = <PagedQuery>[];
      final controller = controllerWith((query) async {
        queries.add(query);
        final first = (query.page - 1) * query.pageSize;
        return PagedResult(resultList: List.generate(10, (i) => 'Grad ${first + i}'), count: 40);
      });
      onTestEnd(controller.dispose);
      await tester.pumpWidget(themed(list(controller)));
      await controller.load();
      await tester.pumpAndSettle();
      expect(queries.length, 1);

      await tester.drag(find.byType(ListView), const Offset(0, -2000));
      await tester.pumpAndSettle();

      expect(queries.length, greaterThan(1));
    });
  });
}
