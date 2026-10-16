/// Mirrors the backend's `CountryDto`.
class Country {
  final int id;
  final String code;
  final String name;

  const Country({
    required this.id,
    required this.code,
    required this.name,
  });

  factory Country.fromJson(Map<String, dynamic> json) => Country(
      id: json['id'] as int,
      code: json['code'] as String,
      name: json['name'] as String,
      );
}
