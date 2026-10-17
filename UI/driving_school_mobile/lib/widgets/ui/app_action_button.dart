import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';

enum AppButtonKind { filled, outlined, text }

/// A button that is never silently dead: when an action isn't available it stays visible,
/// disabled, and says *why* in a tooltip (hover on desktop, tap on touch screens).
///
///     AppActionButton(
///       label: 'Otkaži čas',
///       onPressed: cancel,
///       disabledReason: canCancel ? null : 'Čas se može otkazati najkasnije 24h prije termina.',
///     )
class AppActionButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;

  /// Non-null disables the button and becomes its tooltip.
  final String? disabledReason;

  final AppButtonKind kind;
  final IconData? icon;
  final bool isLoading;
  final bool destructive;

  const AppActionButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.disabledReason,
    this.kind = AppButtonKind.filled,
    this.icon,
    this.isLoading = false,
    this.destructive = false,
  });

  bool get _isEnabled => disabledReason == null && !isLoading && onPressed != null;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final callback = _isEnabled ? onPressed : null;

    final Widget leading = isLoading
        ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
        : Icon(icon, size: 18);
    final child = Text(label);
    final hasLeading = isLoading || icon != null;

    final button = switch (kind) {
      AppButtonKind.filled => hasLeading
          ? FilledButton.icon(
              onPressed: callback,
              icon: leading,
              label: child,
              style: destructive ? FilledButton.styleFrom(backgroundColor: c.danger, foregroundColor: c.onPrimary) : null,
            )
          : FilledButton(
              onPressed: callback,
              style: destructive ? FilledButton.styleFrom(backgroundColor: c.danger, foregroundColor: c.onPrimary) : null,
              child: child,
            ),
      AppButtonKind.outlined => hasLeading
          ? OutlinedButton.icon(
              onPressed: callback,
              icon: leading,
              label: child,
              style: destructive ? OutlinedButton.styleFrom(foregroundColor: c.danger, side: BorderSide(color: c.danger)) : null,
            )
          : OutlinedButton(
              onPressed: callback,
              style: destructive ? OutlinedButton.styleFrom(foregroundColor: c.danger, side: BorderSide(color: c.danger)) : null,
              child: child,
            ),
      AppButtonKind.text => hasLeading
          ? TextButton.icon(onPressed: callback, icon: leading, label: child)
          : TextButton(onPressed: callback, child: child),
    };

    final reason = disabledReason;
    if (reason == null) return button;

    return Tooltip(message: reason, triggerMode: TooltipTriggerMode.tap, child: button);
  }
}
