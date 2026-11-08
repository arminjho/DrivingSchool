import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/formatters.dart';
import '../../l10n/app_strings.dart';
import '../../models/lesson.dart';
import '../../providers/lesson_provider.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/star_rating.dart';

/// 4.3.5 - the completion/grading bottom sheet. The proposal describes "STATUS ČASA" as a
/// dropdown for changing the status to Completed; saving always sets Completed regardless, so
/// the dropdown offers only that one value instead of being replaced with a fixed field.
class LessonCompletionSheet extends StatefulWidget {
  const LessonCompletionSheet({super.key, required this.lesson});

  final Lesson lesson;

  @override
  State<LessonCompletionSheet> createState() => _LessonCompletionSheetState();
}

class _LessonCompletionSheetState extends State<LessonCompletionSheet> {
  final _notesController = TextEditingController();
  int _rating = 0;
  String? _ratingError;
  String? _notesError;
  bool _isSaving = false;

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final notes = _notesController.text.trim();
    final hasRating = _rating != 0;
    final hasNotes = notes.isNotEmpty;
    if (!hasRating || !hasNotes) {
      setState(() {
        _ratingError = hasRating ? null : AppStrings.ratingRequired;
        _notesError = hasNotes ? null : AppStrings.instructorNotesRequired;
      });
      return;
    }

    setState(() {
      _isSaving = true;
      _ratingError = null;
      _notesError = null;
    });
    try {
      final provider = LessonProvider(context.read<AuthSession>());
      await provider.complete(
        widget.lesson.id,
        rating: _rating,
        instructorNotes: notes,
      );
      if (mounted) {
        showSuccessMessage(context, AppStrings.lessonCompletedMessage);
        Navigator.of(context).pop(true);
      }
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final lesson = widget.lesson;

    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.sm,
          AppSpacing.lg,
          AppSpacing.lg,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: MainAxisSize.min,
          children: [
            Center(
              child: Container(
                height: 4,
                width: 40,
                margin: const EdgeInsets.only(bottom: AppSpacing.md),
                decoration: BoxDecoration(
                  color: c.border,
                  borderRadius: AppRadius.all(2),
                ),
              ),
            ),
            Row(
              children: [
                Expanded(
                  child: Text(
                    AppStrings.completeLessonTitle,
                    style: context.text.titleLarge,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close_rounded),
                  onPressed: () => Navigator.of(context).pop(false),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            Row(
              children: [
                AppAvatar(
                  name: lesson.studentFullName,
                  size: 40,
                  imagePath: lesson.studentProfileImagePath,
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        lesson.studentFullName,
                        style: context.text.titleSmall,
                      ),
                      Text(
                        '${AppFormat.dateTime(lesson.startsAtUtc)} · ${lesson.drivingZoneName}',
                        style: context.text.bodySmall?.copyWith(
                          color: c.textMuted,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.lg),
            Text(AppStrings.ratingLabel, style: context.text.labelLarge),
            const SizedBox(height: AppSpacing.xs),
            StarRating(
              value: _rating,
              onChanged: (value) => setState(() {
                _rating = value;
                _ratingError = null;
              }),
            ),
            if (_rating == 0) ...[
              const SizedBox(height: AppSpacing.xxs),
              Text(
                _ratingError ?? AppStrings.ratingHint,
                style: context.text.bodySmall?.copyWith(
                  color: _ratingError != null ? c.danger : c.textMuted,
                ),
              ),
            ],
            const SizedBox(height: AppSpacing.lg),
            TextField(
              controller: _notesController,
              maxLines: 4,
              onChanged: (_) {
                if (_notesError != null) setState(() => _notesError = null);
              },
              decoration: InputDecoration(
                labelText: AppStrings.fieldInstructorNotes,
                hintText: AppStrings.instructorNotesHint,
                alignLabelWithHint: true,
                errorText: _notesError,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            DropdownButtonFormField<String>(
              initialValue: AppStrings.lessonCompletedStatusValue,
              decoration: const InputDecoration(labelText: AppStrings.fieldLessonStatus),
              items: const [DropdownMenuItem(value: AppStrings.lessonCompletedStatusValue, child: Text(AppStrings.lessonCompletedStatusValue))],
              onChanged: (_) {},
            ),
            const SizedBox(height: AppSpacing.lg),
            FilledButton.icon(
              onPressed: _isSaving ? null : _save,
              icon: _isSaving
                  ? const SizedBox(
                      height: 16,
                      width: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.save_outlined, size: 18),
              label: const Text(AppStrings.save),
              style: FilledButton.styleFrom(
                minimumSize: const Size.fromHeight(AppSizes.controlHeight),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
