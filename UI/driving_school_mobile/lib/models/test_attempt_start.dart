import 'attempt_question.dart';

/// Mirrors the backend's `TestAttemptStartDto`.
class TestAttemptStart {
  final int attemptId;
  final String testTitle;
  final List<AttemptQuestion> questions;

  const TestAttemptStart({
    required this.attemptId,
    required this.testTitle,
    required this.questions,
  });

  factory TestAttemptStart.fromJson(Map<String, dynamic> json) => TestAttemptStart(
      attemptId: json['attemptId'] as int,
      testTitle: json['testTitle'] as String,
      questions: (json['questions'] as List<dynamic>? ?? const []).map((item) => AttemptQuestion.fromJson(item as Map<String, dynamic>)).toList(),
      );
}
