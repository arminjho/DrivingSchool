/// Mirrors the backend's `DrivingZoneDto`.
class DrivingZone {
  final int id;
  final String name;

  const DrivingZone({
    required this.id,
    required this.name,
  });

  factory DrivingZone.fromJson(Map<String, dynamic> json) => DrivingZone(
      id: json['id'] as int,
      name: json['name'] as String,
      );
}
