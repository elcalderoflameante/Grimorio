import 'package:flutter/material.dart';
import '../constants/app_colors.dart';

class AppTheme {
  static ThemeData lightTheme() {
    return ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme(
        brightness: Brightness.light,
        primary: AppColors.primary,
        onPrimary: AppColors.onPrimary,
        primaryContainer: AppColors.primaryContainer,
        onPrimaryContainer: AppColors.onPrimaryContainer,
        secondary: AppColors.secondary,
        onSecondary: AppColors.onSecondary,
        secondaryContainer: AppColors.secondaryContainer,
        onSecondaryContainer: AppColors.onSecondaryContainer,
        tertiary: AppColors.tertiary,
        onTertiary: AppColors.onTertiary,
        tertiaryContainer: AppColors.tertiaryContainer,
        onTertiaryContainer: AppColors.onTertiaryContainer,
        error: AppColors.error,
        onError: Colors.white,
        errorContainer: const Color(0xFFF9DEDC),
        onErrorContainer: const Color(0xFF410E0B),
        surface: AppColors.surface,
        onSurface: AppColors.onSurface,
        outline: AppColors.outline,
        outlineVariant: AppColors.outlineVariant,
        shadow: AppColors.scrim,
        scrim: AppColors.scrim,
        inverseSurface: AppColors.inverseSurface,
        onInverseSurface: AppColors.inverseOnSurface,
        inversePrimary: AppColors.inversePrimary,
      ),
      appBarTheme: AppBarTheme(
        elevation: 0,
        centerTitle: true,
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.onPrimary,
        titleTextStyle: const TextStyle(
          fontSize: 20,
          fontWeight: FontWeight.w500,
          color: AppColors.onPrimary,
        ),
      ),
      floatingActionButtonTheme: FloatingActionButtonThemeData(
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.onPrimary,
        elevation: 4,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: AppColors.onPrimary,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.primary,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.primary,
        ),
      ),
      cardTheme: CardThemeData(
        elevation: 1,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
        color: AppColors.surface,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.surfaceDim,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.outline),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.outlineVariant),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.primary, width: 2),
        ),
        contentPadding: const EdgeInsets.all(16),
      ),
    );
  }

  static ThemeData darkTheme() {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: ColorScheme(
        brightness: Brightness.dark,
        primary: AppColors.inversePrimary,
        onPrimary: AppColors.primary,
        primaryContainer: Color.alphaBlend(
          AppColors.primary.withAlpha(51),
          AppColors.inverseSurface,
        ),
        onPrimaryContainer: AppColors.primaryContainer,
        secondary: const Color(0xFFCFC7D8),
        onSecondary: const Color(0xFF332D41),
        secondaryContainer: const Color(0xFF4A4458),
        onSecondaryContainer: const Color(0xFFEBDEFF),
        tertiary: const Color(0xFFFFC0CB),
        onTertiary: const Color(0xFF492532),
        tertiaryContainer: const Color(0xFF633B48),
        onTertiaryContainer: const Color(0xFFFFD8E4),
        error: const Color(0xFFF2B8B5),
        onError: const Color(0xFF601410),
        errorContainer: const Color(0xFF8C1D18),
        onErrorContainer: const Color(0xFFF9DEDC),
        surface: AppColors.inverseSurface,
        onSurface: AppColors.inverseOnSurface,
        outline: const Color(0xFF938F99),
        outlineVariant: const Color(0xFF49454E),
        shadow: AppColors.scrim,
        scrim: AppColors.scrim,
        inverseSurface: AppColors.surface,
        onInverseSurface: AppColors.onSurface,
        inversePrimary: AppColors.primary,
      ),
    );
  }
}
