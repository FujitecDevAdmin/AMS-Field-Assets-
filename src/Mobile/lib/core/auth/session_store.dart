import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'auth_models.dart';

/// The session on disk.
///
/// Keychain on iOS, Keystore-backed EncryptedSharedPreferences on Android —
/// never SQLite and never plain SharedPreferences (docs/05 §7). The audit app
/// runs on shared handsets in the field; a token readable by anything else on
/// the device is a token anybody holding it can use.
class SessionStore {
  const SessionStore(this._storage);

  static const String _key = 'ams.session';

  final FlutterSecureStorage _storage;

  Future<Session?> read() async {
    final raw = await _storage.read(key: _key);
    if (raw == null) {
      return null;
    }

    try {
      return Session.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } on FormatException {
      // A value we cannot read is a value we do not trust. Drop it rather than
      // leave the app wedged on a corrupt session it can never clear.
      await clear();
      return null;
    }
  }

  Future<void> write(Session session) =>
      _storage.write(key: _key, value: jsonEncode(session.toJson()));

  Future<void> clear() => _storage.delete(key: _key);
}

/// No options on purpose. flutter_secure_storage 11 encrypts on Android by
/// default — AES-GCM under a Keystore-wrapped key — and the old
/// `encryptedSharedPreferences` flag is gone because it is no longer something
/// you can forget to switch on. iOS has always been the Keychain.
final secureStorageProvider = Provider<FlutterSecureStorage>(
  (ref) => const FlutterSecureStorage(),
);

final sessionStoreProvider = Provider<SessionStore>(
  (ref) => SessionStore(ref.watch(secureStorageProvider)),
);
