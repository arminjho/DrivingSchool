import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/formatters.dart';
import '../../core/paged_controller.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/vehicle.dart';
import '../../models/vehicle_category.dart';
import '../../models/vehicle_service.dart';
import '../../models/vehicle_service_type.dart';
import '../../providers/vehicle_category_provider.dart';
import '../../providers/vehicle_provider.dart';
import '../../providers/vehicle_service_provider.dart';
import '../../providers/vehicle_service_type_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/api_filter_dropdown.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_date_fields.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/paged_table.dart';

class VehicleServicesTab extends StatefulWidget {
  const VehicleServicesTab({super.key});

  @override
  State<VehicleServicesTab> createState() => _VehicleServicesTabState();
}

class _VehicleServicesTabState extends State<VehicleServicesTab> {
  late final VehicleServiceProvider _provider;
  late final VehicleProvider _vehicleProvider;
  late final VehicleCategoryProvider _categoryProvider;
  late final VehicleServiceTypeProvider _serviceTypeProvider;
  late final PagedController<VehicleService> _controller;

  int? _categoryFilter;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _provider = VehicleServiceProvider(session);
    _vehicleProvider = VehicleProvider(session);
    _categoryProvider = VehicleCategoryProvider(session);
    _serviceTypeProvider = VehicleServiceTypeProvider(session);
    _controller = PagedController<VehicleService>(fetch: _provider.getPaged);
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openForm({VehicleService? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    final result = await showAppDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.vehicleServiceFormNewTitle : AppStrings.vehicleServiceFormEditTitle,
          icon: Icons.build_outlined,
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
                      final serviceDate = form.value['serviceDate'] as DateTime;
                      final request = {
                        'vehicleId': form.value['vehicleId'],
                        'serviceDate': serviceDate.toUtc().toIso8601String(),
                        'vehicleServiceTypeId': form.value['vehicleServiceTypeId'],
                        'description': form.value['description'],
                        'cost': double.parse((form.value['cost'] as String).replaceAll(',', '.')),
                        if (initial != null) 'status': form.value['status'],
                      };
                      try {
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
                ApiDropdownField<Vehicle>(
                  name: 'vehicleId',
                  label: AppStrings.columnVehicle,
                  icon: AppIcons.vehicle,
                  initialValue: initial?.vehicleId,
                  loadItems: () async => (await _vehicleProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (v) => v.id,
                  labelOf: (v) => '${v.registrationNumber} — ${v.make} ${v.model}',
                  validator: AppValidators.requiredSelection<int>(),
                  emptyMessage: AppStrings.noVehiclesPrefix,
                ),
                const SizedBox(height: AppSpacing.md),
                AppDateField(
                  name: 'serviceDate',
                  label: AppStrings.fieldServiceDate,
                  initialValue: initial?.serviceDate,
                  validator: (value) => value == null ? AppStrings.dateRequired : null,
                ),
                const SizedBox(height: AppSpacing.md),
                ApiDropdownField<VehicleServiceType>(
                  name: 'vehicleServiceTypeId',
                  label: AppStrings.fieldServiceType,
                  icon: Icons.build_outlined,
                  initialValue: initial?.vehicleServiceTypeId,
                  loadItems: () async => (await _serviceTypeProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (t) => t.id,
                  labelOf: (t) => t.name,
                  validator: AppValidators.requiredSelection<int>(),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'description',
                  label: AppStrings.columnDescription,
                  initialValue: initial?.description,
                  maxLines: 2,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(300, AppStrings.columnDescription)]),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'cost',
                  label: AppStrings.fieldCost,
                  icon: AppIcons.price,
                  initialValue: initial?.cost.toStringAsFixed(2),
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  validator: AppValidators.decimal(min: 0),
                ),
                if (initial != null) ...[
                  const SizedBox(height: AppSpacing.md),
                  FormBuilderDropdown<int>(
                    name: 'status',
                    initialValue: _statusValue(initial.status),
                    decoration: const InputDecoration(labelText: AppStrings.fieldServiceStatus),
                    items: const [
                      DropdownMenuItem(value: 0, child: Text(AppStrings.vehicleServiceStatusScheduled)),
                      DropdownMenuItem(value: 1, child: Text(AppStrings.vehicleServiceStatusInProgress)),
                      DropdownMenuItem(value: 2, child: Text(AppStrings.vehicleServiceStatusCompleted)),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );

    if (result != true) return;
    await _controller.reload();
    if (mounted) {
      showSuccessMessage(context, initial == null ? AppStrings.vehicleServiceCreated : AppStrings.vehicleServiceUpdated);
    }
  }

  int _statusValue(String status) => switch (status) {
        'InProgress' => 1,
        'Completed' => 2,
        _ => 0,
      };

  Future<void> _delete(VehicleService service) async {
    await _provider.delete(service.id);
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<VehicleService>(
      controller: _controller,
      title: AppStrings.vehicleServicesTab,
      showSearch: false,
      addLabel: AppStrings.vehicleServicesAddNew,
      onAdd: () => _openForm(),
      onEdit: (service) => _openForm(initial: service),
      onDelete: _delete,
      itemLabel: (service) => service.vehicleRegistrationNumber,
      deletedMessage: (_) => AppStrings.vehicleServiceDeleted,
      extraActions: [
        ApiFilterDropdown<VehicleCategory>(
          allLabel: AppStrings.vehiclesAllCategories,
          loadItems: () async => (await _categoryProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
          idOf: (c) => c.id,
          labelOf: (c) => c.name,
          value: _categoryFilter,
          onChanged: (id) {
            setState(() => _categoryFilter = id);
            _controller.setFilter('vehicleCategoryId', id);
          },
        ),
      ],
      columns: [
        AppColumn<VehicleService>(label: AppStrings.columnVehicle, flex: 2, sortKey: 'ServiceDate', cell: (context, row) => Text(row.vehicleRegistrationNumber)),
        AppColumn<VehicleService>(label: AppStrings.columnDate, width: 110, sortKey: 'ServiceDate', cell: (context, row) => Text(AppFormat.date(row.serviceDate))),
        AppColumn<VehicleService>(label: AppStrings.columnServiceType, flex: 2, cell: (context, row) => Text(row.vehicleServiceTypeName)),
        AppColumn<VehicleService>(label: AppStrings.columnCost, width: 110, numeric: true, sortKey: 'Cost', cell: (context, row) => Text(AppFormat.money(row.cost))),
        AppColumn<VehicleService>(
          label: AppStrings.columnStatus,
          width: 120,
          cell: (context, row) => AppStatusBadge(
            label: switch (row.status) {
              'InProgress' => AppStrings.vehicleServiceStatusInProgress,
              'Completed' => AppStrings.vehicleServiceStatusCompleted,
              _ => AppStrings.vehicleServiceStatusScheduled,
            },
            tone: switch (row.status) {
              'InProgress' => AppTone.info,
              'Completed' => AppTone.success,
              _ => AppTone.primary,
            },
          ),
        ),
      ],
    );
  }
}
