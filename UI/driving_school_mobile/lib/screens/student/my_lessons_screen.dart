import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/formatters.dart';
import '../../core/paged_controller.dart';
import '../../l10n/app_strings.dart';
import '../../layouts/shell_scope.dart';
import '../../models/lesson.dart';
import '../../providers/lesson_provider.dart';
import '../../widgets/ui/app_list_card.dart';
import '../../widgets/ui/lesson_status_badge.dart';
import '../../widgets/ui/paged_list_view.dart';
import 'lesson_details_screen.dart';

/// 4.2.3 - three tabs, each the student's own lessons in that bucket (Pending+Confirmed /
/// Completed / Cancelled - matches the backend's `MyLessonsSearchObject.Tab`).
class MyLessonsScreen extends StatefulWidget {
  const MyLessonsScreen({super.key});

  @override
  State<MyLessonsScreen> createState() => _MyLessonsScreenState();
}

class _MyLessonsScreenState extends State<MyLessonsScreen> with SingleTickerProviderStateMixin {
  late final LessonProvider _provider;
  late final TabController _tabController;
  late final List<PagedController<Lesson>> _controllers;

  @override
  void initState() {
    super.initState();
    _provider = LessonProvider(context.read<AuthSession>());
    _tabController = TabController(length: 3, vsync: this);
    _controllers = [
      for (final tab in const ['active', 'completed', 'cancelled'])
        PagedController<Lesson>(fetch: _provider.getMy, filters: {'tab': tab}),
    ];
    for (final controller in _controllers) {
      controller.load();
    }
  }

  @override
  void dispose() {
    _tabController.dispose();
    for (final controller in _controllers) {
      controller.dispose();
    }
    super.dispose();
  }

  Future<void> _openDetails(Lesson lesson) async {
    await Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => LessonDetailsScreen(lessonId: lesson.id)));
    for (final controller in _controllers) {
      controller.reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    final inShell = ShellScope.isInside(context);
    final tabBar = TabBar(
      controller: _tabController,
      tabs: const [Tab(text: AppStrings.tabActive), Tab(text: AppStrings.tabCompleted), Tab(text: AppStrings.tabCancelled)],
    );

    return Scaffold(
      appBar: inShell ? null : AppBar(title: const Text(AppStrings.myLessonsTitle), bottom: tabBar),
      body: Column(
        children: [
          if (inShell) tabBar,
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                _tabView(_controllers[0], AppStrings.noActiveLessons),
                _tabView(_controllers[1], AppStrings.noCompletedLessons),
                _tabView(_controllers[2], AppStrings.noCancelledLessons),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _tabView(PagedController<Lesson> controller, String emptyMessage) {
    return PagedListView<Lesson>(
      controller: controller,
      showSearch: false,
      emptyIcon: Icons.event_note_outlined,
      emptyMessage: emptyMessage,
      itemBuilder: (context, lesson) => AppListCard(
        leading: Icon(AppIcons.date, color: context.colors.textMuted),
        title: AppFormat.dateTimeRange(lesson.startsAtUtc, lesson.endsAtUtc),
        subtitle: '${lesson.instructorFullName} · ${lesson.drivingZoneName} · ${lesson.vehicleName ?? AppStrings.noVehicleAssignedYet}',
        trailing: LessonStatusBadge(status: lesson.status),
        footer: OutlinedButton(onPressed: () => _openDetails(lesson), child: const Text(AppStrings.viewDetails)),
        onTap: () => _openDetails(lesson),
      ),
    );
  }
}
