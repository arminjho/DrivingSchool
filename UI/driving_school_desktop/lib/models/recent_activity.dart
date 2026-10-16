import '../core/json_helpers.dart';

/// Mirrors the backend's `RecentActivityDto`.
class RecentActivity {
  final String type;
  final String title;
  final String description;
  final DateTime occurredAtUtc;

  const RecentActivity({
    required this.type,
    required this.title,
    required this.description,
    required this.occurredAtUtc,
  });

  factory RecentActivity.fromJson(Map<String, dynamic> json) => RecentActivity(
      type: json['type'] as String,
      title: json['title'] as String,
      description: json['description'] as String,
      occurredAtUtc: parseUtcDateTime(json['occurredAtUtc'] as String),
      );
}
