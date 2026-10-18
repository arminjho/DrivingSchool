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
import '../../models/vehicle.dart';
import '../../models/vehicle_category.dart';
import '../../providers/vehicle_category_provider.dart';
import '../../providers/vehicle_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/api_filter_dropdown.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/paged_table.dart';

class VehiclesTab extends StatefulWidget {
  const VehiclesTab({super.key});

  @override
  State<VehiclesTab> createState() => _VehiclesTabState();
}

class _VehiclesTabState extends State<VehiclesTab> {
  late final VehicleProvider _provider;
  late final VehicleCategoryProvider _categoryProvider;
  late final PagedController<Vehicle> _controller;

  int? _categoryFilter;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _provider = VehicleProvider(session);
    _categoryProvider = VehicleCategoryProvider(session);
    _controller = PagedController<Vehicle>(fetch: _provider.getPaged, searchParam: 'registrationNumber');
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _setCategoryFilter(int? categoryId) {
    setState(() => _categoryFilter = categoryId);
    _controller.setFilter('vehicleCategoryId', categoryId);
  }

  Future<void> _openForm({Vehicle? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    final registration = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.vehicleFormNewTitle : AppStrings.vehicleFormEditTitle,
          icon: AppIcons.vehicle,
          width: 480,
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
                        'registrationNumber': (form.value['registrationNumber'] as String).toUpperCase(),
                        'make': form.value['make'],
                        'model': form.value['model'],
                        'vehicleCategoryId': form.value['vehicleCategoryId'],
                        'manufactureYear': int.parse(form.value['manufactureYear'] as String),
                      };
                      try {
                        if (initial == null) {
                          await _provider.insert(request);
                        } else {
                          await _provider.update(initial.id, request);
                        }
                        if (dialogContext.mounted) Navigator.of(dialogContext).pop(request['registrationNumber'] as String);
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
                  name: 'registrationNumber',
                  label: AppStrings.fieldRegistrationNumber,
                  hint: 'E22-A-123',
                  initialValue: initial?.registrationNumber,
                  validator: AppValidators.required<String>(),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'make',
                  label: AppStrings.fieldMake,
                  initialValue: initial?.make,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(50, AppStrings.fieldMake)]),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'model',
                  label: AppStrings.fieldModel,
                  initialValue: initial?.model,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(50, AppStrings.fieldModel)]),
                ),
                const SizedBox(height: AppSpacing.md),
                ApiDropdownField<VehicleCategory>(
                  name: 'vehicleCategoryId',
                  label: AppStrings.fieldVehicleCategory,
                  icon: AppIcons.vehicle,
                  initialValue: initial?.vehicleCategoryId,
                  loadItems: () async => (await _categoryProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (c) => c.id,
                  labelOf: (c) => '${c.code} — ${c.name}',
                  validator: AppValidators.requiredSelection<int>(),
                  emptyMessage: AppStrings.noVehicleCategoryPrefix,
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'manufactureYear',
                  label: AppStrings.fieldManufactureYear,
                  icon: AppIcons.date,
                  initialValue: initial?.manufactureYear.toString(),
                  keyboardType: TextInputType.number,
                  validator: AppValidators.wholeNumber(min: 1980, max: DateTime.now().year + 1),
                ),
              ],
            ),
          ),
        ),
      ),
    );

    if (registration == null) return;
    await _controller.reload();
    if (mounted) {
      showSuccessMessage(context, initial == null ? AppStrings.vehicleCreated(registration) : AppStrings.vehicleUpdated(registration));
    }
  }

  Future<void> _delete(Vehicle vehicle) async {
    await _provider.delete(vehicle.id);
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<Vehicle>(
      controller: _controller,
      title: AppStrings.vehiclesTab,
      searchHint: AppStrings.vehiclesSearchHint,
      addLabel: AppStrings.vehiclesAddNew,
      onAdd: () => _openForm(),
      onEdit: (vehicle) => _openForm(initial: vehicle),
      onDelete: _delete,
      itemLabel: (vehicle) => vehicle.registrationNumber,
      deletedMessage: (vehicle) => AppStrings.vehicleDeleted(vehicle.registrationNumber),
      extraActions: [
        ApiFilterDropdown<VehicleCategory>(
          allLabel: AppStrings.vehiclesAllCategories,
          loadItems: () async => (await _categoryProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
          idOf: (c) => c.id,
          labelOf: (c) => c.name,
          value: _categoryFilter,
          onChanged: _setCategoryFilter,
        ),
      ],
      columns: [
        AppColumn<Vehicle>(label: AppStrings.columnRegistration, width: 140, sortKey: 'RegistrationNumber', cell: (context, row) => Text(row.registrationNumber)),
        AppColumn<Vehicle>(label: AppStrings.columnMakeModel, flex: 2, cell: (context, row) => Text('${row.make} ${row.model}')),
        AppColumn<Vehicle>(
          label: AppStrings.columnCategory,
          width: 90,
          cell: (context, row) => CircleAvatar(radius: 12, child: Text(row.vehicleCategoryName.isEmpty ? '' : row.vehicleCategoryName[0])),
        ),
        AppColumn<Vehicle>(label: AppStrings.columnYear, width: 90, numeric: true, sortKey: 'ManufactureYear', cell: (context, row) => Text('${row.manufactureYear}')),
        AppColumn<Vehicle>(
          label: AppStrings.columnStatus,
          width: 130,
          cell: (context, row) => AppStatusBadge(
            label: row.status == 'InService' ? AppStrings.vehicleStatusInService : AppStrings.vehicleStatusActive,
            tone: row.status == 'InService' ? AppTone.warning : AppTone.success,
          ),
        ),
      ],
    );
  }
}
