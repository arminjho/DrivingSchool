import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../core/auth_api.dart';
import '../core/auth_session.dart';
import '../core/design_tokens.dart';
import '../l10n/app_strings.dart';
import '../screens/home_screen.dart';
import '../screens/notifications/notifications_screen.dart';
import '../widgets/notifications_bell.dart';
import '../widgets/ui/app_badge.dart';

/// One place the user can go: what the sidebar shows, what the header says, what the content
/// area builds. A screen is added to the app by adding an entry here.
class _Destination {
  final IconData icon;
  final String label;
  final String title;
  final String subtitle;
  final WidgetBuilder builder;

  const _Destination({
    required this.icon,
    required this.label,
    required this.title,
    required this.subtitle,
    required this.builder,
  });
}

/// The signed-in layout: a dark navigation sidebar, a page header with the notification bell and
/// the user menu, and the content area. Never navigates to login itself - clearing the
/// `AuthSession` (logout, a 401, token expiry) is enough.
class AppShell extends StatefulWidget {
  const AppShell({super.key});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  static const int _notificationsIndex = 1;

  static final List<_Destination> _destinations = [
    _Destination(
      icon: Icons.home_outlined,
      label: AppStrings.homeTitle,
      title: AppStrings.homeTitle,
      subtitle: AppStrings.homeSubtitle,
      builder: (_) => const HomeScreen(),
    ),
    _Destination(
      icon: AppIcons.notifications,
      label: AppStrings.notificationsTitle,
      title: AppStrings.notificationsTitle,
      subtitle: AppStrings.notificationsSubtitle,
      builder: (_) => const NotificationsScreen(),
    ),
  ];

  int _selected = 0;
  bool _collapsed = false;
  bool _isLoggingOut = false;

  Future<void> _logout() async {
    setState(() => _isLoggingOut = true);
    await context.read<AuthApi>().logout();
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final current = _destinations[_selected];

    return Scaffold(
      body: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _Sidebar(
            destinations: _destinations,
            selected: _selected,
            collapsed: _collapsed,
            onSelect: (index) => setState(() => _selected = index),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _TopBar(
                  title: current.title,
                  subtitle: current.subtitle,
                  onToggleSidebar: () => setState(() => _collapsed = !_collapsed),
                  onOpenNotifications: () => setState(() => _selected = _notificationsIndex),
                  onLogout: _isLoggingOut ? null : _logout,
                ),
                Expanded(
                  child: ColoredBox(
                    color: c.canvas,
                    child: KeyedSubtree(key: ValueKey(current.label), child: current.builder(context)),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Sidebar extends StatelessWidget {
  final List<_Destination> destinations;
  final int selected;
  final bool collapsed;
  final ValueChanged<int> onSelect;

  const _Sidebar({required this.destinations, required this.selected, required this.collapsed, required this.onSelect});

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return AnimatedContainer(
      duration: AppDuration.normal,
      curve: AppDuration.curve,
      width: collapsed ? AppSizes.sidebarCollapsed : AppSizes.sidebarExpanded,
      decoration: BoxDecoration(color: c.navBackground, border: Border(right: BorderSide(color: c.navBorder))),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Container(
            height: AppSizes.topBarHeight,
            padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
            alignment: Alignment.centerLeft,
            decoration: BoxDecoration(border: Border(bottom: BorderSide(color: c.navBorder))),
            child: Row(
              children: [
                Icon(AppIcons.vehicle, color: c.primary),
                if (!collapsed) ...[
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(AppStrings.appName, style: context.text.titleMedium?.copyWith(color: c.navItemSelectedText)),
                        Text(AppStrings.adminPanel, style: context.text.bodySmall?.copyWith(color: c.navTextMuted, fontSize: 12)),
                      ],
                    ),
                  ),
                ],
              ],
            ),
          ),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.all(AppSpacing.sm),
              children: [
                for (var i = 0; i < destinations.length; i++)
                  _NavItem(destination: destinations[i], selected: i == selected, collapsed: collapsed, onTap: () => onSelect(i)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _NavItem extends StatefulWidget {
  final _Destination destination;
  final bool selected;
  final bool collapsed;
  final VoidCallback onTap;

  const _NavItem({required this.destination, required this.selected, required this.collapsed, required this.onTap});

  @override
  State<_NavItem> createState() => _NavItemState();
}

class _NavItemState extends State<_NavItem> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final selected = widget.selected;
    final foreground = selected ? c.navItemSelectedText : c.navText;

    final item = MouseRegion(
      cursor: SystemMouseCursors.click,
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      child: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onTap: widget.onTap,
        child: Container(
          height: 44,
          margin: const EdgeInsets.only(bottom: 2),
          padding: EdgeInsets.symmetric(horizontal: widget.collapsed ? 0 : AppSpacing.sm),
          decoration: BoxDecoration(
            color: selected ? c.navItemSelected : (_hovered ? c.navItemHover : Colors.transparent),
            borderRadius: AppRadius.all(AppRadius.md),
          ),
          child: Row(
            mainAxisAlignment: widget.collapsed ? MainAxisAlignment.center : MainAxisAlignment.start,
            children: [
              Icon(widget.destination.icon, size: 20, color: selected ? c.primaryStrong : foreground),
              if (!widget.collapsed) ...[
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Text(
                    widget.destination.label,
                    overflow: TextOverflow.ellipsis,
                    style: context.text.labelLarge?.copyWith(color: foreground),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );

    return widget.collapsed ? Tooltip(message: widget.destination.label, child: item) : item;
  }
}

class _TopBar extends StatelessWidget {
  final String title;
  final String subtitle;
  final VoidCallback onToggleSidebar;
  final VoidCallback onOpenNotifications;
  final VoidCallback? onLogout;

  const _TopBar({
    required this.title,
    required this.subtitle,
    required this.onToggleSidebar,
    required this.onOpenNotifications,
    required this.onLogout,
  });

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final session = context.watch<AuthSession>();

    return Container(
      height: AppSizes.topBarHeight,
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      decoration: BoxDecoration(color: c.surface, border: Border(bottom: BorderSide(color: c.border))),
      child: Row(
        children: [
          IconButton(tooltip: AppStrings.toggleSidebar, onPressed: onToggleSidebar, icon: const Icon(Icons.menu_rounded)),
          const SizedBox(width: AppSpacing.xs),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: context.text.titleLarge),
                Text(subtitle, style: context.text.bodySmall, overflow: TextOverflow.ellipsis),
              ],
            ),
          ),
          NotificationsBell(onPressed: onOpenNotifications),
          const SizedBox(width: AppSpacing.xs),
          PopupMenuButton<void>(
            tooltip: session.fullName,
            offset: const Offset(0, 46),
            itemBuilder: (context) => [
              PopupMenuItem<void>(
                enabled: false,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(session.fullName, style: context.text.titleSmall),
                    Text(session.roleLabel, style: context.text.bodySmall),
                  ],
                ),
              ),
              const PopupMenuDivider(),
              PopupMenuItem<void>(
                onTap: onLogout,
                child: const Row(children: [Icon(Icons.logout_rounded, size: 18), SizedBox(width: AppSpacing.xs), Text(AppStrings.logout)]),
              ),
            ],
            child: AppAvatar(name: session.fullName, size: 36),
          ),
        ],
      ),
    );
  }
}
