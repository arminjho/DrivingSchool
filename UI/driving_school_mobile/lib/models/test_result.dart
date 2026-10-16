/// Mirrors the backend's `TestResultDto`.
class TestResult {
  final int attemptId;
  final double percent;
  final int correctCount;
  final int incorrectCount;
  final bool passed;
  final List<String> weakAreas;

  const TestResult({
    required this.attemptId,
    required this.percent,
    required this.correctCount,
    required this.incorrectCount,
    required this.passed,
    required this.weakAreas,
  });

  factory TestResult.fromJson(Map<String, dynamic> json) => TestResult(
      attemptId: json['attemptId'] as int,
      percent: (json['percent'] as num).toDouble(),
      correctCount: json['correctCount'] as int,
      incorrectCount: json['incorrectCount'] as int,
      passed: json['passed'] as bool,
      weakAreas: List<String>.from(json['weakAreas'] as List<dynamic>? ?? const []),
      );
}
