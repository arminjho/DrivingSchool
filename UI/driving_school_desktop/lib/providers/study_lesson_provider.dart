import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/study_lesson.dart';

class StudyLessonProvider extends BaseProvider<StudyLesson> {
  StudyLessonProvider(AuthSession session) : super('study-lessons', session);

  @override
  StudyLesson fromJson(Map<String, dynamic> json) => StudyLesson.fromJson(json);
}
