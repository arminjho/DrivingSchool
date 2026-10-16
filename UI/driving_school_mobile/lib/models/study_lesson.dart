/// Mirrors the backend's `StudyLessonDto`.
class StudyLesson {
  final int id;
  final String title;
  final int questionAreaId;
  final String questionAreaName;
  final String tags;
  final int validForYear;
  final int readingMinutes;
  final String chapterLabel;
  final int sectionCount;

  const StudyLesson({
    required this.id,
    required this.title,
    required this.questionAreaId,
    required this.questionAreaName,
    required this.tags,
    required this.validForYear,
    required this.readingMinutes,
    required this.chapterLabel,
    required this.sectionCount,
  });

  factory StudyLesson.fromJson(Map<String, dynamic> json) => StudyLesson(
      id: json['id'] as int,
      title: json['title'] as String,
      questionAreaId: json['questionAreaId'] as int,
      questionAreaName: json['questionAreaName'] as String,
      tags: json['tags'] as String,
      validForYear: json['validForYear'] as int,
      readingMinutes: json['readingMinutes'] as int,
      chapterLabel: json['chapterLabel'] as String,
      sectionCount: json['sectionCount'] as int,
      );
}
