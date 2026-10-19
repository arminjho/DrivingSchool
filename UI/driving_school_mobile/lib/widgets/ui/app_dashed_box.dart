import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';

/// A rounded box with a dashed outline - the "nothing here yet, this could be filled" look of an
/// empty slot in the instructor's schedule (4.3.4).
class AppDashedBox extends StatelessWidget {
  final Widget child;

  const AppDashedBox({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    return CustomPaint(
      painter: _DashedBorderPainter(color: context.colors.borderStrong, radius: AppRadius.md),
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: SizedBox(width: double.infinity, child: child),
      ),
    );
  }
}

class _DashedBorderPainter extends CustomPainter {
  static const double _dashLength = 6;
  static const double _gapLength = 4;

  final Color color;
  final double radius;

  const _DashedBorderPainter({required this.color, required this.radius});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1;
    final outline = Path()..addRRect(RRect.fromRectAndRadius(Offset.zero & size, Radius.circular(radius)));

    for (final metric in outline.computeMetrics()) {
      for (var distance = 0.0; distance < metric.length; distance += _dashLength + _gapLength) {
        canvas.drawPath(metric.extractPath(distance, distance + _dashLength), paint);
      }
    }
  }

  @override
  bool shouldRepaint(_DashedBorderPainter oldDelegate) => oldDelegate.color != color || oldDelegate.radius != radius;
}
