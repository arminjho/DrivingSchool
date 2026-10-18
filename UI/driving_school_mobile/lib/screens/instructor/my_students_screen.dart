import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/paged_controller.dart';
import '../../l10n/app_strings.dart';
import '../../models/student_summary.dart';
import '../../providers/instructor_api.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_list_card.dart';
import '../../widgets/ui/paged_list_view.dart';

/// "Moji učenici" (docs/prijava.md §3.3 - no mockup, but required by the text alongside
/// 4.3.1-4.3.5): every student who has at least one lesson with the signed-in instructor.
class MyStudentsScreen extends StatefulWidget {
  const MyStudentsScreen({super.key});

  @override
  State<MyStudentsScreen> createState() => _MyStudentsScreenState();
}

class _MyStudentsScreenState extends State<MyStudentsScreen> {
  late final PagedController<StudentSummary> _controller;

  @override
  void initState() {
    super.initState();
    final api = InstructorApi(context.read<AuthSession>());
    _controller = PagedController<StudentSummary>(fetch: api.getMyStudents, accumulate: true);
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.myStudentsTitle)),
      body: PagedListView<StudentSummary>(
        controller: _controller,
        emptyIcon: Icons.groups_outlined,
        emptyMessage: AppStrings.noStudents,
        itemBuilder: (context, student) => AppListCard(
          leading: AppAvatar(name: student.fullName),
          title: student.fullName,
          subtitle: '${student.vehicleCategoryName} · ${AppStrings.totalLessonsLabel(student.totalLessons)}',
          trailing: Text(student.phoneNumber, style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
        ),
      ),
    );
  }
}
