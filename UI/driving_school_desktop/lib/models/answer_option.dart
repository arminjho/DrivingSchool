/// Mirrors the backend's `AnswerOptionDto`.
class AnswerOption {
  final int id;
  final String text;
  final bool isCorrect;

  const AnswerOption({
    required this.id,
    required this.text,
    required this.isCorrect,
  });

  factory AnswerOption.fromJson(Map<String, dynamic> json) => AnswerOption(
      id: json['id'] as int,
      text: json['text'] as String,
      isCorrect: json['isCorrect'] as bool,
      );
}
