import 'dart:async';

import 'package:flutter/foundation.dart';

import '../l10n/app_strings.dart';
import 'jwt_claims.dart';
import 'roles.dart';
import 'token_store.dart';

/// The single source of truth for "who is signed in". `main.dart` shows the login screen or the
/// shell purely from [isLoggedIn], so clearing the session from anywhere - an explicit logout,
/// an HTTP 401 in [BaseProvider], or the token reaching its expiry - sends the user back to
/// login without any screen navigating by hand.
class AuthSession extends ChangeNotifier {
  AuthSession(this._store);

  final TokenStore _store;

  String? _token;
  JwtClaims? _claims;
  Timer? _expiryTimer;

  String? get token => _token;
  bool get isLoggedIn => _token != null;

  int? get userId => _claims?.userId;
  String? get role => _claims?.role;
  String? get email => _claims?.email;
  String get firstName => _claims?.firstName ?? '';
  String get lastName => _claims?.lastName ?? '';
  DateTime? get expiresAtUtc => _claims?.expiresAtUtc;

  String get fullName => [firstName, lastName].where((part) => part.isNotEmpty).join(' ');

  String get roleLabel => switch (_claims?.role) {
        Roles.admin => AppStrings.roleAdmin,
        Roles.instructor => AppStrings.roleInstructor,
        Roles.student => AppStrings.roleStudent,
        _ => '',
      };

  /// Picks up a token saved by a previous run, if it is still valid.
  Future<void> restore() async {
    final stored = await _store.read();
    if (stored == null) return;

    if (!_adopt(stored)) {
      await _store.delete();
      return;
    }
    notifyListeners();
  }

  /// Starts a session from a token the API has just issued. Returns false when the token cannot
  /// be read or is already expired, in which case nothing changes.
  Future<bool> signIn(String token) async {
    if (!_adopt(token)) return false;

    await _store.write(token);
    notifyListeners();
    return true;
  }

  /// Ends the session locally. Idempotent, and safe to call from a response handler.
  void clear() {
    final hadSession = _token != null;
    _expiryTimer?.cancel();
    _expiryTimer = null;
    _token = null;
    _claims = null;

    unawaited(_store.delete());
    if (hadSession) notifyListeners();
  }

  bool _adopt(String token) {
    final claims = JwtClaims.tryParse(token);
    if (claims == null) return false;

    final remaining = claims.expiresAtUtc.difference(DateTime.now().toUtc());
    if (remaining <= Duration.zero) return false;

    _token = token;
    _claims = claims;

    // Don't wait for a 401 to notice the token is dead - sign out the moment it expires.
    _expiryTimer?.cancel();
    _expiryTimer = Timer(remaining, clear);
    return true;
  }

  @override
  void dispose() {
    _expiryTimer?.cancel();
    super.dispose();
  }
}
