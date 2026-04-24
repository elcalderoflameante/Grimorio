import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';

// ── Paleta mágica — El Caldero Flameante ──────────────────────────────────
const kBgDeep   = Color(0xFF080612);
const kBgMid    = Color(0xFF0F0820);
const kBgCard   = Color(0xFF120A28);
const kGold     = Color(0xFFD4A017);
const kGoldLight = Color(0xFFFFD060);
const kGoldDark  = Color(0xFF8B6400);
const kParchment = Color(0xFFF5E6C8);
const kParchmentDim = Color(0xFFDDC48A);
const kBrown    = Color(0xFF3E2008);

class AppTheme {
  static ThemeData magicTheme() {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      scaffoldBackgroundColor: kBgDeep,
      colorScheme: ColorScheme(
        brightness: Brightness.dark,
        primary: kGold,
        onPrimary: kBrown,
        primaryContainer: const Color(0xFF1A1035),
        onPrimaryContainer: kGoldLight,
        secondary: const Color(0xFFC8860A),
        onSecondary: kBrown,
        secondaryContainer: kBgCard,
        onSecondaryContainer: kParchment,
        tertiary: kGoldLight,
        onTertiary: kBrown,
        tertiaryContainer: kBgMid,
        onTertiaryContainer: kParchment,
        error: const Color(0xFFFF6B6B),
        onError: const Color(0xFF3E0000),
        errorContainer: const Color(0xFF6B1A1A),
        onErrorContainer: const Color(0xFFFFCDD2),
        surface: kBgDeep,
        onSurface: kParchment,
        surfaceContainerHighest: kBgCard,
        surfaceContainerHigh: kBgCard,
        surfaceContainer: kBgMid,
        surfaceContainerLow: kBgMid,
        surfaceContainerLowest: kBgDeep,
        onSurfaceVariant: kParchmentDim,
        outline: kGoldDark,
        outlineVariant: const Color(0xFF3A2800),
        shadow: Colors.black,
        scrim: Colors.black,
        inverseSurface: kParchment,
        onInverseSurface: kBrown,
        inversePrimary: kGoldDark,
      ),
      textTheme: GoogleFonts.latoTextTheme().copyWith(
        titleLarge: GoogleFonts.cinzel(
          color: kParchment, fontSize: 18,
          fontWeight: FontWeight.w700, letterSpacing: 1,
        ),
        titleMedium: GoogleFonts.lato(color: kParchment, fontSize: 15, fontWeight: FontWeight.w600),
        titleSmall: GoogleFonts.lato(color: kParchment, fontSize: 13, fontWeight: FontWeight.w600),
        bodyLarge: GoogleFonts.lato(color: kParchment, fontSize: 15),
        bodyMedium: GoogleFonts.lato(color: kParchment, fontSize: 14),
        bodySmall: GoogleFonts.lato(color: kParchmentDim, fontSize: 12),
        labelLarge: GoogleFonts.lato(color: kParchment, fontSize: 14, fontWeight: FontWeight.w600),
        labelMedium: GoogleFonts.lato(color: kParchmentDim, fontSize: 12),
        labelSmall: GoogleFonts.lato(color: kParchmentDim, fontSize: 11),
      ),
      appBarTheme: AppBarTheme(
        elevation: 0,
        centerTitle: true,
        backgroundColor: kBgMid,
        surfaceTintColor: Colors.transparent,
        foregroundColor: kGold,
        titleTextStyle: GoogleFonts.cinzel(
          color: kGold, fontSize: 17,
          fontWeight: FontWeight.w700, letterSpacing: 2,
        ),
        iconTheme: const IconThemeData(color: kGold),
        actionsIconTheme: const IconThemeData(color: kGold),
        systemOverlayStyle: SystemUiOverlayStyle.light,
      ),
      cardTheme: CardThemeData(
        color: kBgCard,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(color: kGoldDark.withAlpha(80), width: 1),
        ),
      ),
      tabBarTheme: TabBarThemeData(
        labelColor: kGold,
        unselectedLabelColor: kParchmentDim,
        indicatorColor: kGold,
        indicatorSize: TabBarIndicatorSize.tab,
        labelStyle: GoogleFonts.cinzel(
          fontSize: 12, fontWeight: FontWeight.w700, letterSpacing: 1,
        ),
        unselectedLabelStyle: GoogleFonts.cinzel(
          fontSize: 12, fontWeight: FontWeight.w500,
        ),
        dividerColor: kGoldDark.withAlpha(50),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: kBgMid,
        indicatorColor: kGoldDark.withAlpha(90),
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        height: 64,
        iconTheme: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return const IconThemeData(color: kGold, size: 24);
          }
          return IconThemeData(color: kParchment.withAlpha(110), size: 24);
        }),
        labelTextStyle: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return GoogleFonts.cinzel(
              color: kGold, fontSize: 11,
              fontWeight: FontWeight.w700, letterSpacing: 0.5,
            );
          }
          return GoogleFonts.cinzel(
            color: kParchment.withAlpha(110), fontSize: 11,
          );
        }),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: ButtonStyle(
          backgroundColor: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.disabled)) return kBgCard;
            return kGold;
          }),
          foregroundColor: WidgetStateProperty.all(kBrown),
          textStyle: WidgetStateProperty.all(
            GoogleFonts.cinzel(fontSize: 13, fontWeight: FontWeight.w700, letterSpacing: 1),
          ),
          shape: WidgetStateProperty.all(
            RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          ),
          padding: WidgetStateProperty.all(
            const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          ),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: kGold,
          foregroundColor: kBrown,
          textStyle: GoogleFonts.cinzel(fontSize: 13, fontWeight: FontWeight.w700, letterSpacing: 1),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: kGold,
          side: BorderSide(color: kGoldDark.withAlpha(150), width: 1),
          textStyle: GoogleFonts.cinzel(fontSize: 13, fontWeight: FontWeight.w600, letterSpacing: 1),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
      ),
      dividerTheme: DividerThemeData(
        color: kGoldDark.withAlpha(60),
        thickness: 1,
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: kBgCard,
        contentTextStyle: GoogleFonts.lato(color: kParchment, fontSize: 14),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(10),
          side: BorderSide(color: kGoldDark.withAlpha(80)),
        ),
      ),
      badgeTheme: const BadgeThemeData(
        backgroundColor: kGold,
        textColor: kBrown,
      ),
      progressIndicatorTheme: const ProgressIndicatorThemeData(color: kGold),
    );
  }

  // Compatibilidad con llamadas existentes
  static ThemeData lightTheme() => magicTheme();
  static ThemeData darkTheme() => magicTheme();
}
