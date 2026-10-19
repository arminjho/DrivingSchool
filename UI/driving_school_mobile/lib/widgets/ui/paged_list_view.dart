import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../core/paged_controller.dart';
import '../../l10n/app_strings.dart';
import 'app_search_field.dart';
import 'app_states.dart';

/// The mobile equivalent of the desktop `PagedTable`: "search + pull-to-refresh + infinite
/// scroll" is the shape every mobile list shares. [itemBuilder] draws one row - typically an
/// [AppListCard] - the chrome around it (search, loading skeleton, empty/error state, scroll to
/// load more) lives here once.
///
/// Built on [PagedController]: each scroll-triggered page is appended rather than replacing the
/// list, which is the natural mobile pattern (no page-number footer).
class PagedListView<T> extends StatefulWidget {
  final PagedController<T> controller;
  final Widget Function(BuildContext context, T item) itemBuilder;
  final String? title;
  final String searchHint;
  final bool showSearch;
  final String emptyTitle;
  final String emptyMessage;
  final IconData emptyIcon;
  final Widget? emptyAction;

  const PagedListView({
    super.key,
    required this.controller,
    required this.itemBuilder,
    this.title,
    this.searchHint = AppStrings.search,
    this.showSearch = true,
    this.emptyTitle = AppStrings.emptyTitle,
    this.emptyMessage = AppStrings.emptyMessage,
    this.emptyIcon = Icons.inbox_outlined,
    this.emptyAction,
  });

  @override
  State<PagedListView<T>> createState() => _PagedListViewState<T>();
}

class _PagedListViewState<T> extends State<PagedListView<T>> {
  final _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController.removeListener(_onScroll);
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels < _scrollController.position.maxScrollExtent - 200) return;
    widget.controller.loadMore();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.controller,
      builder: (context, _) {
        final controller = widget.controller;
        final hasSearch = controller.search.isNotEmpty;

        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (widget.title != null || widget.showSearch)
              Padding(
                padding: const EdgeInsets.fromLTRB(AppSpacing.md, AppSpacing.md, AppSpacing.md, AppSpacing.sm),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (widget.title != null) ...[
                      Text(widget.title!, style: context.text.headlineSmall),
                      const SizedBox(height: AppSpacing.sm),
                    ],
                    if (widget.showSearch)
                      AppSearchField(hint: widget.searchHint, onChanged: controller.setSearch),
                  ],
                ),
              ),
            Expanded(child: _body(context, controller, hasSearch)),
          ],
        );
      },
    );
  }

  Widget _body(BuildContext context, PagedController<T> controller, bool hasSearch) {
    if (controller.error != null && controller.items.isEmpty) {
      return AppErrorState(message: controller.error!, onRetry: controller.reload);
    }

    if (controller.isLoading && controller.items.isEmpty) {
      return ListView.separated(
        padding: const EdgeInsets.fromLTRB(AppSpacing.md, 0, AppSpacing.md, AppSpacing.md),
        physics: const NeverScrollableScrollPhysics(),
        itemCount: 6,
        separatorBuilder: (context, index) => const SizedBox(height: AppSpacing.sm),
        itemBuilder: (context, index) => const _CardSkeleton(),
      );
    }

    if (controller.items.isEmpty) {
      return AppEmptyState(
        icon: widget.emptyIcon,
        title: hasSearch ? AppStrings.noResults : widget.emptyTitle,
        message: hasSearch ? AppStrings.emptyForSearch(controller.search) : widget.emptyMessage,
        action: hasSearch ? null : widget.emptyAction,
      );
    }

    return RefreshIndicator(
      onRefresh: controller.reload,
      child: ListView.separated(
        controller: _scrollController,
        padding: const EdgeInsets.fromLTRB(AppSpacing.md, 0, AppSpacing.md, AppSpacing.md),
        itemCount: controller.items.length + (controller.isLoadingMore ? 1 : 0),
        separatorBuilder: (context, index) => const SizedBox(height: AppSpacing.sm),
        itemBuilder: (context, index) {
          if (index >= controller.items.length) {
            return const Padding(
              padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
              child: Center(child: SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))),
            );
          }
          return widget.itemBuilder(context, controller.items[index]);
        },
      ),
    );
  }
}

class _CardSkeleton extends StatelessWidget {
  const _CardSkeleton();

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Container(
      height: AppSizes.listCardMinHeight,
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: c.surface,
        borderRadius: AppRadius.all(AppRadius.lg),
        border: Border.all(color: c.border),
      ),
      child: const Row(
        children: [
          AppSkeleton(width: 40, height: 40),
          SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                AppSkeleton(width: 140, height: 12),
                SizedBox(height: AppSpacing.xs),
                AppSkeleton(width: 90, height: 10),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
