import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import 'core/auth/auth_controller.dart';
import 'core/theme/theme_mode_controller.dart';
import 'features/auth/sign_in_page.dart';
import 'features/auth/change_password_page.dart';
import 'features/dashboard/auditor_shell.dart';
import 'shared/widgets/techy_loader.dart';

/// The audit app does one job: somebody stands in front of an asset and
/// confirms it is there. See docs/05FLUTTERMOBILEAUDIT.md.
void main() {
  runApp(const ProviderScope(child: AmsAuditApp()));
}

/// Fujitec red, sampled from the brand mark in `assets/images/fujitec-logo.png`.
const Color fujitecRed = Color(0xFFD01126);
const Color _surface = Color(0xFFFFFFFF);
const Color _canvas = Color(0xFFF6F7F9);
const Color _fieldFill = Color(0xFFF1F3F5);
const double _applicationFontScale = 0.80;

/// Application-wide type scale. Screens should use the semantic styles from
/// `Theme.of(context).textTheme` instead of declaring arbitrary font sizes.
TextTheme _buildTextTheme() {
  return GoogleFonts.poppinsTextTheme().copyWith(
    displayLarge: GoogleFonts.poppins(
      fontSize: 32,
      fontWeight: FontWeight.w700,
    ),
    displayMedium: GoogleFonts.poppins(
      fontSize: 28,
      fontWeight: FontWeight.w700,
    ),
    displaySmall: GoogleFonts.poppins(
      fontSize: 24,
      fontWeight: FontWeight.w700,
    ),
    headlineLarge: GoogleFonts.poppins(
      fontSize: 24,
      fontWeight: FontWeight.w700,
    ),
    headlineMedium: GoogleFonts.poppins(
      fontSize: 22,
      fontWeight: FontWeight.w700,
    ),
    headlineSmall: GoogleFonts.poppins(
      fontSize: 20,
      fontWeight: FontWeight.w600,
    ),
    titleLarge: GoogleFonts.poppins(fontSize: 18, fontWeight: FontWeight.w600),
    titleMedium: GoogleFonts.poppins(fontSize: 16, fontWeight: FontWeight.w600),
    titleSmall: GoogleFonts.poppins(fontSize: 14, fontWeight: FontWeight.w600),
    bodyLarge: GoogleFonts.poppins(fontSize: 16, fontWeight: FontWeight.w400),
    bodyMedium: GoogleFonts.poppins(fontSize: 14, fontWeight: FontWeight.w400),
    bodySmall: GoogleFonts.poppins(fontSize: 12, fontWeight: FontWeight.w400),
    labelLarge: GoogleFonts.poppins(fontSize: 14, fontWeight: FontWeight.w600),
    labelMedium: GoogleFonts.poppins(fontSize: 12, fontWeight: FontWeight.w600),
    labelSmall: GoogleFonts.poppins(fontSize: 11, fontWeight: FontWeight.w500),
  );
}

