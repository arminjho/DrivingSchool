import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/formatters.dart';
import '../../core/paged_controller.dart';
import '../../l10n/app_strings.dart';
import '../../models/instructor.dart';
import '../../models/lesson.dart';
import '../../providers/instructor_provider.dart';
import '../../providers/lesson_admin_provider.dart';
import '../../widgets/ui/api_filter_dropdown.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/lesson_status_badge.dart';
import '../../widgets/ui/paged_table.dart';

/// Admin "sve časove" overview - not in the mockup directly, but every lesson needs to be
/// visible and cancellable from somewhere other than the per-role mobile screens.
class LessonsOverviewScreen extends StatefulWidget {
  const LessonsOverviewScreen({super.key});

  @override
  State<LessonsOverviewScreen> createState() => _LessonsOverviewScreenState();
}

class _LessonsOverviewScreenState extends State<LessonsOverviewScreen> {
  late final LessonAdminProvider _provider;
  late final InstructorProvider _instructorProvider;
  late final PagedController<Lesson> _controller;

  String? _statusFilter;
  int? _instructorFilter;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _provider = LessonAdminProvider(session);
    _instructorProvider = InstructorProvider(session);
    _controller = PagedController<Lesson>(fetch: _provider.getPaged);
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  bool _canCancel(Lesson lesson) => lesson.status == 'Pending' || lesson.status == 'Confirmed';

  Future<void> _cancel(Lesson lesson) async {
    final requiresReason = lesson.status == 'Confirmed';
    final reasonController = TextEditingController();
    var reasonError = requiresReason ? AppStrings.required : null;

    final confirmed = await showAppDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: AppStrings.cancelLessonTitle,
          subtitle: '${lesson.studentFullName} · ${AppFormat.dateTime(lesson.startsAtUtc)}',
          icon: Icons.cancel_outlined,
          tone: AppTone.danger,
          width: 460,
          actions: [
            OutlinedButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text(AppStrings.cancel)),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: dialogContext.colors.danger, foregroundColor: dialogContext.colors.onPrimary),
              onPressed: () {
                if (requiresReason && reasonController.text.trim().isEmpty) {
                  setDialogState(() => reasonError = AppStrings.required);
                  return;
                }
                Navigator.of(dialogContext).pop(true);
              },
              child: const Text(AppStrings.cancelLessonAction),
            ),
          ],
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(AppStrings.cancelLessonReasonHint, style: dialogContext.text.bodySmall?.copyWith(color: dialogContext.colors.textMuted)),
              const SizedBox(height: AppSpacing.sm),
              TextField(
                controller: reasonController,
                maxLines: 3,
                decoration: InputDecoration(labelText: AppStrings.cancelLessonReasonLabel, errorText: reasonError),
                onChanged: (_) => setDialogState(() => reasonError = null),
              ),
            ],
          ),
        ),
      ),
    );

    if (confirmed != true) return;
    try {
      await _provider.cancel(lesson.id, reason: reasonController.text.trim().isEmpty ? null : reasonController.text.trim());
      await _controller.reload();
      if (mounted) showSuccessMessage(context, AppStrings.lessonCancelledMessage);
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    }
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<Lesson>(
      controller: _controller,
      showSearch: false,
      extraActions: [
        SizedBox(
          width: 160,
          height: AppSizes.controlHeight,
          child: DropdownButtonFormField<String?>(
            initialValue: _statusFilter,
            isDense: true,
            decoration: const InputDecoration(contentPadding: EdgeInsets.symmetric(horizontal: AppSpacing.sm)),
            hint: const Text(AppStrings.lessonsAllStatuses),
            items: [
              const DropdownMenuItem<String?>(value: null, child: Text(AppStrings.lessonsAllStatuses)),
              DropdownMenuItem<String?>(value: 'Pending', child: Text(AppStrings.lessonPending)),
              DropdownMenuItem<String?>(value: 'Confirmed', child: Text(AppStrings.lessonConfirmed)),
              DropdownMenuItem<String?>(value: 'Completed', child: Text(AppStrings.lessonCompleted)),
              DropdownMenuItem<String?>(value: 'Cancelled', child: Text(AppStrings.lessonCancelled)),
            ],
            onChanged: (value) {
              setState(() => _statusFilter = value);
              _controller.setFilter('status', value);
            },
          ),
        ),
        const SizedBox(width: AppSpacing.xs),
        ApiFilterDropdown<Instructor>(
          allLabel: AppStrings.allInstructors,
          loadItems: () async => (await _instructorProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
          idOf: (i) => i.id,
          labelOf: (i) => i.fullName,
          value: _instructorFilter,
          onChanged: (id) {
            setState(() => _instructorFilter = id);
            _controller.setFilter('instructorId', id);
          },
        ),
      ],
      itemLabel: (lesson) => lesson.studentFullName,
      extraRowActions: (lesson) => [
        AppRowAction(
          icon: Icons.cancel_outlined,
          tooltip: _canCancel(lesson) ? AppStrings.cancelLessonAction : (lesson.status == 'Completed' ? AppStrings.cannotCancelCompleted : AppStrings.cannotCancelAlready),
          destructive: true,
          onPressed: _canCancel(lesson) ? () => _cancel(lesson) : null,
        ),
      ],
      columns: [
        AppColumn<Lesson>(label: AppStrings.columnDateTime, width: 150, sortKey: 'StartsAtUtc', cell: (context, row) => Text(AppFormat.dateTime(row.startsAtUtc))),
        AppColumn<Lesson>(label: AppStrings.columnStudent, flex: 2, cell: (context, row) => Text(row.studentFullName, overflow: TextOverflow.ellipsis)),
        AppColumn<Lesson>(label: AppStrings.fieldInstructor, flex: 2, cell: (context, row) => Text(row.instructorFullName, overflow: TextOverflow.ellipsis)),
        AppColumn<Lesson>(label: AppStrings.columnZone, flex: 2, cell: (context, row) => Text(row.drivingZoneName, overflow: TextOverflow.ellipsis)),
        AppColumn<Lesson>(label: AppStrings.usersColumnStatus, width: 120, cell: (context, row) => LessonStatusBadge(status: row.status)),
      ],
    );
  }
}
