import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/service_type.dart';

class ServiceTypeProvider extends BaseProvider<ServiceType> {
  ServiceTypeProvider(AuthSession session) : super('service-types', session);

  @override
  ServiceType fromJson(Map<String, dynamic> json) => ServiceType.fromJson(json);
}
