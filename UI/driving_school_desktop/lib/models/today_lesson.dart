import '../core/json_helpers.dart';

/// Mirrors the backend's `TodayLessonDto`.
class TodayLesson {
  final int lessonId;
  final DateTime startsAtUtc;
  final DateTime endsAtUtc;
  final String studentFullName;
  final String instructorFullName;
  final String drivingZoneName;
  final String status;

  const TodayLesson({
    required this.lessonId,
    required this.startsAtUtc,
    required this.endsAtUtc,
    required this.studentFullName,
    required this.instructorFullName,
    required this.drivingZoneName,
    required this.status,
  });

  factory TodayLesson.fromJson(Map<String, dynamic> json) => TodayLesson(
      lessonId: json['lessonId'] as int,
      startsAtUtc: parseUtcDateTime(json['startsAtUtc'] as String),
      endsAtUtc: parseUtcDateTime(json['endsAtUtc'] as String),
      studentFullName: json['studentFullName'] as String,
      instructorFullName: json['instructorFullName'] as String,
      drivingZoneName: json['drivingZoneName'] as String,
      status: json['status'] as String,
      );
}
