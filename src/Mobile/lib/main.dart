import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/auth/auth_controller.dart';
import 'features/auth/sign_in_page.dart';
import 'features/auth/change_password_page.dart';
import 'features/dashboard/auditor_shell.dart';

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

/// Material 3 derives every surface from the seed, which with a red seed tints
/// the whole app pink and desaturates the accent into brown — the button came
/// out a muddy maroon that is not the Fujitec mark. Primary and the surfaces
/// are therefore stated outright; the seed still generates the rest.
ThemeData _buildTheme() {
  final scheme = ColorScheme.fromSeed(
    seedColor: fujitecRed,
    primary: fujitecRed,
    onPrimary: Colors.white,
    surface: _surface,
  );

  return ThemeData(
    colorScheme: scheme,
    useMaterial3: true,
    scaffoldBackgroundColor: _canvas,
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: _fieldFill,
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 18),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide.none,
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide.none,
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: fujitecRed, width: 1.5),
      ),
      floatingLabelStyle: const TextStyle(
        color: fujitecRed,
        fontWeight: FontWeight.w600,
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size.fromHeight(54),
        textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      ),
    ),
  );
}

class AmsAuditApp extends StatelessWidget {
  const AmsAuditApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AMS Audit',
      debugShowCheckedModeBanner: false,
      theme: _buildTheme(),
      home: const _AuthGate(),
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
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    if (!state.isSignedIn) return const SignInPage();
    if (state.session!.mustChangePassword) return const ChangePasswordPage();
    return const AuditorShell();
  }
}

/// Placeholder home. Two screens ship — Scan & Verify and Offline Queue — and
/// neither can be written before the local database and the outbox exist
/// (§3, §4). This page is replaced by the router when they do.
