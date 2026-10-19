import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/paged_controller.dart';
import '../../core/roles.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/city.dart';
import '../../models/user.dart';
import '../../models/vehicle_category.dart';
import '../../providers/city_provider.dart';
import '../../providers/user_provider.dart';
import '../../providers/vehicle_category_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_data_table.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import '../../widgets/ui/paged_table.dart';
import 'user_role_dropdown.dart';

/// 4.1.2 - search by name/e-mail, role filter, table of all users, "+ Novi korisnik", edit
/// with an optional "Izmijeni lozinku" toggle, activate/deactivate instead of a hard delete so
/// account history (lessons, payments) stays intact.
class UsersScreen extends StatefulWidget {
  const UsersScreen({super.key});

  @override
  State<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  late final UserProvider _provider;
  late final CityProvider _cityProvider;
  late final VehicleCategoryProvider _categoryProvider;
  late final PagedController<User> _controller;

  String? _roleFilter;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _provider = UserProvider(session);
    _cityProvider = CityProvider(session);
    _categoryProvider = VehicleCategoryProvider(session);
    _controller = PagedController<User>(fetch: _provider.getPaged, searchParam: 'search');
    _controller.load();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _toggleActive(User user) async {
    try {
      if (user.isActive) {
        await _provider.deactivate(user.id);
      } else {
        await _provider.activate(user.id);
      }
      await _controller.reload();
      if (mounted) {
        showSuccessMessage(context, user.isActive ? AppStrings.userDeactivated(user.fullName) : AppStrings.userActivated(user.fullName));
      }
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    }
  }

  Future<void> _delete(User user) async {
    await _provider.delete(user.id);
  }

  Future<void> _openForm({User? initial}) async {
    final formKey = GlobalKey<FormBuilderState>();
    var isSubmitting = false;
    var changePassword = false;
    String? role = initial?.role ?? Roles.student;
    String? banner;

    final name = await showAppDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) {
          final needsVehicleCategory = role == Roles.student || role == Roles.instructor;

          return AppDialog(
            title: initial == null ? AppStrings.userFormNewTitle : AppStrings.userFormEditTitle,
            icon: AppIcons.person,
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

                        final fullName = '${form.value['firstName']} ${form.value['lastName']}';
                        final address = (form.value['address'] as String?)?.trim();
                        final request = <String, dynamic>{
                          'firstName': form.value['firstName'],
                          'lastName': form.value['lastName'],
                          'phoneNumber': form.value['phoneNumber'],
                          'address': address?.isEmpty ?? true ? null : address,
                          'cityId': form.value['cityId'],
                        };

                        try {
                          if (initial == null) {
                            await _provider.insert({
                              ...request,
                              'email': form.value['email'],
                              'password': form.value['password'],
                              'role': role,
                              if (needsVehicleCategory) 'vehicleCategoryId': form.value['vehicleCategoryId'],
                            });
                          } else {
                            await _provider.update(initial.id, {
                              ...request,
                              if (changePassword) 'newPassword': form.value['newPassword'],
                              if (changePassword) 'confirmNewPassword': form.value['confirmNewPassword'],
                            });
                          }
                          if (dialogContext.mounted) Navigator.of(dialogContext).pop(fullName);
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
                  Row(
                    children: [
                      Expanded(
                        child: AppTextField(
                          name: 'firstName',
                          label: AppStrings.fieldFirstName,
                          initialValue: initial?.firstName,
                          validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(100, AppStrings.fieldFirstName)]),
                        ),
                      ),
                      const SizedBox(width: AppSpacing.md),
                      Expanded(
                        child: AppTextField(
                          name: 'lastName',
                          label: AppStrings.fieldLastName,
                          initialValue: initial?.lastName,
                          validator: AppValidators.compose([AppValidators.required(), AppValidators.maxLength(100, AppStrings.fieldLastName)]),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.md),
                  if (initial == null)
                    AppTextField(
                      name: 'email',
                      label: AppStrings.fieldEmail,
                      icon: Icons.mail_outline_rounded,
                      keyboardType: TextInputType.emailAddress,
                      validator: AppValidators.email(),
                    )
                  else
                    AppTextField(name: 'emailDisplay', label: AppStrings.fieldEmail, initialValue: initial.email, enabled: false),
                  const SizedBox(height: AppSpacing.md),
                  AppTextField(
                    name: 'phoneNumber',
                    label: AppStrings.fieldPhone,
                    icon: Icons.phone_outlined,
                    hint: AppStrings.phoneHint,
                    initialValue: initial?.phoneNumber,
                    validator: AppValidators.phone(),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  AppTextField(
                    name: 'address',
                    label: AppStrings.fieldAddress,
                    icon: AppIcons.place,
                    initialValue: initial?.address,
                    validator: AppValidators.maxLength(200, AppStrings.fieldAddress),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  ApiDropdownField<City>(
                    name: 'cityId',
                    label: AppStrings.fieldCity,
                    icon: Icons.location_city_outlined,
                    initialValue: initial?.cityId,
                    loadItems: () async => (await _cityProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                    idOf: (city) => city.id,
                    labelOf: (city) => city.name,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  if (initial == null)
                    UserRoleDropdown(
                      value: role,
                      onChanged: (value) => setDialogState(() => role = value),
                    )
                  else
                    AppTextField(name: 'roleDisplay', label: AppStrings.fieldRole, initialValue: _roleLabel(initial.role), enabled: false),
                  if (needsVehicleCategory) ...[
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
                  ],
                  if (initial == null) ...[
                    const SizedBox(height: AppSpacing.md),
                    AppTextField(name: 'password', label: AppStrings.fieldPassword, obscureText: true, validator: AppValidators.password()),
                    const SizedBox(height: AppSpacing.md),
                    AppTextField(
                      name: 'confirmPassword',
                      label: AppStrings.fieldConfirmPassword,
                      obscureText: true,
                      validator: AppValidators.sameAs(() => formKey.currentState?.fields['password']?.value as String? ?? ''),
                    ),
                  ] else ...[
                    const SizedBox(height: AppSpacing.sm),
                    CheckboxListTile(
                      value: changePassword,
                      onChanged: (value) => setDialogState(() => changePassword = value ?? false),
                      title: const Text(AppStrings.changePasswordToggle),
                      contentPadding: EdgeInsets.zero,
                      controlAffinity: ListTileControlAffinity.leading,
                    ),
                    if (changePassword) ...[
                      AppTextField(name: 'newPassword', label: AppStrings.fieldNewPassword, obscureText: true, validator: AppValidators.password()),
                      const SizedBox(height: AppSpacing.md),
                      AppTextField(
                        name: 'confirmNewPassword',
                        label: AppStrings.fieldConfirmPassword,
                        obscureText: true,
                        validator: AppValidators.sameAs(() => formKey.currentState?.fields['newPassword']?.value as String? ?? ''),
                      ),
                    ],
                  ],
                ],
              ),
            ),
          );
        },
      ),
    );

    if (name == null) return;
    await _controller.reload();
    if (mounted) showSuccessMessage(context, initial == null ? AppStrings.userCreated(name) : AppStrings.userUpdated(name));
  }

