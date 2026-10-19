import 'dart:convert';

import 'package:driving_school_mobile/core/auth_session.dart';
import 'package:driving_school_mobile/core/formatters.dart';
import 'package:driving_school_mobile/l10n/app_strings.dart';
import 'package:driving_school_mobile/main.dart';
import 'package:driving_school_mobile/models/lesson.dart';
import 'package:driving_school_mobile/screens/instructor/schedule_screen.dart';
import 'package:driving_school_mobile/screens/student/book_lesson_screen.dart';
import 'package:driving_school_mobile/screens/student/finance_screen.dart';
import 'package:driving_school_mobile/screens/student/my_lessons_screen.dart';
import 'package:driving_school_mobile/screens/student/reschedule_lesson_screen.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../support/test_support.dart';

Map<String, dynamic> _lessonJson(
  int id, {
  String status = 'Pending',
  String startsAtUtc = '2026-10-23T10:00:00Z',
  String endsAtUtc = '2026-10-23T11:00:00Z',
}) =>
    {
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
      'startsAtUtc': startsAtUtc,
      'endsAtUtc': endsAtUtc,
      'lessonNumber': 4,
      'note': null,
      'status': status,
      'rejectionReason': null,
      'rating': null,
      'instructorNotes': null,
      'startedAt': null,
      'completedAt': null,
      'createdAt': '2026-10-19T06:27:32Z',
      'requiresPayment': false,
      'isPaid': false,
    };

/// A screen that is reached the way the app reaches it - pushed from another route - so a screen
/// that pops or replaces itself has somewhere to go.
Future<void> _pumpPushed(
  WidgetTester tester,
  Widget screen,
  Future<http.Response> Function(http.Request request) api, {
  String role = 'Student',
  int userId = 1,
}) async {
  installClient(api);
  final session = AuthSession(MemoryTokenStore());
  await session.signIn(makeJwt(role: role, userId: userId));
  onTestEnd(session.dispose);

  await tester.pumpWidget(ChangeNotifierProvider<AuthSession>.value(
    value: session,
    child: MaterialApp(
      home: Builder(
        builder: (context) => Scaffold(
          body: TextButton(
            onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => screen)),
            child: const Text('otvori'),
          ),
        ),
      ),
    ),
  ));
  await tester.tap(find.text('otvori'));
  await tester.pumpAndSettle();
}

