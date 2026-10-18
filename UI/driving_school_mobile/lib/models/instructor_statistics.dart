/// Mirrors the backend's `InstructorStatisticsDto` - 4.3.1's "lična statistika".
class InstructorStatistics {
  final int completedThisMonth;
  final int completedTotal;
  final double averageRating;

  const InstructorStatistics({
    required this.completedThisMonth,
    required this.completedTotal,
    required this.averageRating,
  });

  factory InstructorStatistics.fromJson(Map<String, dynamic> json) => InstructorStatistics(
      completedThisMonth: json['completedThisMonth'] as int,
      completedTotal: json['completedTotal'] as int,
      averageRating: (json['averageRating'] as num).toDouble(),
      );
}
