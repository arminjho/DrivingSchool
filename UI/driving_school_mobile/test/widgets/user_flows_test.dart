import 'dart:convert';

import 'package:driving_school_mobile/core/auth_api.dart';
import 'package:driving_school_mobile/core/auth_session.dart';
import 'package:driving_school_mobile/core/roles.dart';
import 'package:driving_school_mobile/l10n/app_strings.dart';
import 'package:driving_school_mobile/models/lesson.dart';
import 'package:driving_school_mobile/screens/auth/register_screen.dart';
import 'package:driving_school_mobile/screens/instructor/lesson_completion_sheet.dart';
import 'package:driving_school_mobile/screens/instructor/lesson_requests_screen.dart';
import 'package:driving_school_mobile/screens/student/test_question_screen.dart';
import 'package:driving_school_mobile/widgets/ui/app_dialog.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../support/test_support.dart';

Map<String, dynamic> _lessonJson(int id, {String status = 'Pending', String? note}) => {
      'id': id,
      'studentId': 1,
      'studentFullName': 'Adnan Zukić',
      'studentEmail': 'mobile',
      'studentPhoneNumber': '+387 61 519 980',
      'studentProfileImagePath': null,
      'instructorId': 3,
      'instructorFullName': 'Instruktor Test',
      'vehicleId': 2,
      'vehicleRegistrationNumber': 'E22-A-123',
      'vehicleMake': 'VW',
      'vehicleModel': 'Polo',
      'drivingZoneId': 1,
      'drivingZoneName': 'Centar',
      'startsAtUtc': '2026-10-23T10:00:00Z',
      'endsAtUtc': '2026-10-23T11:00:00Z',
      'lessonNumber': 4,
      'note': note,
      'status': status,
      'rejectionReason': null,
      'rating': null,
      'instructorNotes': null,
      'startedAt': status == 'Confirmed' ? '2026-10-23T10:01:00Z' : null,
      'completedAt': null,
      'createdAt': '2026-10-19T06:27:32Z',
      'requiresPayment': false,
      'isPaid': false,
    };

Map<String, dynamic> _authBody(String token, {String role = 'Student'}) => {
      'token': token,
      'expiresAtUtc': '2026-10-16T13:00:00Z',
      'userId': 1,
      'email': 'armin@test.ba',
      'firstName': 'Armin',
      'lastName': 'M',
      'role': role,
    };

/// [screen] opened the way the app opens it - pushed from another route - with the providers
/// above the whole app, as `main.dart` has them.
Future<AuthSession> _pump(
  WidgetTester tester,
  Future<http.Response> Function(http.Request request) api, {
  required WidgetBuilder screen,
  String role = 'Student',
  bool signedIn = true,
  bool openAsRoute = true,
}) async {
  // A tall window: lists and forms build only the rows they can show.
  tester.view.physicalSize = const Size(800, 2000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  installClient(api);
  final session = AuthSession(MemoryTokenStore());
  if (signedIn) await session.signIn(makeJwt(role: role));
  onTestEnd(session.dispose);

  await tester.pumpWidget(MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthSession>.value(value: session),
      Provider<AuthApi>(create: (_) => AuthApi(session: session, allowedRoles: const {Roles.student, Roles.instructor}, roleNotAllowedMessage: AppStrings.roleNotAllowed)),
    ],
    child: MaterialApp(
      home: Builder(
        builder: (context) => Scaffold(
          body: TextButton(
            onPressed: () => openAsRoute
                ? Navigator.of(context).push(MaterialPageRoute<void>(builder: screen))
                : showModalBottomSheet<bool>(context: context, isScrollControlled: true, builder: screen),
            child: const Text('otvori'),
          ),
        ),
      ),
    ),
  ));
  await tester.tap(find.text('otvori'));
  await tester.pumpAndSettle();
  return session;
}

