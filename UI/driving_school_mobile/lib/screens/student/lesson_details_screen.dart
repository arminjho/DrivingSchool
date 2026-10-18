import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/formatters.dart';
import '../../l10n/app_strings.dart';
import '../../models/lesson.dart';
import '../../providers/lesson_provider.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/lesson_status_badge.dart';
import '../../widgets/ui/star_rating.dart';
import 'reschedule_lesson_screen.dart';

/// 4.2.4 - the full record, with "Otkaži zahtjev"/"Izmijeni termin" shown or disabled-with-why
/// depending on the lesson's current state (upute.md §7: never a silently dead action).
class LessonDetailsScreen extends StatefulWidget {
  const LessonDetailsScreen({super.key, required this.lessonId});

  final int lessonId;

  @override
  State<LessonDetailsScreen> createState() => _LessonDetailsScreenState();
}

class _LessonDetailsScreenState extends State<LessonDetailsScreen> {
  late final LessonProvider _provider;

  Lesson? _lesson;
  String? _error;
  bool _isBusy = false;

  @override
  void initState() {
    super.initState();
    _provider = LessonProvider(context.read<AuthSession>());
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final lesson = await _provider.getMyLessonById(widget.lessonId);
      if (mounted) setState(() => _lesson = lesson);
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  /// Mirrors `LessonStateMachine.CancellationCutoffHours` - a Student may only cancel a
  /// Confirmed lesson at least 24h ahead; Pending has no cutoff at all.
  bool get _canCancel {
    final lesson = _lesson;
    if (lesson == null) return false;
    if (lesson.status == 'Pending') return true;
    if (lesson.status != 'Confirmed') return false;
    return lesson.startsAtUtc.difference(DateTime.now().toUtc()) >= const Duration(hours: 24);
  }

  String? get _cancelDisabledReason {
    final lesson = _lesson;
    if (lesson == null) return null;
    if (lesson.status == 'Completed') return AppStrings.cancelNotAllowedCompleted;
    if (lesson.status == 'Cancelled') return AppStrings.cancelNotAllowedCancelled;
    if (!_canCancel) return AppStrings.cancelNotAllowedCutoff;
    return null;
  }

  bool get _canReschedule => _lesson?.status == 'Pending';

  Future<void> _cancel() async {
    final lesson = _lesson;
    if (lesson == null) return;

    final requiresReason = lesson.status == 'Confirmed';
    final reasonController = TextEditingController();
    String? reasonError = requiresReason ? AppStrings.required : null;

    final confirmed = await showAppDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: AppStrings.cancelRequestAction,
          icon: Icons.cancel_outlined,
          tone: AppTone.danger,
          width: 440,
          actions: [
            OutlinedButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text(AppStrings.cancel)),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: dialogContext.colors.danger, foregroundColor: dialogContext.colors.onPrimary),
              onPressed: () {
                if (requiresReason && reasonController.text.trim().isEmpty) {
                  setDialogState(() => reasonError = AppStrings.required);
                  return;
                }
                Navigator.of(dialogContext).pop(true);
              },
              child: const Text(AppStrings.cancelRequestAction),
            ),
          ],
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(AppStrings.confirmDeleteMessage(AppFormat.dateTime(lesson.startsAtUtc)), style: dialogContext.text.bodyMedium),
              if (requiresReason) ...[
                const SizedBox(height: AppSpacing.md),
                TextField(
                  controller: reasonController,
                  maxLines: 2,
                  decoration: InputDecoration(labelText: AppStrings.required, errorText: reasonError),
                  onChanged: (_) => setDialogState(() => reasonError = null),
                ),
              ],
            ],
          ),
        ),
      ),
    );

    if (confirmed != true) return;
    setState(() => _isBusy = true);
    try {
      final updated = await _provider.cancel(lesson.id, reason: reasonController.text.trim().isEmpty ? null : reasonController.text.trim());
      if (mounted) {
        setState(() => _lesson = updated);
        showSuccessMessage(context, AppStrings.lessonCancelledMessage);
      }
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _reschedule() async {
    final lesson = _lesson;
    if (lesson == null) return;
    final changed = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => RescheduleLessonScreen(lesson: lesson)));
    if (changed == true) _load();
  }

  @override
  Widget build(BuildContext context) {
    final lesson = _lesson;
    final c = context.colors;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.lessonDetailsTitle)),
      body: lesson == null
          ? Padding(
              padding: AppSpacing.page,
              child: _error != null ? AppErrorState(message: _error!, onRetry: _load) : const Center(child: CircularProgressIndicator()),
            )
          : ListView(
              padding: AppSpacing.page,
              children: [
                AppCard(
                  child: Row(
                    children: [
                      Icon(AppIcons.vehicle, size: 28, color: c.primary),
                      const SizedBox(width: AppSpacing.sm),
                      Expanded(child: Text(AppStrings.lessonNumberLabel(lesson.lessonNumber), style: context.text.titleMedium)),
                      LessonStatusBadge(status: lesson.status),
                    ],
                  ),
                ),
                const SizedBox(height: AppSpacing.md),
                AppCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      _infoRow(context, icon: AppIcons.date, label: AppStrings.infoDateTime, value: AppFormat.dateTime(lesson.startsAtUtc)),
                      _infoRow(context, icon: AppIcons.place, label: AppStrings.infoZone, value: lesson.drivingZoneName),
                      _infoRow(context, icon: AppIcons.person, label: AppStrings.infoInstructor, value: lesson.instructorFullName),
                      _infoRow(context, icon: AppIcons.vehicle, label: AppStrings.infoVehicle, value: lesson.vehicleRegistrationNumber ?? AppStrings.noVehicleAssignedYet),
                      if (lesson.status == 'Completed') ...[
                        const Divider(height: AppSpacing.lg),
                        Row(
                          children: [
                            Text(AppStrings.infoRating, style: context.text.titleSmall),
                            const Spacer(),
                            StarRating(value: lesson.rating ?? 0, size: 20),
                          ],
                        ),
                        if (lesson.instructorNotes != null) ...[
                          const SizedBox(height: AppSpacing.sm),
                          Text(AppStrings.infoInstructorNotes, style: context.text.labelMedium),
                          const SizedBox(height: 2),
                          Text(lesson.instructorNotes!, style: context.text.bodyMedium),
                        ],
                      ],
                    ],
                  ),
                ),
                if (lesson.note != null) ...[
                  const SizedBox(height: AppSpacing.md),
                  AppNotice(tone: AppTone.warning, icon: Icons.sticky_note_2_outlined, message: lesson.note!),
                ],
                if (lesson.rejectionReason != null) ...[
                  const SizedBox(height: AppSpacing.md),
                  AppNotice(tone: AppTone.danger, icon: Icons.info_outline_rounded, message: lesson.rejectionReason!),
                ],
                const SizedBox(height: AppSpacing.lg),
                if (lesson.status == 'Pending' || lesson.status == 'Confirmed') ...[
                  if (_canReschedule) ...[
                    OutlinedButton.icon(
                      onPressed: _isBusy ? null : _reschedule,
                      icon: const Icon(Icons.edit_calendar_outlined, size: 18),
                      label: const Text(AppStrings.rescheduleAction),
                      style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
                    ),
                    const SizedBox(height: AppSpacing.sm),
                  ],
                  Tooltip(
                    message: _cancelDisabledReason ?? '',
                    triggerMode: TooltipTriggerMode.tap,
                    child: OutlinedButton.icon(
                      onPressed: _isBusy || !_canCancel ? null : _cancel,
                      icon: _isBusy
                          ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2))
                          : const Icon(Icons.cancel_outlined, size: 18),
                      label: const Text(AppStrings.cancelRequestAction),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: c.danger,
                        side: BorderSide(color: c.danger),
                        minimumSize: const Size.fromHeight(AppSizes.controlHeight),
                      ),
                    ),
                  ),
                ],
              ],
            ),
    );
  }

  Widget _infoRow(BuildContext context, {required IconData icon, required String label, required String value}) {
    final c = context.colors;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 16, color: c.textMuted),
          const SizedBox(width: AppSpacing.sm),
          Expanded(child: Text(label, style: context.text.bodySmall?.copyWith(color: c.textSecondary))),
          Expanded(flex: 2, child: Text(value, textAlign: TextAlign.right, style: context.text.bodyMedium)),
        ],
      ),
    );
  }
}
