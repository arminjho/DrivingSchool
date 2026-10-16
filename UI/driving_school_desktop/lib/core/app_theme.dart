import 'package:material_ui/material_ui.dart';

import 'design_tokens.dart';

/// Wires [AppColors] into Material's component themes, so a plain `FilledButton` or
/// `TextField` already looks right without any screen styling it locally.
abstract final class AppTheme {
  static ThemeData dark() {
    const c = AppColors.dark;

    final scheme = ColorScheme.fromSeed(seedColor: c.primary, brightness: Brightness.dark).copyWith(
      primary: c.primary,
      onPrimary: c.onPrimary,
      primaryContainer: c.primarySoft,
      onPrimaryContainer: c.primaryStrong,
      secondary: c.primary,
      onSecondary: c.onPrimary,
      surface: c.surface,
      onSurface: c.textPrimary,
      onSurfaceVariant: c.textSecondary,
      surfaceContainerLowest: c.surface,
      surfaceContainerLow: c.surface,
      surfaceContainer: c.surfaceMuted,
      surfaceContainerHigh: c.surfaceMuted,
      surfaceContainerHighest: c.surfaceMuted,
      outline: c.border,
      outlineVariant: c.border,
      error: c.danger,
      onError: c.onPrimary,
      errorContainer: c.dangerSoft,
      onErrorContainer: c.danger,
      shadow: c.shadowColor,
    );

    final base = ThemeData(colorScheme: scheme, useMaterial3: true, brightness: Brightness.dark);
    final textTheme = _textTheme(base.textTheme, c);

    final fieldBorder = OutlineInputBorder(
      borderRadius: AppRadius.all(AppRadius.md),
      borderSide: BorderSide(color: c.border),
    );

    return base.copyWith(
      extensions: const <ThemeExtension<dynamic>>[c],
      textTheme: textTheme,
      scaffoldBackgroundColor: c.canvas,
      canvasColor: c.canvas,
      dividerColor: c.border,
      cardTheme: CardThemeData(
        elevation: 0,
        color: c.surface,
        surfaceTintColor: Colors.transparent,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(borderRadius: AppRadius.all(AppRadius.lg), side: BorderSide(color: c.border)),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: c.surface,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        barrierColor: c.shadowColor.withValues(alpha: 0.62),
        shape: RoundedRectangleBorder(borderRadius: AppRadius.all(AppRadius.xl), side: BorderSide(color: c.border)),
        titleTextStyle: textTheme.headlineSmall,
        contentTextStyle: textTheme.bodyMedium,
      ),
      appBarTheme: AppBarTheme(
        backgroundColor: c.navBackground,
        surfaceTintColor: Colors.transparent,
        foregroundColor: c.textPrimary,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: textTheme.titleLarge,
      ),
      dividerTheme: DividerThemeData(color: c.border, thickness: 1, space: 1),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: c.primary,
          foregroundColor: c.onPrimary,
          disabledBackgroundColor: c.surfaceMuted,
          disabledForegroundColor: c.textMuted,
          elevation: 0,
          minimumSize: const Size(0, AppSizes.controlHeight),
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          shape: RoundedRectangleBorder(borderRadius: AppRadius.all(AppRadius.md)),
          textStyle: textTheme.labelLarge,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: c.textPrimary,
          disabledForegroundColor: c.textMuted,
          backgroundColor: c.surface,
          side: BorderSide(color: c.borderStrong),
          minimumSize: const Size(0, AppSizes.controlHeight),
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
          shape: RoundedRectangleBorder(borderRadius: AppRadius.all(AppRadius.md)),
          textStyle: textTheme.labelLarge,
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: c.primary,
          disabledForegroundColor: c.textMuted,
          minimumSize: const Size(0, AppSizes.controlHeightSm),
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
          shape: RoundedRectangleBorder(borderRadius: AppRadius.all(AppRadius.sm)),
          textStyle: textTheme.labelLarge,
        ),
      ),
      iconButtonTheme: IconButtonThemeData(
        style: IconButton.styleFrom(
          foregroundColor: c.textSecondary,
          disabledForegroundColor: c.textMuted,
          highlightColor: c.surfaceHover,
          shape: RoundedRectangleBorder(borderRadius: AppRadius.all(AppRadius.sm)),
        ),
      ),
      iconTheme: IconThemeData(color: c.textSecondary, size: 20),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: c.surface,
        isDense: true,
        border: fieldBorder,
        enabledBorder: fieldBorder,
        focusedBorder: fieldBorder.copyWith(borderSide: BorderSide(color: c.primary, width: 1.6)),
        errorBorder: fieldBorder.copyWith(borderSide: BorderSide(color: c.danger)),
        focusedErrorBorder: fieldBorder.copyWith(borderSide: BorderSide(color: c.danger, width: 1.6)),
        disabledBorder: fieldBorder.copyWith(borderSide: BorderSide(color: c.border)),
        contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm + 2, vertical: AppSpacing.sm),
        hintStyle: textTheme.bodyMedium?.copyWith(color: c.textMuted),
        labelStyle: textTheme.bodyMedium?.copyWith(color: c.textSecondary),
        floatingLabelStyle: textTheme.labelMedium?.copyWith(color: c.primary),
        helperStyle: textTheme.bodySmall?.copyWith(color: c.textMuted),
        errorStyle: textTheme.bodySmall?.copyWith(color: c.danger, fontWeight: FontWeight.w500),
        errorMaxLines: 3,
        prefixIconColor: c.textMuted,
        suffixIconColor: c.textMuted,
      ),
      checkboxTheme: CheckboxThemeData(
        shape: RoundedRectangleBorder(borderRadius: AppRadius.all(4)),
        side: BorderSide(color: c.borderStrong, width: 1.4),
        fillColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected) ? c.primary : Colors.transparent,
        ),
      ),
      switchTheme: SwitchThemeData(
        thumbColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected) ? c.onPrimary : c.textSecondary,
        ),
        trackColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected) ? c.primary : c.surfaceMuted,
        ),
        trackOutlineColor: WidgetStateProperty.resolveWith(
          (states) => states.contains(WidgetState.selected) ? Colors.transparent : c.borderStrong,
        ),
      ),
      tooltipTheme: TooltipThemeData(
        decoration: BoxDecoration(color: c.textPrimary, borderRadius: AppRadius.all(AppRadius.xs)),
        textStyle: textTheme.bodySmall?.copyWith(color: c.canvas),
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xs, vertical: 6),
        waitDuration: const Duration(milliseconds: 300),
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: c.surfaceMuted,
        contentTextStyle: textTheme.bodyMedium,
        actionTextColor: c.primaryStrong,
        behavior: SnackBarBehavior.floating,
        elevation: 0,
        shape: RoundedRectangleBorder(borderRadius: AppRadius.all(AppRadius.md), side: BorderSide(color: c.border)),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(
        color: c.primary,
        linearTrackColor: c.surfaceMuted,
        circularTrackColor: c.surfaceMuted,
      ),
    );
  }

  static TextTheme _textTheme(TextTheme base, AppColors c) => base.copyWith(
        headlineMedium: TextStyle(fontSize: 24, fontWeight: FontWeight.w600, height: 1.2, color: c.textPrimary),
        headlineSmall: TextStyle(fontSize: 20, fontWeight: FontWeight.w600, height: 1.2, color: c.textPrimary),
        titleLarge: TextStyle(fontSize: 17, fontWeight: FontWeight.w600, height: 1.3, color: c.textPrimary),
        titleMedium: TextStyle(fontSize: 15, fontWeight: FontWeight.w600, height: 1.35, color: c.textPrimary),
        titleSmall: TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600, height: 1.35, color: c.textPrimary),
        bodyLarge: TextStyle(fontSize: 15, height: 1.5, color: c.textPrimary),
        bodyMedium: TextStyle(fontSize: 14, height: 1.5, color: c.textPrimary),
        bodySmall: TextStyle(fontSize: 13, height: 1.45, color: c.textSecondary),
        labelLarge: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: c.textPrimary),
        labelMedium: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: c.textSecondary),
        labelSmall: TextStyle(fontSize: 11.5, fontWeight: FontWeight.w600, letterSpacing: 0.6, color: c.textMuted),
      );
}
