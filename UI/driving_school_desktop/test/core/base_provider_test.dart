import 'dart:async';
import 'dart:convert';

import 'package:driving_school_desktop/core/api_exception.dart';
import 'package:driving_school_desktop/core/auth_session.dart';
import 'package:driving_school_desktop/core/base_provider.dart';
import 'package:driving_school_desktop/models/city.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import '../support/test_support.dart';

class _CityProvider extends BaseProvider<City> {
  _CityProvider(AuthSession session) : super('cities', session);

  @override
  City fromJson(Map<String, dynamic> json) => City.fromJson(json);
}

void main() {
  late AuthSession session;
  late _CityProvider provider;

  Map<String, dynamic> cityJson(int id) =>
      {'id': id, 'name': 'Sarajevo', 'postalCode': '71000', 'countryId': 1, 'countryName': 'Bosna i Hercegovina'};

  setUp(() async {
    session = AuthSession(MemoryTokenStore());
    await session.signIn(makeJwt());
    provider = _CityProvider(session);
  });

  tearDown(() => session.dispose());

  test('asks for a page with the API\'s own parameter names and the bearer token', () async {
    late http.Request captured;
    installClient((request) async {
      captured = request;
      return jsonResponse({'resultList': [cityJson(1), cityJson(2)], 'count': 12});
    });

    final result = await provider.getPaged(const PagedQuery(
      page: 2,
      pageSize: 5,
      sortBy: 'Name',
      ascending: false,
      filters: {'name': 'Sar'},
    ));

    expect(captured.url.path, '/api/cities');
    expect(captured.url.queryParameters, {'page': '2', 'pageSize': '5', 'sortBy': 'Name', 'sortDirection': 'desc', 'name': 'Sar'});
    expect(captured.headers['Authorization'], 'Bearer ${session.token}');
    expect(result.count, 12);
    expect(result.resultList.map((c) => c.id), [1, 2]);
    expect(result.resultList.first.countryName, 'Bosna i Hercegovina');
  });

  test('decodes UTF-8 responses so Bosnian letters survive', () async {
    installClient((request) async => http.Response.bytes(
          utf8.encode('{"resultList":[{"id":1,"name":"Čapljina","postalCode":"88300","countryId":1,"countryName":"BiH"}],"count":1}'),
          200,
          headers: {'content-type': 'application/json'},
        ));

    final result = await provider.getPaged();

    expect(result.resultList.single.name, 'Čapljina');
  });

  test('an HTTP 401 signs the user out, which is what sends the app back to login', () async {
    installClient((request) async => jsonResponse({'message': 'Token je opozvan.', 'errors': null}, 401));

    final failure = await provider.getById(1).then<ApiException?>((_) => null, onError: (Object e) => e as ApiException);

    expect(failure?.isUnauthorized, isTrue);
    expect(session.isLoggedIn, isFalse);
  });

  test('keeps the backend\'s per-field validation messages, keyed by the lower-camel field name', () async {
    installClient((request) async => jsonResponse({
          'message': 'Dati podaci nisu validni.',
          'errors': {
            'Email': ['Unesite e-mail u formatu ime@domena.com.'],
            'StartsAtUtc': ['Termin mora biti u budućnosti.'],
          },
        }, 400));

    final failure = await provider.insert({}).then<ApiException?>((_) => null, onError: (Object e) => e as ApiException);

    expect(failure?.fieldErrors['email'], ['Unesite e-mail u formatu ime@domena.com.']);
    expect(failure?.fieldErrors['startsAtUtc'], ['Termin mora biti u budućnosti.']);
    // The headline is the specific message, not the generic "invalid data" one.
    expect(failure?.message, 'Unesite e-mail u formatu ime@domena.com.');
    expect(session.isLoggedIn, isTrue);
  });

  test('a business rule has no field errors and shows the backend message as is', () async {
    installClient((request) async => jsonResponse({'message': 'Termin je zauzet.', 'errors': null}, 409));

    final failure = await provider.insert({}).then<ApiException?>((_) => null, onError: (Object e) => e as ApiException);

    expect(failure?.statusCode, 409);
    expect(failure?.message, 'Termin je zauzet.');
    expect(failure?.fieldErrors, isEmpty);
  });

  test('a non-JSON error body falls back to a message that names the status', () async {
    installClient((request) async => http.Response('<html>Bad gateway</html>', 502));

    final failure = await provider.getById(1).then<ApiException?>((_) => null, onError: (Object e) => e as ApiException);

    expect(failure?.statusCode, 502);
    expect(failure?.message, contains('502'));
  });

  test('a request that never reaches the server is status 0, distinct from a rejection', () async {
    installClient((request) async => throw http.ClientException('connection refused'));

    final failure = await provider.getById(1).then<ApiException?>((_) => null, onError: (Object e) => e as ApiException);

    expect(failure?.isTransportFailure, isTrue);
  });

  test('a request that hangs is cut off by the timeout instead of spinning forever', () async {
    installClient((request) => Completer<http.Response>().future);

    final failure = await BaseProvider.send(
      () => BaseProvider.client.get(Uri.parse('http://localhost/never')),
      timeout: const Duration(milliseconds: 50),
    ).then<ApiException?>((_) => null, onError: (Object e) => e as ApiException);

    expect(failure?.isTransportFailure, isTrue);
  });

  test('insert and update send JSON, delete accepts an empty 204', () async {
    final seen = <String>[];
    installClient((request) async {
      seen.add('${request.method} ${request.url.path} ${request.body}');
      return request.method == 'DELETE' ? http.Response('', 204) : jsonResponse(cityJson(3), request.method == 'POST' ? 201 : 200);
    });

    await provider.insert({'name': 'Mostar'});
    await provider.update(3, {'name': 'Mostar'});
    await provider.delete(3);

    expect(seen, [
      'POST /api/cities {"name":"Mostar"}',
      'PUT /api/cities/3 {"name":"Mostar"}',
      'DELETE /api/cities/3 ',
    ]);
  });
}
