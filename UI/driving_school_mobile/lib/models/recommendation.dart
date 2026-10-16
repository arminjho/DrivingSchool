/// Mirrors the backend's `RecommendationDto`.
class Recommendation {
  final int studyLessonId;
  final String title;
  final String chapterLabel;
  final int readingMinutes;
  final double score;
  final String explanation;
  final bool isCriticalArea;

  const Recommendation({
    required this.studyLessonId,
    required this.title,
    required this.chapterLabel,
    required this.readingMinutes,
    required this.score,
    required this.explanation,
    required this.isCriticalArea,
  });

  factory Recommendation.fromJson(Map<String, dynamic> json) => Recommendation(
      studyLessonId: json['studyLessonId'] as int,
      title: json['title'] as String,
      chapterLabel: json['chapterLabel'] as String,
      readingMinutes: json['readingMinutes'] as int,
      score: (json['score'] as num).toDouble(),
      explanation: json['explanation'] as String,
      isCriticalArea: json['isCriticalArea'] as bool,
      );
}
