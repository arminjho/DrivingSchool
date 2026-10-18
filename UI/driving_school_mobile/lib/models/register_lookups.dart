import 'city.dart';
import 'vehicle_category.dart';

/// Mirrors the backend's `RegisterLookupsDto` - the only two reference-data lists the
/// registration form needs before it has a token.
class RegisterLookups {
  final List<City> cities;
  final List<VehicleCategory> vehicleCategories;

  const RegisterLookups({required this.cities, required this.vehicleCategories});

  factory RegisterLookups.fromJson(Map<String, dynamic> json) => RegisterLookups(
      cities: (json['cities'] as List<dynamic>? ?? const []).map((item) => City.fromJson(item as Map<String, dynamic>)).toList(),
      vehicleCategories: (json['vehicleCategories'] as List<dynamic>? ?? const []).map((item) => VehicleCategory.fromJson(item as Map<String, dynamic>)).toList(),
      );
}
