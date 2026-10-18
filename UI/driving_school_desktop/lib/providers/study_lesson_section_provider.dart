import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/study_lesson_section.dart';

class StudyLessonSectionProvider extends BaseProvider<StudyLessonSection> {
  StudyLessonSectionProvider(AuthSession session) : super('study-lesson-sections', session);

  @override
  StudyLessonSection fromJson(Map<String, dynamic> json) => StudyLessonSection.fromJson(json);
}
