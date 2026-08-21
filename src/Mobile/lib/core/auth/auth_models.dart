/// Wire shapes for the identity module's sign-in slices.
///
/// Mirrors, field for field:
///   Identity/Features/SignIn/SignInResponse.cs
///   Identity/Features/VerifyMfaCode/VerifyMfaCodeResponse.cs
///
/// Hand-written rather than generated, and parsed defensively: a field that
/// arrives null where the contract says it cannot is a bug worth a clear
/// exception here, not a null crash three screens later.
library;

class SignInResult {
  const SignInResult({
    required this.userId,
    required this.username,
    required this.displayName,
    required this.mustChangePassword,
    required this.mfaRequired,
    this.mfaChallengeToken,
    this.accessToken,
    this.accessTokenExpiresOnUtc,
  });

  factory SignInResult.fromJson(Map<String, dynamic> json) {
    final expires = json['accessTokenExpiresOnUtc'] as String?;

    return SignInResult(
      userId: json['userId'] as int,
      username: json['username'] as String,
      displayName: json['displayName'] as String,
      mustChangePassword: json['mustChangePassword'] as bool,
      mfaRequired: json['mfaRequired'] as bool,
      mfaChallengeToken: json['mfaChallengeToken'] as String?,
      accessToken: json['accessToken'] as String?,
      accessTokenExpiresOnUtc: expires == null
          ? null
          : DateTime.parse(expires).toUtc(),
    );
  }

  final int userId;
  final String username;
  final String displayName;

  /// New or admin-reset account: the password must be changed before anything else.
  final bool mustChangePassword;

  /// Enrolled user. The session is NOT usable until the code is verified.
  final bool mfaRequired;

  /// Identifies this half-finished sign-in. Null when MFA is not required.
  final String? mfaChallengeToken;

  /// Null whenever [mfaRequired] — that is the point of the second factor.
  final String? accessToken;
  final DateTime? accessTokenExpiresOnUtc;
}

class MfaResult {
  const MfaResult({
    required this.userId,
    required this.username,
    required this.displayName,
    required this.mustChangePassword,
    required this.usedRecoveryCode,
    required this.remainingRecoveryCodes,
    required this.accessToken,
    required this.accessTokenExpiresOnUtc,
  });

  factory MfaResult.fromJson(Map<String, dynamic> json) => MfaResult(
    userId: json['userId'] as int,
    username: json['username'] as String,
    displayName: json['displayName'] as String,
    mustChangePassword: json['mustChangePassword'] as bool,
    usedRecoveryCode: json['usedRecoveryCode'] as bool,
    remainingRecoveryCodes: json['remainingRecoveryCodes'] as int,
    accessToken: json['accessToken'] as String,
    accessTokenExpiresOnUtc: DateTime.parse(
      json['accessTokenExpiresOnUtc'] as String,
    ).toUtc(),
  );

  final int userId;
  final String username;
  final String displayName;
  final bool mustChangePassword;

  /// A recovery code was spent rather than an authenticator code. Say so.
  final bool usedRecoveryCode;
  final int remainingRecoveryCodes;
  final String accessToken;
  final DateTime accessTokenExpiresOnUtc;
}

/// What the device keeps once a sign-in is COMPLETE.
///
/// Held in platform secure storage, never in SQLite or SharedPreferences
/// (docs/05 §7).
class Session {
  const Session({
    required this.userId,
    required this.username,
    required this.displayName,
    required this.accessToken,
    required this.expiresOnUtc,
    required this.mustChangePassword,
  });

  factory Session.fromJson(Map<String, dynamic> json) => Session(
    userId: json['userId'] as int,
    username: json['username'] as String,
    displayName: json['displayName'] as String,
    accessToken: json['accessToken'] as String,
    expiresOnUtc: DateTime.parse(json['expiresOnUtc'] as String).toUtc(),
    mustChangePassword: json['mustChangePassword'] as bool,
  );

  final int userId;
  final String username;
  final String displayName;
  final String accessToken;
  final DateTime expiresOnUtc;
  final bool mustChangePassword;

  bool get isExpired => DateTime.now().toUtc().isAfter(expiresOnUtc);

  Map<String, dynamic> toJson() => <String, dynamic>{
    'userId': userId,
    'username': username,
    'displayName': displayName,
    'accessToken': accessToken,
    'expiresOnUtc': expiresOnUtc.toIso8601String(),
    'mustChangePassword': mustChangePassword,
  };
}

/// A sign-in that failed for a reason worth showing.
class AuthFailure implements Exception {
  const AuthFailure(this.message);

  final String message;

  @override
  String toString() => message;
}
