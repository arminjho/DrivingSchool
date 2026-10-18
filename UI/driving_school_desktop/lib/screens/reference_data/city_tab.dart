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
import '../../models/city.dart';
import '../../models/country.dart';
import '../../providers/city_provider.dart';
import '../../providers/country_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/paged_table.dart';

class CityTab extends StatefulWidget {
  const CityTab({super.key});

  @override
  State<CityTab> createState() => _CityTabState();
}

class _CityTabState extends State<CityTab> {
  late final CityProvider _provider;
  late final CountryProvider _countryProvider;
  late final PagedController<City> _controller;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _provider = CityProvider(session);
    _countryProvider = CountryProvider(session);
    _controller = PagedController<City>(fetch: _provider.getPaged, searchParam: 'name');
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _openForm({City? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    String? banner;

    final name = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => AppDialog(
          title: initial == null ? AppStrings.addCity : AppStrings.tabCities,
          icon: Icons.location_city_outlined,
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
                        'name': form.value['name'],
                        'postalCode': form.value['postalCode'],
                        'countryId': form.value['countryId'],
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
                  name: 'postalCode',
                  label: AppStrings.fieldPostalCode,
                  initialValue: initial?.postalCode,
                  validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(10, AppStrings.fieldPostalCode)]),
                ),
                const SizedBox(height: AppSpacing.md),
                ApiDropdownField<Country>(
                  name: 'countryId',
                  label: AppStrings.fieldCountry,
                  icon: Icons.flag_outlined,
                  initialValue: initial?.countryId,
                  loadItems: () async => (await _countryProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (c) => c.id,
                  labelOf: (c) => c.name,
                  validator: AppValidators.requiredSelection<int>(),
                  emptyMessage: AppStrings.tabCountries,
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

  Future<void> _delete(City city) async {
    await _provider.delete(city.id);
  }

  @override
  Widget build(BuildContext context) {
    return PagedTable<City>(
      controller: _controller,
      title: AppStrings.tabCities,
      searchHint: AppStrings.search,
      addLabel: AppStrings.addCity,
      onAdd: () => _openForm(),
      onEdit: (city) => _openForm(initial: city),
      onDelete: _delete,
      itemLabel: (city) => city.name,
      deletedMessage: (city) => AppStrings.entryDeleted(city.name),
      columns: [
        AppColumn<City>(label: AppStrings.fieldName, sortKey: 'Name', cell: (context, row) => Text(row.name)),
        AppColumn<City>(label: AppStrings.fieldPostalCode, width: 140, sortKey: 'PostalCode', cell: (context, row) => Text(row.postalCode)),
        AppColumn<City>(label: AppStrings.fieldCountry, width: 160, cell: (context, row) => Text(row.countryName)),
      ],
    );
  }
}
