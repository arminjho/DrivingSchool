import 'package:driving_school_mobile/core/auth_session.dart';
import 'package:driving_school_mobile/screens/instructor/reports_screen.dart';
import 'package:driving_school_mobile/screens/student/recommendations_screen.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../support/test_support.dart';

Future<void> _pumpScreen(
  WidgetTester tester,
  Widget screen,
  Future<http.Response> Function(http.Request request) api, {
  String role = 'Student',
}) async {
  installClient(api);
  final session = AuthSession(MemoryTokenStore());
  await session.signIn(makeJwt(role: role));
  onTestEnd(session.dispose);

  await tester.pumpWidget(MultiProvider(
    providers: [ChangeNotifierProvider<AuthSession>.value(value: session)],
    child: MaterialApp(home: screen),
  ));
  await tester.pumpAndSettle();
}

void main() {
  group('ReportsScreen (dorada #17 - the instructor menu\'s "Izvještaji" item)', () {
    testApp('shows the monthly completed-lesson statistics the backend already returns', (tester) async {
      await _pumpScreen(
        tester,
        const ReportsScreen(),
        (request) async => jsonResponse({'completedThisMonth': 7, 'completedTotal': 42, 'averageRating': 4.5}),
        role: 'Instructor',
      );

      expect(find.text('7'), findsOneWidget);
      expect(find.text('42'), findsOneWidget);
      expect(find.text('4.5'), findsOneWidget);
    });
  });

  group('RecommendationsScreen (dorada #18 - an explanation for every card, not just the first)', () {
    testApp('shows each recommendation\'s own explanation', (tester) async {
      await _pumpScreen(
        tester,
        const RecommendationsScreen(),
        (request) async => jsonResponse({
          'recommendations': [
            {
              'studyLessonId': 1, 'title': 'Znakovi opasnosti', 'chapterLabel': '1.1', 'readingMinutes': 5,
              'score': 0.9, 'explanation': 'Griješite u ovoj oblasti u 3 pokušaja.', 'isCriticalArea': true,
            },
            {
              'studyLessonId': 2, 'title': 'Prvenstvo prolaza', 'chapterLabel': '2.3', 'readingMinutes': 4,
              'score': 0.7, 'explanation': 'Niste pokušali ovo poglavlje.', 'isCriticalArea': false,
            },
          ],
          'coldStartMessage': null,
        }),
      );

      // The first recommendation's explanation was already shown, in the summary card - the
      // second's was not shown anywhere before the fix.
      expect(find.text('Griješite u ovoj oblasti u 3 pokušaja.'), findsWidgets);
      expect(find.text('Niste pokušali ovo poglavlje.'), findsOneWidget);
    });
  });
}
