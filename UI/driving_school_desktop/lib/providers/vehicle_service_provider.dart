import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/vehicle_service.dart';

class VehicleServiceProvider extends BaseProvider<VehicleService> {
  VehicleServiceProvider(AuthSession session) : super('vehicle-services', session);

  @override
  VehicleService fromJson(Map<String, dynamic> json) => VehicleService.fromJson(json);
}
