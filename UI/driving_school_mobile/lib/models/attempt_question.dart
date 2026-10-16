import 'attempt_answer_option.dart';

/// Mirrors the backend's `AttemptQuestionDto`.
class AttemptQuestion {
  final int id;
  final String text;
  final String? imagePath;
  final List<AttemptAnswerOption> answerOptions;

  const AttemptQuestion({
    required this.id,
    required this.text,
    this.imagePath,
    required this.answerOptions,
  });

  factory AttemptQuestion.fromJson(Map<String, dynamic> json) => AttemptQuestion(
      id: json['id'] as int,
      text: json['text'] as String,
      imagePath: json['imagePath'] as String?,
      answerOptions: (json['answerOptions'] as List<dynamic>? ?? const []).map((item) => AttemptAnswerOption.fromJson(item as Map<String, dynamic>)).toList(),
      );
}
