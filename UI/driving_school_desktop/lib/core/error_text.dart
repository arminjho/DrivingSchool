import '../l10n/app_strings.dart';
import 'api_exception.dart';

/// The "why it happened and what to do now" half of an error message.
///
/// A screen supplies the outcome ("Obavijesti nisu učitane"); this supplies the cause and the
/// next step, which depend only on how the call failed:
///   - the session died          -> sign in again
///   - the server failed         -> wait and retry
///   - the request was rejected  -> the backend already wrote a specific message; show it as is
///   - anything else             -> the server could not be reached
///
/// A 401 normally means the session died. When the call *is* the sign-in attempt
/// ([isSignIn]) it means wrong credentials instead, so the backend's own message is shown.
String failureCause(Object error, {bool isSignIn = false}) {
  if (error is! ApiException) return AppStrings.cannotReachServer;
  if (error.isUnauthorized && !isSignIn) return AppStrings.sessionExpired;
  if (error.statusCode >= 500) return AppStrings.serverFailure;
  return error.message;
}
