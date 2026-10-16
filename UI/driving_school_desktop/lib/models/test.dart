/// Mirrors the backend's `TestDto`.
class Test {
  final int id;
  final String title;
  final int questionAreaId;
  final String questionAreaName;
  final int passThresholdPercent;
  final int questionCount;

  const Test({
    required this.id,
    required this.title,
    required this.questionAreaId,
    required this.questionAreaName,
    required this.passThresholdPercent,
    required this.questionCount,
  });

  factory Test.fromJson(Map<String, dynamic> json) => Test(
      id: json['id'] as int,
      title: json['title'] as String,
      questionAreaId: json['questionAreaId'] as int,
      questionAreaName: json['questionAreaName'] as String,
      passThresholdPercent: json['passThresholdPercent'] as int,
      questionCount: json['questionCount'] as int,
      );
}
