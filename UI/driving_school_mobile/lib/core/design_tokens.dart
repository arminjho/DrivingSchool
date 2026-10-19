import 'package:material_ui/material_ui.dart';

/// Every visual decision resolves to a value in this file: screens read `context.colors`,
/// [AppSpacing] and [AppRadius] and never hard-code a colour, padding or radius.
///
/// The apps are dark-themed to match the mockups in docs/prijava.md. Neutrals stay neutral and
/// blue appears only where it carries meaning; text/background pairs keep a contrast ratio
/// comfortably above WCAG AA.

/// 4px baseline grid.
abstract final class AppSpacing {
  static const double xxs = 4;
  static const double xs = 8;
  static const double sm = 12;
  static const double md = 16;
  static const double lg = 24;
  static const double xl = 32;

  static const EdgeInsets page = EdgeInsets.symmetric(horizontal: md, vertical: lg);
}

abstract final class AppRadius {
  static const double xs = 6;
  static const double sm = 8;
  static const double md = 10;
  static const double lg = 14;
  static const double xl = 20;

  static BorderRadius all(double radius) => BorderRadius.circular(radius);
}

abstract final class AppSizes {
  static const double bottomNavHeight = 64;
  static const double controlHeight = 46;
  static const double controlHeightSm = 36;
  static const double listCardMinHeight = 64;
}

abstract final class AppIcons {
  static const IconData date = Icons.calendar_today_outlined;
  static const IconData time = Icons.schedule_rounded;
  static const IconData vehicle = Icons.directions_car_outlined;
  static const IconData price = Icons.payments_outlined;
  static const IconData person = Icons.person_outline_rounded;
  static const IconData place = Icons.place_outlined;
  static const IconData notifications = Icons.notifications_outlined;
  static const IconData lesson = Icons.event_note_outlined;
}

@immutable
class AppColors extends ThemeExtension<AppColors> {
  // Surfaces
  final Color canvas;
  final Color surface;
  final Color surfaceMuted;
  final Color surfaceHover;

  // Lines
  final Color border;
  final Color borderStrong;

  // Text
  final Color textPrimary;
  final Color textSecondary;
  final Color textMuted;

  // Brand
  final Color primary;
  final Color primaryStrong;
  final Color primarySoft;
  final Color onPrimary;

  // Status: a solid tone (text, icons, dots) and a soft tone (badge backgrounds).
  final Color success;
  final Color successSoft;
  final Color warning;
  final Color warningSoft;
  final Color danger;
  final Color dangerSoft;
  final Color info;
  final Color infoSoft;
  final Color neutral;
  final Color neutralSoft;

  // Navigation (sidebar / bottom bar), darker than the content surface.
  final Color navBackground;
  final Color navBorder;
  final Color navText;
  final Color navTextMuted;
  final Color navItemHover;
  final Color navItemSelected;
  final Color navItemSelectedText;

  final Color shadowColor;

  const AppColors({
    required this.canvas,
    required this.surface,
    required this.surfaceMuted,
    required this.surfaceHover,
    required this.border,
    required this.borderStrong,
    required this.textPrimary,
    required this.textSecondary,
    required this.textMuted,
    required this.primary,
    required this.primaryStrong,
    required this.primarySoft,
    required this.onPrimary,
    required this.success,
    required this.successSoft,
    required this.warning,
    required this.warningSoft,
    required this.danger,
    required this.dangerSoft,
    required this.info,
    required this.infoSoft,
    required this.neutral,
    required this.neutralSoft,
    required this.navBackground,
    required this.navBorder,
    required this.navText,
    required this.navTextMuted,
    required this.navItemHover,
    required this.navItemSelected,
    required this.navItemSelectedText,
    required this.shadowColor,
  });

  static const AppColors dark = AppColors(
    canvas: Color(0xFF0B0F17),
    surface: Color(0xFF131924),
    surfaceMuted: Color(0xFF1A2130),
    surfaceHover: Color(0xFF1C2434),
    border: Color(0xFF232C3C),
    borderStrong: Color(0xFF313B4E),
    textPrimary: Color(0xFFE9EDF5),
    textSecondary: Color(0xFFA9B3C5),
    textMuted: Color(0xFF8592A6),
    primary: Color(0xFF5B93FF),
    primaryStrong: Color(0xFF7EACFF),
    primarySoft: Color(0xFF17233B),
    onPrimary: Color(0xFF07101F),
    success: Color(0xFF35C47B),
    successSoft: Color(0xFF10241B),
    warning: Color(0xFFF0A23C),
    warningSoft: Color(0xFF2A2013),
    danger: Color(0xFFF06A6F),
    dangerSoft: Color(0xFF2C1719),
    info: Color(0xFF5B93FF),
    infoSoft: Color(0xFF17233B),
    neutral: Color(0xFF9CA7BA),
    neutralSoft: Color(0xFF1A2130),
    navBackground: Color(0xFF090D15),
    navBorder: Color(0xFF1C2432),
    navText: Color(0xFFBFC8D8),
    navTextMuted: Color(0xFF7C889B),
    navItemHover: Color(0xFF171F2C),
    navItemSelected: Color(0xFF1A2A47),
    navItemSelectedText: Color(0xFFFFFFFF),
    shadowColor: Color(0xFF000000),
  );

  List<BoxShadow> get shadowSm => [
        BoxShadow(color: shadowColor.withValues(alpha: 0.18), blurRadius: 2, offset: const Offset(0, 1)),
      ];

  List<BoxShadow> get shadowLg => [
        BoxShadow(color: shadowColor.withValues(alpha: 0.45), blurRadius: 40, offset: const Offset(0, 16)),
        BoxShadow(color: shadowColor.withValues(alpha: 0.25), blurRadius: 8, offset: const Offset(0, 2)),
      ];

  @override
  AppColors copyWith() => this;

  @override
  AppColors lerp(ThemeExtension<AppColors>? other, double t) => this;
}

extension AppColorsX on BuildContext {
  AppColors get colors => Theme.of(this).extension<AppColors>() ?? AppColors.dark;

  TextTheme get text => Theme.of(this).textTheme;
}

abstract final class AppTypography {
  /// Tabular figures line up digits in columns of times and amounts.
  static const List<FontFeature> tabular = [FontFeature.tabularFigures()];
}
