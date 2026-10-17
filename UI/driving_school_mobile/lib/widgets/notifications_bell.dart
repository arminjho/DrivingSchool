import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../core/design_tokens.dart';
import '../core/notification_center.dart';
import '../l10n/app_strings.dart';

/// A bell with the unread count, fed by the shared [NotificationCenter] (SignalR push with
/// polling as the fallback). Tapping it is up to the host - a shell opens its notifications.
class NotificationsBell extends StatelessWidget {
  final VoidCallback onPressed;

  const NotificationsBell({super.key, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    final unread = context.select<NotificationCenter, int>((center) => center.unreadCount);

    return IconButton(
      tooltip: unread > 0 ? AppStrings.notificationsUnread(unread) : AppStrings.notificationsTitle,
      onPressed: onPressed,
      icon: Badge(
        label: Text(unread > 99 ? '99+' : '$unread'),
        isLabelVisible: unread > 0,
        child: const Icon(AppIcons.notifications),
      ),
    );
  }
}
