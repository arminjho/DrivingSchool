import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';

/// A semantic tone, not a colour: callers say what a thing *means* and the token layer decides
/// how it looks, which keeps every status pill in the app identical.
enum AppTone { neutral, primary, info, success, warning, danger }

extension AppToneX on AppTone {
  Color foreground(BuildContext context) {
    final c = context.colors;
    return switch (this) {
      AppTone.neutral => c.neutral,
      AppTone.primary => c.primary,
      AppTone.info => c.info,
      AppTone.success => c.success,
      AppTone.warning => c.warning,
      AppTone.danger => c.danger,
    };
  }

  Color background(BuildContext context) {
    final c = context.colors;
    return switch (this) {
      AppTone.neutral => c.neutralSoft,
      AppTone.primary => c.primarySoft,
      AppTone.info => c.infoSoft,
      AppTone.success => c.successSoft,
      AppTone.warning => c.warningSoft,
      AppTone.danger => c.dangerSoft,
    };
  }
}

/// A status pill: coloured dot, label, soft background. The dot gives the colour a shape to
/// attach to, so the status is not carried by colour alone.
class AppStatusBadge extends StatelessWidget {
  final String label;
  final AppTone tone;

  const AppStatusBadge({super.key, required this.label, this.tone = AppTone.neutral});

  @override
  Widget build(BuildContext context) {
    final foreground = tone.foreground(context);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xs, vertical: 5),
      decoration: BoxDecoration(
        color: tone.background(context),
        borderRadius: AppRadius.all(AppRadius.xs),
        border: Border.all(color: foreground.withValues(alpha: 0.3)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 6,
            height: 6,
            margin: const EdgeInsets.only(right: 6),
            decoration: BoxDecoration(color: foreground, shape: BoxShape.circle),
          ),
          // One line, always: the pill sits inside fixed-height table rows.
          Flexible(
            child: Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: context.text.labelMedium?.copyWith(color: foreground, fontSize: 12, height: 1.1),
            ),
          ),
        ],
      ),
    );
  }
}

/// Initials in a tile; the colour is derived from the name so a person keeps theirs everywhere.
class AppAvatar extends StatelessWidget {
  static const List<Color> _palette = [
    Color(0xFF5B93FF),
    Color(0xFF35C47B),
    Color(0xFFA78BFA),
    Color(0xFFFBBF24),
    Color(0xFF22D3EE),
    Color(0xFFF472B6),
  ];

  final String name;
  final double size;

  const AppAvatar({super.key, required this.name, this.size = 34});

  static String initials(String value) {
    final parts = value.trim().split(RegExp(r'\s+')).where((part) => part.isNotEmpty).toList();
    if (parts.isEmpty) return '?';
    if (parts.length == 1) return parts.first.substring(0, 1).toUpperCase();
    return (parts.first.substring(0, 1) + parts.last.substring(0, 1)).toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final color = _palette[name.hashCode.abs() % _palette.length];

    return Container(
      width: size,
      height: size,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.22),
        borderRadius: AppRadius.all(size / 3.2),
        border: Border.all(color: color.withValues(alpha: 0.3)),
      ),
      child: Text(
        initials(name),
        style: context.text.labelMedium?.copyWith(color: color, fontSize: size * 0.36, height: 1.1),
      ),
    );
  }
}
