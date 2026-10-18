import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';

/// Five stars (docs/prijava.md §3.3 "ocjena 1-5 zvjezdica"). Read-only for a Student viewing a
/// completed lesson; interactive for an Instructor grading one.
class StarRating extends StatelessWidget {
  final int value;
  final int max;
  final double size;
  final ValueChanged<int>? onChanged;

  const StarRating({super.key, required this.value, this.max = 5, this.size = 28, this.onChanged});

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        for (var i = 1; i <= max; i++)
          Padding(
            padding: const EdgeInsets.only(right: 2),
            child: onChanged == null
                ? Icon(i <= value ? Icons.star_rounded : Icons.star_border_rounded, size: size, color: i <= value ? c.warning : c.textMuted)
                : InkWell(
                    borderRadius: AppRadius.all(size),
                    onTap: () => onChanged!(i),
                    child: Padding(
                      padding: const EdgeInsets.all(2),
                      child: Icon(i <= value ? Icons.star_rounded : Icons.star_border_rounded, size: size, color: i <= value ? c.warning : c.textMuted),
                    ),
                  ),
          ),
      ],
    );
  }
}
