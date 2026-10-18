import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/vehicle_category.dart';

class VehicleCategoryProvider extends BaseProvider<VehicleCategory> {
  VehicleCategoryProvider(AuthSession session) : super('vehicle-categories', session);

  @override
  VehicleCategory fromJson(Map<String, dynamic> json) => VehicleCategory.fromJson(json);
}
