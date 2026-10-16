/// Mirrors the backend's `LessonStatusCountDto`.
class LessonStatusCount {
  final String status;
  final int count;

  const LessonStatusCount({
    required this.status,
    required this.count,
  });

  factory LessonStatusCount.fromJson(Map<String, dynamic> json) => LessonStatusCount(
      status: json['status'] as String,
      count: json['count'] as int,
      );
}
