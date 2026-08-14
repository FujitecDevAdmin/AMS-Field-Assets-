import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../main.dart' show fujitecRed;

/// Sign in. Two steps, because the API has two: an enrolled user gets a
/// challenge token and no access token, and is not signed in until the code is
/// verified.
///
/// A branded hero with the form on a sheet that overlaps it — the same shape as
/// the web sign-in, so the two do not look like different products.
class SignInPage extends ConsumerStatefulWidget {
  const SignInPage({super.key});

  @override
  ConsumerState<SignInPage> createState() => _SignInPageState();
}

class _SignInPageState extends ConsumerState<SignInPage> {
  final _username = TextEditingController();
  final _password = TextEditingController();
  final _code = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  bool _obscure = true;

  @override
  void dispose() {
    _username.dispose();
    _password.dispose();
    _code.dispose();
    super.dispose();
  }

  Future<void> _submitCredentials() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }
    await ref
        .read(authControllerProvider.notifier)
        .signIn(username: _username.text, password: _password.text);
    _password.clear();
  }

  Future<void> _submitCode() async {
    if (_code.text.trim().isEmpty) {
      return;
    }
    await ref.read(authControllerProvider.notifier).verifyMfaCode(_code.text);
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(authControllerProvider);
    final isMfa = state.stage == SignInStage.mfa;

    return Scaffold(
      body: Column(
        children: [
          const _Hero(),
          Expanded(
            child: Transform.translate(
              offset: const Offset(0, -28),
              child: Container(
                decoration: const BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
                ),
                child: SingleChildScrollView(
                  // The keyboard covers half a handset; without this the
                  // password field sits behind it and the button is off-screen.
                  padding: EdgeInsets.fromLTRB(
                    24,
                    28,
                    24,
                    24 + MediaQuery.viewInsetsOf(context).bottom,
                  ),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _Title(isMfa: isMfa),
                        const SizedBox(height: 24),
                        if (isMfa) ..._mfaFields() else ..._credentialFields(),
                        if (state.error case final String message) ...[
                          const SizedBox(height: 16),
                          _ErrorBox(message: message),
                        ],
                        const SizedBox(height: 28),
                        FilledButton(
                          onPressed:
                              state.busy ? null : (isMfa ? _submitCode : _submitCredentials),
                          child: state.busy
                              ? const SizedBox(
                                  width: 20,
                                  height: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white,
                                  ),
                                )
                              : Text(isMfa ? 'Verify' : 'Sign in'),
                        ),
                        if (isMfa) ...[
                          const SizedBox(height: 6),
                          TextButton(
                            onPressed: state.busy
                                ? null
                                : () {
                                    _code.clear();
                                    ref.read(authControllerProvider.notifier).cancelMfa();
                                  },
                            child: const Text('Use a different account'),
                          ),
                        ],
                        const SizedBox(height: 24),
                        const _Foot(),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  List<Widget> _credentialFields() => [
    TextFormField(
      controller: _username,
      autofillHints: const [AutofillHints.username],
      textInputAction: TextInputAction.next,
      maxLength: 100,
      decoration: const InputDecoration(
        labelText: 'Username',
        prefixIcon: Icon(Icons.person_outline),
        counterText: '',
      ),
      validator: (value) =>
          (value == null || value.trim().isEmpty) ? 'Enter your username' : null,
    ),
    const SizedBox(height: 14),
    TextFormField(
      controller: _password,
      autofillHints: const [AutofillHints.password],
      obscureText: _obscure,
      maxLength: 256,
      onFieldSubmitted: (_) => _submitCredentials(),
      decoration: InputDecoration(
        labelText: 'Password',
        prefixIcon: const Icon(Icons.lock_outline),
        counterText: '',
        suffixIcon: IconButton(
          icon: Icon(_obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined),
          tooltip: _obscure ? 'Show password' : 'Hide password',
          onPressed: () => setState(() => _obscure = !_obscure),
        ),
      ),
      validator: (value) => (value == null || value.isEmpty) ? 'Enter your password' : null,
    ),
  ];

  List<Widget> _mfaFields() => [
    TextFormField(
      controller: _code,
      autofillHints: const [AutofillHints.oneTimeCode],
      textAlign: TextAlign.center,
      style: const TextStyle(fontSize: 24, letterSpacing: 8, fontWeight: FontWeight.w600),
      onFieldSubmitted: (_) => _submitCode(),
      decoration: const InputDecoration(labelText: 'Code', hintText: '000000'),
    ),
  ];
}

/// The brand half. Decorative: the form beside it carries the semantics.
class _Hero extends StatelessWidget {
  const _Hero();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: EdgeInsets.fromLTRB(24, MediaQuery.paddingOf(context).top + 36, 24, 52),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFFE01020), fujitecRed, Color(0xFF7D000C)],
          stops: [0, 0.45, 1],
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          // The wordmark is red artwork and cannot sit on a red panel. Rather
          // than park it on a white card — which reads as a sticker stuck to the
          // screen — it is knocked out to white, which is what a reversed logo
          // is for. srcIn recolours the opaque pixels and leaves the alpha, so
          // one asset serves both treatments.
          Image.asset(
            'assets/images/fujitec-logo.png',
            width: 150,
            color: Colors.white,
            colorBlendMode: BlendMode.srcIn,
          ),
          const SizedBox(height: 22),
          const Text(
            'Asset Audit',
            style: TextStyle(
              color: Colors.white,
              fontSize: 28,
              fontWeight: FontWeight.w700,
              letterSpacing: -0.3,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            'Verify what is actually there — online or not.',
            style: TextStyle(color: Colors.white.withValues(alpha: 0.88), fontSize: 14),
          ),
        ],
      ),
    );
  }
}

class _Title extends StatelessWidget {
  const _Title({required this.isMfa});

  final bool isMfa;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          isMfa ? 'Two-factor code' : 'Sign in',
          style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 6),
        Text(
          isMfa
              ? 'Enter the code from your authenticator app, or a recovery code.'
              : 'Use your AMS account to record verifications.',
          style: theme.textTheme.bodyMedium?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}

class _ErrorBox extends StatelessWidget {
  const _ErrorBox({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
      decoration: BoxDecoration(
        color: const Color(0x14CA0012),
        border: Border.all(color: const Color(0x38CA0012)),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.error_outline, size: 18, color: fujitecRed),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              message,
              style: const TextStyle(color: fujitecRed, fontSize: 13, height: 1.35),
            ),
          ),
        ],
      ),
    );
  }
}

class _Foot extends StatelessWidget {
  const _Foot();

  @override
  Widget build(BuildContext context) {
    return Text(
      'Fujitec India · Asset Management System',
      textAlign: TextAlign.center,
      style: TextStyle(fontSize: 11, color: Theme.of(context).colorScheme.onSurfaceVariant),
    );
  }
}
