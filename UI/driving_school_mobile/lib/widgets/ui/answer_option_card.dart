import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';

/// One lettered answer option (4.2.6: "A", "B", "C", "D" as rounded selectable cards). [state]
/// controls the read-only review colouring (correct/incorrect/plain) once a test is submitted.
enum AnswerOptionState { plain, selected, correct, incorrect }

class AnswerOptionCard extends StatelessWidget {
  final String letter;
  final String text;
  final AnswerOptionState state;
  final VoidCallback? onTap;

  const AnswerOptionCard({super.key, required this.letter, required this.text, this.state = AnswerOptionState.plain, this.onTap});

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final (border, background, badgeColor) = switch (state) {
      AnswerOptionState.selected => (c.primary, c.primarySoft, c.primary),
      AnswerOptionState.correct => (c.success, c.successSoft, c.success),
      AnswerOptionState.incorrect => (c.danger, c.dangerSoft, c.danger),
      AnswerOptionState.plain => (c.border, c.surface, c.textMuted),
    };

    return InkWell(
      borderRadius: AppRadius.all(AppRadius.lg),
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.all(AppSpacing.md),
        decoration: BoxDecoration(
          color: background,
          borderRadius: AppRadius.all(AppRadius.lg),
          border: Border.all(color: border, width: state == AnswerOptionState.plain ? 1 : 1.6),
        ),
        child: Row(
          children: [
            Container(
              width: 28,
              height: 28,
              alignment: Alignment.center,
              decoration: BoxDecoration(shape: BoxShape.circle, border: Border.all(color: badgeColor, width: 1.6)),
              child: Text(letter, style: context.text.labelLarge?.copyWith(color: badgeColor)),
            ),
            const SizedBox(width: AppSpacing.sm),
            Expanded(child: Text(text, style: context.text.bodyMedium)),
            if (state == AnswerOptionState.correct) Icon(Icons.check_circle_rounded, color: c.success, size: 20),
            if (state == AnswerOptionState.incorrect) Icon(Icons.cancel_rounded, color: c.danger, size: 20),
          ],
        ),
      ),
    );
  }
}
