import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/formatters.dart';
import '../../l10n/app_strings.dart';
import '../../layouts/shell_scope.dart';
import '../../models/lesson.dart';
import '../../providers/instructor_api.dart';
import '../../providers/lesson_provider.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_dashed_box.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/lesson_status_badge.dart';
import 'lesson_completion_sheet.dart';

/// 4.3.4 - a day strip (7 days, today centred) and that day's lessons in a vertical timeline,
/// each with "▶ Počni čas" (Confirmed, not yet started) or "■ Završi čas" (Confirmed, started).
/// Both are UI-only steps on the client - the state machine still goes straight
/// Confirmed -> Completed when 4.3.5 is saved (docs/prijava.md §4.3.4 note). The hours the
/// instructor is still free for come from the server's available-slots list and are drawn as
/// dashed "Slobodan termin" placeholders between the lessons.
class ScheduleScreen extends StatefulWidget {
  const ScheduleScreen({super.key});

  @override
  State<ScheduleScreen> createState() => _ScheduleScreenState();
}

class _ScheduleScreenState extends State<ScheduleScreen> {
  late final InstructorApi _instructorApi;
  late final LessonProvider _lessonProvider;

  late final int? _instructorId;
  late DateTime _selectedDate;
  List<Lesson>? _lessons;
  List<DateTime> _freeSlots = const [];
  String? _error;
  bool _isBusy = false;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _instructorApi = InstructorApi(session);
    _lessonProvider = LessonProvider(session);
    _instructorId = session.userId;
    final today = DateTime.now();
    _selectedDate = DateTime(today.year, today.month, today.day);
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final instructorId = _instructorId;
      final results = await (
        _instructorApi.getSchedule(_selectedDate),
        instructorId == null ? Future.value(const <DateTime>[]) : _lessonProvider.getAvailableSlots(instructorId: instructorId, date: _selectedDate),
      ).wait;
      if (mounted) {
        setState(() {
          _lessons = results.$1;
          _freeSlots = results.$2;
        });
      }
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  void _selectDate(DateTime date) {
    setState(() {
      _selectedDate = date;
      _lessons = null;
    });
    _load();
  }

