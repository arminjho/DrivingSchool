/// Mirrors the backend's `RequestStatusDto`.
class RequestStatus {
  final int id;
  final String code;
  final String name;
  final String? description;

  const RequestStatus({
    required this.id,
    required this.code,
    required this.name,
    this.description,
  });

  factory RequestStatus.fromJson(Map<String, dynamic> json) => RequestStatus(
      id: json['id'] as int,
      code: json['code'] as String,
      name: json['name'] as String,
      description: json['description'] as String?,
      );
}
