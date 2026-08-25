import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../main.dart' show fujitecRed;
import '../../shared/widgets/fujitec_header_logo.dart';
import '../../shared/widgets/techy_loader.dart';

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
  bool _rememberMe = false;

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

  Future<void> _showPasswordHelp() => showDialog<void>(
    context: context,
    builder: (context) => AlertDialog(
      icon: const Icon(Icons.support_agent_rounded, color: fujitecRed),
      title: const Text('Password assistance'),
      content: const Text(
        'Please contact your AMS administrator to reset your password.',
        textAlign: TextAlign.center,
      ),
      actionsAlignment: MainAxisAlignment.center,
      actions: [
        FilledButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Close'),
        ),
      ],
    ),
  );

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(authControllerProvider);
    final isMfa = state.stage == SignInStage.mfa;

    return Scaffold(
      resizeToAvoidBottomInset: true,
      backgroundColor: const Color(0xFFF7F5F4),
      body: Stack(
        fit: StackFit.expand,
        children: [
          Image.asset(
            'assets/images/fujitec-login-corporate-background.png',
            fit: BoxFit.cover,
            alignment: Alignment.topCenter,
            filterQuality: FilterQuality.high,
          ),
          const ColoredBox(color: Color(0x0AFFFFFF)),
          SafeArea(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final compact = constraints.maxWidth < 360;
                final horizontalPadding = compact ? 16.0 : 22.0;
                return SingleChildScrollView(
                  keyboardDismissBehavior:
                      ScrollViewKeyboardDismissBehavior.onDrag,
                  padding: EdgeInsets.fromLTRB(
                    horizontalPadding,
                    compact ? 14 : 20,
                    horizontalPadding,
                    18,
                  ),
                  child: ConstrainedBox(
                    constraints: BoxConstraints(
                      minHeight: constraints.maxHeight - (compact ? 32 : 38),
                    ),
                    child: Center(
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 420),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            const _Hero(),
                            SizedBox(height: compact ? 16 : 22),
                            Container(
                              padding: EdgeInsets.fromLTRB(
                                compact ? 20 : 26,
                                compact ? 22 : 28,
                                compact ? 20 : 26,
                                compact ? 22 : 26,
                              ),
                              decoration: BoxDecoration(
                                color: const Color(0xF7FFFFFF),
                                borderRadius: BorderRadius.circular(26),
                                border: Border.all(
                                  color: const Color(0xCCFFFFFF),
                                ),
                                boxShadow: const [
                                  BoxShadow(
                                    color: Color(0x24101828),
                                    blurRadius: 34,
                                    spreadRadius: 2,
                                    offset: Offset(0, 16),
                                  ),
                                  BoxShadow(
                                    color: Color(0x12D01126),
                                    blurRadius: 20,
                                    offset: Offset(0, 6),
                                  ),
                                ],
                              ),
                              child: Form(
                                key: _formKey,
                                child: Column(
                                  crossAxisAlignment:
                                      CrossAxisAlignment.stretch,
                                  children: [
                                    _Title(isMfa: isMfa),
                                    const SizedBox(height: 22),
                                    if (isMfa)
                                      ..._mfaFields()
                                    else
                                      ..._credentialFields(),
                                    if (!isMfa) ...[
                                      const SizedBox(height: 12),
                                      Row(
                                        children: [
                                          SizedBox(
                                            width: 22,
                                            height: 22,
                                            child: Checkbox(
                                              value: _rememberMe,
                                              activeColor: fujitecRed,
                                              shape: RoundedRectangleBorder(
                                                borderRadius:
                                                    BorderRadius.circular(4),
                                              ),
                                              onChanged: state.busy
                                                  ? null
                                                  : (value) => setState(
                                                      () => _rememberMe =
                                                          value ?? false,
                                                    ),
                                            ),
                                          ),
                                          const SizedBox(width: 8),
                                          const Expanded(
                                            child: Text(
                                              'Remember me',
                                              style: TextStyle(
                                                color: Color(0xFF344054),
                                                fontSize: 13,
                                                fontWeight: FontWeight.w500,
                                              ),
                                            ),
                                          ),
                                          TextButton(
                                            style: TextButton.styleFrom(
                                              foregroundColor: fujitecRed,
                                              padding:
                                                  const EdgeInsets.symmetric(
                                                    horizontal: 4,
                                                    vertical: 6,
                                                  ),
                                              visualDensity:
                                                  VisualDensity.compact,
                                            ),
                                            onPressed: state.busy
                                                ? null
                                                : _showPasswordHelp,
                                            child: const Text(
                                              'Forgot Password?',
                                              style: TextStyle(
                                                fontSize: 13,
                                                fontWeight: FontWeight.w600,
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ],
                                    if (state.error
                                        case final String message) ...[
                                      const SizedBox(height: 14),
                                      _ErrorBox(message: message),
                                    ],
                                    if (isMfa) ...[
                                      const SizedBox(height: 6),
                                      TextButton(
                                        onPressed: state.busy
                                            ? null
                                            : () {
                                                _code.clear();
                                                ref
                                                    .read(
                                                      authControllerProvider
                                                          .notifier,
                                                    )
                                                    .cancelMfa();
                                              },
                                        child: const Text(
                                          'Use a different account',
                                        ),
                                      ),
                                    ],
                                    const SizedBox(height: 20),
                                    DecoratedBox(
                                      decoration: BoxDecoration(
                                        borderRadius: BorderRadius.circular(13),
                                        boxShadow: const [
                                          BoxShadow(
                                            color: Color(0x38D01126),
                                            blurRadius: 14,
                                            offset: Offset(0, 7),
                                          ),
                                        ],
                                      ),
                                      child: FilledButton(
                                        style: FilledButton.styleFrom(
                                          minimumSize: const Size.fromHeight(
                                            52,
                                          ),
                                          backgroundColor: fujitecRed,
                                          foregroundColor: Colors.white,
                                          disabledBackgroundColor: const Color(
                                            0xFFDEA0A8,
                                          ),
                                          elevation: 0,
                                          shape: RoundedRectangleBorder(
                                            borderRadius: BorderRadius.circular(
                                              13,
                                            ),
                                          ),
                                          textStyle: const TextStyle(
                                            fontSize: 16,
                                            fontWeight: FontWeight.w700,
                                            letterSpacing: 0.2,
                                          ),
                                        ),
                                        onPressed: state.busy
                                            ? null
                                            : (isMfa
                                                  ? _submitCode
                                                  : _submitCredentials),
                                        child: state.busy
                                            ? const TechyLoader(size: 18)
                                            : Text(
                                                isMfa ? 'Verify' : 'Sign In',
                                              ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                            SizedBox(height: compact ? 22 : 30),
                            const _Foot(),
                          ],
                        ),
                      ),
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  List<Widget> _credentialFields() => [
    const _InputLabel(label: 'User ID', icon: Icons.person_outline_rounded),
    const SizedBox(height: 7),
    TextFormField(
      controller: _username,
      autofillHints: const [AutofillHints.username],
      textInputAction: TextInputAction.next,
      maxLength: 100,
      decoration: _loginInputDecoration(
        hintText: 'Enter your user ID',
        prefixIcon: const Icon(Icons.person_outline_rounded),
      ),
      validator: (value) => (value == null || value.trim().isEmpty)
          ? 'Enter your username'
          : null,
    ),
    const SizedBox(height: 15),
    const _InputLabel(label: 'Password', icon: Icons.lock_outline_rounded),
    const SizedBox(height: 7),
    TextFormField(
      controller: _password,
      autofillHints: const [AutofillHints.password],
      obscureText: _obscure,
      maxLength: 256,
      onFieldSubmitted: (_) => _submitCredentials(),
      decoration: _loginInputDecoration(
        hintText: 'Enter your password',
        prefixIcon: const Icon(Icons.lock_outline_rounded),
        suffixIcon: IconButton(
          constraints: const BoxConstraints.tightFor(width: 40, height: 40),
          padding: EdgeInsets.zero,
          visualDensity: VisualDensity.compact,
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

  InputDecoration _loginInputDecoration({
    required String hintText,
    required Widget prefixIcon,
    Widget? suffixIcon,
  }) {
    const border = OutlineInputBorder(
      borderRadius: BorderRadius.all(Radius.circular(12)),
      borderSide: BorderSide(color: Color(0xFFD8DDE5)),
    );

    return InputDecoration(
      hintText: hintText,
      hintStyle: const TextStyle(
        color: Color(0xFF98A2B3),
        fontSize: 13.5,
        fontWeight: FontWeight.w400,
      ),
      counterText: '',
      prefixIcon: prefixIcon,
      suffixIcon: suffixIcon,
      prefixIconColor: fujitecRed,
      suffixIconColor: const Color(0xFF475467),
      prefixIconConstraints: const BoxConstraints.tightFor(
        width: 44,
        height: 46,
      ),
      suffixIconConstraints: const BoxConstraints.tightFor(
        width: 42,
        height: 46,
      ),
      isDense: true,
      filled: true,
      fillColor: const Color(0xFFF8F9FB),
      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 13),
      enabledBorder: border,
      border: border,
      focusedBorder: const OutlineInputBorder(
        borderRadius: BorderRadius.all(Radius.circular(12)),
        borderSide: BorderSide(color: fujitecRed, width: 1.6),
      ),
      errorBorder: const OutlineInputBorder(
        borderRadius: BorderRadius.all(Radius.circular(12)),
        borderSide: BorderSide(color: fujitecRed),
      ),
      focusedErrorBorder: const OutlineInputBorder(
        borderRadius: BorderRadius.all(Radius.circular(12)),
        borderSide: BorderSide(color: fujitecRed, width: 1.5),
      ),
    );
  }
}

class _InputLabel extends StatelessWidget {
  const _InputLabel({required this.label, required this.icon});

  final String label;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Icon(icon, size: 15, color: const Color(0xFF667085)),
      const SizedBox(width: 6),
      Text(
        label,
        style: Theme.of(context).textTheme.labelMedium?.copyWith(
          color: const Color(0xFF344054),
          fontWeight: FontWeight.w700,
          letterSpacing: 0.1,
        ),
      ),
    ],
  );
}

/// Mandatory supplied brand artwork, kept separate from the form so that the
/// keyboard and compact displays can never cause the two sections to overlap.
class _Hero extends StatelessWidget {
  const _Hero();

  @override
  Widget build(BuildContext context) {
    return const Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        FujitecHeaderLogo(width: 176, height: 48, alignment: Alignment.center),
        SizedBox(height: 14),
        Text(
          'Asset Management\nSystem',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Color(0xFF202A38),
            fontSize: 25,
            height: 1.14,
            fontWeight: FontWeight.w800,
            letterSpacing: -0.45,
            shadows: [Shadow(color: Color(0x2EFFFFFF), blurRadius: 5)],
          ),
        ),
        SizedBox(height: 12),
        DecoratedBox(
          decoration: BoxDecoration(
            color: fujitecRed,
            borderRadius: BorderRadius.all(Radius.circular(3)),
          ),
          child: SizedBox(width: 42, height: 3),
        ),
        SizedBox(height: 9),
        Text(
          'Smart Assets. Stronger Future.',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Color(0xFF667085),
            fontSize: 13.5,
            fontWeight: FontWeight.w500,
            letterSpacing: 0.15,
            shadows: [Shadow(color: Color(0xA6FFFFFF), blurRadius: 4)],
          ),
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
          isMfa ? 'Two-factor code' : 'Sign In',
          style: theme.textTheme.headlineSmall?.copyWith(
            color: const Color(0xFF101828),
            fontWeight: FontWeight.w800,
            letterSpacing: -0.2,
          ),
        ),
        const SizedBox(height: 6),
        Text(
          isMfa
              ? 'Enter the code from your authenticator app, or a recovery code.'
              : 'Please enter your credentials to continue',
          style: theme.textTheme.bodyMedium?.copyWith(
            color: const Color(0xFF667085),
            height: 1.35,
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
    final currentYear = DateTime.now().year;
    final style = Theme.of(context).textTheme.labelSmall?.copyWith(
      color: const Color(0xFF667085),
      fontWeight: FontWeight.w400,
      height: 1.45,
    );

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: const Color(0xCFFFFFFF),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            'Designed and Developed by Digital Transformation Team',
            textAlign: TextAlign.center,
            softWrap: true,
            style: style?.copyWith(fontWeight: FontWeight.w500),
          ),
          const SizedBox(height: 3),
          Text(
            '© $currentYear Fujitec India Private Limited',
            textAlign: TextAlign.center,
            softWrap: true,
            style: style,
          ),
        ],
      ),
    );
  }
}
