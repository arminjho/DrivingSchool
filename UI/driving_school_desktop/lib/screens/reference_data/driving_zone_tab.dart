import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/paged_controller.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/driving_zone.dart';
import '../../providers/driving_zone_provider.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/paged_table.dart';

class DrivingZoneTab extends StatefulWidget {
  const DrivingZoneTab({super.key});

  @override
  State<DrivingZoneTab> createState() => _DrivingZoneTabState();
}

class _DrivingZoneTabState extends State<DrivingZoneTab> {
  late final DrivingZoneProvider _provider;
  late final PagedController<DrivingZone> _controller;

  @override
  void initState() {
    super.initState();
    _provider = DrivingZoneProvider(context.read<AuthSession>());
    _controller = PagedController<DrivingZone>(fetch: _provider.getPaged, searchParam: 'name');
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openForm({DrivingZone? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    final name = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.addDrivingZone : AppStrings.tabDrivingZones,
          icon: AppIcons.place,
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

  Future<void> _delete(DrivingZone zone) async {
    await _provider.delete(zone.id);
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<DrivingZone>(
      controller: _controller,
      title: AppStrings.tabDrivingZones,
      searchHint: AppStrings.search,
      addLabel: AppStrings.addDrivingZone,
      onAdd: () => _openForm(),
      onEdit: (zone) => _openForm(initial: zone),
      onDelete: _delete,
      itemLabel: (zone) => zone.name,
      deletedMessage: (zone) => AppStrings.entryDeleted(zone.name),
      columns: [
        AppColumn<DrivingZone>(label: AppStrings.fieldName, sortKey: 'Name', cell: (context, row) => Text(row.name)),
      ],
    );
  }
}
