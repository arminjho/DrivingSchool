import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/paged_result.dart';
import 'base_provider.dart';
import 'error_text.dart';

/// State for one paged, searchable list with infinite scroll: each page is appended to the one
/// before it ([loadMore]), and a new search or filter starts over from the first page.
///
/// Paging, search and filters always go to the API (a `PagedQuery`), never filtered in memory.
class PagedController<T> extends ChangeNotifier {
  PagedController({
    required this._fetch,
    this.pageSize = 10,
    this.searchParam = 'search',
    Map<String, dynamic> filters = const {},
    this.debounce = const Duration(milliseconds: 350),
  }) : _filters = Map.of(filters);

  final Future<PagedResult<T>> Function(PagedQuery query) _fetch;
  final int pageSize;
  final String searchParam;
  final Duration debounce;

  final Map<String, dynamic> _filters;
  String _search = '';
  Timer? _debounceTimer;
  int _requestId = 0;
  bool _isDisposed = false;

  int _page = 1;
  List<T> _items = const [];
  int _totalCount = 0;
  bool _isLoading = false;
  bool _isLoadingMore = false;
  String? _error;

  /// One-based, like the API.
  int get page => _page;
  List<T> get items => _items;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  String? get error => _error;
  String get search => _search;
  bool get hasMore => _items.length < _totalCount;

  Future<void> load() => _loadPage(1);

  Future<void> reload() => _loadPage(1);

  /// Loads the next page onto the end of the list.
  Future<void> loadMore() async {
    if (_isLoading || _isLoadingMore || !hasMore) return;
    await _loadPage(_page + 1, append: true);
  }

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

  Future<void> _loadPage(int page, {bool append = false}) async {
    final requestId = ++_requestId;
    if (append) {
      _isLoadingMore = true;
    } else {
      _isLoading = true;
      _error = null;
    }
    notifyListeners();

    try {
      final result = await _fetch(PagedQuery(
        page: page,
        pageSize: pageSize,
        filters: {..._filters, if (_search.isNotEmpty) searchParam: _search},
      ));

      // A newer request (another keystroke, another page) supersedes this answer.
      if (_isDisposed || requestId != _requestId) return;
      _page = page;
      _totalCount = result.count;
      _items = append ? [..._items, ...result.resultList] : result.resultList;
    } on Object catch (error) {
      if (_isDisposed || requestId != _requestId) return;
      _error = failureCause(error);
    } finally {
      if (!_isDisposed && requestId == _requestId) {
        _isLoading = false;
        _isLoadingMore = false;
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
