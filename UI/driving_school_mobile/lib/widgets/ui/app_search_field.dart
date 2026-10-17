import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../l10n/app_strings.dart';

/// The search box above every list.
class AppSearchField extends StatefulWidget {
  final String hint;
  final ValueChanged<String> onChanged;
  final double? width;

  const AppSearchField({super.key, this.hint = AppStrings.search, required this.onChanged, this.width});

  @override
  State<AppSearchField> createState() => _AppSearchFieldState();
}

class _AppSearchFieldState extends State<AppSearchField> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _clear() {
    _controller.clear();
    widget.onChanged('');
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return SizedBox(
      width: widget.width,
      height: AppSizes.controlHeight,
      child: ValueListenableBuilder<TextEditingValue>(
        valueListenable: _controller,
        builder: (context, value, _) => TextField(
          controller: _controller,
          onChanged: widget.onChanged,
          style: context.text.bodyMedium,
          decoration: InputDecoration(
            hintText: widget.hint,
            prefixIcon: Icon(Icons.search_rounded, size: 18, color: c.textMuted),
            suffixIcon: value.text.isEmpty
                ? null
                : IconButton(
                    tooltip: AppStrings.clearSearch,
                    iconSize: 16,
                    onPressed: _clear,
                    icon: const Icon(Icons.close_rounded),
                  ),
            contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
          ),
        ),
      ),
    );
  }
}
