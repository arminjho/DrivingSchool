import 'answer_option.dart';

/// Mirrors the backend's `QuestionDto`.
class Question {
  final int id;
  final String text;
  final String? imagePath;
  final int questionAreaId;
  final String questionAreaName;
  final int testId;
  final List<AnswerOption> answerOptions;

  const Question({
    required this.id,
    required this.text,
    this.imagePath,
    required this.questionAreaId,
    required this.questionAreaName,
    required this.testId,
    required this.answerOptions,
  });

  factory Question.fromJson(Map<String, dynamic> json) => Question(
      id: json['id'] as int,
      text: json['text'] as String,
      imagePath: json['imagePath'] as String?,
      questionAreaId: json['questionAreaId'] as int,
      questionAreaName: json['questionAreaName'] as String,
      testId: json['testId'] as int,
      answerOptions: (json['answerOptions'] as List<dynamic>? ?? const []).map((item) => AnswerOption.fromJson(item as Map<String, dynamic>)).toList(),
      );
}
