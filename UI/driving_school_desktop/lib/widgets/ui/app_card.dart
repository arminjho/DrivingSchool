import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';

/// The single panel primitive: radius, border and shadow can't drift between screens.
class AppCard extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry padding;
  final VoidCallback? onTap;

  const AppCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(AppSpacing.lg),
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final content = Padding(padding: padding, child: child);

    return DecoratedBox(
      decoration: BoxDecoration(
        color: c.surface,
        borderRadius: AppRadius.all(AppRadius.lg),
        border: Border.all(color: c.border),
        boxShadow: c.shadowSm,
      ),
      child: ClipRRect(
        borderRadius: AppRadius.all(AppRadius.lg),
        child: onTap == null
            ? content
            : Material(color: Colors.transparent, child: InkWell(onTap: onTap, child: content)),
      ),
    );
  }
}