/// Material 3 derives every surface from the seed, which with a red seed tints
/// the whole app pink and desaturates the accent into brown — the button came
/// out a muddy maroon that is not the Fujitec mark. Primary and the surfaces
/// are therefore stated outright; the seed still generates the rest.
ThemeData _buildTheme({Brightness brightness = Brightness.light}) {
  final isDark = brightness == Brightness.dark;
  final surface = isDark ? const Color(0xFF1D1B1C) : _surface;
  final canvas = isDark ? const Color(0xFF121112) : _canvas;
  final fieldFill = isDark ? const Color(0xFF292627) : _fieldFill;
  final scheme = ColorScheme.fromSeed(
    seedColor: fujitecRed,
    brightness: brightness,
    primary: fujitecRed,
    onPrimary: Colors.white,
    surface: surface,
  );
  final textTheme = _buildTextTheme();

  return ThemeData(
    brightness: brightness,
    colorScheme: scheme,
    useMaterial3: true,
    splashFactory: InkRipple.splashFactory,
    hoverColor: const Color(0x14D01126),
    splashColor: const Color(0x24D01126),
    highlightColor: const Color(0x12D01126),
    fontFamily: GoogleFonts.poppins().fontFamily,
    textTheme: textTheme,
    scaffoldBackgroundColor: canvas,
    cardTheme: CardThemeData(
      color: surface,
      surfaceTintColor: Colors.transparent,
      shadowColor: isDark ? Colors.black54 : const Color(0x1A000000),
    ),
    popupMenuTheme: PopupMenuThemeData(
      color: surface,
      surfaceTintColor: Colors.transparent,
      textStyle: textTheme.bodyMedium?.copyWith(color: scheme.onSurface),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
    ),
    dividerColor: isDark ? const Color(0xFF3A3436) : const Color(0xFFE4E7EC),
    appBarTheme: AppBarTheme(
      titleTextStyle: textTheme.titleLarge?.copyWith(color: scheme.onSurface),
      toolbarTextStyle: textTheme.bodyMedium,
    ),
    inputDecorationTheme: InputDecorationTheme(
      isDense: true,
      filled: true,
      fillColor: fieldFill,
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 13),
      prefixIconConstraints: const BoxConstraints(minWidth: 44, minHeight: 44),
      suffixIconConstraints: const BoxConstraints(minWidth: 44, minHeight: 44),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: BorderSide.none,
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: BorderSide.none,
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: fujitecRed, width: 1.5),
      ),
      labelStyle: textTheme.bodyMedium,
      hintStyle: textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
      helperStyle: textTheme.bodySmall,
      errorStyle: textTheme.bodySmall?.copyWith(color: scheme.error),
      floatingLabelStyle: textTheme.labelMedium?.copyWith(
        color: fujitecRed,
        fontWeight: FontWeight.w600,
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size(0, 54),
        textStyle: textTheme.labelLarge,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      ),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(textStyle: textTheme.labelLarge),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(textStyle: textTheme.labelLarge),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(textStyle: textTheme.labelLarge),
    ),
    dialogTheme: DialogThemeData(
      titleTextStyle: textTheme.headlineSmall?.copyWith(
        color: scheme.onSurface,
      ),
      contentTextStyle: textTheme.bodyMedium?.copyWith(color: scheme.onSurface),
    ),
    dataTableTheme: DataTableThemeData(
      headingTextStyle: textTheme.labelMedium,
      dataTextStyle: textTheme.bodySmall,
    ),
    listTileTheme: ListTileThemeData(
      titleTextStyle: textTheme.bodyLarge,
      subtitleTextStyle: textTheme.bodySmall,
      leadingAndTrailingTextStyle: textTheme.labelMedium,
    ),
    bottomNavigationBarTheme: BottomNavigationBarThemeData(
      selectedLabelStyle: textTheme.labelSmall,
      unselectedLabelStyle: textTheme.labelSmall,
    ),
    navigationBarTheme: NavigationBarThemeData(
      labelTextStyle: WidgetStatePropertyAll(textTheme.labelSmall),
    ),
    tabBarTheme: TabBarThemeData(
      labelStyle: textTheme.labelMedium,
      unselectedLabelStyle: textTheme.labelMedium,
    ),
    chipTheme: ChipThemeData(
      labelStyle: textTheme.labelMedium!,
      side: BorderSide(color: scheme.outlineVariant),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
    ),
    snackBarTheme: SnackBarThemeData(
      contentTextStyle: textTheme.bodyMedium?.copyWith(color: Colors.white),
    ),
    tooltipTheme: TooltipThemeData(textStyle: textTheme.bodySmall),
  );
}

class AmsAuditApp extends ConsumerWidget {
  const AmsAuditApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return MaterialApp(
      title: 'AMS Audit',
      debugShowCheckedModeBanner: false,
      theme: _buildTheme(),
      darkTheme: _buildTheme(brightness: Brightness.dark),
      themeMode: ref.watch(themeModeProvider),
      builder: (context, child) {
        final mediaQuery = MediaQuery.of(context);
        final systemScale = mediaQuery.textScaler.scale(14) / 14;
        return MediaQuery(
          data: mediaQuery.copyWith(
            textScaler: TextScaler.linear(systemScale * _applicationFontScale),
          ),
          child: child ?? const SizedBox.shrink(),
        );
      },
      home: const _SplashGate(),
    );
  }
}

class _SplashGate extends StatefulWidget {
  const _SplashGate();

  @override
  State<_SplashGate> createState() => _SplashGateState();
}

class _SplashGateState extends State<_SplashGate> {
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _timer = Timer(const Duration(seconds: 5), () {
      if (mounted) setState(() {});
    });
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    unawaited(
      precacheImage(const AssetImage('assets/images/ams-splash.png'), context),
    );
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_timer?.isActive == false) return const _AuthGate();
    return Scaffold(
      backgroundColor: Colors.white,
      body: SizedBox.expand(
        child: Image.asset(
          'assets/images/ams-splash.png',
          fit: BoxFit.cover,
          alignment: Alignment.center,
          filterQuality: FilterQuality.high,
        ),
      ),
    );
  }
}

/// Sign-in or the app, and a spinner while the stored session is read.
///
/// The spinner matters: without it the app shows the sign-in screen for a frame
/// to somebody who is already signed in, which on a handset reads as being
/// signed out.
class _AuthGate extends ConsumerWidget {
  const _AuthGate();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(authControllerProvider);

    if (state.restoring) {
      return const Scaffold(body: Center(child: TechyLoader(size: 40)));
    }

    if (!state.isSignedIn) return const SignInPage();
    if (state.session!.mustChangePassword) return const ChangePasswordPage();
    return const AuditorShell();
  }
}

/// Placeholder home. Two screens ship — Scan & Verify and Offline Queue — and
/// neither can be written before the local database and the outbox exist
/// (§3, §4). This page is replaced by the router when they do.
