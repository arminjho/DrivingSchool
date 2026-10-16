import 'test_attempt_review_item.dart';

/// Mirrors the backend's `TestAttemptReviewDto`.
class TestAttemptReview {
  final List<TestAttemptReviewItem> incorrectAnswers;

  const TestAttemptReview({
    required this.incorrectAnswers,
  });

  factory TestAttemptReview.fromJson(Map<String, dynamic> json) => TestAttemptReview(
      incorrectAnswers: (json['incorrectAnswers'] as List<dynamic>? ?? const []).map((item) => TestAttemptReviewItem.fromJson(item as Map<String, dynamic>)).toList(),
      );
}
