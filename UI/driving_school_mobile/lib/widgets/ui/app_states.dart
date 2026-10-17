import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../l10n/app_strings.dart';
import 'app_badge.dart';

/// An empty screen names what is missing and, where there is one, offers the action that fixes it.
class AppEmptyState extends StatelessWidget {
  final IconData icon;
  final String title;
  final String message;
  final Widget? action;

  const AppEmptyState({
    super.key,
    this.icon = Icons.inbox_outlined,
    required this.title,
    required this.message,
    this.action,
  });

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.xl, horizontal: AppSpacing.md),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              height: 52,
              width: 52,
              decoration: BoxDecoration(
                color: c.surfaceMuted,
                borderRadius: AppRadius.all(AppRadius.lg),
                border: Border.all(color: c.border),
              ),
              child: Icon(icon, size: 24, color: c.textMuted),
            ),
            const SizedBox(height: AppSpacing.md),
            Text(title, style: context.text.titleMedium, textAlign: TextAlign.center),
            const SizedBox(height: AppSpacing.xxs),
            ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 360),
              child: Text(
                message,
                textAlign: TextAlign.center,
                style: context.text.bodySmall?.copyWith(color: c.textMuted),
              ),
            ),
            if (action != null) ...[const SizedBox(height: AppSpacing.md), action!],
          ],
        ),
      ),
    );
  }
}

/// [title] is the outcome in the user's terms ("Obavijesti nisu učitane"); [message] is the
/// cause and next step - build it from `failureCause()` so those read the same everywhere.
class AppErrorState extends StatelessWidget {
  final String message;
  final String title;
  final VoidCallback? onRetry;

  const AppErrorState({
    super.key,
    required this.message,
    this.title = AppStrings.loadFailedTitle,
    this.onRetry,
  });

  @override
  Widget build(BuildContext context) => AppEmptyState(
        icon: Icons.error_outline_rounded,
        title: title,
        message: message,
        action: onRetry == null
            ? null
            : OutlinedButton.icon(
                onPressed: onRetry,
                icon: const Icon(Icons.refresh_rounded, size: 17),
                label: const Text(AppStrings.retry),
              ),
      );
}

/// A shimmering placeholder block. A skeleton tells the user what is coming and stops the layout
/// jumping when data lands.
class AppSkeleton extends StatefulWidget {
  final double? width;
  final double height;

  const AppSkeleton({super.key, this.width, this.height = 12});

  @override
  State<AppSkeleton> createState() => _AppSkeletonState();
}

class _AppSkeletonState extends State<AppSkeleton> with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1200),
  )..repeat(reverse: true);

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return AnimatedBuilder(
      animation: _controller,
      builder: (context, _) => Container(
        width: widget.width,
        height: widget.height,
        decoration: BoxDecoration(
          color: Color.lerp(c.surfaceMuted, c.border, _controller.value),
          borderRadius: AppRadius.all(AppRadius.xs),
        ),
      ),
    );
  }
}

/// An inline banner for a non-blocking message above a form or panel.
class AppNotice extends StatelessWidget {
  final String message;
  final AppTone tone;
  final IconData icon;

  const AppNotice({
    super.key,
    required this.message,
    this.tone = AppTone.danger,
    this.icon = Icons.error_outline_rounded,
  });

  @override
  Widget build(BuildContext context) {
    final foreground = tone.foreground(context);

    return Container(
      padding: const EdgeInsets.all(AppSpacing.sm),
      decoration: BoxDecoration(
        color: tone.background(context),
        borderRadius: AppRadius.all(AppRadius.md),
        border: Border.all(color: foreground.withValues(alpha: 0.35)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 17, color: foreground),
          const SizedBox(width: AppSpacing.xs),
          Expanded(child: Text(message, style: context.text.bodySmall?.copyWith(color: foreground, height: 1.4))),
        ],
      ),
    );
  }
}
