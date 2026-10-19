import 'dart:convert';

import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../core/json_helpers.dart';
import '../models/lesson.dart';
import '../models/paged_result.dart';

/// Everything a Student does with their own lessons: browse available slots, book, reschedule
/// a still-pending request, cancel, and list "my" lessons by tab (Aktivni/Završeni/Otkazani).
class LessonProvider extends BaseProvider<Lesson> {
  LessonProvider(AuthSession session) : super('lessons', session);

  @override
  Lesson fromJson(Map<String, dynamic> json) => Lesson.fromJson(json);

  Future<List<DateTime>> getAvailableSlots({required int instructorId, required DateTime date}) async {
    final isoDate = '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    final response = await apiGet(
      buildUri('api/lessons/available-slots', {'instructorId': instructorId, 'date': isoDate}),
      headers: authHeaders(),
    );
    final decoded = decode(response) as List<dynamic>;
    return decoded.map((value) => parseUtcDateTime(value as String)).toList();
  }

  /// `GET /api/lessons/my` - the signed-in student's own lessons, bucketed by the `tab` filter
  /// (`active` / `completed` / `cancelled`). The plain `GET /api/lessons` that [getPaged] calls is
  /// the Admin-only list and answers a student with 403, so it must not be used for this.
  Future<PagedResult<Lesson>> getMy([PagedQuery query = const PagedQuery()]) async {
    final response = await apiGet(buildUri('api/lessons/my', query.toQuery()), headers: authHeaders());
    return PagedResult<Lesson>.fromJson(decode(response) as Map<String, dynamic>, Lesson.fromJson);
  }

  Future<Lesson> getMyLessonById(int id) => getById(id);

  Future<Lesson> book({required int instructorId, required DateTime startsAtUtc, required int drivingZoneId, String? note}) async {
    final response = await apiPost(
      buildUri('api/lessons'),
      headers: authHeaders(),
      body: jsonEncode({
        'instructorId': instructorId,
        'startsAtUtc': startsAtUtc.toUtc().toIso8601String(),
        'drivingZoneId': drivingZoneId,
        'note': note,
      }),
    );
    return fromJson(decode(response) as Map<String, dynamic>);
  }

  Future<Lesson> reschedule(int id, {required DateTime startsAtUtc, required int drivingZoneId}) async {
    final response = await apiPut(
      buildUri('api/lessons/$id/reschedule'),
      headers: authHeaders(),
      body: jsonEncode({'startsAtUtc': startsAtUtc.toUtc().toIso8601String(), 'drivingZoneId': drivingZoneId}),
    );
    return fromJson(decode(response) as Map<String, dynamic>);
  }

  Future<Lesson> cancel(int id, {String? reason}) async {
    final response = await apiPost(
      buildUri('api/lessons/$id/cancel'),
      headers: authHeaders(),
      body: reason == null ? null : jsonEncode({'reason': reason}),
    );
    return fromJson(decode(response) as Map<String, dynamic>);
  }

  /// 4.3.2/4.3.3 - Pending -> Confirmed.
  Future<Lesson> confirm(int id) async {
    final response = await apiPost(buildUri('api/lessons/$id/confirm'), headers: authHeaders());
    return fromJson(decode(response) as Map<String, dynamic>);
  }

  /// 4.3.2/4.3.3 - Pending -> Cancelled, with a required reason.
  Future<Lesson> reject(int id, {required String reason}) async {
    final response = await apiPost(buildUri('api/lessons/$id/reject'), headers: authHeaders(), body: jsonEncode({'reason': reason}));
    return fromJson(decode(response) as Map<String, dynamic>);
  }

  /// 4.3.4's "Počni čas" - a client-visible marker only; the status stays Confirmed.
  Future<Lesson> start(int id) async {
    final response = await apiPost(buildUri('api/lessons/$id/start'), headers: authHeaders());
    return fromJson(decode(response) as Map<String, dynamic>);
  }

  /// 4.3.5 - Confirmed -> Completed, with the rating and instructor notes.
  Future<Lesson> complete(int id, {required int rating, required String instructorNotes}) async {
    final response = await apiPost(
      buildUri('api/lessons/$id/complete'),
      headers: authHeaders(),
      body: jsonEncode({'rating': rating, 'instructorNotes': instructorNotes}),
    );
    return fromJson(decode(response) as Map<String, dynamic>);
  }
}
