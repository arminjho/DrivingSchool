import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/formatters.dart';
import '../../core/paged_controller.dart';
import '../../l10n/app_strings.dart';
import '../../models/lesson.dart';
import '../../models/lesson_request_counts.dart';
import '../../providers/instructor_api.dart';
import '../../providers/lesson_provider.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/lesson_status_badge.dart';
import '../../widgets/ui/paged_list_view.dart';
import 'request_details_screen.dart';

/// 4.3.2 - the four filter chips (Sve/Na čekanju/Odobreni/Odbijeni) with counts, and
/// Odobri/Odbij directly on a Pending card.
class LessonRequestsScreen extends StatefulWidget {
  const LessonRequestsScreen({super.key});

  @override
  State<LessonRequestsScreen> createState() => _LessonRequestsScreenState();
}

class _LessonRequestsScreenState extends State<LessonRequestsScreen> {
  late final InstructorApi _instructorApi;
  late final LessonProvider _lessonProvider;
  late final PagedController<Lesson> _controller;

  LessonRequestCounts? _counts;
  String _status = 'all';

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _instructorApi = InstructorApi(session);
    _lessonProvider = LessonProvider(session);
    _controller = PagedController<Lesson>(
      fetch: (query) => _instructorApi.getLessonRequests(query),
      accumulate: true,
      filters: {'status': _status},
    );
    _controller.load();
    _loadCounts();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _loadCounts() async {
    try {
      final counts = await _instructorApi.getLessonRequestCounts();
      if (mounted) setState(() => _counts = counts);
    } on Object {
      // The chip counts are a convenience, not required for the list itself to work.
    }
  }

  void _selectStatus(String status) {
    if (status == _status) return;
    setState(() => _status = status);
    _controller.setFilter('status', status);
  }

  Future<void> _refresh() async {
    await (_controller.reload(), _loadCounts()).wait;
  }

  Future<void> _approve(Lesson lesson) async {
    try {
      await _lessonProvider.confirm(lesson.id);
      if (!mounted) return;
      showSuccessMessage(context, AppStrings.requestApprovedMessage);
      _refresh();
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    }
  }

  Future<void> _reject(Lesson lesson) async {
    final reasonController = TextEditingController();
    String? reasonError;

    final confirmed = await showAppDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: AppStrings.rejectDialogTitle,
          icon: Icons.cancel_outlined,
          tone: AppTone.danger,
          width: 440,
          actions: [
            OutlinedButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text(AppStrings.cancel)),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: dialogContext.colors.danger, foregroundColor: dialogContext.colors.onPrimary),
              onPressed: () {
                if (reasonController.text.trim().isEmpty) {
                  setDialogState(() => reasonError = AppStrings.required);
                  return;
                }
                Navigator.of(dialogContext).pop(true);
              },
              child: const Text(AppStrings.rejectAction),
            ),
          ],
          child: TextField(
            controller: reasonController,
            maxLines: 2,
            autofocus: true,
            decoration: InputDecoration(labelText: AppStrings.fieldRejectionReason, errorText: reasonError),
            onChanged: (_) => setDialogState(() => reasonError = null),
          ),
        ),
      ),
    );
    if (confirmed != true) return;

    try {
      await _lessonProvider.reject(lesson.id, reason: reasonController.text.trim());
      if (!mounted) return;
      showSuccessMessage(context, AppStrings.requestRejectedMessage);
      _refresh();
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.lessonRequestsTitle)),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(AppSpacing.md, AppSpacing.md, AppSpacing.md, 0),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  _chip(context, value: 'all', label: AppStrings.filterAll, count: _counts?.all),
                  const SizedBox(width: AppSpacing.xs),
                  _chip(context, value: 'pending', label: AppStrings.filterPending, count: _counts?.pending),
                  const SizedBox(width: AppSpacing.xs),
                  _chip(context, value: 'confirmed', label: AppStrings.filterConfirmed, count: _counts?.confirmed),
                  const SizedBox(width: AppSpacing.xs),
                  _chip(context, value: 'cancelled', label: AppStrings.filterCancelled, count: _counts?.cancelled),
                ],
              ),
            ),
          ),
          Expanded(
            child: PagedListView<Lesson>(
              controller: _controller,
              showSearch: false,
              emptyIcon: Icons.event_note_outlined,
              emptyMessage: AppStrings.noLessonRequests,
              itemBuilder: (context, lesson) => _requestCard(context, lesson),
            ),
          ),
        ],
      ),
    );
  }

  Widget _chip(BuildContext context, {required String value, required String label, int? count}) {
    final selected = _status == value;
    return ChoiceChip(
      label: Text(count == null ? label : '$label ($count)'),
      selected: selected,
      onSelected: (_) => _selectStatus(value),
    );
  }

  Widget _requestCard(BuildContext context, Lesson lesson) {
    final c = context.colors;
    final isPending = lesson.status == 'Pending';

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(child: Text(lesson.studentFullName, style: context.text.titleMedium)),
              LessonStatusBadge(status: lesson.status),
            ],
          ),
          const SizedBox(height: AppSpacing.xs),
          Row(children: [Icon(AppIcons.date, size: 14, color: c.textMuted), const SizedBox(width: AppSpacing.xs), Text(AppFormat.dateTime(lesson.startsAtUtc), style: context.text.bodySmall)]),
          const SizedBox(height: 2),
          Row(children: [Icon(AppIcons.place, size: 14, color: c.textMuted), const SizedBox(width: AppSpacing.xs), Text(lesson.drivingZoneName, style: context.text.bodySmall)]),
          if (isPending && lesson.note != null) ...[
            const SizedBox(height: AppSpacing.sm),
            Text('"${lesson.note}"', style: context.text.bodySmall?.copyWith(color: c.textSecondary, fontStyle: FontStyle.italic)),
          ],
          const SizedBox(height: AppSpacing.sm),
          if (isPending)
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () => _reject(lesson),
                    icon: const Icon(Icons.close_rounded, size: 16),
                    label: const Text(AppStrings.rejectAction),
                    style: OutlinedButton.styleFrom(foregroundColor: c.danger, side: BorderSide(color: c.danger)),
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: FilledButton.icon(
                    onPressed: () => _approve(lesson),
                    icon: const Icon(Icons.check_rounded, size: 16),
                    label: const Text(AppStrings.approveAction),
                    style: FilledButton.styleFrom(backgroundColor: c.success),
                  ),
                ),
              ],
            )
          else
            Align(
              alignment: Alignment.centerRight,
              child: TextButton(
                onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => RequestDetailsScreen(lessonId: lesson.id))).then((_) => _refresh()),
                child: const Text(AppStrings.viewDetails),
              ),
            ),
        ],
      ),
    );
  }
}
