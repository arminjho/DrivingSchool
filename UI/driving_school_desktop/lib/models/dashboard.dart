import '../core/json_helpers.dart';
import 'lesson_status_count.dart';
import 'monthly_lesson_count.dart';
import 'recent_activity.dart';
import 'today_lesson.dart';

/// Mirrors the backend's `DashboardDto`.
class Dashboard {
  final int activeStudents;
  final int newStudentsThisMonth;
  final int activeInstructors;
  final int inactiveInstructors;
  final int activeVehicles;
  final int vehiclesInService;
  final int lessonsThisMonth;
  final int lessonsLastMonth;
  final double? lessonsChangePercent;
  final int year;
  final List<MonthlyLessonCount> completedLessonsPerMonth;
  final List<LessonStatusCount> statusDistribution;
  final List<RecentActivity> recentActivity;
  final List<TodayLesson> todayLessons;
  final DateTime generatedAtUtc;

  const Dashboard({
    required this.activeStudents,
    required this.newStudentsThisMonth,
    required this.activeInstructors,
    required this.inactiveInstructors,
    required this.activeVehicles,
    required this.vehiclesInService,
    required this.lessonsThisMonth,
    required this.lessonsLastMonth,
    this.lessonsChangePercent,
    required this.year,
    required this.completedLessonsPerMonth,
    required this.statusDistribution,
    required this.recentActivity,
    required this.todayLessons,
    required this.generatedAtUtc,
  });

  factory Dashboard.fromJson(Map<String, dynamic> json) => Dashboard(
      activeStudents: json['activeStudents'] as int,
      newStudentsThisMonth: json['newStudentsThisMonth'] as int,
      activeInstructors: json['activeInstructors'] as int,
      inactiveInstructors: json['inactiveInstructors'] as int,
      activeVehicles: json['activeVehicles'] as int,
      vehiclesInService: json['vehiclesInService'] as int,
      lessonsThisMonth: json['lessonsThisMonth'] as int,
      lessonsLastMonth: json['lessonsLastMonth'] as int,
      lessonsChangePercent: (json['lessonsChangePercent'] as num?)?.toDouble(),
      year: json['year'] as int,
      completedLessonsPerMonth: (json['completedLessonsPerMonth'] as List<dynamic>? ?? const []).map((item) => MonthlyLessonCount.fromJson(item as Map<String, dynamic>)).toList(),
      statusDistribution: (json['statusDistribution'] as List<dynamic>? ?? const []).map((item) => LessonStatusCount.fromJson(item as Map<String, dynamic>)).toList(),
      recentActivity: (json['recentActivity'] as List<dynamic>? ?? const []).map((item) => RecentActivity.fromJson(item as Map<String, dynamic>)).toList(),
      todayLessons: (json['todayLessons'] as List<dynamic>? ?? const []).map((item) => TodayLesson.fromJson(item as Map<String, dynamic>)).toList(),
      generatedAtUtc: parseUtcDateTime(json['generatedAtUtc'] as String),
      );
}
