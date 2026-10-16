/// Mirrors the backend's `QuestionAreaDto`.
class QuestionArea {
  final int id;
  final String name;

  const QuestionArea({
    required this.id,
    required this.name,
  });

  factory QuestionArea.fromJson(Map<String, dynamic> json) => QuestionArea(
      id: json['id'] as int,
      name: json['name'] as String,
      );
}
