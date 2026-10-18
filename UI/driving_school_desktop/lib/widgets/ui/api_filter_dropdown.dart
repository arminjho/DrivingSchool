import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';

/// A freestanding (non-`FormBuilder`) API-backed dropdown for a toolbar filter - e.g. "Sve
/// kategorije" above a table. [ApiDropdownField] assumes a `FormBuilder` ancestor and cannot be
/// used outside a form, which is exactly where a list filter lives.
class ApiFilterDropdown<T> extends StatefulWidget {
  final String allLabel;
  final Future<List<T>> Function() loadItems;
  final int Function(T item) idOf;
  final String Function(T item) labelOf;
  final int? value;
  final ValueChanged<int?> onChanged;
  final double width;

  const ApiFilterDropdown({
    super.key,
    required this.allLabel,
    required this.loadItems,
    required this.idOf,
    required this.labelOf,
    required this.value,
    required this.onChanged,
    this.width = 220,
  });

  @override
  State<ApiFilterDropdown<T>> createState() => _ApiFilterDropdownState<T>();
}

class _ApiFilterDropdownState<T> extends State<ApiFilterDropdown<T>> {
  List<T>? _items;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final items = await widget.loadItems();
      if (mounted) setState(() => _items = items);
    } on Object {
      // The filter is a convenience; a failed load just leaves it at "all" instead of
      // blocking the list the filter sits above.
    }
  }

  @override
  Widget build(BuildContext context) {
    final items = _items ?? const [];
    final selected = items.any((item) => widget.idOf(item) == widget.value) ? widget.value : null;

    return SizedBox(
      width: widget.width,
      height: AppSizes.controlHeight,
      child: DropdownButtonFormField<int?>(
        initialValue: selected,
        isDense: true,
        decoration: const InputDecoration(contentPadding: EdgeInsets.symmetric(horizontal: AppSpacing.sm)),
        hint: Text(widget.allLabel),
        items: [
          DropdownMenuItem<int?>(value: null, child: Text(widget.allLabel)),
          for (final item in items) DropdownMenuItem<int?>(value: widget.idOf(item), child: Text(widget.labelOf(item), overflow: TextOverflow.ellipsis)),
        ],
        onChanged: widget.onChanged,
      ),
    );
  }
}
