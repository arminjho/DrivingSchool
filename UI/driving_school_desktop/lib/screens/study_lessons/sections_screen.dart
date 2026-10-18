import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/paged_controller.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/study_lesson.dart';
import '../../models/study_lesson_section.dart';
import '../../providers/study_lesson_section_provider.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/paged_table.dart';

/// Sekcije jedne teorijske lekcije - "Details" strana master-details forme (master je
/// [StudyLessonsScreen]).
class SectionsScreen extends StatefulWidget {
  const SectionsScreen({super.key, required this.lesson});

  final StudyLesson lesson;

  @override
  State<SectionsScreen> createState() => _SectionsScreenState();
}

class _SectionsScreenState extends State<SectionsScreen> {
  late final StudyLessonSectionProvider _provider;
  late final PagedController<StudyLessonSection> _controller;

  @override
  void initState() {
    super.initState();
    _provider = StudyLessonSectionProvider(context.read<AuthSession>());
    _controller = PagedController<StudyLessonSection>(
      fetch: _provider.getPaged,
      filters: {'studyLessonId': widget.lesson.id},
      sortBy: 'SortOrder',
    );
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openForm({StudyLessonSection? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    final name = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.sectionFormNewTitle : AppStrings.sectionFormEditTitle,
          icon: Icons.article_outlined,
          width: 560,
          actions: [
            OutlinedButton(onPressed: isSubmitting ? null : () => Navigator.of(dialogContext).pop(), child: const Text(AppStrings.cancel)),
            FilledButton(
              onPressed: isSubmitting
                  ? null
                  : () async {
                      final form = formKey.currentState;
                      if (form == null || !form.saveAndValidate()) return;
                      setDialogState(() {
                        isSubmitting = true;
                        banner = null;
                      });
                      final request = {
                        'studyLessonId': widget.lesson.id,
                        'title': form.value['title'],
                        'content': form.value['content'],
                        'sortOrder': int.parse(form.value['sortOrder'] as String),
                      };
                      try {
                        if (initial == null) {
                          await _provider.insert(request);
                        } else {
                          await _provider.update(initial.id, request);
                        }
                        if (dialogContext.mounted) Navigator.of(dialogContext).pop(request['title'] as String);
                      } on Object catch (error) {
                        setDialogState(() {
                          banner = applyApiErrors(formKey, error);
                          isSubmitting = false;
                        });
                      }
                    },
              child: isSubmitting
                  ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text(AppStrings.save),
            ),
          ],
          child: FormBuilder(
            key: formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (banner != null) ...[AppNotice(message: banner!), const SizedBox(height: AppSpacing.md)],
                AppTextField(
                  name: 'title',
                  label: AppStrings.fieldSectionTitle,
                  initialValue: initial?.title,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(150, AppStrings.fieldSectionTitle)]),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'content',
                  label: AppStrings.fieldContent,
                  initialValue: initial?.content,
                  maxLines: 8,
                  validator: AppValidators.required<String>(),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'sortOrder',
                  label: AppStrings.fieldSortOrder,
                  initialValue: (initial?.sortOrder ?? 1).toString(),
                  keyboardType: TextInputType.number,
                  validator: AppValidators.wholeNumber(min: 1),
                ),
              ],
            ),
          ),
        ),
      ),
    );

    if (name == null) return;
    await _controller.reload();
    if (mounted) showSuccessMessage(context, initial == null ? AppStrings.sectionCreated : AppStrings.sectionUpdated);
  }

  Future<void> _delete(StudyLessonSection section) async {
    await _provider.delete(section.id);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.sectionsTitle)),
      body: Padding(
        padding: AppSpacing.page,
        child: PagedTable<StudyLessonSection>(
          controller: _controller,
          title: AppStrings.sectionsTitle,
          subtitle: AppStrings.sectionsSubtitle(widget.lesson.title),
          showSearch: false,
          addLabel: AppStrings.sectionsAddNew,
          onAdd: () => _openForm(),
          onEdit: (section) => _openForm(initial: section),
          onDelete: _delete,
          itemLabel: (section) => section.title,
          deletedMessage: (_) => AppStrings.sectionDeleted,
          columns: [
            AppColumn<StudyLessonSection>(label: AppStrings.fieldSortOrder, width: 90, numeric: true, sortKey: 'SortOrder', cell: (context, row) => Text('${row.sortOrder}')),
            AppColumn<StudyLessonSection>(label: AppStrings.fieldSectionTitle, flex: 2, cell: (context, row) => Text(row.title)),
            AppColumn<StudyLessonSection>(label: AppStrings.fieldContent, flex: 3, cell: (context, row) => Text(row.content, overflow: TextOverflow.ellipsis)),
          ],
        ),
      ),
    );
  }
}
