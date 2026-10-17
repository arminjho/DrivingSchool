import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../l10n/app_strings.dart';
import 'app_badge.dart';

/// The single dialog shell: header (icon, title, close "X" top-right), a body that scrolls on
/// its own, and a pinned footer with the actions - so a long form never pushes the primary
/// button off-screen.
class AppDialog extends StatelessWidget {
  final String title;
  final String? subtitle;
  final IconData? icon;
  final AppTone tone;
  final Widget child;
  final List<Widget> actions;
  final double width;
  final bool showClose;

  const AppDialog({
    super.key,
    required this.title,
    required this.child,
    this.subtitle,
    this.icon,
    this.tone = AppTone.primary,
    this.actions = const [],
    this.width = 520,
    this.showClose = true,
  });

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final maxHeight = MediaQuery.sizeOf(context).height * 0.86;

    return Dialog(
      backgroundColor: Colors.transparent,
      elevation: 0,
      insetPadding: const EdgeInsets.all(AppSpacing.md),
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: width, maxHeight: maxHeight),
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: c.surface,
            borderRadius: AppRadius.all(AppRadius.xl),
            border: Border.all(color: c.border),
            boxShadow: c.shadowLg,
          ),
          child: ClipRRect(
            borderRadius: AppRadius.all(AppRadius.xl),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _header(context),
                Flexible(child: SingleChildScrollView(padding: const EdgeInsets.all(AppSpacing.lg), child: child)),
                if (actions.isNotEmpty) _footer(context),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _header(BuildContext context) {
    final c = context.colors;

    return Container(
      padding: const EdgeInsets.fromLTRB(AppSpacing.lg, AppSpacing.md, AppSpacing.sm, AppSpacing.md),
      decoration: BoxDecoration(border: Border(bottom: BorderSide(color: c.border))),
      child: Row(
        children: [
          if (icon != null) ...[
            Container(
              height: 38,
              width: 38,
              decoration: BoxDecoration(
                color: tone.background(context),
                borderRadius: AppRadius.all(AppRadius.md),
                border: Border.all(color: tone.foreground(context).withValues(alpha: 0.3)),
              ),
              child: Icon(icon, size: 19, color: tone.foreground(context)),
            ),
            const SizedBox(width: AppSpacing.sm),
          ],
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(title, style: context.text.titleLarge),
                if (subtitle != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(subtitle!, style: context.text.bodySmall?.copyWith(color: c.textMuted)),
                  ),
              ],
            ),
          ),
          if (showClose)
            IconButton(
              iconSize: 18,
              tooltip: AppStrings.close,
              onPressed: () => Navigator.of(context).maybePop(),
              icon: const Icon(Icons.close_rounded),
            ),
        ],
      ),
    );
  }

  Widget _footer(BuildContext context) {
    final c = context.colors;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
      decoration: BoxDecoration(
        color: c.surfaceMuted,
        border: Border(top: BorderSide(color: c.border)),
      ),
      child: Wrap(
        alignment: WrapAlignment.end,
        spacing: AppSpacing.xs,
        runSpacing: AppSpacing.xs,
        children: actions,
      ),
    );
  }
}

Future<T?> showAppDialog<T>({
  required BuildContext context,
  required WidgetBuilder builder,
  bool barrierDismissible = true,
}) =>
    showDialog<T>(context: context, barrierDismissible: barrierDismissible, builder: builder);

/// Confirmation for anything irreversible (delete, pay, send). The confirm label should be the
/// verb ("Obriši"), never "OK", so the result is what the person predicted before clicking.
Future<bool> showConfirmDialog({
  required BuildContext context,
  required String title,
  required String message,
  String confirmLabel = AppStrings.confirm,
  String cancelLabel = AppStrings.cancel,
  bool destructive = false,
  IconData? icon,
}) async {
  final confirmed = await showAppDialog<bool>(
    context: context,
    builder: (context) {
      final c = context.colors;

      return AppDialog(
        title: title,
        icon: icon ?? (destructive ? Icons.delete_outline_rounded : Icons.help_outline_rounded),
        tone: destructive ? AppTone.danger : AppTone.primary,
        width: 460,
        showClose: false,
        actions: [
          OutlinedButton(onPressed: () => Navigator.of(context).pop(false), child: Text(cancelLabel)),
          FilledButton(
            style: destructive ? FilledButton.styleFrom(backgroundColor: c.danger, foregroundColor: c.onPrimary) : null,
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(confirmLabel),
          ),
        ],
        child: Text(message, style: context.text.bodyMedium?.copyWith(color: c.textSecondary, height: 1.5)),
      );
    },
  );

  return confirmed ?? false;
}
