import 'dart:convert';

import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/lesson.dart';

/// Admin "all lessons" overview (`GET /api/lessons`, admin-only) plus the admin cancel action.
/// Booking/confirm/reject/start/complete are Student/Instructor actions and belong to those
/// apps, not here.
class LessonAdminProvider extends BaseProvider<Lesson> {
  LessonAdminProvider(AuthSession session) : super('lessons', session);

  @override
  Lesson fromJson(Map<String, dynamic> json) => Lesson.fromJson(json);

  Future<Lesson> cancel(int id, {String? reason}) async {
    final response = await apiPost(
      buildUri('api/lessons/$id/cancel'),
      headers: authHeaders(),
      body: reason == null ? null : jsonEncode({'reason': reason}),
    );
    return fromJson(decode(response) as Map<String, dynamic>);
  }
}
