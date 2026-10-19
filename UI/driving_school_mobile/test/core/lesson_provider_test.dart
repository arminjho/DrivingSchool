import 'package:driving_school_mobile/core/auth_session.dart';
import 'package:driving_school_mobile/core/base_provider.dart';
import 'package:driving_school_mobile/providers/lesson_provider.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import '../support/test_support.dart';

/// The shape `GET /api/lessons/my` returns (the backend's `LessonDto`, UTC values with a zone).
Map<String, dynamic> _lessonJson(int id) => {
      'id': id,
      'studentId': 9,
      'studentFullName': 'Mobile Učenik',
      'studentEmail': 'mobile',
      'studentPhoneNumber': '+387 61 519 980',
      'studentProfileImagePath': null,
      'instructorId': 3,
      'instructorFullName': 'Instruktor Test',
      'vehicleId': 2,
      'vehicleRegistrationNumber': 'E54-K-456',
      'vehicleMake': 'Škoda',
      'vehicleModel': 'Octavia',
      'drivingZoneId': 2,
      'drivingZoneName': 'Stari Grad',
      'startsAtUtc': '2026-10-23T14:00:00Z',
      'endsAtUtc': '2026-10-23T15:00:00Z',
      'lessonNumber': 15,
      'note': null,
      'status': 'Pending',
      'rejectionReason': null,
      'rating': null,
      'instructorNotes': null,
      'startedAt': null,
      'completedAt': null,
      'createdAt': '2026-10-19T06:27:32.3104579Z',
      'requiresPayment': false,
      'isPaid': false,
    };

void main() {
  late AuthSession session;
  late LessonProvider provider;

  setUp(() async {
    session = AuthSession(MemoryTokenStore());
    await session.signIn(makeJwt());
    provider = LessonProvider(session);
  });

  tearDown(() => session.dispose());

  test("a student's lessons come from /api/lessons/my - the plain list is Admin-only and answers 403", () async {
    late http.Request captured;
    installClient((request) async {
      captured = request;
      return jsonResponse({'resultList': [_lessonJson(46)], 'count': 2});
    });

    final result = await provider.getMy(const PagedQuery(page: 1, pageSize: 1, filters: {'tab': 'active'}));

    expect(captured.url.path, '/api/lessons/my');
    expect(captured.url.queryParameters['tab'], 'active');
    expect(captured.headers['Authorization'], 'Bearer ${session.token}');
    expect(result.count, 2);
    expect(result.resultList.single.id, 46);
    expect(result.resultList.single.startsAtUtc.isUtc, isTrue);
    expect(result.resultList.single.startsAtUtc, DateTime.utc(2026, 10, 23, 14));
  });

  test('available slots are read as UTC instants even when the API omits the zone designator', () async {
    installClient((request) async => jsonResponse(['2026-10-23T08:00:00', '2026-10-23T09:00:00Z']));

    final slots = await provider.getAvailableSlots(instructorId: 3, date: DateTime(2026, 10, 23));

    expect(slots, [DateTime.utc(2026, 10, 23, 8), DateTime.utc(2026, 10, 23, 9)]);
    expect(slots.every((s) => s.isUtc), isTrue);
  });
}
