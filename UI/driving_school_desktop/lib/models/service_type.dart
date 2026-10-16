/// Mirrors the backend's `ServiceTypeDto`.
class ServiceType {
  final int id;
  final String code;
  final String name;
  final double price;

  const ServiceType({
    required this.id,
    required this.code,
    required this.name,
    required this.price,
  });

  factory ServiceType.fromJson(Map<String, dynamic> json) => ServiceType(
      id: json['id'] as int,
      code: json['code'] as String,
      name: json['name'] as String,
      price: (json['price'] as num).toDouble(),
      );
}