void main() {
  group('booking and rescheduling', () {
    final slotUtc = DateTime.utc(2026, 10, 30, 8);
    final calls = <http.Request>[];

    Future<http.Response> api(http.Request request) async {
      calls.add(request);
      final path = request.url.path;
      if (path.endsWith('/available-slots')) return jsonResponse([slotUtc.toIso8601String()]);
      if (path.endsWith('/driving-zones')) return jsonResponse({'resultList': [{'id': 1, 'name': 'Centar'}], 'count': 1});
      if (path.endsWith('/instructors')) {
        return jsonResponse({'resultList': [{'id': 3, 'fullName': 'Instruktor Test', 'vehicleCategoryId': 1, 'vehicleCategoryName': 'B'}], 'count': 1});
      }
      if (request.method == 'POST' && path.endsWith('/lessons')) return jsonResponse(_lessonJson(77), 201);
      if (request.method == 'PUT' && path.endsWith('/reschedule')) return jsonResponse(_lessonJson(9, startsAtUtc: slotUtc.toIso8601String()));
      if (path.endsWith('/lessons/my')) {
        return jsonResponse({'resultList': request.url.queryParameters['tab'] == 'active' ? [_lessonJson(77)] : [], 'count': 1});
      }
      return http.Response('', 204);
    }

    setUp(calls.clear);

    testApp('a booked request is sent with the chosen slot, confirmed, and the history opens with it', (tester) async {
      await _pumpPushed(tester, const BookLessonScreen(), api);
      final form = tester.state<FormBuilderState>(find.byType(FormBuilder));

      form.fields['instructorId']!.didChange(3);
      await tester.pump();
      form.fields['date']!.didChange(DateTime.now().add(const Duration(days: 3)));
      await tester.pumpAndSettle();
      form.fields['slot']!.didChange(slotUtc.millisecondsSinceEpoch);
      form.fields['drivingZoneId']!.didChange(1);
      await tester.pump();

      await tester.tap(find.text(AppStrings.sendRequest));
      await tester.pumpAndSettle();

      final booking = calls.singleWhere((c) => c.method == 'POST' && c.url.path.endsWith('/lessons'));
      final body = jsonDecode(booking.body) as Map<String, dynamic>;
      expect(DateTime.parse(body['startsAtUtc'] as String), slotUtc);
      expect(body['instructorId'], 3);
      expect(body['drivingZoneId'], 1);
      expect(find.text(AppStrings.bookingSuccess), findsOneWidget);
      expect(find.byType(BookLessonScreen), findsNothing);
      expect(find.byType(MyLessonsScreen), findsOneWidget);
      expect(find.text('Instruktor Test · Centar · VW Polo'), findsOneWidget, reason: 'the new request is the first card of the active tab');
    });

    testApp('a new time for a pending request is sent and confirmed', (tester) async {
      final lesson = Lesson.fromJson(_lessonJson(9));
      await _pumpPushed(tester, RescheduleLessonScreen(lesson: lesson), api);
      final form = tester.state<FormBuilderState>(find.byType(FormBuilder));

      form.fields['date']!.didChange(DateTime.now().add(const Duration(days: 3)));
      await tester.pumpAndSettle();
      form.fields['slot']!.didChange(slotUtc.millisecondsSinceEpoch);
      await tester.pump();

      await tester.tap(find.text(AppStrings.save));
      await tester.pumpAndSettle();

      final request = calls.singleWhere((c) => c.method == 'PUT');
      expect(DateTime.parse((jsonDecode(request.body) as Map<String, dynamic>)['startsAtUtc'] as String), slotUtc);
      expect(find.text(AppStrings.rescheduleSuccess), findsOneWidget);
      expect(find.byType(RescheduleLessonScreen), findsNothing);
    });
  });

  group('my lessons', () {
    testApp('a card shows the slot as a range with instructor, zone, vehicle and the details button', (tester) async {
      installClient((request) async {
        final active = request.url.queryParameters['tab'] == 'active';
        return jsonResponse({'resultList': active ? [_lessonJson(5, status: 'Confirmed')] : [], 'count': active ? 1 : 0});
      });
      final session = AuthSession(MemoryTokenStore());
      await session.signIn(makeJwt());
      onTestEnd(session.dispose);

      await tester.pumpWidget(ChangeNotifierProvider<AuthSession>.value(value: session, child: const MaterialApp(home: MyLessonsScreen())));
      await tester.pumpAndSettle();

      final start = DateTime.utc(2026, 10, 23, 10);
      expect(find.text(AppFormat.dateTimeRange(start, start.add(const Duration(hours: 1)))), findsOneWidget);
      expect(find.text('Instruktor Test · Centar · VW Polo'), findsOneWidget);
      expect(find.text(AppStrings.viewDetails), findsOneWidget);
      expect(find.text(AppStrings.lessonConfirmed), findsOneWidget);
    });
  });

  group('paying the next installment', () {
    Map<String, dynamic> summary() => {
          'costBreakdown': <Map<String, dynamic>>[],
          'paymentHistory': <Map<String, dynamic>>[],
          'remainingBalance': 500.0,
          'nextInstallment': {'installmentId': 5, 'number': 2, 'description': 'Rata 2', 'amount': 166.67, 'dueDate': '2026-11-01T00:00:00Z'},
        };

    testApp('asks for confirmation - amount, PayPal note, Odustani - and declining creates nothing at PayPal', (tester) async {
      final calls = <String>[];
      await _pumpPushed(tester, const FinanceScreen(), (request) async {
        calls.add('${request.method} ${request.url.path}');
        return jsonResponse(summary());
      });

      await tester.tap(find.text(AppStrings.payNextInstallment));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.confirmPaymentTitle), findsOneWidget);
      expect(find.textContaining('${AppStrings.amountToPay} — 166.67 KM'), findsOneWidget);
      expect(find.textContaining(AppStrings.paypalSecureNote), findsOneWidget);
      expect(find.text(AppStrings.payWithPayPal), findsOneWidget);

      await tester.tap(find.text(AppStrings.cancel));
      await tester.pumpAndSettle();

      expect(find.text(AppStrings.confirmPaymentTitle), findsNothing);
      expect(calls.where((c) => c.startsWith('POST')), isEmpty);
    });
  });

  group("the instructor's schedule", () {
    testApp('shows the month, the lessons, and a "Slobodan termin" placeholder for each free hour', (tester) async {
      // A tall window: the list builds only the rows it can show.
      tester.view.physicalSize = const Size(800, 2000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);

      final today = DateTime.now();
      DateTime utcAt(int hour) => DateTime.utc(today.year, today.month, today.day, hour);

      await _pumpPushed(
        tester,
        const ScheduleScreen(),
        (request) async {
          if (request.url.path.endsWith('/available-slots')) return jsonResponse([utcAt(11).toIso8601String(), utcAt(13).toIso8601String()]);
          return jsonResponse([
            _lessonJson(1, status: 'Completed', startsAtUtc: utcAt(9).toIso8601String(), endsAtUtc: utcAt(10).toIso8601String()),
            _lessonJson(2, status: 'Confirmed', startsAtUtc: utcAt(12).toIso8601String(), endsAtUtc: utcAt(13).toIso8601String()),
          ]);
        },
        role: 'Instructor',
        userId: 3,
      );

      expect(find.text(AppStrings.monthYear(today.month, today.year)), findsOneWidget);
      expect(find.text(AppStrings.scheduleEmptySlot), findsNWidgets(2));
      expect(find.text(AppStrings.lessonFinishedLabel), findsOneWidget);
      expect(find.text(AppStrings.startLessonAction), findsOneWidget);
      // Placeholders and lessons are in time order: 09 lesson, 11 free, 12 lesson, 13 free.
      final order = [AppFormat.time(utcAt(9)), AppFormat.time(utcAt(11)), AppFormat.time(utcAt(12)), AppFormat.time(utcAt(13))];
      final tops = [for (final time in order) tester.getTopLeft(find.text(time)).dy];
      expect(tops, orderedEquals([...tops]..sort()));
    });
  });

  group('the shell', () {
    testApp('every destination sits under the shell app bar only, and the home header is the app name', (tester) async {
      installClient((request) async {
        if (request.url.path.endsWith('unread-count')) return jsonResponse(0);
        if (request.url.path.endsWith('/home')) return jsonResponse({'todayLessonCount': 0, 'newRequestCount': 0, 'todayLessons': []});
        return jsonResponse({'resultList': [], 'count': 0});
      });
      final session = AuthSession(MemoryTokenStore(makeJwt()));
      await session.restore();
      onTestEnd(session.dispose);
      await tester.pumpWidget(DrivingSchoolMobileApp(session: session, hub: FakeHub()));
      await tester.pump();
      await tester.pump();

      expect(find.text(AppStrings.appName), findsOneWidget);

      for (final label in [AppStrings.navHome, AppStrings.navLessons, AppStrings.navTests, AppStrings.navStudy, AppStrings.navProfile]) {
        await tester.tap(find.widgetWithText(NavigationDestination, label));
        await tester.pump();
        await tester.pump();
        expect(find.byType(AppBar), findsOneWidget, reason: '$label has a second app bar of its own');
      }
    });
  });
}
