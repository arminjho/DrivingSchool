/// A failed API call. [fieldErrors] carries the backend's per-field validation messages
/// (`{ "errors": { "Email": ["..."] } }`, keys lower-camel-cased) so a form can show each one
/// under its own control instead of one generic message.
class ApiException implements Exception {
  /// 0 means the request never reached the server (timeout, no connection).
  final int statusCode;
  final String message;
  final Map<String, List<String>> fieldErrors;

  const ApiException({
    required this.statusCode,
    required this.message,
    this.fieldErrors = const {},
  });

  bool get isUnauthorized => statusCode == 401;
  bool get isTransportFailure => statusCode == 0;

  @override
  String toString() => message;
}
