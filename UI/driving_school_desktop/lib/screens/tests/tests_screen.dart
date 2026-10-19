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
import '../../models/test.dart';
import '../../providers/question_area_provider.dart';
import '../../providers/test_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/form_prerequisites.dart';
import '../../widgets/ui/paged_table.dart';
import 'questions_screen.dart';

/// Admin management for probni testovi - not in the mockup directly, but every entity the
/// student side exposes (Test/Question/AnswerOption) needs somewhere an admin can create it.
class TestsScreen extends StatefulWidget {
  const TestsScreen({super.key});

  @override
  State<TestsScreen> createState() => _TestsScreenState();
}

class _TestsScreenState extends State<TestsScreen> {
  late final TestProvider _provider;
  late final QuestionAreaProvider _areaProvider;
  late final PagedController<Test> _controller;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _provider = TestProvider(session);
    _areaProvider = QuestionAreaProvider(session);
    _controller = PagedController<Test>(fetch: _provider.getPaged, searchParam: 'title');
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openForm({Test? initial}) async {
    if (initial == null && !await requireRecords(context, count: _areaProvider.totalCount, missingMessage: AppStrings.noQuestionAreasPrefix)) return;
    if (!mounted) return;

    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    final title = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.testFormNewTitle : AppStrings.testFormEditTitle,
          icon: Icons.quiz_outlined,
          width: 460,
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
                        'passThresholdPercent': int.parse(form.value['passThresholdPercent'] as String),
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
                  label: AppStrings.fieldTestTitle,
                  initialValue: initial?.title,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(150, AppStrings.fieldTestTitle)]),
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
                  name: 'passThresholdPercent',
                  label: AppStrings.fieldPassThreshold,
                  initialValue: (initial?.passThresholdPercent ?? 70).toString(),
                  keyboardType: TextInputType.number,
                  validator: AppValidators.wholeNumber(min: 1, max: 100),
                ),
              ],
            ),
          ),
        ),
      ),
    );

    if (title == null) return;
    await _controller.reload();
    if (mounted) showSuccessMessage(context, initial == null ? AppStrings.testCreated(title) : AppStrings.testUpdated(title));
  }

  Future<void> _delete(Test test) async {
    await _provider.delete(test.id);
  }

  void _openQuestions(Test test) {
    Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => QuestionsScreen(test: test)));
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<Test>(
      controller: _controller,
      addLabel: AppStrings.testsAddNew,
      onAdd: () => _openForm(),
      onEdit: (test) => _openForm(initial: test),
      onDelete: _delete,
      itemLabel: (test) => test.title,
      deletedMessage: (test) => AppStrings.testDeleted(test.title),
      onRowTap: _openQuestions,
      extraRowActions: (test) => [
        AppRowAction(icon: Icons.list_alt_rounded, tooltip: AppStrings.openQuestions, onPressed: () => _openQuestions(test)),
      ],
      columns: [
        AppColumn<Test>(label: AppStrings.columnTitle, flex: 2, sortKey: 'Title', cell: (context, row) => Text(row.title)),
        AppColumn<Test>(label: AppStrings.columnQuestionArea, flex: 2, cell: (context, row) => Text(row.questionAreaName)),
        AppColumn<Test>(label: AppStrings.columnQuestionCount, width: 120, numeric: true, cell: (context, row) => Text('${row.questionCount}')),
        AppColumn<Test>(label: AppStrings.columnPassThreshold, width: 120, numeric: true, cell: (context, row) => Text('${row.passThresholdPercent}%')),
      ],
    );
  }
}
