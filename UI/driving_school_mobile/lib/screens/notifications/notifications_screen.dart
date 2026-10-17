import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/design_tokens.dart';
import '../../core/formatters.dart';
import '../../core/notification_center.dart';
import '../../l10n/app_strings.dart';
import '../../models/notification_item.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';

/// The notification feed: unread first-class (bold, marked), tap to mark one read, one action to
/// mark all. It lives on the shared [NotificationCenter], so a notification pushed while it is
/// open appears without any refresh.
class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  late final NotificationCenter _center;

  @override
  void initState() {
    super.initState();
    // Watching is what makes the center fetch the list, not just the badge count.
    _center = context.read<NotificationCenter>()..beginWatchingList();
  }

  @override
  void dispose() {
    _center.endWatchingList();
    super.dispose();
  }

  Future<void> _markRead(NotificationItem item) async {
    try {
      await _center.markAsRead(item);
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error, outcome: AppStrings.markReadFailed);
    }
  }

  Future<void> _markAllRead() async {
    try {
      await _center.markAllAsRead();
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error, outcome: AppStrings.markAllReadFailed);
    }
  }

  @override
  Widget build(BuildContext context) {
    final center = context.watch<NotificationCenter>();
    final items = center.items;

    if (items == null) {
      return center.listError != null
          ? AppErrorState(title: AppStrings.notificationsLoadFailed, message: center.listError!, onRetry: center.refresh)
          : const Center(child: CircularProgressIndicator());
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(AppSpacing.md, AppSpacing.sm, AppSpacing.md, 0),
          child: Row(
            children: [
              if (center.listError != null)
                Expanded(child: Text(AppStrings.notificationsStale(center.listError!), style: context.text.bodySmall))
              else
                const Spacer(),
              TextButton.icon(
                onPressed: center.unreadCount > 0 ? _markAllRead : null,
                icon: const Icon(Icons.done_all_rounded, size: 18),
                label: const Text(AppStrings.markAllRead),
              ),
            ],
          ),
        ),
        Expanded(
          child: items.isEmpty
              ? const AppEmptyState(
                  icon: Icons.notifications_none_rounded,
                  title: AppStrings.notificationsEmpty,
                  message: AppStrings.notificationsEmptyHint,
                )
              : RefreshIndicator(
                  onRefresh: center.refresh,
                  child: ListView.separated(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    itemCount: items.length,
                    separatorBuilder: (context, index) => const SizedBox(height: AppSpacing.xs),
                    itemBuilder: (context, index) => _NotificationTile(item: items[index], onTap: () => _markRead(items[index])),
                  ),
                ),
        ),
      ],
    );
  }
}

class _NotificationTile extends StatelessWidget {
  final NotificationItem item;
  final VoidCallback onTap;

  const _NotificationTile({required this.item, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Semantics(
      label: item.isRead ? null : AppStrings.markAsRead,
      child: AppCard(
        padding: const EdgeInsets.all(AppSpacing.md),
        onTap: item.isRead ? null : onTap,
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(
              item.isRead ? Icons.mail_outline_rounded : Icons.mark_email_unread_rounded,
              color: item.isRead ? c.textMuted : c.primary,
              size: 22,
            ),
            const SizedBox(width: AppSpacing.sm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.title,
                    style: context.text.titleSmall?.copyWith(
                      fontWeight: item.isRead ? FontWeight.w500 : FontWeight.w700,
                      color: item.isRead ? c.textSecondary : c.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(item.message, style: context.text.bodyMedium?.copyWith(color: c.textSecondary)),
                  const SizedBox(height: AppSpacing.xxs),
                  Text(AppFormat.dateTime(item.createdAt), style: context.text.bodySmall?.copyWith(color: c.textMuted)),
                ],
              ),
            ),
            if (!item.isRead)
              Container(width: 8, height: 8, margin: const EdgeInsets.only(top: 6), decoration: BoxDecoration(color: c.primary, shape: BoxShape.circle)),
          ],
        ),
      ),
    );
  }
}
