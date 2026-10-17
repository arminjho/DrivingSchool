import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../core/auth_api.dart';
import '../core/auth_session.dart';
import '../core/design_tokens.dart';
import '../core/roles.dart';
import '../l10n/app_strings.dart';
import '../screens/home_screen.dart';
import '../screens/notifications/notifications_screen.dart';
import '../widgets/notifications_bell.dart';
import '../widgets/ui/app_badge.dart';

/// One place the user can go: what the bottom bar shows, what the app bar says, what the body
/// builds. A screen is added to the app by adding an entry here. [roles] limits a destination to
/// the roles it applies to - Učenik and Instruktor share this one shell, picking a different set
/// of destinations after login based on the JWT's role claim.
class _Destination {
  final IconData icon;
  final IconData selectedIcon;
  final String label;
  final String subtitle;
  final WidgetBuilder builder;
  final Set<String> roles;

  const _Destination({
    required this.icon,
    required this.selectedIcon,
    required this.label,
    required this.subtitle,
    required this.builder,
    required this.roles,
  });
}

/// The signed-in layout: an app bar with the notification bell and the user menu, a bottom
/// navigation bar, and the content area. Never navigates to login itself - clearing the
/// `AuthSession` (logout, a 401, token expiry) is enough.
class AppShell extends StatefulWidget {
  const AppShell({super.key});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  static const List<_Destination> _allDestinations = [
    _Destination(
      icon: Icons.home_outlined,
      selectedIcon: Icons.home_rounded,
      label: AppStrings.homeTitle,
      subtitle: AppStrings.homeSubtitle,
      builder: _homeBuilder,
      roles: {Roles.student, Roles.instructor},
    ),
    _Destination(
      icon: Icons.notifications_outlined,
      selectedIcon: Icons.notifications_rounded,
      label: AppStrings.notificationsTitle,
      subtitle: AppStrings.notificationsSubtitle,
      builder: _notificationsBuilder,
      roles: {Roles.student, Roles.instructor},
    ),
  ];

  static Widget _homeBuilder(BuildContext context) => const HomeScreen();
  static Widget _notificationsBuilder(BuildContext context) => const NotificationsScreen();

  int _selected = 0;
  bool _isLoggingOut = false;

  Future<void> _logout() async {
    setState(() => _isLoggingOut = true);
    await context.read<AuthApi>().logout();
  }

  @override
  Widget build(BuildContext context) {
    final session = context.watch<AuthSession>();
    final destinations = _allDestinations.where((d) => d.roles.contains(session.role)).toList();
    final selected = _selected.clamp(0, destinations.length - 1);
    final current = destinations[selected];

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(current.label, style: context.text.titleLarge),
            Text(current.subtitle, style: context.text.bodySmall, overflow: TextOverflow.ellipsis),
          ],
        ),
        actions: [
          NotificationsBell(
            onPressed: () => setState(() {
              final index = destinations.indexWhere((d) => d.label == AppStrings.notificationsTitle);
              if (index != -1) _selected = index;
            }),
          ),
          PopupMenuButton<void>(
            tooltip: session.fullName,
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
                onTap: _isLoggingOut ? null : _logout,
                child: const Row(children: [Icon(Icons.logout_rounded, size: 18), SizedBox(width: AppSpacing.xs), Text(AppStrings.logout)]),
              ),
            ],
            child: Padding(
              padding: const EdgeInsets.only(right: AppSpacing.md, left: AppSpacing.xs),
              child: AppAvatar(name: session.fullName, size: 32),
            ),
          ),
        ],
      ),
      body: KeyedSubtree(key: ValueKey(current.label), child: current.builder(context)),
      bottomNavigationBar: destinations.length < 2
          ? null
          : NavigationBar(
              selectedIndex: selected,
              onDestinationSelected: (index) => setState(() => _selected = index),
              destinations: [
                for (final destination in destinations)
                  NavigationDestination(
                    icon: Icon(destination.icon),
                    selectedIcon: Icon(destination.selectedIcon),
                    label: destination.label,
                  ),
              ],
            ),
    );
  }
}
