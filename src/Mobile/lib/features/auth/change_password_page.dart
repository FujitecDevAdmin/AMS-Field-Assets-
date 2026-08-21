import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../main.dart' show fujitecRed;

class ChangePasswordPage extends ConsumerStatefulWidget {
  const ChangePasswordPage({super.key});

  @override
  ConsumerState<ChangePasswordPage> createState() => _ChangePasswordPageState();
}

class _ChangePasswordPageState extends ConsumerState<ChangePasswordPage> {
  final _formKey = GlobalKey<FormState>();
  final _current = TextEditingController();
  final _next = TextEditingController();
  final _confirm = TextEditingController();
  bool _showCurrent = false;
  bool _showNext = false;

  @override
  void dispose() {
    _current.dispose();
    _next.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    FocusScope.of(context).unfocus();
    await ref
        .read(authControllerProvider.notifier)
        .changePassword(
          currentPassword: _current.text,
          newPassword: _next.text,
        );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(authControllerProvider);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Secure your account'),
        automaticallyImplyLeading: false,
        actions: [
          IconButton(
            tooltip: 'Sign out',
            icon: const Icon(Icons.logout),
            onPressed: state.busy
                ? null
                : () => ref.read(authControllerProvider.notifier).signOut(),
          ),
        ],
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: EdgeInsets.fromLTRB(
            24,
            24,
            24,
            24 + MediaQuery.viewInsetsOf(context).bottom,
          ),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Icon(Icons.lock_reset, color: fujitecRed, size: 54),
                const SizedBox(height: 16),
                Text(
                  'Change your temporary password',
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Before starting an audit, create a private password with at least 12 characters.',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: 28),
                _passwordField(
                  controller: _current,
                  label: 'Current temporary password',
                  visible: _showCurrent,
                  toggle: () => setState(() => _showCurrent = !_showCurrent),
                  validator: (value) => value == null || value.isEmpty
                      ? 'Enter your current password'
                      : null,
                ),
                const SizedBox(height: 14),
                _passwordField(
                  controller: _next,
                  label: 'New password',
                  visible: _showNext,
                  toggle: () => setState(() => _showNext = !_showNext),
                  validator: (value) {
                    if (value == null || value.length < 12) {
                      return 'Use at least 12 characters';
                    }
                    if (value == _current.text) {
                      return 'Choose a different password';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 14),
                TextFormField(
                  controller: _confirm,
                  obscureText: !_showNext,
                  textInputAction: TextInputAction.done,
                  onFieldSubmitted: (_) => _submit(),
                  decoration: const InputDecoration(
                    labelText: 'Confirm new password',
                    prefixIcon: Icon(Icons.verified_user_outlined),
                  ),
                  validator: (value) =>
                      value != _next.text ? 'Passwords do not match' : null,
                ),
                if (state.error case final String message) ...[
                  const SizedBox(height: 16),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0x14D01126),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      message,
                      style: const TextStyle(color: fujitecRed),
                    ),
                  ),
                ],
                const SizedBox(height: 24),
                FilledButton.icon(
                  onPressed: state.busy ? null : _submit,
                  icon: state.busy
                      ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : const Icon(Icons.check),
                  label: Text(
                    state.busy ? 'Changing password…' : 'Change password',
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _passwordField({
    required TextEditingController controller,
    required String label,
    required bool visible,
    required VoidCallback toggle,
    required String? Function(String?) validator,
  }) => TextFormField(
    controller: controller,
    obscureText: !visible,
    maxLength: 256,
    decoration: InputDecoration(
      labelText: label,
      prefixIcon: const Icon(Icons.lock_outline),
      counterText: '',
      suffixIcon: IconButton(
        onPressed: toggle,
        icon: Icon(
          visible ? Icons.visibility_off_outlined : Icons.visibility_outlined,
        ),
      ),
    ),
    validator: validator,
  );
}
