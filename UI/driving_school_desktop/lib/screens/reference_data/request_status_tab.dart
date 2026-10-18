import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/paged_controller.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/request_status.dart';
import '../../providers/request_status_provider.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/paged_table.dart';

class RequestStatusTab extends StatefulWidget {
  const RequestStatusTab({super.key});

  @override
  State<RequestStatusTab> createState() => _RequestStatusTabState();
}

class _RequestStatusTabState extends State<RequestStatusTab> {
  late final RequestStatusProvider _provider;
  late final PagedController<RequestStatus> _controller;

  @override
  void initState() {
    super.initState();
    _provider = RequestStatusProvider(context.read<AuthSession>());
    _controller = PagedController<RequestStatus>(fetch: _provider.getPaged, searchParam: 'name');
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openForm({RequestStatus? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    final name = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.addRequestStatus : AppStrings.tabRequestStatuses,
          icon: Icons.rule_outlined,
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
                        'code': form.value['code'],
                        'name': form.value['name'],
                        'description': (form.value['description'] as String?)?.trim().isEmpty ?? true ? null : form.value['description'],
                      };
                      try {
                        if (initial == null) {
                          await _provider.insert(request);
                        } else {
                          await _provider.update(initial.id, request);
                        }
                        if (dialogContext.mounted) Navigator.of(dialogContext).pop(request['name'] as String);
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
                  name: 'code',
                  label: AppStrings.fieldCode,
                  initialValue: initial?.code,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(20, AppStrings.fieldCode)]),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'name',
                  label: AppStrings.fieldName,
                  initialValue: initial?.name,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(100, AppStrings.fieldName)]),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'description',
                  label: AppStrings.fieldDescription,
                  initialValue: initial?.description,
                  maxLines: 3,
                  validator: AppValidators.maxLength(300, AppStrings.fieldDescription),
                ),
              ],
            ),
          ),
        ),
      ),
    );

    if (name == null) return;
    await _controller.reload();
    if (mounted) showSuccessMessage(context, initial == null ? AppStrings.entryCreated(name) : AppStrings.entryUpdated(name));
  }

  Future<void> _delete(RequestStatus status) async {
    await _provider.delete(status.id);
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<RequestStatus>(
      controller: _controller,
      title: AppStrings.tabRequestStatuses,
      searchHint: AppStrings.search,
      addLabel: AppStrings.addRequestStatus,
      onAdd: () => _openForm(),
      onEdit: (status) => _openForm(initial: status),
      onDelete: _delete,
      itemLabel: (status) => status.name,
      deletedMessage: (status) => AppStrings.entryDeleted(status.name),
      columns: [
        AppColumn<RequestStatus>(label: AppStrings.fieldCode, width: 140, sortKey: 'Code', cell: (context, row) => Text(row.code)),
        AppColumn<RequestStatus>(label: AppStrings.fieldName, sortKey: 'Name', cell: (context, row) => Text(row.name)),
        AppColumn<RequestStatus>(
          label: AppStrings.fieldDescription,
          flex: 2,
          cell: (context, row) => Text(row.description ?? '', overflow: TextOverflow.ellipsis),
        ),
      ],
    );
  }
}
