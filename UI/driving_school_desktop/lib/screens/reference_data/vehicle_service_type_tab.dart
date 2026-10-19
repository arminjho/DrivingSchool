import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/paged_controller.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/vehicle_service_type.dart';
import '../../providers/vehicle_service_type_provider.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/paged_table.dart';

class VehicleServiceTypeTab extends StatefulWidget {
  const VehicleServiceTypeTab({super.key});

  @override
  State<VehicleServiceTypeTab> createState() => _VehicleServiceTypeTabState();
}

class _VehicleServiceTypeTabState extends State<VehicleServiceTypeTab> {
  late final VehicleServiceTypeProvider _provider;
  late final PagedController<VehicleServiceType> _controller;

  @override
  void initState() {
    super.initState();
    _provider = VehicleServiceTypeProvider(context.read<AuthSession>());
    _controller = PagedController<VehicleServiceType>(fetch: _provider.getPaged, searchParam: 'name');
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openForm({VehicleServiceType? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    final name = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.addVehicleServiceType : AppStrings.tabVehicleServiceTypes,
          icon: Icons.build_outlined,
          width: 420,
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
                      final request = {'name': form.value['name']};
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

  Future<void> _delete(VehicleServiceType type) async {
    await _provider.delete(type.id);
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<VehicleServiceType>(
      controller: _controller,
      title: AppStrings.tabVehicleServiceTypes,
      searchHint: AppStrings.search,
      addLabel: AppStrings.addVehicleServiceType,
      onAdd: () => _openForm(),
      onEdit: (type) => _openForm(initial: type),
      onDelete: _delete,
      itemLabel: (type) => type.name,
      deletedMessage: (type) => AppStrings.entryDeleted(type.name),
      columns: [
        AppColumn<VehicleServiceType>(label: AppStrings.fieldName, sortKey: 'Name', cell: (context, row) => Text(row.name)),
      ],
    );
  }
}
