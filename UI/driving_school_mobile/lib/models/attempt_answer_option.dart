/// Mirrors the backend's `AttemptAnswerOptionDto`.
class AttemptAnswerOption {
  final int id;
  final String text;

  const AttemptAnswerOption({
    required this.id,
    required this.text,
  });

  factory AttemptAnswerOption.fromJson(Map<String, dynamic> json) => AttemptAnswerOption(
      id: json['id'] as int,
      text: json['text'] as String,
      );
}
