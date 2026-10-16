/// Mirrors the backend's `TestSummaryDto`.
class TestSummary {
  final int id;
  final String title;
  final String questionAreaName;
  final int questionCount;
  final bool hasAttempt;
  final double? lastPercent;
  final int? lastCorrectCount;
  final int? lastTotal;
  final bool? lastPassed;

  const TestSummary({
    required this.id,
    required this.title,
    required this.questionAreaName,
    required this.questionCount,
    required this.hasAttempt,
    this.lastPercent,
    this.lastCorrectCount,
    this.lastTotal,
    this.lastPassed,
  });

  factory TestSummary.fromJson(Map<String, dynamic> json) => TestSummary(
      id: json['id'] as int,
      title: json['title'] as String,
      questionAreaName: json['questionAreaName'] as String,
      questionCount: json['questionCount'] as int,
      hasAttempt: json['hasAttempt'] as bool,
      lastPercent: (json['lastPercent'] as num?)?.toDouble(),
      lastCorrectCount: json['lastCorrectCount'] as int?,
      lastTotal: json['lastTotal'] as int?,
      lastPassed: json['lastPassed'] as bool?,
      );
}
