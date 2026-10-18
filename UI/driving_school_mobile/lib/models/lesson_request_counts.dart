/// Mirrors the backend's `LessonRequestCountsDto` - counts behind each filter chip on
/// 4.3.2 (Sve / Na čekanju / Odobreni / Odbijeni).
class LessonRequestCounts {
  final int all;
  final int pending;
  final int confirmed;
  final int cancelled;

  const LessonRequestCounts({
    required this.all,
    required this.pending,
    required this.confirmed,
    required this.cancelled,
  });

  factory LessonRequestCounts.fromJson(Map<String, dynamic> json) => LessonRequestCounts(
      all: json['all'] as int,
      pending: json['pending'] as int,
      confirmed: json['confirmed'] as int,
      cancelled: json['cancelled'] as int,
      );
}
