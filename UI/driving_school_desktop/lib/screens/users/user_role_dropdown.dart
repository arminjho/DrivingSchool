import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../core/roles.dart';
import '../../l10n/app_strings.dart';

/// Role has exactly three fixed values, never loaded from the API, so this is a plain static
/// dropdown rather than [ApiDropdownField]. It drives the vehicle-category field's visibility
/// in the new-user form, which is dialog-local UI state, not something `FormBuilder` validates
/// - the chosen value is read directly by the caller, not through `form.value`.
class UserRoleDropdown extends StatelessWidget {
  final String? value;
  final ValueChanged<String?> onChanged;

  const UserRoleDropdown({super.key, required this.value, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<String>(
      initialValue: value,
      decoration: const InputDecoration(labelText: AppStrings.fieldRole, prefixIcon: Icon(AppIcons.person, size: 18)),
      items: const [
        DropdownMenuItem(value: Roles.student, child: Text(AppStrings.roleStudent)),
        DropdownMenuItem(value: Roles.instructor, child: Text(AppStrings.roleInstructor)),
        DropdownMenuItem(value: Roles.admin, child: Text(AppStrings.roleAdmin)),
      ],
      onChanged: onChanged,
    );
  }
}
