/// Mirrors the backend's `MonthlyLessonCountDto`.
class MonthlyLessonCount {
  final int month;
  final int count;

  const MonthlyLessonCount({
    required this.month,
    required this.count,
  });

  factory MonthlyLessonCount.fromJson(Map<String, dynamic> json) => MonthlyLessonCount(
      month: json['month'] as int,
      count: json['count'] as int,
      );
}
