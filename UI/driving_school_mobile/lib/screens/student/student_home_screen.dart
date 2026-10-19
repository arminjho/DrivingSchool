import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/formatters.dart';
import '../../l10n/app_strings.dart';
import '../../models/lesson.dart';
import '../../providers/lesson_provider.dart';
import '../../providers/test_provider.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/lesson_status_badge.dart';
import 'book_lesson_screen.dart';
import 'finance_screen.dart';
import 'lesson_details_screen.dart';
import 'my_lessons_screen.dart';
import 'recommendations_screen.dart';
import 'tests_screen.dart';

/// 4.2.1 - greeting, "Sljedeći čas" card, the five-item quick-navigation menu, and a stats row.
class StudentHomeScreen extends StatefulWidget {
  const StudentHomeScreen({super.key});

  @override
  State<StudentHomeScreen> createState() => _StudentHomeScreenState();
}

class _StudentHomeScreenState extends State<StudentHomeScreen> {
  late final LessonProvider _lessonProvider;
  late final TestProvider _testProvider;

  Lesson? _nextLesson;
  int _completedLessons = 0;
  int _testsSolved = 0;
  double _averagePercent = 0;
  String? _error;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _lessonProvider = LessonProvider(session);
    _testProvider = TestProvider(session);
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final results = await (
        _lessonProvider.getMy(const PagedQuery(page: 1, pageSize: 1, sortBy: 'StartsAtUtc', ascending: true, filters: {'tab': 'active'})),
        _lessonProvider.getMy(const PagedQuery(page: 1, pageSize: 1, filters: {'tab': 'completed'})),
        _testProvider.getMy(),
      ).wait;

      final active = results.$1;
      final completed = results.$2;
      final tests = results.$3;
      final attempted = tests.where((t) => t.hasAttempt).toList();
      final average = attempted.isEmpty
          ? 0.0
          : attempted.map((t) => t.lastPercent ?? 0).reduce((a, b) => a + b) / attempted.length;

      if (!mounted) return;
      setState(() {
        _nextLesson = active.resultList.firstOrNull;
        _completedLessons = completed.count;
        _testsSolved = attempted.length;
        _averagePercent = average;
      });
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = context.watch<AuthSession>();

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: AppSpacing.page,
          children: [
            Text(AppStrings.welcome(session.fullName), style: context.text.headlineSmall),
            const SizedBox(height: AppSpacing.lg),
            if (_isLoading)
              const Center(child: Padding(padding: EdgeInsets.all(AppSpacing.lg), child: CircularProgressIndicator()))
            else if (_error != null)
              AppErrorState(message: _error!, onRetry: _load)
            else
              _nextLessonCard(context),
            const SizedBox(height: AppSpacing.lg),
            _menuItem(context, icon: Icons.calendar_month_rounded, tone: AppTone.primary, label: AppStrings.menuBookLesson, onTap: _openBooking),
            const SizedBox(height: AppSpacing.sm),
            _menuItem(context, icon: Icons.payments_outlined, tone: AppTone.success, label: AppStrings.menuFinance, onTap: _openFinance),
            const SizedBox(height: AppSpacing.sm),
            _menuItem(context, icon: Icons.event_note_outlined, tone: AppTone.info, label: AppStrings.menuMyLessons, onTap: _openMyLessons),
            const SizedBox(height: AppSpacing.sm),
            _menuItem(context, icon: Icons.quiz_outlined, tone: AppTone.danger, label: AppStrings.menuTests, onTap: _openTests),
            const SizedBox(height: AppSpacing.sm),
            _menuItem(context, icon: Icons.lightbulb_outline_rounded, tone: AppTone.warning, label: AppStrings.menuRecommendations, onTap: _openRecommendations),
            const SizedBox(height: AppSpacing.lg),
            if (!_isLoading && _error == null) _statsRow(context),
          ],
        ),
      ),
    );
  }

  Widget _nextLessonCard(BuildContext context) {
    final lesson = _nextLesson;
    final c = context.colors;

    if (lesson == null) {
      return AppEmptyState(
        icon: Icons.event_available_outlined,
        title: AppStrings.noUpcomingLesson,
        message: AppStrings.noUpcomingLessonHint,
        action: FilledButton.icon(onPressed: _openBooking, icon: const Icon(Icons.add_rounded, size: 18), label: const Text(AppStrings.menuBookLesson)),
      );
    }

    return AppCard(
      onTap: () => _openLessonDetails(lesson),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(AppStrings.nextLessonCard, style: context.text.titleMedium),
              const Spacer(),
              LessonStatusBadge(status: lesson.status),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          Row(children: [Icon(AppIcons.date, size: 16, color: c.textMuted), const SizedBox(width: AppSpacing.xs), Text(AppFormat.dateTime(lesson.startsAtUtc))]),
          const SizedBox(height: AppSpacing.xxs),
          Row(children: [Icon(AppIcons.person, size: 16, color: c.textMuted), const SizedBox(width: AppSpacing.xs), Text(lesson.instructorFullName)]),
          const SizedBox(height: AppSpacing.xxs),
          Row(
            children: [
              Icon(AppIcons.vehicle, size: 16, color: c.textMuted),
              const SizedBox(width: AppSpacing.xs),
              Text('${lesson.vehicleName ?? AppStrings.noVehicleAssignedYet} · ${lesson.drivingZoneName}'),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          Align(alignment: Alignment.centerRight, child: Text(AppStrings.lessonDetailsLink, style: context.text.labelLarge?.copyWith(color: c.primary))),
        ],
      ),
    );
  }

  Widget _menuItem(BuildContext context, {required IconData icon, required AppTone tone, required String label, required VoidCallback onTap}) {
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
          Icon(Icons.chevron_right_rounded, color: context.colors.textMuted),
        ],
      ),
    );
  }

  Widget _statsRow(BuildContext context) {
    return Row(
      children: [
        Expanded(child: _statTile(context, value: '$_completedLessons', label: AppStrings.statLessons, caption: AppStrings.statLessonsDone)),
        const SizedBox(width: AppSpacing.sm),
        Expanded(child: _statTile(context, value: '$_testsSolved', label: AppStrings.statTests, caption: AppStrings.statTestsSolved)),
        const SizedBox(width: AppSpacing.sm),
        Expanded(child: _statTile(context, value: '${_averagePercent.round()}%', label: AppStrings.statAverage, caption: AppStrings.statAverageLabel)),
      ],
    );
  }

  Widget _statTile(BuildContext context, {required String value, required String label, required String caption}) {
    return AppCard(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
      child: Column(
        children: [
          Text(value, style: context.text.headlineMedium),
          Text(label, style: context.text.labelMedium),
          Text(caption, style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
        ],
      ),
    );
  }

  Future<void> _openBooking() async {
    final booked = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => const BookLessonScreen()));
    if (booked == true) _load();
  }

  void _openFinance() => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const FinanceScreen()));

  void _openMyLessons() => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const MyLessonsScreen()));

  void _openTests() => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const TestsScreen()));

  void _openRecommendations() => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const RecommendationsScreen()));

  Future<void> _openLessonDetails(Lesson lesson) async {
    await Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => LessonDetailsScreen(lessonId: lesson.id)));
    _load();
  }
}