  Future<void> _start(Lesson lesson) async {
    setState(() => _isBusy = true);
    try {
      await _lessonProvider.start(lesson.id);
      if (mounted) showSuccessMessage(context, AppStrings.lessonStartedMessage);
      await _load();
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _finish(Lesson lesson) async {
    final completed = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl))),
      builder: (_) => LessonCompletionSheet(lesson: lesson),
    );
    if (completed == true) _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: ShellScope.isInside(context) ? null : AppBar(title: const Text(AppStrings.scheduleTitle)),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _dayStrip(context),
          const Divider(height: 1),
          Expanded(child: _body(context)),
        ],
      ),
    );
  }

  Widget _dayStrip(BuildContext context) {
    final today = DateTime.now();
    final weekStart = DateTime(today.year, today.month, today.day).subtract(Duration(days: today.weekday - 1));

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(AppSpacing.md, 0, AppSpacing.md, AppSpacing.xs),
            child: Text(AppStrings.monthYear(_selectedDate.month, _selectedDate.year), style: context.text.titleMedium),
          ),
          Row(
            children: [
              for (var i = 0; i < 7; i++)
                Expanded(child: _dayCell(context, weekStart.add(Duration(days: i)))),
            ],
          ),
        ],
      ),
    );
  }

  Widget _dayCell(BuildContext context, DateTime date) {
    final c = context.colors;
    final isSelected = date.year == _selectedDate.year && date.month == _selectedDate.month && date.day == _selectedDate.day;

    return InkWell(
      onTap: () => _selectDate(date),
      borderRadius: AppRadius.all(AppRadius.md),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
        child: Column(
          children: [
            Text(AppStrings.dayAbbreviations[date.weekday - 1], style: context.text.labelSmall?.copyWith(color: c.textMuted)),
            const SizedBox(height: AppSpacing.xxs),
            Container(
              height: 34,
              width: 34,
              alignment: Alignment.center,
              decoration: BoxDecoration(shape: BoxShape.circle, color: isSelected ? c.primary : Colors.transparent),
              child: Text('${date.day}', style: context.text.titleSmall?.copyWith(color: isSelected ? c.onPrimary : c.textPrimary)),
            ),
          ],
        ),
      ),
    );
  }

  Widget _body(BuildContext context) {
    final lessons = _lessons;

    if (lessons == null) {
      return Padding(
        padding: AppSpacing.page,
        child: _error != null ? AppErrorState(message: _error!, onRetry: _load) : const Center(child: CircularProgressIndicator()),
      );
    }

    // The day in time order: the instructor's lessons, with a placeholder for each free hour.
    final entries = <({DateTime start, Lesson? lesson})>[
      for (final lesson in lessons) (start: lesson.startsAtUtc, lesson: lesson),
      for (final slot in _freeSlots) (start: slot, lesson: null),
    ]..sort((a, b) => a.start.compareTo(b.start));

    return Padding(
      padding: AppSpacing.page,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(AppFormat.date(_selectedDate), style: context.text.titleSmall),
          const SizedBox(height: AppSpacing.md),
          Expanded(
            child: entries.isEmpty
                ? AppEmptyState(icon: Icons.event_busy_outlined, title: AppStrings.noLessonsForDate, message: AppStrings.noLessonsForDate)
                : ListView.separated(
                    itemCount: entries.length,
                    separatorBuilder: (context, index) => const SizedBox(height: AppSpacing.sm),
                    itemBuilder: (context, index) {
                      final entry = entries[index];
                      final lesson = entry.lesson;
                      return lesson == null ? _freeSlotRow(context, entry.start) : _lessonRow(context, lesson);
                    },
                  ),
          ),
        ],
      ),
    );
  }

  Widget _freeSlotRow(BuildContext context, DateTime start) {
    final c = context.colors;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        SizedBox(width: 48, child: Text(AppFormat.time(start), style: context.text.titleSmall?.copyWith(fontFeatures: AppTypography.tabular, color: c.textMuted))),
        const SizedBox(width: AppSpacing.sm),
        Expanded(child: AppDashedBox(child: Text(AppStrings.scheduleEmptySlot, style: context.text.bodyMedium?.copyWith(color: c.textMuted)))),
      ],
    );
  }

  Widget _lessonRow(BuildContext context, Lesson lesson) {
    final c = context.colors;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(width: 48, child: Text(AppFormat.time(lesson.startsAtUtc), style: context.text.titleSmall?.copyWith(fontFeatures: AppTypography.tabular))),
        const SizedBox(width: AppSpacing.sm),
        Expanded(
          child: AppCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(child: Text(lesson.studentFullName, style: context.text.titleSmall)),
                    LessonStatusBadge(status: lesson.status),
                  ],
                ),
                const SizedBox(height: AppSpacing.xxs),
                Row(children: [Icon(AppIcons.place, size: 14, color: c.textMuted), const SizedBox(width: AppSpacing.xs), Text(lesson.drivingZoneName, style: context.text.bodySmall)]),
                if (lesson.status == 'Completed') ...[
                  const SizedBox(height: AppSpacing.xs),
                  Row(
                    children: [
                      Icon(Icons.check_rounded, size: 16, color: c.success),
                      const SizedBox(width: AppSpacing.xxs),
                      Text(AppStrings.lessonFinishedLabel, style: context.text.bodySmall?.copyWith(color: c.success)),
                    ],
                  ),
                ],
                if (lesson.status == 'Confirmed') ...[
                  const SizedBox(height: AppSpacing.sm),
                  lesson.startedAt == null
                      ? FilledButton.icon(
                          onPressed: _isBusy ? null : () => _start(lesson),
                          icon: const Icon(Icons.play_arrow_rounded, size: 18),
                          label: const Text(AppStrings.startLessonAction),
                          style: FilledButton.styleFrom(backgroundColor: c.success, minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
                        )
                      : OutlinedButton.icon(
                          onPressed: _isBusy ? null : () => _finish(lesson),
                          icon: const Icon(Icons.stop_rounded, size: 18),
                          label: const Text(AppStrings.finishLessonAction),
                          style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
                        ),
                ],
              ],
            ),
          ),
        ),
      ],
    );
  }
}
