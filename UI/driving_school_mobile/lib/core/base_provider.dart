import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import '../l10n/app_strings.dart';
import '../models/paged_result.dart';
import 'api_config.dart';
import 'api_exception.dart';
import 'auth_session.dart';

/// What a list endpoint is asked for. Mirrors the backend's `BaseSearchObject` (page is
/// one-based, page size is capped server-side at 100) plus entity-specific [filters].
class PagedQuery {
  final int page;
  final int pageSize;
  final String? sortBy;
  final bool ascending;
  final Map<String, dynamic> filters;

  const PagedQuery({
    this.page = 1,
    this.pageSize = 10,
    this.sortBy,
    this.ascending = true,
    this.filters = const {},
  });

  Map<String, dynamic> toQuery() => {
        'page': page,
        'pageSize': pageSize,
        if (sortBy != null) 'sortBy': sortBy,
        if (sortBy != null) 'sortDirection': ascending ? 'asc' : 'desc',
        ...filters,
      };
}

/// Generic REST client for one API resource - one thin subclass per entity.
///
/// Every request goes through one shared [http.Client] under a timeout. An HTTP 401 clears the
/// [AuthSession], which is all it takes to send the user back to the login screen.
abstract class BaseProvider<T> {
  static http.Client _client = http.Client();

  /// Dart's default is no timeout, so on a dying connection a request never completes and never
  /// fails - the user would watch a spinner forever.
  static const Duration requestTimeout = Duration(seconds: 20);
  static const Duration fileTransferTimeout = Duration(seconds: 120);

  @visibleForTesting
  static set client(http.Client value) => _client = value;

  static http.Client get client => _client;

  final String _endpoint;
  final AuthSession _session;

  BaseProvider(String endpoint, AuthSession session)
      : _endpoint = endpoint,
        _session = session;

  T fromJson(Map<String, dynamic> json);

  Uri buildUri(String path, [Map<String, dynamic>? query]) => ApiConfig.uri(path, query);

  Map<String, String> authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  /// Runs one call under a timeout and turns transport failures into an [ApiException] with
  /// status 0, so callers can tell "never reached the server" from a real rejection.
  static Future<http.Response> send(
    Future<http.Response> Function() request, {
    Duration? timeout,
  }) async {
    try {
      return await request().timeout(timeout ?? requestTimeout);
    } on TimeoutException {
      throw const ApiException(statusCode: 0, message: AppStrings.requestTimeout);
    } on http.ClientException {
      throw const ApiException(statusCode: 0, message: AppStrings.serverUnreachable);
    }
  }

  /// Decodes a response, surfacing the backend's own validation messages and signing the user
  /// out on 401.
  dynamic decode(http.Response response, {bool allowEmptyBody = false}) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) return allowEmptyBody ? null : <String, dynamic>{};
      return jsonDecode(utf8.decode(response.bodyBytes));
    }

    if (response.statusCode == 401) _session.clear();
    throw parseError(response);
  }

  static ApiException parseError(http.Response response) {
    final fieldErrors = <String, List<String>>{};
    String? serverMessage;

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>;
      serverMessage = body['message'] as String?;
      (body['errors'] as Map<String, dynamic>?)?.forEach((field, messages) {
        fieldErrors[_lowerFirst(field)] = (messages as List<dynamic>).map((m) => '$m').toList();
      });
    } on Object {
      // Not a JSON error body (e.g. a proxy page) - fall back to the generic message below.
    }

    final firstFieldError = fieldErrors.values.expand((messages) => messages).firstOrNull;
    return ApiException(
      statusCode: response.statusCode,
      message: firstFieldError ?? serverMessage ?? AppStrings.requestRejected(response.statusCode),
      fieldErrors: fieldErrors,
    );
  }

  static String _lowerFirst(String value) =>
      value.isEmpty ? value : value[0].toLowerCase() + value.substring(1);

  Future<PagedResult<T>> getPaged([PagedQuery query = const PagedQuery()]) async {
    final response = await send(
      () => _client.get(buildUri('api/$_endpoint', query.toQuery()), headers: authHeaders()),
    );
    return PagedResult<T>.fromJson(decode(response) as Map<String, dynamic>, fromJson);
  }

  Future<T> getById(int id) async {
    final response = await send(() => _client.get(buildUri('api/$_endpoint/$id'), headers: authHeaders()));
    return fromJson(decode(response) as Map<String, dynamic>);
  }

  Future<T> insert(Map<String, dynamic> request) async {
    final response = await send(
      () => _client.post(buildUri('api/$_endpoint'), headers: authHeaders(), body: jsonEncode(request)),
      timeout: fileTransferTimeout,
    );
    return fromJson(decode(response) as Map<String, dynamic>);
  }

  Future<T> update(int id, Map<String, dynamic> request) async {
    final response = await send(
      () => _client.put(buildUri('api/$_endpoint/$id'), headers: authHeaders(), body: jsonEncode(request)),
      timeout: fileTransferTimeout,
    );
    return fromJson(decode(response) as Map<String, dynamic>);
  }

  Future<void> delete(int id) async {
    final response = await send(() => _client.delete(buildUri('api/$_endpoint/$id'), headers: authHeaders()));
    decode(response, allowEmptyBody: true);
  }
}
