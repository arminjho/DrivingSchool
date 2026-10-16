import '../core/json_helpers.dart';

/// Mirrors the backend's `StudentStudyLessonDto`.
class StudentStudyLesson {
  final int studyLessonId;
  final String title;
  final String questionAreaName;
  final String chapterLabel;
  final int readingMinutes;
  final int sectionCount;
  final DateTime openedAt;
  final DateTime lastOpenedAt;
  final int progressPercent;

  const StudentStudyLesson({
    required this.studyLessonId,
    required this.title,
    required this.questionAreaName,
    required this.chapterLabel,
    required this.readingMinutes,
    required this.sectionCount,
    required this.openedAt,
    required this.lastOpenedAt,
    required this.progressPercent,
  });

  factory StudentStudyLesson.fromJson(Map<String, dynamic> json) => StudentStudyLesson(
      studyLessonId: json['studyLessonId'] as int,
      title: json['title'] as String,
      questionAreaName: json['questionAreaName'] as String,
      chapterLabel: json['chapterLabel'] as String,
      readingMinutes: json['readingMinutes'] as int,
      sectionCount: json['sectionCount'] as int,
      openedAt: parseUtcDateTime(json['openedAt'] as String),
      lastOpenedAt: parseUtcDateTime(json['lastOpenedAt'] as String),
      progressPercent: json['progressPercent'] as int,
      );
}
