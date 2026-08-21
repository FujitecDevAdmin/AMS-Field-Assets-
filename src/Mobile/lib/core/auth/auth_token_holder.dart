import 'package:flutter_riverpod/flutter_riverpod.dart';

/// The current bearer token, and what to do when the server rejects it.
///
/// This exists to break a cycle. The API client needs the token; the thing that
/// owns the token is the auth controller; the auth controller is built ON the
/// API client. Riverpod would refuse that, and rightly. A holder the controller
/// writes to and the client reads from depends on neither.
///
/// It is deliberately plain mutable state rather than a provider the client
/// watches: an interceptor runs per request and needs the value at that moment,
/// not a rebuild when it changes.
class AuthTokenHolder {
  String? token;

  /// Called when the server answers 401 to a request that carried a token —
  /// the session is over, whatever the client's own expiry check believed.
  void Function()? onUnauthorized;
}

final authTokenHolderProvider = Provider<AuthTokenHolder>(
  (ref) => AuthTokenHolder(),
);
