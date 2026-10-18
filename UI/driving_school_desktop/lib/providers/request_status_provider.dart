import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/request_status.dart';

class RequestStatusProvider extends BaseProvider<RequestStatus> {
  RequestStatusProvider(AuthSession session) : super('request-statuses', session);

  @override
  RequestStatus fromJson(Map<String, dynamic> json) => RequestStatus.fromJson(json);
}
