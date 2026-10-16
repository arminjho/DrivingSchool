import 'dart:convert';

import 'package:driving_school_desktop/core/api_exception.dart';
import 'package:driving_school_desktop/core/auth_api.dart';
import 'package:driving_school_desktop/core/auth_session.dart';
import 'package:driving_school_desktop/core/jwt_claims.dart';
import 'package:driving_school_desktop/core/roles.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import '../support/test_support.dart';

void main() {
  group('JwtClaims', () {
    test('reads the role, user and names from the same claims the API issues', () {
      final claims = JwtClaims.tryParse(makeJwt(role: 'Instructor', userId: 7, email: 'a@b.ba', firstName: 'Damir', lastName: 'Kovačević'))!;

      expect(claims.role, 'Instructor');
      expect(claims.userId, 7);
      expect(claims.email, 'a@b.ba');
      expect(claims.firstName, 'Damir');
      expect(claims.lastName, 'Kovačević');
      expect(claims.expiresAtUtc.isAfter(DateTime.now().toUtc()), isTrue);
    });

    test('rejects anything that is not a token with a role, a user id and an expiry', () {
      expect(JwtClaims.tryParse('not-a-token'), isNull);
      expect(JwtClaims.tryParse('a.b.c'), isNull);
      expect(JwtClaims.tryParse('${base64Url.encode(utf8.encode('{}'))}.${base64Url.encode(utf8.encode('{}'))}.x'), isNull);
    });
  });

  group('AuthSession', () {
    test('restore picks up a stored token that is still valid', () async {
      final session = AuthSession(MemoryTokenStore(makeJwt(role: 'Student')));
      addTearDown(session.dispose);

      await session.restore();

      expect(session.isLoggedIn, isTrue);
      expect(session.role, Roles.student);
      expect(session.roleLabel, 'Učenik');
      expect(session.fullName, 'Admin Korisnik');
    });

    test('restore discards an expired or unreadable token and clears the store', () async {
      final store = MemoryTokenStore(makeJwt(validFor: const Duration(seconds: -5)));
      final session = AuthSession(store);
      addTearDown(session.dispose);

      await session.restore();

      expect(session.isLoggedIn, isFalse);
      expect(store.token, isNull);
    });

    test('signIn persists the token and notifies listeners; clear forgets it', () async {
      final store = MemoryTokenStore();
      final session = AuthSession(store);
      addTearDown(session.dispose);
      var notified = 0;
      session.addListener(() => notified++);
      final token = makeJwt();

      expect(await session.signIn(token), isTrue);
      expect(store.token, token);
      expect(session.token, token);

      session.clear();
      await Future<void>.delayed(Duration.zero);
      expect(session.isLoggedIn, isFalse);
      expect(store.token, isNull);
      expect(notified, 2);
    });

    test('signIn refuses a token it cannot read', () async {
      final session = AuthSession(MemoryTokenStore());
      addTearDown(session.dispose);

      expect(await session.signIn('garbage'), isFalse);
      expect(session.isLoggedIn, isFalse);
    });

    test('signs the user out by itself when the token reaches its expiry', () async {
      final session = AuthSession(MemoryTokenStore());
      addTearDown(session.dispose);
      await session.signIn(makeJwt(validFor: const Duration(milliseconds: 1500)));
      expect(session.isLoggedIn, isTrue);

      await Future<void>.delayed(const Duration(milliseconds: 1700));

      expect(session.isLoggedIn, isFalse);
    });
  });

  group('AuthApi', () {
    late AuthSession session;
    late AuthApi api;

    setUp(() {
      session = AuthSession(MemoryTokenStore());
      api = AuthApi(
        session: session,
        allowedRoles: const {Roles.admin},
        roleNotAllowedMessage: 'Samo administratori.',
      );
    });

    tearDown(() => session.dispose());

    Map<String, dynamic> authBody(String token) => {
          'token': token,
          'expiresAtUtc': '2026-10-16T13:00:00Z',
          'userId': 1,
          'email': 'desktop',
          'firstName': 'Admin',
          'lastName': 'Korisnik',
          'role': 'Admin',
        };

    test('sends the credentials in the POST body, never the query string, and starts the session', () async {
      late ({String method, String url, String body}) captured;
      installClient((request) async {
        captured = (method: request.method, url: request.url.toString(), body: request.body);
        return jsonResponse(authBody(makeJwt()));
      });

      await api.login(email: 'desktop', password: 'test');

      expect(captured.method, 'POST');
      expect(captured.url, endsWith('/api/auth/login'));
      expect(captured.url, isNot(contains('password')));
      expect(jsonDecode(captured.body), {'email': 'desktop', 'password': 'test'});
      expect(session.isLoggedIn, isTrue);
      expect(session.role, Roles.admin);
    });

    test('turns away a role the app is not built for and revokes the token it was just given', () async {
      final requests = <String>[];
      installClient((request) async {
        requests.add('${request.method} ${request.url.path}');
        return request.url.path.endsWith('login') ? jsonResponse(authBody(makeJwt(role: 'Student'))) : http.Response('', 204);
      });

      final failure = await api.login(email: 'mobile', password: 'test').then<ApiException?>((_) => null, onError: (Object e) => e as ApiException);

      expect(failure?.message, 'Samo administratori.');
      expect(session.isLoggedIn, isFalse);
      expect(requests, ['POST /api/auth/login', 'POST /api/auth/logout']);
    });

    test('shows the backend\'s own message when the credentials are wrong', () async {
      installClient((request) async => jsonResponse({'message': 'Pogrešan e-mail ili lozinka.', 'errors': null}, 401));

      final failure = await api.login(email: 'x', password: 'y').then<ApiException?>((_) => null, onError: (Object e) => e as ApiException);

      expect(failure?.statusCode, 401);
      expect(failure?.message, 'Pogrešan e-mail ili lozinka.');
      expect(session.isLoggedIn, isFalse);
    });

    test('logout revokes the token server-side and always clears the local session', () async {
      var logoutCalled = false;
      installClient((request) async {
        if (request.url.path.endsWith('logout')) {
          logoutCalled = true;
          expect(request.headers['Authorization'], startsWith('Bearer '));
        }
        return http.Response('', 204);
      });
      await session.signIn(makeJwt());

      await api.logout();

      expect(logoutCalled, isTrue);
      expect(session.isLoggedIn, isFalse);
    });

    test('logout still signs the user out locally when the server cannot be reached', () async {
      installClient((request) async => throw http.ClientException('offline'));
      await session.signIn(makeJwt());

      await api.logout();

      expect(session.isLoggedIn, isFalse);
    });
  });
}
