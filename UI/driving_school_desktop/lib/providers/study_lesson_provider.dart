import 'dart:convert';

import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/study_lesson.dart';
import '../models/study_lesson_detail.dart';

class StudyLessonProvider extends BaseProvider<StudyLesson> {
  StudyLessonProvider(AuthSession session) : super('study-lessons', session);

  @override
  StudyLesson fromJson(Map<String, dynamic> json) => StudyLesson.fromJson(json);

  /// `GET /api/study-lessons/{id}` actually returns the detail shape (with sections), not the
  /// plain list row - [getById] would silently drop them, so the admin "view lekciju" screen
  /// goes through this instead.
  Future<StudyLessonDetail> getDetail(int id) async {
    final response = await BaseProvider.send(
      () => BaseProvider.client.get(buildUri('api/study-lessons/$id'), headers: authHeaders()),
    );
    return StudyLessonDetail.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }
}
