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

/// 4.3.3 - the student's contact card, the lesson's info, their note, and, while the request
/// is still Pending, the "✕ Odbij" / "✓ Potvrdi čas" actions.
class RequestDetailsScreen extends StatefulWidget {
  const RequestDetailsScreen({super.key, required this.lessonId});

  final int lessonId;

  @override
  State<RequestDetailsScreen> createState() => _RequestDetailsScreenState();
}

class _RequestDetailsScreenState extends State<RequestDetailsScreen> {
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
      final lesson = await _provider.getById(widget.lessonId);
      if (mounted) setState(() => _lesson = lesson);
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  Future<void> _approve() async {
    setState(() => _isBusy = true);
    try {
      final updated = await _provider.confirm(widget.lessonId);
      if (mounted) {
        setState(() => _lesson = updated);
        showSuccessMessage(context, AppStrings.requestApprovedMessage);
      }
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _reject() async {
    final reasonController = TextEditingController();
    String? reasonError;

    final confirmed = await showAppDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: AppStrings.rejectDialogTitle,
          icon: Icons.cancel_outlined,
          tone: AppTone.danger,
          width: 440,
          actions: [
            OutlinedButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text(AppStrings.cancel)),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: dialogContext.colors.danger, foregroundColor: dialogContext.colors.onPrimary),
              onPressed: () {
                if (reasonController.text.trim().isEmpty) {
                  setDialogState(() => reasonError = AppStrings.required);
                  return;
                }
                Navigator.of(dialogContext).pop(true);
              },
              child: const Text(AppStrings.rejectAction),
            ),
          ],
          child: TextField(
            controller: reasonController,
            maxLines: 2,
            autofocus: true,
            decoration: InputDecoration(labelText: AppStrings.fieldRejectionReason, errorText: reasonError),
            onChanged: (_) => setDialogState(() => reasonError = null),
          ),
        ),
      ),
    );
    if (confirmed != true) return;

    setState(() => _isBusy = true);
    try {
      final updated = await _provider.reject(widget.lessonId, reason: reasonController.text.trim());
      if (mounted) {
        setState(() => _lesson = updated);
        showSuccessMessage(context, AppStrings.requestRejectedMessage);
      }
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final lesson = _lesson;
    final c = context.colors;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.requestDetailsTitle)),
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
                      AppAvatar(name: lesson.studentFullName, size: 44, imagePath: lesson.studentProfileImagePath),
                      const SizedBox(width: AppSpacing.sm),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(lesson.studentFullName, style: context.text.titleMedium),
                            Text(lesson.studentEmail, style: context.text.bodySmall?.copyWith(color: c.textMuted)),
                            Text(lesson.studentPhoneNumber, style: context.text.bodySmall?.copyWith(color: c.textMuted)),
                          ],
                        ),
                      ),
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
                      _infoRow(context, icon: AppIcons.vehicle, label: AppStrings.infoVehicle, value: lesson.vehicleLabel ?? AppStrings.noVehicleAssignedYet),
                      _infoRow(context, icon: AppIcons.lesson, label: AppStrings.infoLessonNumber, value: AppStrings.instructorLessonNumberLabel(lesson.lessonNumber)),
                    ],
                  ),
                ),
                if (lesson.note != null) ...[
                  const SizedBox(height: AppSpacing.md),
                  AppCard(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(AppStrings.studentNoteTitle, style: context.text.labelMedium),
                        const SizedBox(height: AppSpacing.xxs),
                        Text('"${lesson.note}"', style: context.text.bodyMedium?.copyWith(fontStyle: FontStyle.italic, color: c.textSecondary)),
                      ],
                    ),
                  ),
                ],
                if (lesson.rejectionReason != null) ...[
                  const SizedBox(height: AppSpacing.md),
                  AppNotice(tone: AppTone.danger, icon: Icons.info_outline_rounded, message: lesson.rejectionReason!),
                ],
                if (lesson.status == 'Pending') ...[
                  const SizedBox(height: AppSpacing.lg),
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: _isBusy ? null : _reject,
                          icon: const Icon(Icons.close_rounded, size: 18),
                          label: const Text(AppStrings.rejectAction),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: c.danger,
                            side: BorderSide(color: c.danger),
                            minimumSize: const Size.fromHeight(AppSizes.controlHeight),
                          ),
                        ),
                      ),
                      const SizedBox(width: AppSpacing.sm),
                      Expanded(
                        child: FilledButton.icon(
                          onPressed: _isBusy ? null : _approve,
                          icon: _isBusy
                              ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2))
                              : const Icon(Icons.check_rounded, size: 18),
                          label: const Text(AppStrings.confirmLessonAction),
                          style: FilledButton.styleFrom(backgroundColor: c.success, minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
                        ),
                      ),
                    ],
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
