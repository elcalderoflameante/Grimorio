import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

// ── Paleta mágica — El Caldero Flameante ──────────────────────────────────
const kBgDeep = Color(0xFF080612);
const kBgMid = Color(0xFF0F0820);
const kBgCard = Color(0xFF120A28);
const kGold = Color(0xFFD4A017);
const kGoldLight = Color(0xFFFFD060);
const kGoldDark = Color(0xFF8B6400);
const kParchment = Color(0xFFF5E6C8);
const kParchmentDim = Color(0xFFDDC48A);
const kBrown = Color(0xFF3E2008);

class AppTheme {
  static ThemeData magicTheme() {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      fontFamily: 'Lato',
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
      textTheme: ThemeData.dark().textTheme
          .apply(fontFamily: 'Lato')
          .copyWith(
            titleLarge: TextStyle(
              fontFamily: 'Cinzel',
              color: kParchment,
              fontSize: 18,
              fontWeight: FontWeight.w700,
              letterSpacing: 1,
            ),
            titleMedium: TextStyle(
              fontFamily: 'Lato',
              color: kParchment,
              fontSize: 15,
              fontWeight: FontWeight.w600,
            ),
            titleSmall: TextStyle(
              fontFamily: 'Lato',
              color: kParchment,
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
            bodyLarge: TextStyle(
              fontFamily: 'Lato',
              color: kParchment,
              fontSize: 15,
            ),
            bodyMedium: TextStyle(
              fontFamily: 'Lato',
              color: kParchment,
              fontSize: 14,
            ),
            bodySmall: TextStyle(
              fontFamily: 'Lato',
              color: kParchmentDim,
              fontSize: 12,
            ),
            labelLarge: TextStyle(
              fontFamily: 'Lato',
              color: kParchment,
              fontSize: 14,
              fontWeight: FontWeight.w600,
            ),
            labelMedium: TextStyle(
              fontFamily: 'Lato',
              color: kParchmentDim,
              fontSize: 12,
            ),
            labelSmall: TextStyle(
              fontFamily: 'Lato',
              color: kParchmentDim,
              fontSize: 11,
            ),
          ),
      appBarTheme: AppBarTheme(
        elevation: 0,
        centerTitle: true,
        backgroundColor: kBgMid,
        surfaceTintColor: Colors.transparent,
        foregroundColor: kGold,
        titleTextStyle: TextStyle(
          fontFamily: 'Cinzel',
          color: kGold,
          fontSize: 17,
          fontWeight: FontWeight.w700,
          letterSpacing: 2,
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
        labelStyle: TextStyle(
          fontFamily: 'Cinzel',
          fontSize: 12,
          fontWeight: FontWeight.w700,
          letterSpacing: 1,
        ),
        unselectedLabelStyle: TextStyle(
          fontFamily: 'Cinzel',
          fontSize: 12,
          fontWeight: FontWeight.w500,
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
            return TextStyle(
              fontFamily: 'Cinzel',
              color: kGold,
              fontSize: 11,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.5,
            );
          }
          return TextStyle(
            fontFamily: 'Cinzel',
            color: kParchment.withAlpha(110),
            fontSize: 11,
          );
        }),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: ButtonStyle(
          minimumSize: WidgetStateProperty.all(const Size(48, 52)),
          backgroundColor: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.disabled)) return kBgCard;
            return kGold;
          }),
          foregroundColor: WidgetStateProperty.resolveWith(
            (states) => states.contains(WidgetState.disabled)
                ? kParchmentDim.withAlpha(120)
                : kBrown,
          ),
          textStyle: WidgetStateProperty.all(
            TextStyle(
              fontFamily: 'Cinzel',
              fontSize: 13,
              fontWeight: FontWeight.w700,
              letterSpacing: 1,
            ),
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
          minimumSize: const Size(48, 52),
          backgroundColor: kGold,
          foregroundColor: kBrown,
          textStyle: TextStyle(
            fontFamily: 'Cinzel',
            fontSize: 13,
            fontWeight: FontWeight.w700,
            letterSpacing: 1,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(48, 52),
          foregroundColor: kGold,
          side: BorderSide(color: kGoldDark.withAlpha(150), width: 1),
          textStyle: TextStyle(
            fontFamily: 'Cinzel',
            fontSize: 13,
            fontWeight: FontWeight.w600,
            letterSpacing: 1,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          minimumSize: const Size(48, 48),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          textStyle: TextStyle(
            fontFamily: 'Lato',
            fontSize: 15,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
      iconButtonTheme: const IconButtonThemeData(
        style: ButtonStyle(
          minimumSize: WidgetStatePropertyAll(Size(48, 48)),
          iconSize: WidgetStatePropertyAll(26),
        ),
      ),
      radioTheme: const RadioThemeData(
        materialTapTargetSize: MaterialTapTargetSize.padded,
        visualDensity: VisualDensity.standard,
      ),
      checkboxTheme: const CheckboxThemeData(
        materialTapTargetSize: MaterialTapTargetSize.padded,
        visualDensity: VisualDensity.standard,
      ),
      dividerTheme: DividerThemeData(
        color: kGoldDark.withAlpha(60),
        thickness: 1,
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: kBgCard,
        contentTextStyle: TextStyle(
          fontFamily: 'Lato',
          color: kParchment,
          fontSize: 14,
        ),
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
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: kBgCard,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 18,
        ),
        labelStyle: const TextStyle(color: kParchmentDim),
        hintStyle: TextStyle(color: kParchmentDim.withAlpha(160)),
        prefixIconColor: kGold,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: kGoldDark),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide(color: kGoldDark.withAlpha(110)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: kGold, width: 1.5),
        ),
        disabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide(color: kGoldDark.withAlpha(45)),
        ),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: kBgCard,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: BorderSide(color: kGoldDark.withAlpha(140)),
        ),
        titleTextStyle: const TextStyle(
          fontFamily: 'Cinzel',
          color: kGold,
          fontSize: 20,
          fontWeight: FontWeight.w700,
        ),
        contentTextStyle: const TextStyle(
          fontFamily: 'Lato',
          color: kParchment,
          fontSize: 15,
          height: 1.5,
        ),
      ),
      listTileTheme: const ListTileThemeData(
        textColor: kParchment,
        iconColor: kGold,
      ),
    );
  }

  // Compatibilidad con llamadas existentes
  static ThemeData lightTheme() => magicTheme();
  static ThemeData darkTheme() => magicTheme();
}
