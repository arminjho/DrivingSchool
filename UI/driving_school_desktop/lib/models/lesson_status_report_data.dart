import '../core/json_helpers.dart';
import 'lesson_status_report_row.dart';

/// Mirrors the backend's `LessonStatusReportData`.
class LessonStatusReportData {
  final DateTime? dateFrom;
  final DateTime? dateTo;
  final int? instructorId;
  final String? instructorName;
  final DateTime generatedAtUtc;
  final List<LessonStatusReportRow> rows;
  final int totalCompleted;
  final int totalScheduled;
  final int totalCancelled;
  final int totalLessons;

  const LessonStatusReportData({
    this.dateFrom,
    this.dateTo,
    this.instructorId,
    this.instructorName,
    required this.generatedAtUtc,
    required this.rows,
    required this.totalCompleted,
    required this.totalScheduled,
    required this.totalCancelled,
    required this.totalLessons,
  });

  factory LessonStatusReportData.fromJson(Map<String, dynamic> json) => LessonStatusReportData(
      dateFrom: parseUtcDateTimeOrNull(json['dateFrom'] as String?),
      dateTo: parseUtcDateTimeOrNull(json['dateTo'] as String?),
      instructorId: json['instructorId'] as int?,
      instructorName: json['instructorName'] as String?,
      generatedAtUtc: parseUtcDateTime(json['generatedAtUtc'] as String),
      rows: (json['rows'] as List<dynamic>? ?? const []).map((item) => LessonStatusReportRow.fromJson(item as Map<String, dynamic>)).toList(),
      totalCompleted: json['totalCompleted'] as int,
      totalScheduled: json['totalScheduled'] as int,
      totalCancelled: json['totalCancelled'] as int,
      totalLessons: json['totalLessons'] as int,
      );
}
