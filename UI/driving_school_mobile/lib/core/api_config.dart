/// The one place the API address is read, via `--dart-define=API_BASE_URL=...`. Defaults to the
/// Android emulator's alias for the host machine's `localhost`.
abstract final class ApiConfig {
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5000',
  );

  static Uri uri(String path, [Map<String, dynamic>? query]) {
    final base = baseUrl.endsWith('/') ? baseUrl : '$baseUrl/';
    final uri = Uri.parse('$base$path');
    if (query == null || query.isEmpty) return uri;

    final params = <String, String>{};
    query.forEach((key, value) {
      if (value != null) params[key] = '$value';
    });
    return uri.replace(queryParameters: params);
  }
}
