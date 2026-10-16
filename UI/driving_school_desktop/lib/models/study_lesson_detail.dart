import 'study_lesson_section.dart';

/// Mirrors the backend's `StudyLessonDetailDto`.
class StudyLessonDetail {
  final int id;
  final String title;
  final int questionAreaId;
  final String questionAreaName;
  final String tags;
  final int validForYear;
  final int readingMinutes;
  final String chapterLabel;
  final int sectionCount;
  final List<StudyLessonSection> sections;

  const StudyLessonDetail({
    required this.id,
    required this.title,
    required this.questionAreaId,
    required this.questionAreaName,
    required this.tags,
    required this.validForYear,
    required this.readingMinutes,
    required this.chapterLabel,
    required this.sectionCount,
    required this.sections,
  });

  factory StudyLessonDetail.fromJson(Map<String, dynamic> json) => StudyLessonDetail(
      id: json['id'] as int,
      title: json['title'] as String,
      questionAreaId: json['questionAreaId'] as int,
      questionAreaName: json['questionAreaName'] as String,
      tags: json['tags'] as String,
      validForYear: json['validForYear'] as int,
      readingMinutes: json['readingMinutes'] as int,
      chapterLabel: json['chapterLabel'] as String,
      sectionCount: json['sectionCount'] as int,
      sections: (json['sections'] as List<dynamic>? ?? const []).map((item) => StudyLessonSection.fromJson(item as Map<String, dynamic>)).toList(),
      );
}
