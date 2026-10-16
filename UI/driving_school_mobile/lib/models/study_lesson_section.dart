/// Mirrors the backend's `StudyLessonSectionDto`.
class StudyLessonSection {
  final int id;
  final int studyLessonId;
  final String title;
  final String content;
  final int sortOrder;

  const StudyLessonSection({
    required this.id,
    required this.studyLessonId,
    required this.title,
    required this.content,
    required this.sortOrder,
  });

  factory StudyLessonSection.fromJson(Map<String, dynamic> json) => StudyLessonSection(
      id: json['id'] as int,
      studyLessonId: json['studyLessonId'] as int,
      title: json['title'] as String,
      content: json['content'] as String,
      sortOrder: json['sortOrder'] as int,
      );
}
