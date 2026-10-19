import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../core/auth_api.dart';
import '../core/auth_session.dart';
import '../core/design_tokens.dart';
import '../l10n/app_strings.dart';
import '../screens/dashboard/dashboard_screen.dart';
import '../screens/lessons/lessons_overview_screen.dart';
import '../screens/notifications/notifications_screen.dart';
import '../screens/profile/profile_dialogs.dart';
import '../screens/reference_data/reference_data_screen.dart';
import '../screens/reports/reports_screen.dart';
import '../screens/study_lessons/study_lessons_screen.dart';
import '../screens/tests/tests_screen.dart';
import '../screens/users/users_screen.dart';
import '../screens/vehicles/vehicle_fleet_screen.dart';
import '../widgets/notifications_bell.dart';
import '../widgets/ui/app_badge.dart';

/// One place the user can go: what the sidebar shows, what the header says, what the content
/// area builds. A screen is added to the app by adding an entry here.
class _Destination {
  final IconData icon;
  final String label;
  final String subtitle;
  final WidgetBuilder builder;

  const _Destination({
    required this.icon,
    required this.label,
    required this.subtitle,
    required this.builder,
  });
}

/// The signed-in layout: a dark navigation sidebar, a breadcrumb header with the notification
/// bell and the user menu, and the content area. Never navigates to login itself - clearing the
/// `AuthSession` (logout, a 401, token expiry) is enough.
///
/// Notifications are reached only through the bell (4.1.1's sidebar has no "Obavijesti" item),
/// so they are a separate overlay flag rather than one more entry in [_destinations].
class AppShell extends StatefulWidget {
  const AppShell({super.key});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  static final List<_Destination> _destinations = [
    _Destination(icon: Icons.dashboard_outlined, label: AppStrings.navDashboard, subtitle: AppStrings.homeSubtitle, builder: (_) => const DashboardScreen()),
    _Destination(icon: AppIcons.person, label: AppStrings.navUsers, subtitle: AppStrings.usersSubtitle, builder: (_) => const UsersScreen()),
    _Destination(icon: AppIcons.vehicle, label: AppStrings.navVehicleFleet, subtitle: AppStrings.vehicleFleetSubtitle, builder: (_) => const VehicleFleetScreen()),
    _Destination(icon: Icons.description_outlined, label: AppStrings.navReports, subtitle: AppStrings.reportsSubtitle, builder: (_) => const ReportsScreen()),
    _Destination(icon: Icons.list_alt_rounded, label: AppStrings.navReferenceData, subtitle: AppStrings.referenceDataSubtitle, builder: (_) => const ReferenceDataScreen()),
    _Destination(icon: Icons.quiz_outlined, label: AppStrings.navTests, subtitle: AppStrings.testsSubtitle, builder: (_) => const TestsScreen()),
    _Destination(icon: Icons.menu_book_outlined, label: AppStrings.navStudyLessons, subtitle: AppStrings.studyLessonsSubtitle, builder: (_) => const StudyLessonsScreen()),
    _Destination(icon: AppIcons.lesson, label: AppStrings.navLessons, subtitle: AppStrings.lessonsOverviewSubtitle, builder: (_) => const LessonsOverviewScreen()),
  ];

  int _selected = 0;
  bool _collapsed = false;
  bool _showingNotifications = false;
  bool _isLoggingOut = false;

  Future<void> _logout() async {
    setState(() => _isLoggingOut = true);
    await context.read<AuthApi>().logout();
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final current = _destinations[_selected];
    final title = _showingNotifications ? AppStrings.notificationsTitle : current.label;
    final subtitle = _showingNotifications ? AppStrings.notificationsSubtitle : current.subtitle;

    return Scaffold(
      body: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _Sidebar(
            destinations: _destinations,
            selected: _selected,
            collapsed: _collapsed,
            onSelect: (index) => setState(() {
              _selected = index;
              _showingNotifications = false;
            }),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _TopBar(
                  breadcrumb: '${AppStrings.breadcrumbRoot} > $title',
                  title: title,
                  subtitle: subtitle,
                  onToggleSidebar: () => setState(() => _collapsed = !_collapsed),
                  onOpenNotifications: () => setState(() => _showingNotifications = true),
                  onLogout: _isLoggingOut ? null : _logout,
                ),
                Expanded(
                  child: ColoredBox(
                    color: c.canvas,
                    child: _showingNotifications
                        ? const NotificationsScreen()
                        : KeyedSubtree(key: ValueKey(current.label), child: current.builder(context)),
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
  final String breadcrumb;
  final String title;
  final String subtitle;
  final VoidCallback onToggleSidebar;
  final VoidCallback onOpenNotifications;
  final VoidCallback? onLogout;

  const _TopBar({
    required this.breadcrumb,
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
                Text(breadcrumb, style: context.text.bodySmall?.copyWith(color: c.textMuted)),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.baseline,
                  textBaseline: TextBaseline.alphabetic,
                  children: [
                    Text(title, style: context.text.titleLarge),
                    const SizedBox(width: AppSpacing.sm),
                    Flexible(child: Text(subtitle, style: context.text.bodySmall?.copyWith(color: c.textMuted), overflow: TextOverflow.ellipsis)),
                  ],
                ),
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
                onTap: () => showMyProfileDialog(context),
                child: const _MenuRow(icon: AppIcons.person, label: AppStrings.myProfile),
              ),
              PopupMenuItem<void>(
                onTap: () => showChangePasswordDialog(context),
                child: const _MenuRow(icon: Icons.lock_outline_rounded, label: AppStrings.changePassword),
              ),
              const PopupMenuDivider(),
              PopupMenuItem<void>(
                onTap: onLogout,
                child: const _MenuRow(icon: Icons.logout_rounded, label: AppStrings.logout),
              ),
            ],
            child: AppAvatar(name: session.fullName, size: 36),
          ),
        ],
      ),
    );
  }
}

/// A menu-item row whose label can ellipsize instead of overflowing the menu's own width - the
/// menu sizes itself to its widest item, and without `Flexible` here, "Promijeni lozinku" could
/// be wider than the space left near a screen edge.
class _MenuRow extends StatelessWidget {
  final IconData icon;
  final String label;

  const _MenuRow({required this.icon, required this.label});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 18),
        const SizedBox(width: AppSpacing.xs),
        Flexible(child: Text(label, overflow: TextOverflow.ellipsis)),
      ],
    );
  }
}
