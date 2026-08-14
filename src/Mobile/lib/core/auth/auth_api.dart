import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_client.dart';
import 'auth_models.dart';

/// The identity module's sign-in routes, and nowhere else.
class AuthApi {
  const AuthApi(this._dio);

  static const String _base = '/api/v1/identity';

  final Dio _dio;

  Future<SignInResult> signIn({required String username, required String password}) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '$_base/sign-in',
      data: <String, String>{'username': username, 'password': password},
    );

    _throwOnFailure(response, 'Those credentials were not accepted.');
    return SignInResult.fromJson(response.data!);
  }

  Future<MfaResult> verifyMfaCode({
    required String mfaChallengeToken,
    required String code,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '$_base/sign-in/mfa',
      data: <String, String>{'mfaChallengeToken': mfaChallengeToken, 'code': code},
    );

    _throwOnFailure(response, 'That code was not accepted.');
    return MfaResult.fromJson(response.data!);
  }

  /// The API never says WHY a sign-in failed, deliberately — a message that
  /// tells "no such user" from "wrong password" is a user enumeration tool.
  /// This does not invent a reason it was not given.
  void _throwOnFailure(Response<Map<String, dynamic>> response, String unauthorised) {
    final status = response.statusCode ?? 0;

    if (status == 200 && response.data != null) {
      return;
    }
    if (status == 401) {
      throw AuthFailure(unauthorised);
    }
    if (status == 400 || status == 422) {
      throw const AuthFailure('Check the details and try again.');
    }
    throw AuthFailure('Sign-in failed (HTTP $status).');
  }
}

final authApiProvider = Provider<AuthApi>((ref) => AuthApi(ref.watch(dioProvider)));