void main() {
  group('registration', () {
    testApp('the filled form is sent as a student account, the student is signed in and welcomed', (tester) async {
      final calls = <http.Request>[];
      final session = await _pump(
        tester,
        (request) async {
          calls.add(request);
          final path = request.url.path;
          if (path.endsWith('/register-lookups')) {
            return jsonResponse({
              'cities': [{'id': 1, 'name': 'Mostar', 'postalCode': '88000', 'countryId': 1, 'countryName': 'Bosna i Hercegovina'}],
              'vehicleCategories': [{'id': 1, 'code': 'B', 'name': 'Osobna vozila', 'description': null}],
            });
          }
          if (path.endsWith('/register')) return jsonResponse(_authBody(makeJwt(firstName: 'Armin', lastName: 'M')));
          return http.Response('', 204);
        },
        screen: (_) => const RegisterScreen(),
        signedIn: false,
      );
      final form = tester.state<FormBuilderState>(find.byType(FormBuilder));

      form.fields['firstName']!.didChange('Armin');
      form.fields['lastName']!.didChange('Muratspahić');
      form.fields['email']!.didChange('armin@test.ba');
      form.fields['phoneNumber']!.didChange('+387 61 123 456');
      form.fields['cityId']!.didChange(1);
      form.fields['vehicleCategoryId']!.didChange(1);
      form.fields['password']!.didChange('Test1234!');
      form.fields['confirmPassword']!.didChange('Test1234!');
      await tester.pump();

      await tester.tap(find.text(AppStrings.registerAction));
      await tester.pumpAndSettle();

      final body = jsonDecode(calls.singleWhere((c) => c.url.path.endsWith('/register')).body) as Map<String, dynamic>;
      expect(body['email'], 'armin@test.ba');
      expect(body['vehicleCategoryId'], 1);
      expect(body['cityId'], 1);
      expect(body.containsKey('role'), isFalse, reason: 'a client never picks its own role');
      expect(session.isLoggedIn, isTrue);
      expect(find.text(AppStrings.registerSuccess('Armin')), findsOneWidget);
      expect(find.byType(RegisterScreen), findsNothing);
    });
  });

  group('a theory test', () {
    Map<String, dynamic> attempt() => {
          'attemptId': 9,
          'testTitle': 'Test 1 - Saobraćajni znakovi',
          'questions': [
            {
              'id': 11,
              'text': 'Koji znak označava obavezno skretanje udesno?',
              'imagePath': null,
              'answerOptions': [
                {'id': 101, 'text': 'Zabrana skretanja'},
                {'id': 102, 'text': 'Obavezno skretanje udesno'},
              ],
            },
            {
              'id': 12,
              'text': 'Šta znači znak Stop?',
              'imagePath': null,
              'answerOptions': [
                {'id': 103, 'text': 'Obavezno zaustavljanje'},
                {'id': 104, 'text': 'Prednost prolaza'},
              ],
            },
          ],
        };

    testApp('is answered question by question, confirmed, submitted and ends on the result with its three actions', (tester) async {
      final calls = <http.Request>[];
      await _pump(
        tester,
        (request) async {
          calls.add(request);
          final path = request.url.path;
          if (path.endsWith('/tests/1/attempts')) return jsonResponse(attempt(), 201);
          if (path.endsWith('/test-attempts/9/submit')) {
            return jsonResponse({'attemptId': 9, 'percent': 50.0, 'correctCount': 1, 'incorrectCount': 1, 'passed': false, 'weakAreas': ['Saobraćajni znakovi']});
          }
          return http.Response('', 204);
        },
        screen: (_) => const TestQuestionScreen(testId: 1, testTitle: 'Test 1 - Saobraćajni znakovi'),
      );

      expect(find.text(AppStrings.questionProgress(1, 2)), findsOneWidget);
      expect(tester.widget<OutlinedButton>(find.widgetWithText(OutlinedButton, AppStrings.previousQuestion)).onPressed, isNull, reason: 'nothing before the first question');

      await tester.tap(find.text('Obavezno skretanje udesno'));
      await tester.tap(find.text(AppStrings.nextQuestion));
      await tester.pumpAndSettle();
      expect(find.text(AppStrings.questionProgress(2, 2)), findsOneWidget);

      await tester.tap(find.text('Prednost prolaza'));
      await tester.tap(find.text(AppStrings.submitTest));
      await tester.pumpAndSettle();
      expect(find.text(AppStrings.confirmSubmitTitle), findsOneWidget);

      await tester.tap(find.text(AppStrings.confirmSubmitAction));
      await tester.pumpAndSettle();

      final answers = (jsonDecode(calls.singleWhere((c) => c.url.path.endsWith('/submit')).body) as Map<String, dynamic>)['answers'] as List<dynamic>;
      expect(answers, [
        {'questionId': 11, 'selectedAnswerOptionId': 102},
        {'questionId': 12, 'selectedAnswerOptionId': 104},
      ]);
      expect(find.text(AppStrings.testResultTitle), findsOneWidget);
      expect(find.text('50%'), findsOneWidget);
      expect(find.text(AppStrings.testFailed), findsOneWidget);
      expect(find.text('Saobraćajni znakovi'), findsOneWidget);
      expect(find.text(AppStrings.viewTestReview), findsOneWidget);
      expect(find.text(AppStrings.viewRecommendations), findsOneWidget);
      expect(find.text(AppStrings.retakeTest), findsOneWidget);
    });
  });

  group('the instructor grading a lesson', () {
    testApp('needs a rating; the rating and the notes are then sent and the sheet closes', (tester) async {
      final calls = <http.Request>[];
      await _pump(
        tester,
        (request) async {
          calls.add(request);
          return request.url.path.endsWith('/lessons/5/complete') ? jsonResponse(_lessonJson(5, status: 'Completed')) : http.Response('', 204);
        },
        screen: (_) => LessonCompletionSheet(lesson: Lesson.fromJson(_lessonJson(5, status: 'Confirmed'))),
        role: 'Instructor',
        openAsRoute: false,
      );

      await tester.tap(find.text(AppStrings.save));
      await tester.pump();
      expect(find.text(AppStrings.ratingRequired), findsOneWidget);
      expect(calls.where((c) => c.method == 'POST'), isEmpty);

      await tester.tap(find.byIcon(Icons.star_border_rounded).at(3));
      await tester.enterText(find.byType(TextField), 'Dobro parkiranje, treba vježbati kružni tok.');
      await tester.pump();
      await tester.tap(find.text(AppStrings.save));
      await tester.pumpAndSettle();

      final body = jsonDecode(calls.singleWhere((c) => c.url.path.endsWith('/complete')).body) as Map<String, dynamic>;
      expect(body, {'rating': 4, 'instructorNotes': 'Dobro parkiranje, treba vježbati kružni tok.'});
      expect(find.text(AppStrings.lessonCompletedMessage), findsOneWidget);
      expect(find.byType(LessonCompletionSheet), findsNothing);
    });
  });

  group('lesson requests', () {
    Future<http.Response> api(List<http.Request> calls, http.Request request) async {
      calls.add(request);
      final path = request.url.path;
      if (path.endsWith('/lesson-requests/counts')) return jsonResponse({'all': 2, 'pending': 1, 'confirmed': 1, 'cancelled': 0});
      if (path.endsWith('/lesson-requests')) {
        return jsonResponse({'resultList': [_lessonJson(5, note: 'Molim da vježbamo parkiranje.'), _lessonJson(6, status: 'Confirmed')], 'count': 2});
      }
      if (path.endsWith('/lessons/5/confirm')) return jsonResponse(_lessonJson(5, status: 'Confirmed'));
      if (path.endsWith('/lessons/5/reject')) return jsonResponse(_lessonJson(5, status: 'Cancelled'));
      return http.Response('', 204);
    }

    testApp('approving a pending request confirms it and says so', (tester) async {
      final calls = <http.Request>[];
      await _pump(tester, (r) => api(calls, r), screen: (_) => const LessonRequestsScreen(), role: 'Instructor');

      expect(find.text('${AppStrings.filterPending} (1)'), findsOneWidget);
      expect(find.text('"Molim da vježbamo parkiranje."'), findsOneWidget);

      await tester.tap(find.text(AppStrings.approveAction));
      await tester.pumpAndSettle();

      expect(calls.any((c) => c.method == 'POST' && c.url.path.endsWith('/lessons/5/confirm')), isTrue);
      expect(find.text(AppStrings.requestApprovedMessage), findsOneWidget);
    });

    testApp('rejecting needs a reason, which is then sent with the rejection', (tester) async {
      final calls = <http.Request>[];
      await _pump(tester, (r) => api(calls, r), screen: (_) => const LessonRequestsScreen(), role: 'Instructor');

      await tester.tap(find.text(AppStrings.rejectAction));
      await tester.pumpAndSettle();
      final dialogReject = find.descendant(of: find.byType(AppDialog), matching: find.text(AppStrings.rejectAction));

      await tester.tap(dialogReject);
      await tester.pump();
      expect(find.text(AppStrings.required), findsOneWidget);
      expect(calls.where((c) => c.url.path.endsWith('/reject')), isEmpty);

      await tester.enterText(find.byType(TextField).last, 'Instruktor je zauzet u tom terminu.');
      await tester.tap(dialogReject);
      await tester.pumpAndSettle();

      final body = jsonDecode(calls.singleWhere((c) => c.url.path.endsWith('/lessons/5/reject')).body) as Map<String, dynamic>;
      expect(body['reason'], 'Instruktor je zauzet u tom terminu.');
      expect(find.text(AppStrings.requestRejectedMessage), findsOneWidget);
    });
  });
}
