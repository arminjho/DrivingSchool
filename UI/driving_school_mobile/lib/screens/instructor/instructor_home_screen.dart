import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/formatters.dart';
import '../../l10n/app_strings.dart';
import '../../models/instructor_home.dart';
import '../../models/instructor_statistics.dart';
import '../../providers/instructor_api.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/lesson_status_badge.dart';
import 'lesson_requests_screen.dart';
import 'my_students_screen.dart';
import 'request_details_screen.dart';
import 'schedule_screen.dart';

/// 4.3.1 - "Danas" card, the three business-process menu rows (with a pending-request count
/// badge), today's lesson list, and a stats row (text's "statistika o ukupnom broju odrađenih
/// časova" - the mockup screenshot omits it, but CLAUDE.md §8 says satisfy both sources).
class InstructorHomeScreen extends StatefulWidget {
  const InstructorHomeScreen({super.key});

  @override
  State<InstructorHomeScreen> createState() => _InstructorHomeScreenState();
}

class _InstructorHomeScreenState extends State<InstructorHomeScreen> {
  late final InstructorApi _api;

  InstructorHome? _home;
  InstructorStatistics? _statistics;
  String? _error;

  @override
  void initState() {
    super.initState();
    _api = InstructorApi(context.read<AuthSession>());
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final results = await (_api.getHome(), _api.getStatistics()).wait;
      if (!mounted) return;
      setState(() {
        _home = results.$1;
        _statistics = results.$2;
      });
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = context.watch<AuthSession>();
    final home = _home;

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: AppSpacing.page,
          children: [
            Text(AppStrings.instructorGreeting(session.fullName), style: context.text.headlineSmall),
            const SizedBox(height: AppSpacing.lg),
            if (home == null)
              _error != null
                  ? AppErrorState(message: _error!, onRetry: _load)
                  : const Center(child: Padding(padding: EdgeInsets.all(AppSpacing.lg), child: CircularProgressIndicator()))
            else ...[
              _todayCard(context, home),
              const SizedBox(height: AppSpacing.lg),
              _menuRow(
                context,
                icon: Icons.notifications_active_outlined,
                tone: AppTone.warning,
                label: AppStrings.menuLessonRequests,
                badgeCount: home.newRequestCount,
                onTap: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const LessonRequestsScreen())).then((_) => _load()),
              ),
              const SizedBox(height: AppSpacing.sm),
              _menuRow(
                context,
                icon: Icons.calendar_month_rounded,
                tone: AppTone.info,
                label: AppStrings.menuMySchedule,
                onTap: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const ScheduleScreen())),
              ),
              const SizedBox(height: AppSpacing.sm),
              _menuRow(
                context,
                icon: Icons.groups_outlined,
                tone: AppTone.success,
                label: AppStrings.menuMyStudents,
                onTap: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const MyStudentsScreen())),
              ),
              const SizedBox(height: AppSpacing.lg),
              Text(AppStrings.todaysLessonsTitle, style: context.text.titleSmall),
              const SizedBox(height: AppSpacing.sm),
              if (home.todayLessons.isEmpty)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
                  child: Text(AppStrings.noLessonsToday, style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
                )
              else
                for (final lesson in home.todayLessons)
                  Padding(
                    padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                    child: AppCard(
                      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.sm),
                      onTap: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => RequestDetailsScreen(lessonId: lesson.id))),
                      child: Row(
                        children: [
                          Text(AppFormat.time(lesson.startsAtUtc), style: context.text.titleSmall?.copyWith(fontFeatures: AppTypography.tabular)),
                          const SizedBox(width: AppSpacing.sm),
                          Expanded(child: Text(lesson.studentFullName)),
                          LessonStatusBadge(status: lesson.status),
                        ],
                      ),
                    ),
                  ),
              const SizedBox(height: AppSpacing.md),
              if (_statistics != null) _statsRow(context, _statistics!),
            ],
          ],
        ),
      ),
    );
  }

  Widget _todayCard(BuildContext context, InstructorHome home) {
    final c = context.colors;

    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(color: c.primarySoft, borderRadius: AppRadius.all(AppRadius.lg), border: Border.all(color: c.primary.withValues(alpha: 0.3))),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(AppStrings.todayCardTitle, style: context.text.titleMedium),
              const Spacer(),
              if (home.newRequestCount > 0) AppStatusBadge(label: AppStrings.newRequestsBadge(home.newRequestCount), tone: AppTone.warning),
            ],
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(AppFormat.date(DateTime.now()), style: context.text.bodySmall?.copyWith(color: c.textSecondary)),
          const SizedBox(height: AppSpacing.sm),
          Text(AppStrings.lessonCountToday(home.todayLessonCount), style: context.text.headlineSmall),
          if (home.todayLessons.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xxs),
            Text(home.todayLessons.map((l) => AppFormat.time(l.startsAtUtc)).join(' · '), style: context.text.bodyMedium?.copyWith(color: c.textSecondary)),
          ],
          const SizedBox(height: AppSpacing.md),
          OutlinedButton(
            onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const ScheduleScreen())),
            style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
            child: const Text(AppStrings.viewScheduleAction),
          ),
        ],
      ),
    );
  }

  Widget _menuRow(BuildContext context, {required IconData icon, required AppTone tone, required String label, required VoidCallback onTap, int badgeCount = 0}) {
    return AppCard(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.sm),
      onTap: onTap,
      child: Row(
        children: [
          Container(
            height: 38,
            width: 38,
            decoration: BoxDecoration(color: tone.background(context), borderRadius: AppRadius.all(AppRadius.md)),
            child: Icon(icon, color: tone.foreground(context), size: 19),
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(child: Text(label, style: context.text.titleSmall)),
          if (badgeCount > 0) ...[
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
              decoration: BoxDecoration(color: context.colors.danger, borderRadius: AppRadius.all(20)),
              child: Text('$badgeCount', style: context.text.labelMedium?.copyWith(color: context.colors.onPrimary)),
            ),
            const SizedBox(width: AppSpacing.xs),
          ],
          Icon(Icons.chevron_right_rounded, color: context.colors.textMuted),
        ],
      ),
    );
  }

  Widget _statsRow(BuildContext context, InstructorStatistics statistics) {
    return Row(
      children: [
        Expanded(child: _statTile(context, value: '${statistics.completedThisMonth}', label: AppStrings.statCompletedThisMonth)),
        const SizedBox(width: AppSpacing.sm),
        Expanded(child: _statTile(context, value: '${statistics.completedTotal}', label: AppStrings.statCompletedTotal)),
        const SizedBox(width: AppSpacing.sm),
        Expanded(child: _statTile(context, value: statistics.averageRating.toStringAsFixed(1), label: AppStrings.statAverageRating)),
      ],
    );
  }

  Widget _statTile(BuildContext context, {required String value, required String label}) {
    return AppCard(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
      child: Column(
        children: [
          Text(value, style: context.text.headlineMedium),
          Text(label, style: context.text.labelMedium, textAlign: TextAlign.center),
        ],
      ),
    );
  }
}
