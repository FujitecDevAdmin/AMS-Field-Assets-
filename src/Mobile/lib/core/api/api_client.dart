import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/auth_token_holder.dart';

/// Where the API lives.
///
/// Development uses ADB reverse port forwarding, so a USB-connected audit
/// phone reaches the workstation API through its own loopback address.
/// Deployment builds override this with `--dart-define=AMS_API_BASE=...`.
const String apiBaseUrl = String.fromEnvironment(
  'AMS_API_BASE',
  defaultValue: 'http://127.0.0.1:5069',
);

final dioProvider = Provider<Dio>((ref) {
  final dio = Dio(
    BaseOptions(
      baseUrl: apiBaseUrl,
      // A technician in a plant room is on a bad connection, not no connection.
      // These are long enough to ride out a slow handshake and short enough
      // that the UI does not appear to hang.
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 30),
      // 4xx are answers, not exceptions: the sign-in screen needs to read a 401
      // rather than catch it.
      validateStatus: (status) => status != null && status < 500,
      contentType: Headers.jsonContentType,
    ),
  );

  final tokens = ref.watch(authTokenHolderProvider);

  dio.interceptors.add(
    InterceptorsWrapper(
      onRequest: (options, handler) {
        // The sign-in routes are anonymous. Sending a stale token to them would
        // turn a wrong password into a sign-out through the handler below.
        final isSignIn = options.path.contains('/identity/sign-in');
        final token = tokens.token;

        if (token != null && !isSignIn) {
          options.headers['Authorization'] = 'Bearer $token';
        }

        handler.next(options);
      },
      onResponse: (response, handler) {
        final rejected =
            response.statusCode == 401 &&
            response.requestOptions.headers.containsKey('Authorization');

        if (rejected) {
          // A token the server refuses is over, whatever its expiry claimed —
          // the account may have been locked or the password reset since.
          tokens.onUnauthorized?.call();
        }

        handler.next(response);
      },
    ),
  );

  return dio;
});
