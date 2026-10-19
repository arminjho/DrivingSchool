import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/paged_controller.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/question_area.dart';
import '../../models/study_lesson.dart';
import '../../providers/question_area_provider.dart';
import '../../providers/study_lesson_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/form_prerequisites.dart';
import '../../widgets/ui/paged_table.dart';
import 'sections_screen.dart';

/// Admin management for teorijske lekcije (čitanje/skripte) - not in the mockup directly, but
/// StudyLesson/StudyLessonSection need somewhere an admin can create and edit them.
class StudyLessonsScreen extends StatefulWidget {
  const StudyLessonsScreen({super.key});

  @override
  State<StudyLessonsScreen> createState() => _StudyLessonsScreenState();
}

class _StudyLessonsScreenState extends State<StudyLessonsScreen> {
  late final StudyLessonProvider _provider;
  late final QuestionAreaProvider _areaProvider;
  late final PagedController<StudyLesson> _controller;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _provider = StudyLessonProvider(session);
    _areaProvider = QuestionAreaProvider(session);
    _controller = PagedController<StudyLesson>(fetch: _provider.getPaged, searchParam: 'title');
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openForm({StudyLesson? initial}) async {
    if (initial == null && !await requireRecords(context, count: _areaProvider.totalCount, missingMessage: AppStrings.noQuestionAreasPrefix)) return;
    if (!mounted) return;

    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    final title = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.studyLessonFormNewTitle : AppStrings.studyLessonFormEditTitle,
          icon: Icons.menu_book_outlined,
          width: 520,
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
                        'title': form.value['title'],
                        'questionAreaId': form.value['questionAreaId'],
                        'tags': form.value['tags'],
                        'validForYear': int.parse(form.value['validForYear'] as String),
                        'readingMinutes': int.parse(form.value['readingMinutes'] as String),
                        'chapterLabel': form.value['chapterLabel'],
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
                  label: AppStrings.fieldLessonTitle,
                  initialValue: initial?.title,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(150, AppStrings.fieldLessonTitle)]),
                ),
                const SizedBox(height: AppSpacing.md),
                ApiDropdownField<QuestionArea>(
                  name: 'questionAreaId',
                  label: AppStrings.fieldQuestionAreaId,
                  icon: Icons.quiz_outlined,
                  initialValue: initial?.questionAreaId,
                  loadItems: () async => (await _areaProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (a) => a.id,
                  labelOf: (a) => a.name,
                  validator: AppValidators.requiredSelection<int>(),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'chapterLabel',
                  label: AppStrings.fieldChapterLabel,
                  hint: AppStrings.chapterLabelHint,
                  initialValue: initial?.chapterLabel,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(50, AppStrings.fieldChapterLabel)]),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'tags',
                  label: AppStrings.fieldTags,
                  initialValue: initial?.tags,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(200, AppStrings.fieldTags)]),
                ),
                const SizedBox(height: AppSpacing.md),
                Row(
                  children: [
                    Expanded(
                      child: AppTextField(
                        name: 'validForYear',
                        label: AppStrings.fieldValidForYear,
                        icon: AppIcons.date,
                        initialValue: (initial?.validForYear ?? DateTime.now().year).toString(),
                        keyboardType: TextInputType.number,
                        validator: AppValidators.wholeNumber(min: 2000, max: DateTime.now().year + 1),
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: AppTextField(
                        name: 'readingMinutes',
                        label: AppStrings.fieldReadingMinutes,
                        icon: AppIcons.time,
                        initialValue: initial?.readingMinutes.toString(),
                        keyboardType: TextInputType.number,
                        validator: AppValidators.wholeNumber(min: 1, max: 180),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );

    if (title == null) return;
    await _controller.reload();
    if (mounted) showSuccessMessage(context, initial == null ? AppStrings.studyLessonCreated(title) : AppStrings.studyLessonUpdated(title));
  }

  Future<void> _delete(StudyLesson lesson) async {
    await _provider.delete(lesson.id);
  }

  void _openSections(StudyLesson lesson) {
    Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => SectionsScreen(lesson: lesson)));
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<StudyLesson>(
      controller: _controller,
      addLabel: AppStrings.studyLessonsAddNew,
      onAdd: () => _openForm(),
      onEdit: (lesson) => _openForm(initial: lesson),
      onDelete: _delete,
      itemLabel: (lesson) => lesson.title,
      deletedMessage: (lesson) => AppStrings.studyLessonDeleted(lesson.title),
      onRowTap: _openSections,
      extraRowActions: (lesson) => [
        AppRowAction(icon: Icons.list_alt_rounded, tooltip: AppStrings.openSections, onPressed: () => _openSections(lesson)),
      ],
      columns: [
        AppColumn<StudyLesson>(label: AppStrings.fieldChapterLabel, width: 130, cell: (context, row) => Text(row.chapterLabel)),
        AppColumn<StudyLesson>(label: AppStrings.fieldLessonTitle, flex: 2, sortKey: 'Title', cell: (context, row) => Text(row.title)),
        AppColumn<StudyLesson>(label: AppStrings.columnQuestionArea, flex: 2, cell: (context, row) => Text(row.questionAreaName)),
        AppColumn<StudyLesson>(label: AppStrings.columnReadingMinutes, width: 110, numeric: true, cell: (context, row) => Text('${row.readingMinutes}')),
        AppColumn<StudyLesson>(label: AppStrings.columnSectionCount, width: 110, numeric: true, cell: (context, row) => Text('${row.sectionCount}')),
      ],
    );
  }
}
