import 'package:material_ui/material_ui.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/image_validation.dart';
import '../../core/paged_controller.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/question.dart';
import '../../models/question_area.dart';
import '../../models/test.dart';
import '../../providers/files_api.dart';
import '../../providers/question_area_provider.dart';
import '../../providers/question_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/image_picker_field.dart';
import '../../widgets/ui/paged_table.dart';

/// Pitanja za jedan test - "Details" strane master-details forme na admin strani (mastering
/// strana, probni test, je u [TestsScreen]).
class QuestionsScreen extends StatefulWidget {
  const QuestionsScreen({super.key, required this.test});

  final Test test;

  @override
  State<QuestionsScreen> createState() => _QuestionsScreenState();
}

class _QuestionsScreenState extends State<QuestionsScreen> {
  late final QuestionProvider _provider;
  late final QuestionAreaProvider _areaProvider;
  late final FilesApi _filesApi;
  late final PagedController<Question> _controller;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _provider = QuestionProvider(session);
    _areaProvider = QuestionAreaProvider(session);
    _filesApi = FilesApi(session);
    _controller = PagedController<Question>(fetch: _provider.getPaged, searchParam: 'text', filters: {'testId': widget.test.id});
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<PickedImage?> _pickImage() async {
    final files = await FilePicker.pickFiles(type: FileType.image);
    if (files.isEmpty) return null;
    final file = files.first;
    return PickedImage(name: file.name, bytes: await file.readAsBytes());
  }

  Future<void> _openForm({Question? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    var correctIndex = initial?.answerOptions.indexWhere((o) => o.isCorrect) ?? 0;
    if (correctIndex < 0) correctIndex = 0;
    String? banner;

    final ok = await showAppDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.questionFormNewTitle : AppStrings.questionFormEditTitle,
          icon: Icons.help_outline_rounded,
          width: 560,
          actions: [
            OutlinedButton(onPressed: isSubmitting ? null : () => Navigator.of(dialogContext).pop(), child: const Text(AppStrings.cancel)),
            FilledButton(
              onPressed: isSubmitting
                  ? null
                  : () async {
                      final form = formKey.currentState;
                      if (form == null || !form.saveAndValidate()) return;

                      final optionTexts = [for (var i = 0; i < 4; i++) (form.value['option$i'] as String).trim()];
                      if (optionTexts.any((t) => t.isEmpty)) return;

                      setDialogState(() {
                        isSubmitting = true;
                        banner = null;
                      });

                      try {
                        var imagePath = initial?.imagePath;
                        final picked = form.value['image'] as PickedImage?;
                        if (picked != null) imagePath = await _filesApi.upload(picked);

                        final request = {
                          'text': form.value['text'],
                          'imagePath': imagePath,
                          'questionAreaId': form.value['questionAreaId'],
                          'testId': widget.test.id,
                          'answerOptions': [
                            for (var i = 0; i < 4; i++) {'text': optionTexts[i], 'isCorrect': i == correctIndex},
                          ],
                        };
                        if (initial == null) {
                          await _provider.insert(request);
                        } else {
                          await _provider.update(initial.id, request);
                        }
                        if (dialogContext.mounted) Navigator.of(dialogContext).pop(true);
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
                  name: 'text',
                  label: AppStrings.fieldQuestionText,
                  initialValue: initial?.text,
                  maxLines: 2,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(400, AppStrings.fieldQuestionText)]),
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
                AppImagePickerField(name: 'image', label: AppStrings.fieldQuestionImage, pick: _pickImage),
                const SizedBox(height: AppSpacing.lg),
                Text(AppStrings.answerOptionsTitle, style: context.text.titleSmall),
                const SizedBox(height: AppSpacing.sm),
                for (var i = 0; i < 4; i++)
                  Padding(
                    padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        Tooltip(
                          message: AppStrings.answerOptionsNeedOneCorrect,
                          child: InkWell(
                            borderRadius: AppRadius.all(20),
                            onTap: () => setDialogState(() => correctIndex = i),
                            child: Padding(
                              padding: const EdgeInsets.all(8),
                              child: Icon(
                                i == correctIndex ? Icons.radio_button_checked_rounded : Icons.radio_button_unchecked_rounded,
                                color: i == correctIndex ? context.colors.success : context.colors.textMuted,
                                size: 20,
                              ),
                            ),
                          ),
                        ),
                        Expanded(
                          child: AppTextField(
                            name: 'option$i',
                            label: AppStrings.answerOptionLabel(i),
                            initialValue: initial != null && i < initial.answerOptions.length ? initial.answerOptions[i].text : null,
                            validator: AppValidators.required<String>(),
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );

    if (ok != true) return;
    await _controller.reload();
    if (mounted) showSuccessMessage(context, initial == null ? AppStrings.questionCreated : AppStrings.questionUpdated);
  }

  Future<void> _delete(Question question) async {
    await _provider.delete(question.id);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(AppStrings.questionsTitle), bottom: PreferredSize(preferredSize: const Size.fromHeight(0), child: Container())),
      body: Padding(
        padding: AppSpacing.page,
        child: PagedTable<Question>(
          controller: _controller,
          title: AppStrings.questionsTitle,
          subtitle: AppStrings.questionsSubtitle(widget.test.title),
          showSearch: true,
          addLabel: AppStrings.questionsAddNew,
          onAdd: () => _openForm(),
          onEdit: (question) => _openForm(initial: question),
          onDelete: _delete,
          itemLabel: (question) => question.text,
          deletedMessage: (_) => AppStrings.questionDeleted,
          columns: [
            AppColumn<Question>(label: AppStrings.fieldQuestionText, flex: 3, cell: (context, row) => Text(row.text, overflow: TextOverflow.ellipsis)),
            AppColumn<Question>(label: AppStrings.columnQuestionArea, flex: 2, cell: (context, row) => Text(row.questionAreaName)),
            AppColumn<Question>(
              label: AppStrings.fieldQuestionImage,
              width: 90,
              cell: (context, row) => Icon(row.imagePath == null ? Icons.image_not_supported_outlined : Icons.image_outlined, size: 18),
            ),
          ],
        ),
      ),
    );
  }
}
