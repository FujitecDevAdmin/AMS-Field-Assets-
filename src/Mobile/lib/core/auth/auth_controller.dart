import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'auth_api.dart';
import 'auth_models.dart';
import 'auth_token_holder.dart';
import 'session_store.dart';

/// What the sign-in screen is currently asking for.
enum SignInStage { credentials, mfa }

/// Everything the sign-in screen renders from.
class AuthState {
  const AuthState({
    this.session,
    this.stage = SignInStage.credentials,
    this.challengeToken,
    this.busy = false,
    this.error,
    this.notice,
    this.restoring = true,
  });

  final Session? session;
  final SignInStage stage;
  final String? challengeToken;
  final bool busy;
  final String? error;

  /// Something worth saying that is not a failure — a spent recovery code.
  final String? notice;

  /// True until the stored session has been read, so the app does not flash the
  /// sign-in screen at somebody who is already signed in.
  final bool restoring;

  bool get isSignedIn => session != null && !session!.isExpired;

  AuthState copyWith({
    Session? session,
    bool clearSession = false,
    SignInStage? stage,
    String? challengeToken,
    bool clearChallenge = false,
    bool? busy,
    String? error,
    bool clearError = false,
    String? notice,
    bool clearNotice = false,
    bool? restoring,
  }) => AuthState(
    session: clearSession ? null : session ?? this.session,
    stage: stage ?? this.stage,
    challengeToken: clearChallenge
        ? null
        : challengeToken ?? this.challengeToken,
    busy: busy ?? this.busy,
    error: clearError ? null : error ?? this.error,
    notice: clearNotice ? null : notice ?? this.notice,
    restoring: restoring ?? this.restoring,
  );
}

/// The session, and the two-step sign-in that produces one.
///
/// The API issues NO access token beside an MFA challenge — an enrolled user is
/// not signed in until the code is verified. That property is kept here:
/// `session` stays null through the challenge, so anything asking "are we
/// signed in" reads a half-finished sign-in as no.
class AuthController extends Notifier<AuthState> {
  @override
  AuthState build() {
    // The API client reads the token from here rather than from this
    // controller, which would be a cycle. Setting the callback in build means
    // it is in place before any request can be made.
    ref.read(authTokenHolderProvider).onUnauthorized = () =>
        unawaited(signOut());

    // Fire and forget: the screen renders a spinner while restoring is true.
    unawaited(_restore());
    return const AuthState();
  }

  Future<void> _restore() async {
    final stored = await ref.read(sessionStoreProvider).read();

    if (stored != null && stored.isExpired) {
      // An expired token is not a session. Offline or not, it will be refused.
      await ref.read(sessionStoreProvider).clear();
      state = state.copyWith(restoring: false);
      return;
    }

    ref.read(authTokenHolderProvider).token = stored?.accessToken;
    state = state.copyWith(session: stored, restoring: false);
  }

  Future<void> signIn({
    required String username,
    required String password,
  }) async {
    state = state.copyWith(busy: true, clearError: true, clearNotice: true);

    try {
      final result = await ref
          .read(authApiProvider)
          .signIn(username: username.trim(), password: password);

      if (result.mfaRequired) {
        state = state.copyWith(
          busy: false,
          stage: SignInStage.mfa,
          challengeToken: result.mfaChallengeToken,
        );
        return;
      }

      await _begin(
        Session(
          userId: result.userId,
          username: result.username,
          displayName: result.displayName,
          accessToken: result.accessToken!,
          expiresOnUtc: result.accessTokenExpiresOnUtc!,
          mustChangePassword: result.mustChangePassword,
        ),
      );
    } on AuthFailure catch (failure) {
      state = state.copyWith(busy: false, error: failure.message);
    } on Exception {
      state = state.copyWith(
        busy: false,
        error: 'The server could not be reached.',
      );
    }
  }

  Future<void> verifyMfaCode(String code) async {
    final token = state.challengeToken;
    if (token == null) {
      return;
    }

    state = state.copyWith(busy: true, clearError: true);

    try {
      final result = await ref
          .read(authApiProvider)
          .verifyMfaCode(mfaChallengeToken: token, code: code.trim());

      await _begin(
        Session(
          userId: result.userId,
          username: result.username,
          displayName: result.displayName,
          accessToken: result.accessToken,
          expiresOnUtc: result.accessTokenExpiresOnUtc,
          mustChangePassword: result.mustChangePassword,
        ),
        notice: result.usedRecoveryCode
            ? 'Signed in with a recovery code. ${result.remainingRecoveryCodes} left.'
            : null,
      );
    } on AuthFailure catch (failure) {
      state = state.copyWith(busy: false, error: failure.message);
    } on Exception {
      state = state.copyWith(
        busy: false,
        error: 'The server could not be reached.',
      );
    }
  }

  Future<bool> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    final session = state.session;
    if (session == null) return false;

    state = state.copyWith(busy: true, clearError: true, clearNotice: true);
    try {
      await ref
          .read(authApiProvider)
          .changePassword(
            currentPassword: currentPassword,
            newPassword: newPassword,
          );
      final updated = Session(
        userId: session.userId,
        username: session.username,
        displayName: session.displayName,
        accessToken: session.accessToken,
        expiresOnUtc: session.expiresOnUtc,
        mustChangePassword: false,
      );
      await ref.read(sessionStoreProvider).write(updated);
      state = state.copyWith(
        session: updated,
        busy: false,
        clearError: true,
        notice: 'Password changed successfully.',
      );
      return true;
    } on AuthFailure catch (failure) {
      state = state.copyWith(busy: false, error: failure.message);
    } on Exception {
      state = state.copyWith(
        busy: false,
        error: 'The server could not be reached.',
      );
    }
    return false;
  }

  /// Abandon a half-finished sign-in and go back to the credentials step.
  void cancelMfa() {
    state = state.copyWith(
      stage: SignInStage.credentials,
      clearChallenge: true,
      clearError: true,
    );
  }

  Future<void> signOut() async {
    ref.read(authTokenHolderProvider).token = null;
    await ref.read(sessionStoreProvider).clear();
    state = const AuthState(restoring: false);
  }

  Future<void> _begin(Session session, {String? notice}) async {
    ref.read(authTokenHolderProvider).token = session.accessToken;
    await ref.read(sessionStoreProvider).write(session);
    state = state.copyWith(
      session: session,
      busy: false,
      stage: SignInStage.credentials,
      clearChallenge: true,
      clearError: true,
      notice: notice,
    );
  }
}

final authControllerProvider = NotifierProvider<AuthController, AuthState>(
  AuthController.new,
);
