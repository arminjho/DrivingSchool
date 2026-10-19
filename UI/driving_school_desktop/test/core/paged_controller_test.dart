import 'dart:async';

import 'package:driving_school_desktop/core/base_provider.dart';
import 'package:driving_school_desktop/core/paged_controller.dart';
import 'package:driving_school_desktop/models/paged_result.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  late List<PagedQuery> queries;

  PagedResult<int> pageOf(PagedQuery query, {int total = 25}) {
    final first = (query.page - 1) * query.pageSize;
    final count = (total - first).clamp(0, query.pageSize);
    return PagedResult(resultList: List.generate(count, (i) => first + i), count: total);
  }

  PagedController<int> controller({Future<PagedResult<int>> Function(PagedQuery)? fetch}) =>
      PagedController<int>(
        fetch: fetch ??
            (query) async {
              queries.add(query);
              return pageOf(query);
            },
        pageSize: 10,
        searchParam: 'name',
        debounce: const Duration(milliseconds: 20),
      );

  setUp(() => queries = []);

  test('loads the first page and exposes paging numbers', () async {
    final paged = controller();
    addTearDown(paged.dispose);

    await paged.load();

    expect(paged.items.length, 10);
    expect(paged.totalCount, 25);
    expect(paged.totalPages, 3);
    expect(paged.page, 1);
    expect(paged.isLoading, isFalse);
  });

  test('going to another page replaces the rows; the request carries that page', () async {
    final paged = controller();
    addTearDown(paged.dispose);
    await paged.load();

    await paged.goToPage(3);

    expect(paged.items, [20, 21, 22, 23, 24]);
    expect(queries.last.page, 3);
  });

  test('search is debounced into one request, sent to the API, and returns to page 1', () async {
    final paged = controller();
    addTearDown(paged.dispose);
    await paged.load();
    await paged.goToPage(2);
    queries.clear();

    paged
      ..setSearch('S')
      ..setSearch('Sa')
      ..setSearch('Sar');
    await Future<void>.delayed(const Duration(milliseconds: 80));

    expect(queries.length, 1);
    expect(queries.single.page, 1);
    expect(queries.single.filters, {'name': 'Sar'});
  });

  test('sorting on the same column flips direction; a new column starts ascending', () async {
    final paged = controller();
    addTearDown(paged.dispose);
    await paged.load();

    paged.sortOn('Name');
    await Future<void>.delayed(Duration.zero);
    expect((queries.last.sortBy, queries.last.ascending), ('Name', true));

    paged.sortOn('Name');
    await Future<void>.delayed(Duration.zero);
    expect(queries.last.ascending, isFalse);

    paged.sortOn('Code');
    await Future<void>.delayed(Duration.zero);
    expect((queries.last.sortBy, queries.last.ascending), ('Code', true));
  });

  test('a failed load keeps the error and offers a retry through reload', () async {
    var fail = true;
    final paged = controller(fetch: (query) async => fail ? throw Exception('boom') : pageOf(query));
    addTearDown(paged.dispose);

    await paged.load();
    expect(paged.error, isNotNull);
    expect(paged.items, isEmpty);

    fail = false;
    await paged.reload();
    expect(paged.error, isNull);
    expect(paged.items, isNotEmpty);
  });

  test('a slow, superseded response never overwrites a newer one', () async {
    final slow = Completer<PagedResult<int>>();
    var call = 0;
    final paged = controller(fetch: (query) {
      call++;
      return call == 1 ? slow.future : Future.value(PagedResult(resultList: const [99], count: 1));
    });
    addTearDown(paged.dispose);

    final first = paged.load();
    await paged.load();
    slow.complete(const PagedResult(resultList: [1, 2, 3], count: 3));
    await first;

    expect(paged.items, [99]);
  });
}
