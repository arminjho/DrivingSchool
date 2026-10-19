import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/paged_result.dart';
import 'base_provider.dart';
import 'error_text.dart';

/// State for one paged, searchable, sortable table.
///
/// Paging, search, filters and sorting always go to the API (a `PagedQuery`), never filtered in
/// memory. Each page replaces the last.
class PagedController<T> extends ChangeNotifier {
  PagedController({
    required this._fetch,
    this.pageSize = 10,
    this.searchParam = 'search',
    this.sortBy,
    this.ascending = true,
    Map<String, dynamic> filters = const {},
    this.debounce = const Duration(milliseconds: 350),
  }) : _filters = Map.of(filters);

  final Future<PagedResult<T>> Function(PagedQuery query) _fetch;
  final int pageSize;
  final String searchParam;
  final Duration debounce;

  String? sortBy;
  bool ascending;

  final Map<String, dynamic> _filters;
  String _search = '';
  Timer? _debounceTimer;
  int _requestId = 0;
  bool _isDisposed = false;

  int _page = 1;
  List<T> _items = const [];
  int _totalCount = 0;
  bool _isLoading = false;
  String? _error;

  /// One-based, like the API.
  int get page => _page;
  List<T> get items => _items;
  int get totalCount => _totalCount;
  bool get isLoading => _isLoading;
  String? get error => _error;
  String get search => _search;
  int get totalPages => _totalCount <= 0 ? 1 : (_totalCount + pageSize - 1) ~/ pageSize;

  Future<void> load() => _loadPage(1);

  Future<void> reload() => _loadPage(_page);

  Future<void> goToPage(int page) => _loadPage(page.clamp(1, totalPages));

  /// Debounced, so typing doesn't fire a request per keystroke.
  void setSearch(String text) {
    _search = text.trim();
    _debounceTimer?.cancel();
    _debounceTimer = Timer(debounce, load);
  }

  void setFilter(String key, Object? value) {
    if (value == null) {
      _filters.remove(key);
    } else {
      _filters[key] = value;
    }
    unawaited(load());
  }

  /// Sorting by the active column flips the direction; a new column starts ascending.
  void sortOn(String key) {
    if (sortBy == key) {
      ascending = !ascending;
    } else {
      sortBy = key;
      ascending = true;
    }
    unawaited(load());
  }

  Future<void> _loadPage(int page) async {
    final requestId = ++_requestId;
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final result = await _fetch(PagedQuery(
        page: page,
        pageSize: pageSize,
        sortBy: sortBy,
        ascending: ascending,
        filters: {..._filters, if (_search.isNotEmpty) searchParam: _search},
      ));

      // A newer request (another keystroke, another page) supersedes this answer.
      if (_isDisposed || requestId != _requestId) return;
      _page = page;
      _totalCount = result.count;
      _items = result.resultList;
    } on Object catch (error) {
      if (_isDisposed || requestId != _requestId) return;
      _error = failureCause(error);
    } finally {
      if (!_isDisposed && requestId == _requestId) {
        _isLoading = false;
        notifyListeners();
      }
    }
  }

  @override
  void dispose() {
    _isDisposed = true;
    _debounceTimer?.cancel();
    super.dispose();
  }
}
