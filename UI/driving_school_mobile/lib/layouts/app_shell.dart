import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../core/auth_api.dart';
import '../core/auth_session.dart';
import '../core/design_tokens.dart';
import '../core/roles.dart';
import '../l10n/app_strings.dart';
import '../layouts/shell_scope.dart';
import '../screens/instructor/instructor_home_screen.dart';
import '../screens/instructor/my_students_screen.dart';
import '../screens/instructor/schedule_screen.dart';
import '../screens/notifications/notifications_screen.dart';
import '../screens/profile/profile_screen.dart';
import '../screens/student/my_lessons_screen.dart';
import '../screens/student/student_home_screen.dart';
import '../screens/student/study_lessons_screen.dart';
import '../screens/student/tests_screen.dart';
import '../widgets/notifications_bell.dart';
import '../widgets/ui/app_badge.dart';

/// One place the user can go: what the bottom bar shows, what the app bar says, what the body
/// builds. A screen is added to the app by adding an entry here. A home destination sets
/// [showsBrand]: its header is the car icon and the app name (4.2.1 / 4.3.1) instead of a title
/// and subtitle. [roles] limits a destination to
/// the roles it applies to - Učenik and Instruktor share this one shell, picking a different set
/// of destinations after login based on the JWT's role claim. Notifications are reached only
/// through the bell (neither role's bottom nav has an "Obavijesti" icon), so they are a separate
/// overlay flag rather than one more entry here.
class _Destination {
  final IconData icon;
  final IconData selectedIcon;
  final String label;
  final String? title;
  final String? subtitle;
  final bool showsBrand;
  final WidgetBuilder builder;
  final Set<String> roles;

  const _Destination({
    required this.icon,
    required this.selectedIcon,
    required this.label,
    this.title,
    this.subtitle,
    this.showsBrand = false,
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
  static final List<_Destination> _allDestinations = [
    // --- Student (4.2.1) ---
    _Destination(
      icon: Icons.home_outlined,
      selectedIcon: Icons.home_rounded,
      label: AppStrings.navHome,
      showsBrand: true,
      builder: (_) => const StudentHomeScreen(),
      roles: {Roles.student},
    ),
    _Destination(
      icon: Icons.event_note_outlined,
      selectedIcon: Icons.event_note_rounded,
      label: AppStrings.navLessons,
      title: AppStrings.myLessonsTitle,
      builder: (_) => const MyLessonsScreen(),
      roles: {Roles.student},
    ),
    _Destination(
      icon: Icons.quiz_outlined,
      selectedIcon: Icons.quiz_rounded,
      label: AppStrings.navTests,
      title: AppStrings.testsTitle,
      subtitle: AppStrings.testsSubtitle,
      builder: (_) => const TestsScreen(),
      roles: {Roles.student},
    ),
    _Destination(
      icon: Icons.menu_book_outlined,
      selectedIcon: Icons.menu_book_rounded,
      label: AppStrings.navStudy,
      title: AppStrings.studyTitle,
      builder: (_) => const StudyLessonsScreen(),
      roles: {Roles.student},
    ),
    _Destination(
      icon: Icons.person_outline_rounded,
      selectedIcon: Icons.person_rounded,
      label: AppStrings.navProfile,
      title: AppStrings.myProfile,
      builder: (_) => const ProfileScreen(),
      roles: {Roles.student},
    ),
    // --- Instructor (4.3.1) --- 4 destinations, not 5: no "Obavijesti" item here either
    // (same bell-overlay pattern), and the mockup's bottom nav is explicitly 4 icons.
    _Destination(
      icon: Icons.home_outlined,
      selectedIcon: Icons.home_rounded,
      label: AppStrings.navHome,
      showsBrand: true,
      builder: (_) => const InstructorHomeScreen(),
      roles: {Roles.instructor},
    ),
    _Destination(
      icon: Icons.calendar_month_outlined,
      selectedIcon: Icons.calendar_month_rounded,
      label: AppStrings.navSchedule,
      title: AppStrings.scheduleTitle,
      builder: (_) => const ScheduleScreen(),
      roles: {Roles.instructor},
    ),
    _Destination(
      icon: Icons.groups_outlined,
      selectedIcon: Icons.groups_rounded,
      label: AppStrings.navStudents,
      title: AppStrings.myStudentsTitle,
      builder: (_) => const MyStudentsScreen(),
      roles: {Roles.instructor},
    ),
    _Destination(
      icon: Icons.person_outline_rounded,
      selectedIcon: Icons.person_rounded,
      label: AppStrings.navProfile,
      title: AppStrings.myProfile,
      builder: (_) => const ProfileScreen(showCategory: false),
      roles: {Roles.instructor},
    ),
  ];

  int _selected = 0;
  bool _showingNotifications = false;
  bool _isLoggingOut = false;

  Future<void> _logout() async {
    setState(() => _isLoggingOut = true);
    await context.read<AuthApi>().logout();
  }

  @override
  Widget build(BuildContext context) {
    final session = context.watch<AuthSession>();
    final destinations = _allDestinations.where((d) => d.roles.contains(session.role)).toList();

    if (destinations.isEmpty) {
      // Reachable only mid-rollout, between a role being authorized server-side and this app
      // having that role's screens built - never in the finished app.
      return const Scaffold(body: Center(child: Text(AppStrings.emptyMessage)));
    }

    final selected = _selected.clamp(0, destinations.length - 1);
    final current = destinations[selected];
    final showsBrand = !_showingNotifications && current.showsBrand;
    final title = _showingNotifications ? AppStrings.notificationsTitle : current.title ?? current.label;
    final subtitle = _showingNotifications ? AppStrings.notificationsSubtitle : current.subtitle;

    return Scaffold(
      appBar: AppBar(
        title: showsBrand
            ? Row(
                children: [
                  Icon(AppIcons.vehicle, color: context.colors.primary),
                  const SizedBox(width: AppSpacing.xs),
                  Text(AppStrings.appName, style: context.text.titleLarge),
                ],
              )
            : Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(title, style: context.text.titleLarge),
                  if (subtitle != null) Text(subtitle, style: context.text.bodySmall, overflow: TextOverflow.ellipsis),
                ],
              ),
        actions: [
          NotificationsBell(onPressed: () => setState(() => _showingNotifications = true)),
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
      body: _showingNotifications
          ? const NotificationsScreen()
          : ShellScope(child: KeyedSubtree(key: ValueKey(current.label), child: current.builder(context))),
      bottomNavigationBar: destinations.length < 2
          ? null
          : NavigationBar(
              selectedIndex: selected,
              onDestinationSelected: (index) => setState(() {
                _selected = index;
                _showingNotifications = false;
              }),
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
