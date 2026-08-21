import 'package:ams_audit/core/auth/auth_models.dart';
import 'package:ams_audit/core/auth/session_store.dart';
import 'package:ams_audit/main.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';

/// Secure storage is a platform channel, and a widget test has no platform.
/// Overriding the store — rather than mocking the channel — also keeps the test
/// about the gate's behaviour instead of about Keychain.
class _StubSessionStore extends SessionStore {
  const _StubSessionStore(this._stored) : super(const FlutterSecureStorage());

  final Session? _stored;

  @override
  Future<Session?> read() async => _stored;

  @override
  Future<void> write(Session session) async {}

  @override
  Future<void> clear() async {}
}

Widget _app(Session? stored) => ProviderScope(
  overrides: [
    sessionStoreProvider.overrideWithValue(_StubSessionStore(stored)),
  ],
  child: const AmsAuditApp(),
);

Session _session({required DateTime expiresOnUtc}) => Session(
  userId: 1,
  username: 'sverma',
  displayName: 'S Verma',
  accessToken: 'token',
  expiresOnUtc: expiresOnUtc,
  mustChangePassword: false,
);

void main() {
  testWidgets('asks for credentials when there is no session', (tester) async {
    await tester.pumpWidget(_app(null));
    await tester.pumpAndSettle();

    expect(find.text('Username'), findsOneWidget);
    expect(find.text('Password'), findsOneWidget);
  });

  testWidgets('goes straight in when a valid session is stored', (
    tester,
  ) async {
    await tester.pumpWidget(
      _app(
        _session(
          expiresOnUtc: DateTime.now().toUtc().add(const Duration(hours: 8)),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('My Audits'), findsOneWidget);
    expect(find.text('Username'), findsNothing);
  });

  testWidgets('an expired session is not a session', (tester) async {
    await tester.pumpWidget(
      _app(
        _session(
          expiresOnUtc: DateTime.now().toUtc().subtract(
            const Duration(minutes: 1),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Username'), findsOneWidget);
  });
}
