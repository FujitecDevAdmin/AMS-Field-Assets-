import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/auth/auth_controller.dart';
import 'features/auth/sign_in_page.dart';

/// The audit app does one job: somebody stands in front of an asset and
/// confirms it is there. See docs/05FLUTTERMOBILEAUDIT.md.
void main() {
  runApp(const ProviderScope(child: AmsAuditApp()));
}

/// Fujitec red, sampled from the brand mark in `assets/images/fujitec-logo.png`.
const Color fujitecRed = Color(0xFFCA0012);
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
      floatingLabelStyle: const TextStyle(color: fujitecRed, fontWeight: FontWeight.w600),
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

    return state.isSignedIn ? const _SignedInPage() : const SignInPage();
  }
}

/// Placeholder home. Two screens ship — Scan & Verify and Offline Queue — and
/// neither can be written before the local database and the outbox exist
/// (§3, §4). This page is replaced by the router when they do.
class _SignedInPage extends ConsumerWidget {
  const _SignedInPage();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(authControllerProvider);
    final session = state.session!;

    return Scaffold(
      appBar: AppBar(
        title: const Text('AMS Audit'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Sign out',
            onPressed: () => ref.read(authControllerProvider.notifier).signOut(),
          ),
        ],
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Image.asset('assets/images/fujitec-logo.png', width: 200),
              const SizedBox(height: 28),
              Text(
                'Signed in as ${session.displayName}',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 8),
              Text(
                'Scan & Verify and Offline Queue are not built yet.',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              if (session.mustChangePassword) ...[
                const SizedBox(height: 20),
                const _MustChangePasswordNotice(),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

/// The API says the password must change before anything else. There is no
/// change-password screen on the device yet, so this says so plainly rather
/// than letting a technician wonder why captures are refused later.
class _MustChangePasswordNotice extends StatelessWidget {
  const _MustChangePasswordNotice();

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: scheme.errorContainer,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        'Your password must be changed in the web app before you can record verifications.',
        textAlign: TextAlign.center,
        style: TextStyle(color: scheme.onErrorContainer, fontSize: 13),
      ),
    );
  }
}
