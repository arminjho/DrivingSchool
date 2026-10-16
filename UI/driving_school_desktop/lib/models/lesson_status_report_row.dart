/// Mirrors the backend's `LessonStatusReportRow`.
class LessonStatusReportRow {
  final int instructorId;
  final String instructorName;
  final int completed;
  final int scheduled;
  final int cancelled;
  final int total;

  const LessonStatusReportRow({
    required this.instructorId,
    required this.instructorName,
    required this.completed,
    required this.scheduled,
    required this.cancelled,
    required this.total,
  });

  factory LessonStatusReportRow.fromJson(Map<String, dynamic> json) => LessonStatusReportRow(
      instructorId: json['instructorId'] as int,
      instructorName: json['instructorName'] as String,
      completed: json['completed'] as int,
      scheduled: json['scheduled'] as int,
      cancelled: json['cancelled'] as int,
      total: json['total'] as int,
      );
}
