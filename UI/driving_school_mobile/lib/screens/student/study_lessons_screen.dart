import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/paged_controller.dart';
import '../../l10n/app_strings.dart';
import '../../layouts/shell_scope.dart';
import '../../models/student_study_lesson.dart';
import '../../models/study_lesson.dart';
import '../../providers/study_lesson_provider.dart';
import '../../widgets/ui/app_list_card.dart';
import '../../widgets/ui/paged_list_view.dart';
import 'study_lesson_detail_screen.dart';

/// 4.2.9 - "Sve lekcije" / "Moje lekcije" tabs.
class StudyLessonsScreen extends StatefulWidget {
  const StudyLessonsScreen({super.key});

  @override
  State<StudyLessonsScreen> createState() => _StudyLessonsScreenState();
}

class _StudyLessonsScreenState extends State<StudyLessonsScreen> with SingleTickerProviderStateMixin {
  late final StudyLessonProvider _provider;
  late final TabController _tabController;
  late final PagedController<StudyLesson> _allController;
  late final PagedController<StudentStudyLesson> _myController;

  @override
  void initState() {
    super.initState();
    _provider = StudyLessonProvider(context.read<AuthSession>());
    _tabController = TabController(length: 2, vsync: this);
    _allController = PagedController<StudyLesson>(fetch: _provider.getPaged, searchParam: 'title');
    _myController = PagedController<StudentStudyLesson>(fetch: _provider.getMy);
    _allController.load();
    _myController.load();
  }

  @override
  void dispose() {
    _tabController.dispose();
    _allController.dispose();
    _myController.dispose();
    super.dispose();
  }

  void _openLesson(int id) {
    Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => StudyLessonDetailScreen(studyLessonId: id)));
  }

  @override
  Widget build(BuildContext context) {
    final inShell = ShellScope.isInside(context);
    final tabBar = TabBar(controller: _tabController, tabs: const [Tab(text: AppStrings.tabAllLessons), Tab(text: AppStrings.tabMyLessons)]);

    return Scaffold(
      appBar: inShell ? null : AppBar(title: const Text(AppStrings.studyTitle), bottom: tabBar),
      body: Column(
        children: [
          if (inShell) tabBar,
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                PagedListView<StudyLesson>(
                  controller: _allController,
                  emptyIcon: Icons.menu_book_outlined,
                  emptyMessage: AppStrings.noStudyLessons,
                  itemBuilder: (context, lesson) => AppListCard(
                    leading: const Icon(Icons.menu_book_outlined),
                    title: lesson.title,
                    subtitle: '${lesson.chapterLabel} · ${AppStrings.subLessonCount(lesson.sectionCount)}',
                    trailing: const Icon(Icons.chevron_right_rounded),
                    onTap: () => _openLesson(lesson.id),
                  ),
                ),
                PagedListView<StudentStudyLesson>(
                  controller: _myController,
                  showSearch: false,
                  emptyIcon: Icons.menu_book_outlined,
                  emptyMessage: AppStrings.noMyStudyLessons,
                  itemBuilder: (context, lesson) => AppListCard(
                    leading: const Icon(Icons.menu_book_outlined),
                    title: lesson.title,
                    subtitle: '${lesson.chapterLabel} · ${AppStrings.subLessonCount(lesson.sectionCount)} · ${lesson.progressPercent}%',
                    trailing: const Icon(Icons.chevron_right_rounded),
                    onTap: () => _openLesson(lesson.studyLessonId),
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
