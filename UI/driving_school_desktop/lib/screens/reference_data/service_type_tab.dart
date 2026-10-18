import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/formatters.dart';
import '../../core/paged_controller.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/service_type.dart';
import '../../providers/service_type_provider.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/paged_table.dart';

class ServiceTypeTab extends StatefulWidget {
  const ServiceTypeTab({super.key});

  @override
  State<ServiceTypeTab> createState() => _ServiceTypeTabState();
}

class _ServiceTypeTabState extends State<ServiceTypeTab> {
  late final ServiceTypeProvider _provider;
  late final PagedController<ServiceType> _controller;

  @override
  void initState() {
    super.initState();
    _provider = ServiceTypeProvider(context.read<AuthSession>());
    _controller = PagedController<ServiceType>(fetch: _provider.getPaged, searchParam: 'name');
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openForm({ServiceType? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    // ServiceTypeRequest has no `code` field - the backend assigns/keeps it, the form only
    // edits name and price.
    final name = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.addServiceType : AppStrings.tabServiceTypes,
          icon: AppIcons.price,
          width: 440,
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
                        'name': form.value['name'],
                        'price': double.parse((form.value['price'] as String).replaceAll(',', '.')),
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
                  name: 'name',
                  label: AppStrings.fieldName,
                  initialValue: initial?.name,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(100, AppStrings.fieldName)]),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'price',
                  label: AppStrings.fieldPrice,
                  icon: AppIcons.price,
                  initialValue: initial?.price.toStringAsFixed(2),
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  validator: AppValidators.decimal(min: 0),
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

  Future<void> _delete(ServiceType type) async {
    await _provider.delete(type.id);
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<ServiceType>(
      controller: _controller,
      title: AppStrings.tabServiceTypes,
      searchHint: AppStrings.search,
      addLabel: AppStrings.addServiceType,
      onAdd: () => _openForm(),
      onEdit: (type) => _openForm(initial: type),
      onDelete: _delete,
      itemLabel: (type) => type.name,
      deletedMessage: (type) => AppStrings.entryDeleted(type.name),
      columns: [
        AppColumn<ServiceType>(label: AppStrings.fieldCode, width: 100, sortKey: 'Code', cell: (context, row) => Text(row.code)),
        AppColumn<ServiceType>(label: AppStrings.fieldName, sortKey: 'Name', cell: (context, row) => Text(row.name)),
        AppColumn<ServiceType>(
          label: AppStrings.fieldPrice,
          width: 120,
          numeric: true,
          sortKey: 'Price',
          cell: (context, row) => Text(AppFormat.money(row.price)),
        ),
      ],
    );
  }
}
