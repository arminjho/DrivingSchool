import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';

import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../l10n/app_strings.dart';

/// A dropdown whose options come from the API - never a free-text box for something that is a
/// foreign key. The form value is the selected item's id.
///
/// It handles its own lifecycle so a form needs no loading code: a spinner while loading, an
/// error with a retry button if the call fails, and a message (instead of an empty list) when
/// there is nothing to choose - so a form whose prerequisite data is missing says so.
class ApiDropdownField<T> extends StatefulWidget {
  final String name;
  final String label;
  final Future<List<T>> Function() loadItems;
  final int Function(T item) idOf;
  final String Function(T item) labelOf;
  final int? initialValue;
  final FormFieldValidator<int>? validator;
  final void Function(int? id, T? item)? onChanged;
  final bool enabled;
  final IconData? icon;

  /// Shown when the API returns no items, e.g. "Prvo dodajte kategoriju vozila."
  final String? emptyMessage;

  /// Change it to make the dropdown reload (e.g. a dependent dropdown whose parent changed).
  final Object? reloadKey;

  const ApiDropdownField({
    super.key,
    required this.name,
    required this.label,
    required this.loadItems,
    required this.idOf,
    required this.labelOf,
    this.initialValue,
    this.validator,
    this.onChanged,
    this.enabled = true,
    this.icon,
    this.emptyMessage,
    this.reloadKey,
  });

  @override
  State<ApiDropdownField<T>> createState() => _ApiDropdownFieldState<T>();
}

class _ApiDropdownFieldState<T> extends State<ApiDropdownField<T>> {
  List<T>? _items;
  String? _loadError;
  bool _isLoading = true;
  int _generation = 0;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(ApiDropdownField<T> oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.reloadKey != oldWidget.reloadKey) _load();
  }

  Future<void> _load() async {
    final generation = ++_generation;
    setState(() {
      _isLoading = true;
      _loadError = null;
    });

    try {
      final items = await widget.loadItems();
      if (!mounted || generation != _generation) return;
      setState(() => _items = items);
    } on Object catch (error) {
      if (!mounted || generation != _generation) return;
      setState(() {
        _items = null;
        _loadError = '${AppStrings.dropdownLoadFailed} ${failureCause(error)}';
      });
    } finally {
      if (mounted && generation == _generation) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final items = _items ?? const [];
    final isEmpty = !_isLoading && _loadError == null && items.isEmpty;
    final interactive = widget.enabled && !_isLoading && _loadError == null && items.isNotEmpty;

    return FormBuilderField<int>(
      name: widget.name,
      initialValue: widget.initialValue,
      enabled: interactive,
      validator: widget.validator,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      onChanged: (id) {
        final callback = widget.onChanged;
        if (callback == null) return;
        final item = id == null ? null : items.where((i) => widget.idOf(i) == id).firstOrNull;
        callback(id, item);
      },
      builder: (field) {
        // A stale value (the list reloaded without it) must not stay selected.
        final selected = items.any((item) => widget.idOf(item) == field.value) ? field.value : null;

        return InputDecorator(
          isEmpty: selected == null,
          isFocused: false,
          decoration: InputDecoration(
            labelText: widget.label,
            hintText: _isLoading ? AppStrings.dropdownLoading : AppStrings.dropdownSelect,
            errorText: field.errorText ?? _loadError,
            helperText: isEmpty ? (widget.emptyMessage ?? AppStrings.dropdownEmpty) : null,
            helperMaxLines: 2,
            prefixIcon: widget.icon == null ? null : Icon(widget.icon, size: 18),
            suffixIcon: _isLoading
                ? const Padding(
                    padding: EdgeInsets.all(AppSpacing.sm),
                    child: SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)),
                  )
                : _loadError != null
                    ? IconButton(tooltip: AppStrings.retry, icon: const Icon(Icons.refresh_rounded, size: 18), onPressed: _load)
                    : null,
            enabled: interactive,
          ),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<int>(
              value: selected,
              isExpanded: true,
              isDense: true,
              dropdownColor: c.surface,
              style: context.text.bodyMedium,
              hint: const SizedBox.shrink(),
              onChanged: interactive ? field.didChange : null,
              items: [
                for (final item in items)
                  DropdownMenuItem<int>(
                    value: widget.idOf(item),
                    child: Text(widget.labelOf(item), overflow: TextOverflow.ellipsis),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}
