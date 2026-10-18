import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/vehicle.dart';

class VehicleProvider extends BaseProvider<Vehicle> {
  VehicleProvider(AuthSession session) : super('vehicles', session);

  @override
  Vehicle fromJson(Map<String, dynamic> json) => Vehicle.fromJson(json);
}
