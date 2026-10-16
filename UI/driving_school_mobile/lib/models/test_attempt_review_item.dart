/// Mirrors the backend's `TestAttemptReviewItemDto`.
class TestAttemptReviewItem {
  final String questionText;
  final String? yourAnswerText;
  final String correctAnswerText;

  const TestAttemptReviewItem({
    required this.questionText,
    this.yourAnswerText,
    required this.correctAnswerText,
  });

  factory TestAttemptReviewItem.fromJson(Map<String, dynamic> json) => TestAttemptReviewItem(
      questionText: json['questionText'] as String,
      yourAnswerText: json['yourAnswerText'] as String?,
      correctAnswerText: json['correctAnswerText'] as String,
      );
}