  String _roleLabel(String role) => switch (role) {
        Roles.admin => AppStrings.roleAdmin,
        Roles.instructor => AppStrings.roleInstructor,
        _ => AppStrings.roleStudent,
      };

  AppTone _roleTone(String role) => switch (role) {
        Roles.admin => AppTone.danger,
        Roles.instructor => AppTone.primary,
        _ => AppTone.info,
      };

  @override
  Widget build(BuildContext context) {
    return PagedTable<User>(
      controller: _controller,
      searchHint: AppStrings.usersSearchHint,
      addLabel: AppStrings.usersAddNew,
      onAdd: () => _openForm(),
      onEdit: (user) => _openForm(initial: user),
      onDelete: _delete,
      itemLabel: (user) => user.fullName,
      deletedMessage: (user) => AppStrings.userDeleted(user.fullName),
      extraActions: [
        SizedBox(
          width: 170,
          height: AppSizes.controlHeight,
          child: DropdownButtonFormField<String?>(
            initialValue: _roleFilter,
            isDense: true,
            decoration: const InputDecoration(contentPadding: EdgeInsets.symmetric(horizontal: AppSpacing.sm)),
            hint: const Text(AppStrings.usersAllRoles),
            items: [
              const DropdownMenuItem<String?>(value: null, child: Text(AppStrings.usersAllRoles)),
              DropdownMenuItem<String?>(value: Roles.student, child: Text(AppStrings.roleStudent)),
              DropdownMenuItem<String?>(value: Roles.instructor, child: Text(AppStrings.roleInstructor)),
              DropdownMenuItem<String?>(value: Roles.admin, child: Text(AppStrings.roleAdmin)),
            ],
            onChanged: (value) {
              setState(() => _roleFilter = value);
              _controller.setFilter('role', value);
            },
          ),
        ),
      ],
      extraRowActions: (user) => [
        AppRowAction(
          icon: user.isActive ? Icons.toggle_on_outlined : Icons.toggle_off_outlined,
          tooltip: user.isActive ? AppStrings.deactivate : AppStrings.activate,
          onPressed: () => _toggleActive(user),
        ),
      ],
      columns: [
        AppColumn<User>(
          label: AppStrings.usersColumnName,
          flex: 2,
          sortKey: 'FirstName',
          cell: (context, row) => Row(
            children: [
              AppAvatar(name: row.fullName, size: 30, imagePath: row.profileImagePath),
              const SizedBox(width: AppSpacing.sm),
              Flexible(child: Text(row.fullName, overflow: TextOverflow.ellipsis)),
            ],
          ),
        ),
        AppColumn<User>(label: AppStrings.usersColumnEmail, flex: 2, sortKey: 'Email', cell: (context, row) => Text(row.email, overflow: TextOverflow.ellipsis)),
        AppColumn<User>(
          label: AppStrings.usersColumnRole,
          width: 130,
          cell: (context, row) => AppStatusBadge(label: _roleLabel(row.role), tone: _roleTone(row.role)),
        ),
        AppColumn<User>(
          label: AppStrings.usersColumnStatus,
          width: 120,
          cell: (context, row) => AppStatusBadge(
            label: row.isActive ? AppStrings.usersStatusActive : AppStrings.usersStatusInactive,
            tone: row.isActive ? AppTone.success : AppTone.neutral,
          ),
        ),
      ],
    );
  }
}
