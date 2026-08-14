import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/auth_token_holder.dart';

/// Where the API lives.
///
/// `10.0.2.2` is the host machine as seen from the Android emulator; a phone on
/// the bench needs the machine's LAN address instead. Passed with
/// `--dart-define=AMS_API_BASE=...` rather than edited, so a build for a real
/// device does not depend on somebody remembering to change a constant back.
const String apiBaseUrl = String.fromEnvironment(
  'AMS_API_BASE',
  defaultValue: 'http://10.0.2.2:5069',
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
