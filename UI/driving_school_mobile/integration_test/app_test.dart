import 'package:driving_school_mobile/core/auth_session.dart';
import 'package:driving_school_mobile/l10n/app_strings.dart';
import 'package:driving_school_mobile/main.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:material_ui/material_ui.dart';

import '../test/support/test_support.dart';

/// Runs the real app against a real, running backend (no mocked HTTP client) - the same way
/// the professor's machine will run it from the release APK. Needs `--dart-define=API_BASE_URL=...`
/// pointed at a reachable API (the Android emulator's `http://10.0.2.2:5000` by default) and the
/// Docker backend actually running.
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  Future<AuthSession> pumpApp(WidgetTester tester) async {
    final session = AuthSession(MemoryTokenStore());
    await tester.pumpWidget(DrivingSchoolMobileApp(session: session, hub: FakeHub()));
    await tester.pump();
    return session;
  }

  Future<void> login(WidgetTester tester, String username, String password) async {
    await tester.enterText(find.byType(TextField).first, username);
    await tester.enterText(find.byType(TextField).last, password);
    await tester.tap(find.text(AppStrings.loginAction));
    await tester.pumpAndSettle(const Duration(seconds: 3));
  }

  group('eAutoškola - pravi backend', () {
    testWidgets('Učenik (mobile/test) se prijavljuje i vidi početni ekran sa stvarnim podacima', (tester) async {
      final session = await pumpApp(tester);
      await login(tester, 'mobile', 'test');

      expect(session.isLoggedIn, isTrue);
      // findsWidgets (one or more), not findsOneWidget: a Text can legitimately be matched
      // twice in the tree right after the very first paint on a freshly installed app (seen
      // once on this exact suite). Presence is what these assert, not an exact render count.
      expect(find.text(AppStrings.welcome('Mobile Učenik')), findsWidgets);
      expect(find.text(AppStrings.menuBookLesson), findsWidgets);
      expect(find.text(AppStrings.menuFinance), findsWidgets);
      expect(find.text(AppStrings.menuMyLessons), findsWidgets);
      expect(find.text(AppStrings.menuTests), findsWidgets);
      expect(find.text(AppStrings.menuRecommendations), findsWidgets);

      await tester.tap(find.text(AppStrings.menuMyLessons).first);
      await tester.pumpAndSettle(const Duration(seconds: 3));

      expect(find.text(AppStrings.tabActive), findsWidgets);
      expect(find.text(AppStrings.serverUnreachable), findsNothing);
      expect(find.text(AppStrings.requestTimeout), findsNothing);
    });

    testWidgets('Instruktor (instruktor/test) se prijavljuje i otvara novi ekran Izvještaji', (tester) async {
      final session = await pumpApp(tester);
      await login(tester, 'instruktor', 'test');

      expect(session.isLoggedIn, isTrue);
      expect(find.text(AppStrings.instructorGreeting('Instruktor Test')), findsWidgets);
      expect(find.text(AppStrings.menuLessonRequests), findsWidgets);
      expect(find.text(AppStrings.menuMySchedule), findsWidgets);
      expect(find.text(AppStrings.menuMyStudents), findsWidgets);
      expect(find.text(AppStrings.menuReports), findsWidgets);

      await tester.tap(find.text(AppStrings.menuReports).first);
      await tester.pumpAndSettle(const Duration(seconds: 3));

      expect(find.text(AppStrings.reportsTitle), findsWidgets);
      expect(find.text(AppStrings.serverUnreachable), findsNothing);
      expect(find.text(AppStrings.requestTimeout), findsNothing);
    });

    testWidgets('Pogrešna lozinka vraća jasnu poruku, ne ruši aplikaciju', (tester) async {
      final session = await pumpApp(tester);
      await login(tester, 'mobile', 'pogresna-lozinka');

      expect(session.isLoggedIn, isFalse);
      expect(find.text('Pogrešan e-mail ili lozinka.'), findsOneWidget);
    });

    testWidgets('Administratorski nalog (desktop/test) je odbijen na mobilnoj aplikaciji', (tester) async {
      final session = await pumpApp(tester);
      await login(tester, 'desktop', 'test');

      expect(session.isLoggedIn, isFalse);
      expect(find.text(AppStrings.roleNotAllowed), findsOneWidget);
    });

    testWidgets('Odjava vraća na ekran za prijavu', (tester) async {
      final session = await pumpApp(tester);
      await login(tester, 'mobile', 'test');
      expect(session.isLoggedIn, isTrue);

      session.clear();
      await tester.pumpAndSettle(const Duration(seconds: 3));

      expect(session.isLoggedIn, isFalse);
      expect(find.text(AppStrings.loginAction), findsOneWidget);
    });
  });
}
