import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/formatters.dart';
import '../../l10n/app_strings.dart';
import '../../models/dashboard.dart';
import '../../models/recent_activity.dart';
import '../../providers/dashboard_api.dart';
import '../../widgets/charts/app_bar_chart.dart';
import '../../widgets/charts/app_donut_chart.dart';
import '../../widgets/charts/chart_card.dart';
import '../../widgets/charts/chart_series.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/stat_card.dart';

/// 4.1.1 - the admin landing screen: 4 KPI cards, a monthly bar chart, a status donut, 3 status
/// count cards, a recent-activity feed and today's upcoming lessons. The whole screen is one
/// cached aggregate from the backend (`IMemoryCache`, short TTL), so a manual refresh here is
/// cheap and never re-runs the heavy GroupBy queries more than the cache allows.
class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  late final DashboardApi _api;
  Dashboard? _dashboard;
  String? _error;
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _api = DashboardApi(context.read<AuthSession>());
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final dashboard = await _api.getDashboard();
      if (mounted) setState(() => _dashboard = dashboard);
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final dashboard = _dashboard;

    if (dashboard == null) {
      return Padding(
        padding: AppSpacing.page,
        child: _error != null
            ? AppErrorState(title: AppStrings.dashboardLoadFailed, message: _error!, onRetry: _load)
            : const Center(child: CircularProgressIndicator()),
      );
    }

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: AppSpacing.page,
        children: [
          Align(
            alignment: Alignment.centerRight,
            child: OutlinedButton.icon(
              onPressed: _isLoading ? null : _load,
              icon: _isLoading
                  ? const SizedBox(height: 14, width: 14, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.refresh_rounded, size: 16),
              label: const Text(AppStrings.retry),
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          if (_error != null) ...[
            AppNotice(tone: AppTone.warning, icon: Icons.sync_problem_rounded, message: _error!),
            const SizedBox(height: AppSpacing.md),
          ],
          _kpiRow(dashboard),
          const SizedBox(height: AppSpacing.lg),
          LayoutBuilder(
            builder: (context, constraints) {
              final stacked = constraints.maxWidth < 900;
              final chart = ChartCard(
                title: AppStrings.dashboardLessonsPerMonth,
                subtitle: AppStrings.dashboardLessonsPerMonthSubtitle(dashboard.year),
                child: dashboard.completedLessonsPerMonth.isEmpty
                    ? const AppEmptyState(icon: Icons.bar_chart_rounded, title: AppStrings.emptyTitle, message: AppStrings.emptyMessage)
                    : AppBarChart(
                        labels: [for (final m in dashboard.completedLessonsPerMonth) AppStrings.monthAbbreviations[m.month - 1]],
                        series: [AppChartSeries(name: AppStrings.navLessons, values: [for (final m in dashboard.completedLessonsPerMonth) m.count.toDouble()])],
                      ),
              );
              final donut = ChartCard(
                title: AppStrings.dashboardStatusChart,
                subtitle: AppStrings.dashboardStatusChartSubtitle,
                child: dashboard.statusDistribution.isEmpty
                    ? const AppEmptyState(icon: Icons.donut_large_rounded, title: AppStrings.emptyTitle, message: AppStrings.emptyMessage)
                    : AppDonutChart(
                        centerLabel: AppStrings.navLessons,
                        slices: [
                          for (final s in dashboard.statusDistribution)
                            AppChartSlice(name: _statusLabel(s.status), value: s.count.toDouble(), color: _statusColor(context, s.status)),
                        ],
                      ),
              );

              if (stacked) {
                return Column(children: [chart, const SizedBox(height: AppSpacing.lg), donut]);
              }
              return Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(flex: 3, child: chart),
                  const SizedBox(width: AppSpacing.lg),
                  Expanded(flex: 2, child: donut),
                ],
              );
            },
          ),
          const SizedBox(height: AppSpacing.lg),
          _statusCountRow(dashboard),
          const SizedBox(height: AppSpacing.lg),
          LayoutBuilder(
            builder: (context, constraints) {
              final stacked = constraints.maxWidth < 900;
              final activity = _recentActivityCard(dashboard);
              final today = _todayLessonsCard(dashboard);

              if (stacked) {
                return Column(children: [today, const SizedBox(height: AppSpacing.lg), activity]);
              }
              return Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(flex: 3, child: today),
                  const SizedBox(width: AppSpacing.lg),
                  Expanded(flex: 2, child: activity),
                ],
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _kpiRow(Dashboard d) {
    final cards = <Widget>[
      StatCard(
        label: AppStrings.dashboardTotalStudents,
        value: '${d.activeStudents}',
        icon: AppIcons.person,
        tone: AppTone.info,
        caption: AppStrings.dashboardNewThisMonth(d.newStudentsThisMonth),
      ),
      StatCard(
        label: AppStrings.dashboardInstructors,
        value: '${d.activeInstructors}',
        icon: Icons.trending_up_rounded,
        tone: AppTone.success,
        caption: AppStrings.dashboardAllActive,
      ),
      StatCard(
        label: AppStrings.dashboardVehicles,
        value: '${d.activeVehicles}',
        icon: AppIcons.vehicle,
        tone: AppTone.primary,
        caption: AppStrings.dashboardInService(d.vehiclesInService),
      ),
      StatCard(
        label: AppStrings.dashboardLessonsThisMonth,
        value: '${d.lessonsThisMonth}',
        icon: AppIcons.time,
        tone: AppTone.warning,
        deltaPercent: d.lessonsChangePercent,
        caption: d.lessonsChangePercent == null ? null : AppStrings.dashboardVsLastMonth,
      ),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        const minCardWidth = 220.0;
        final perRow = (constraints.maxWidth / minCardWidth).floor().clamp(1, cards.length);
        final width = (constraints.maxWidth - (perRow - 1) * AppSpacing.md) / perRow;

        return Wrap(
          spacing: AppSpacing.md,
          runSpacing: AppSpacing.md,
          children: [for (final card in cards) SizedBox(width: width, child: card)],
        );
      },
    );
  }

  Widget _statusCountRow(Dashboard d) {
    final byStatus = {for (final s in d.statusDistribution) s.status: s.count};

    final cards = <Widget>[
      StatCard(label: AppStrings.lessonPending, value: '${byStatus['Pending'] ?? 0}', icon: Icons.schedule_rounded, tone: AppTone.warning),
      StatCard(label: AppStrings.lessonConfirmed, value: '${byStatus['Confirmed'] ?? 0}', icon: Icons.check_circle_outline_rounded, tone: AppTone.info),
      StatCard(label: AppStrings.lessonCancelled, value: '${byStatus['Cancelled'] ?? 0}', icon: Icons.cancel_outlined, tone: AppTone.danger),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        const minCardWidth = 220.0;
        final perRow = (constraints.maxWidth / minCardWidth).floor().clamp(1, cards.length);
        final width = (constraints.maxWidth - (perRow - 1) * AppSpacing.md) / perRow;

        return Wrap(
          spacing: AppSpacing.md,
          runSpacing: AppSpacing.md,
          children: [for (final card in cards) SizedBox(width: width, child: card)],
        );
      },
    );
  }

  Widget _recentActivityCard(Dashboard d) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(AppStrings.dashboardRecentActivity, style: context.text.titleMedium),
          const SizedBox(height: AppSpacing.sm),
          if (d.recentActivity.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
              child: Text(AppStrings.dashboardRecentActivityEmpty, style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
            )
          else
            for (final activity in d.recentActivity) _activityRow(activity),
        ],
      ),
    );
  }

  Widget _activityRow(RecentActivity activity) {
    final c = context.colors;
    final (icon, tone) = switch (activity.type) {
      'LessonRequested' => (Icons.event_note_outlined, AppTone.warning),
      'LessonConfirmed' => (Icons.check_circle_outline_rounded, AppTone.info),
      'LessonCancelled' => (Icons.cancel_outlined, AppTone.danger),
      'LessonCompleted' => (Icons.task_alt_rounded, AppTone.success),
      'VehicleServiceAdded' => (AppIcons.vehicle, AppTone.primary),
      'UserCreated' => (AppIcons.person, AppTone.neutral),
      _ => (Icons.notifications_none_rounded, AppTone.neutral),
    };

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            height: 30,
            width: 30,
            decoration: BoxDecoration(color: tone.background(context), borderRadius: AppRadius.all(AppRadius.sm)),
            child: Icon(icon, size: 15, color: tone.foreground(context)),
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(activity.title, style: context.text.bodyMedium),
                Text(activity.description, style: context.text.bodySmall?.copyWith(color: c.textSecondary)),
                Text(AppFormat.dateTime(activity.occurredAtUtc), style: context.text.bodySmall?.copyWith(color: c.textMuted, fontSize: 11)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _todayLessonsCard(Dashboard d) {
    final c = context.colors;

    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(AppStrings.dashboardTodayLessons, style: context.text.titleMedium),
          const SizedBox(height: AppSpacing.sm),
          if (d.todayLessons.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
              child: Text(AppStrings.dashboardTodayLessonsEmpty, style: context.text.bodySmall?.copyWith(color: c.textMuted)),
            )
          else
            Table(
              columnWidths: const {0: IntrinsicColumnWidth()},
              children: [
                for (final lesson in d.todayLessons)
                  TableRow(
                    children: [
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
                        child: Text(
                          '${AppFormat.time(lesson.startsAtUtc)}–${AppFormat.time(lesson.endsAtUtc)}',
                          style: context.text.bodyMedium?.copyWith(fontFeatures: AppTypography.tabular),
                        ),
                      ),
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs, horizontal: AppSpacing.sm),
                        child: Text('${lesson.studentFullName} · ${lesson.instructorFullName}', overflow: TextOverflow.ellipsis),
                      ),
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
                        child: Text(lesson.drivingZoneName, style: context.text.bodySmall?.copyWith(color: c.textSecondary)),
                      ),
                    ],
                  ),
              ],
            ),
        ],
      ),
    );
  }

  String _statusLabel(String status) => switch (status) {
        'Pending' => AppStrings.lessonPending,
        'Confirmed' => AppStrings.lessonConfirmed,
        'Completed' => AppStrings.lessonCompleted,
        'Cancelled' => AppStrings.lessonCancelled,
        _ => status,
      };

  Color _statusColor(BuildContext context, String status) => switch (status) {
        'Pending' => context.colors.warning,
        'Confirmed' => context.colors.info,
        'Completed' => context.colors.success,
        'Cancelled' => context.colors.danger,
        _ => context.colors.neutral,
      };
}
