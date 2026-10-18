import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/driving_zone.dart';

class DrivingZoneProvider extends BaseProvider<DrivingZone> {
  DrivingZoneProvider(AuthSession session) : super('driving-zones', session);

  @override
  DrivingZone fromJson(Map<String, dynamic> json) => DrivingZone.fromJson(json);
}
