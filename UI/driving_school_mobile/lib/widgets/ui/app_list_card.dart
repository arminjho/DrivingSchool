import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import 'app_card.dart';

/// The mobile equivalent of a desktop table row: one record as a touch-sized card - a leading
/// icon/avatar, a title, an optional subtitle, and a trailing widget (a status badge, a price,
/// a chevron). Used by [PagedListView] and anywhere else a list of records is shown.
class AppListCard extends StatelessWidget {
  final Widget? leading;
  final String title;
  final String? subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;

  const AppListCard({
    super.key,
    this.leading,
    required this.title,
    this.subtitle,
    this.trailing,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.md),
      onTap: onTap,
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: AppSizes.listCardMinHeight - AppSpacing.md * 2),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            if (leading != null) ...[leading!, const SizedBox(width: AppSpacing.sm)],
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(title, style: context.text.titleSmall, maxLines: 1, overflow: TextOverflow.ellipsis),
                  if (subtitle != null) ...[
                    const SizedBox(height: 2),
                    Text(
                      subtitle!,
                      style: context.text.bodySmall?.copyWith(color: c.textSecondary),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ],
              ),
            ),
            if (trailing != null) ...[const SizedBox(width: AppSpacing.sm), trailing!],
          ],
        ),
      ),
    );
  }
}
