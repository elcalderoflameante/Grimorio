import 'package:flutter/material.dart';

abstract final class AppTheme {
  static const Color primary = Color(0xFF1890FF);
  static const Color success = Color(0xFF52C41A);
  static const Color surface = Color(0xFF1F1F1F);

  static ThemeData get dark => ThemeData(
    useMaterial3: true,
    brightness: Brightness.dark,
    colorScheme: ColorScheme.fromSeed(
      seedColor: primary,
      brightness: Brightness.dark,
    ),
    scaffoldBackgroundColor: const Color(0xFF141414),
    cardTheme: const CardThemeData(color: surface),
  );
}
