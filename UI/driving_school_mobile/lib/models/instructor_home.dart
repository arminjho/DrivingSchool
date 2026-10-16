import 'lesson.dart';

/// Mirrors the backend's `InstructorHomeDto`.
class InstructorHome {
  final int todayLessonCount;
  final int newRequestCount;
  final List<Lesson> todayLessons;

  const InstructorHome({
    required this.todayLessonCount,
    required this.newRequestCount,
    required this.todayLessons,
  });

  factory InstructorHome.fromJson(Map<String, dynamic> json) => InstructorHome(
      todayLessonCount: json['todayLessonCount'] as int,
      newRequestCount: json['newRequestCount'] as int,
      todayLessons: (json['todayLessons'] as List<dynamic>? ?? const []).map((item) => Lesson.fromJson(item as Map<String, dynamic>)).toList(),
      );
}
