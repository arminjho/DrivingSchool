/// Mirrors the backend's `VehicleServiceReportCategory`.
class VehicleServiceReportCategory {
  final String categoryCode;
  final String categoryName;
  final int vehicleCount;
  final int serviceCount;
  final double totalCost;

  const VehicleServiceReportCategory({
    required this.categoryCode,
    required this.categoryName,
    required this.vehicleCount,
    required this.serviceCount,
    required this.totalCost,
  });

  factory VehicleServiceReportCategory.fromJson(Map<String, dynamic> json) => VehicleServiceReportCategory(
      categoryCode: json['categoryCode'] as String,
      categoryName: json['categoryName'] as String,
      vehicleCount: json['vehicleCount'] as int,
      serviceCount: json['serviceCount'] as int,
      totalCost: (json['totalCost'] as num).toDouble(),
      );
}
