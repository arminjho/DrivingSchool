/// Mirrors the backend's `PagedResult<T>` - the envelope every list endpoint returns.
class PagedResult<T> {
  final List<T> resultList;
  final int count;

  const PagedResult({required this.resultList, required this.count});

  factory PagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromJsonT,
  ) {
    final rawList = json['resultList'] as List<dynamic>? ?? const [];
    return PagedResult<T>(
      resultList: rawList.map((item) => fromJsonT(item as Map<String, dynamic>)).toList(),
      count: json['count'] as int? ?? 0,
    );
  }
}
