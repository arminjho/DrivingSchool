import 'recommendation.dart';

/// Mirrors the backend's `RecommendationsResultDto`.
class RecommendationsResult {
  final List<Recommendation> recommendations;
  final String? coldStartMessage;

  const RecommendationsResult({
    required this.recommendations,
    this.coldStartMessage,
  });

  factory RecommendationsResult.fromJson(Map<String, dynamic> json) => RecommendationsResult(
      recommendations: (json['recommendations'] as List<dynamic>? ?? const []).map((item) => Recommendation.fromJson(item as Map<String, dynamic>)).toList(),
      coldStartMessage: json['coldStartMessage'] as String?,
      );
}
