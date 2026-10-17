import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Where the access token survives an app restart.
abstract class TokenStore {
  Future<String?> read();
  Future<void> write(String token);
  Future<void> delete();
}

/// The platform's secure storage (Windows Credential Locker / Android Keystore).
class SecureTokenStore implements TokenStore {
  static const String _key = 'access_token';

  final FlutterSecureStorage _storage;

  const SecureTokenStore([this._storage = const FlutterSecureStorage()]);

  // A broken or locked keychain must never stop the app from starting - it just means the
  // user signs in again.
  @override
  Future<String?> read() async {
    try {
      return await _storage.read(key: _key);
    } on Exception {
      return null;
    }
  }

  @override
  Future<void> write(String token) async {
    try {
      await _storage.write(key: _key, value: token);
    } on Exception {
      // Not persisting only costs a re-login after restart.
    }
  }

  @override
  Future<void> delete() async {
    try {
      await _storage.delete(key: _key);
    } on Exception {
      // Nothing useful to do: the in-memory session is already cleared.
    }
  }
}
