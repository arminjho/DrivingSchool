import 'package:http/http.dart' as http;

import 'base_provider.dart';

/// Timeout-bounded replacements for the top-level `http.get`/`http.post`/... helpers, for the
/// calls that don't fit [BaseProvider]'s CRUD shape (confirm, cancel, mark-read, capture, ...).
Future<http.Response> apiGet(Uri url, {Map<String, String>? headers, Duration? timeout}) =>
    BaseProvider.send(() => BaseProvider.client.get(url, headers: headers), timeout: timeout);

Future<http.Response> apiPost(Uri url, {Map<String, String>? headers, Object? body, Duration? timeout}) =>
    BaseProvider.send(() => BaseProvider.client.post(url, headers: headers, body: body), timeout: timeout);

Future<http.Response> apiPut(Uri url, {Map<String, String>? headers, Object? body, Duration? timeout}) =>
    BaseProvider.send(() => BaseProvider.client.put(url, headers: headers, body: body), timeout: timeout);

Future<http.Response> apiDelete(Uri url, {Map<String, String>? headers, Object? body, Duration? timeout}) =>
    BaseProvider.send(() => BaseProvider.client.delete(url, headers: headers), timeout: timeout);
