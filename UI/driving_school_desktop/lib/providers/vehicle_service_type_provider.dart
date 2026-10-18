import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/vehicle_service_type.dart';

class VehicleServiceTypeProvider extends BaseProvider<VehicleServiceType> {
  VehicleServiceTypeProvider(AuthSession session) : super('vehicle-service-types', session);

  @override
  VehicleServiceType fromJson(Map<String, dynamic> json) => VehicleServiceType.fromJson(json);
}
