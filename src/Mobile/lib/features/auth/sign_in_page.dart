import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../main.dart' show fujitecRed;
import '../../shared/widgets/fujitec_header_logo.dart';

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

    final keyboardInset = MediaQuery.viewInsetsOf(context).bottom;
    return Scaffold(
      resizeToAvoidBottomInset: true,
      backgroundColor: const Color(0xFFFFF7F8),
      body: DecoratedBox(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFFFFF9FA), Color(0xFFFCE8EB)],
          ),
        ),
        child: SafeArea(
          child: LayoutBuilder(
            builder: (context, constraints) => SingleChildScrollView(
              keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
              padding: EdgeInsets.fromLTRB(16, 18, 16, 20 + keyboardInset),
              child: ConstrainedBox(
                constraints: BoxConstraints(
                  minHeight: constraints.maxHeight > keyboardInset + 38
                      ? constraints.maxHeight - keyboardInset - 38
                      : 0,
                ),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const _Hero(),
                    const SizedBox(height: 18),
                    Container(
                      padding: const EdgeInsets.fromLTRB(22, 24, 22, 20),
                      decoration: BoxDecoration(
                        color: const Color(0xFFFFFDFD),
                        borderRadius: BorderRadius.circular(22),
                        border: Border.all(color: const Color(0xFFEBC8CD)),
                        boxShadow: const [
                          BoxShadow(
                            color: Color(0x1FD01126),
                            blurRadius: 24,
                            offset: Offset(0, 10),
                          ),
                        ],
                      ),
                      child: Form(
                        key: _formKey,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            _Title(isMfa: isMfa),
                            const SizedBox(height: 22),
                            if (isMfa)
                              ..._mfaFields()
                            else
                              ..._credentialFields(),
                            if (state.error case final String message) ...[
                              const SizedBox(height: 16),
                              _ErrorBox(message: message),
                            ],
                            const SizedBox(height: 24),
                            DecoratedBox(
                              decoration: BoxDecoration(
                                gradient: const LinearGradient(
                                  colors: [
                                    Color(0xFFD01126),
                                    Color(0xFFD01126),
                                  ],
                                ),
                                borderRadius: BorderRadius.circular(14),
                              ),
                              child: FilledButton(
                                style: FilledButton.styleFrom(
                                  backgroundColor: Colors.transparent,
                                  foregroundColor: Colors.white,
                                  shadowColor: Colors.transparent,
                                ),
                                onPressed: state.busy
                                    ? null
                                    : (isMfa
                                          ? _submitCode
                                          : _submitCredentials),
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
                            ),
                            if (isMfa) ...[
                              const SizedBox(height: 6),
                              TextButton(
                                onPressed: state.busy
                                    ? null
                                    : () {
                                        _code.clear();
                                        ref
                                            .read(
                                              authControllerProvider.notifier,
                                            )
                                            .cancelMfa();
                                      },
                                child: const Text('Use a different account'),
                              ),
                            ],
                            const SizedBox(height: 20),
                            const _Foot(),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
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
      validator: (value) => (value == null || value.trim().isEmpty)
          ? 'Enter your username'
          : null,
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
          icon: Icon(
            _obscure
                ? Icons.visibility_outlined
                : Icons.visibility_off_outlined,
          ),
          tooltip: _obscure ? 'Show password' : 'Hide password',
          onPressed: () => setState(() => _obscure = !_obscure),
        ),
      ),
      validator: (value) =>
          (value == null || value.isEmpty) ? 'Enter your password' : null,
    ),
  ];

  List<Widget> _mfaFields() => [
    TextFormField(
      controller: _code,
      autofillHints: const [AutofillHints.oneTimeCode],
      textAlign: TextAlign.center,
      style: const TextStyle(
        fontSize: 24,
        letterSpacing: 8,
        fontWeight: FontWeight.w600,
      ),
      onFieldSubmitted: (_) => _submitCode(),
      decoration: const InputDecoration(labelText: 'Code', hintText: '000000'),
    ),
  ];
}

/// Mandatory supplied brand artwork, kept separate from the form so that the
/// keyboard and compact displays can never cause the two sections to overlap.
class _Hero extends StatelessWidget {
  const _Hero();

  @override
  Widget build(BuildContext context) {
    return const Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Align(
          alignment: Alignment.center,
          child: FujitecHeaderLogo(),
        ),
        SizedBox(height: 18),
        Text(
          'Asset Audit',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Color(0xFFD01126),
            fontSize: 24,
            fontWeight: FontWeight.w800,
            letterSpacing: -0.3,
          ),
        ),
        SizedBox(height: 4),
        Text(
          'Verify what is actually there — online or not.',
          textAlign: TextAlign.center,
          style: TextStyle(color: Color(0xFF675A5D), fontSize: 13),
        ),
      ],
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
          style: theme.textTheme.headlineSmall?.copyWith(
            fontWeight: FontWeight.w700,
          ),
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
        color: const Color(0x14D01126),
        border: Border.all(color: const Color(0x38D01126)),
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
              style: const TextStyle(
                color: fujitecRed,
                fontSize: 13,
                height: 1.35,
              ),
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
      style: TextStyle(
        fontSize: 11,
        color: Theme.of(context).colorScheme.onSurfaceVariant,
      ),
    );
  }
}
