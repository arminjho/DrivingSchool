import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';

import '../../core/design_tokens.dart';
import '../../core/image_validation.dart';
import '../../l10n/app_strings.dart';

/// Opens the platform's picker (file dialog on desktop, gallery on mobile) and returns the
/// chosen image, or null when the user cancels. Supplied by each app.
typedef PickImage = Future<PickedImage?> Function();

/// An image control whose value is a [PickedImage]. The picked file is checked immediately -
/// type by extension and magic bytes, size - and a rejection is shown under the control.
class AppImagePickerField extends StatefulWidget {
  final String name;
  final String label;
  final PickImage pick;
  final bool isRequired;
  final ValueChanged<PickedImage?>? onChanged;
  final bool enabled;

  const AppImagePickerField({
    super.key,
    required this.name,
    required this.label,
    required this.pick,
    this.isRequired = false,
    this.onChanged,
    this.enabled = true,
  });

  @override
  State<AppImagePickerField> createState() => _AppImagePickerFieldState();
}

class _AppImagePickerFieldState extends State<AppImagePickerField> {
  // The preview provider is created once per picked image, so the bytes are decoded once and
  // reused across rebuilds instead of being re-decoded on every frame.
  PickedImage? _previewSource;
  ImageProvider? _preview;

  ImageProvider _previewFor(PickedImage image) {
    if (!identical(_previewSource, image)) {
      _previewSource = image;
      _preview = MemoryImage(image.bytes);
    }
    return _preview!;
  }

  String? _validate(PickedImage? image) {
    if (image == null) return widget.isRequired ? AppStrings.required : null;
    return ImageValidator.validate(image.name, image.bytes);
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return FormBuilderField<PickedImage>(
      name: widget.name,
      enabled: widget.enabled,
      validator: _validate,
      onChanged: widget.onChanged,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      builder: (field) {
        final image = field.value;
        final isValid = image != null && ImageValidator.validate(image.name, image.bytes) == null;

        return InputDecorator(
          decoration: InputDecoration(labelText: widget.label, errorText: field.errorText, enabled: widget.enabled),
          child: Row(
            children: [
              Container(
                width: 72,
                height: 72,
                clipBehavior: Clip.antiAlias,
                decoration: BoxDecoration(
                  color: c.surfaceMuted,
                  borderRadius: AppRadius.all(AppRadius.md),
                  border: Border.all(color: c.border),
                ),
                child: isValid
                    ? Image(image: _previewFor(image), fit: BoxFit.cover)
                    : Icon(Icons.image_outlined, color: c.textMuted),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Wrap(
                  spacing: AppSpacing.xs,
                  runSpacing: AppSpacing.xs,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    OutlinedButton.icon(
                      onPressed: widget.enabled
                          ? () async {
                              final picked = await widget.pick();
                              if (picked == null) return;
                              field.didChange(picked);
                              field.validate();
                            }
                          : null,
                      icon: const Icon(Icons.add_photo_alternate_outlined, size: 18),
                      label: Text(image == null ? AppStrings.pickImage : AppStrings.changeImage),
                    ),
                    if (image != null)
                      TextButton(
                        onPressed: widget.enabled ? () => field.didChange(null) : null,
                        child: Text(AppStrings.removeImage),
                      ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
