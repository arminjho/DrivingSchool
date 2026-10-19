import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../core/paged_controller.dart';
import '../../l10n/app_strings.dart';
import 'app_data_table.dart';
import 'app_dialog.dart';
import 'app_messages.dart';
import 'app_search_field.dart';

/// "Title + search + primary action + grid with paging" - the page shape every desktop list
/// shares. The columns and forms differ per entity; the chrome around them lives here once.
///
/// Delete always goes through a confirmation dialog, and success/failure is reported with the
/// specific message the screen supplies.
class PagedTable<T> extends StatelessWidget {
  final PagedController<T> controller;
  final List<AppColumn<T>> columns;
  final String? title;
  final String? subtitle;
  final String searchHint;
  final bool showSearch;

  final VoidCallback? onAdd;
  final String addLabel;

  final void Function(T item)? onEdit;
  final Future<void> Function(T item)? onDelete;
  final String Function(T item)? itemLabel;

  /// The message shown after a successful delete, e.g. "Vozilo E22-A-123 je obrisano."
  final String Function(T item)? deletedMessage;

  final List<Widget> Function(T item)? extraRowActions;
  final List<Widget> extraActions;
  final void Function(T row)? onRowTap;
  final String emptyMessage;

  const PagedTable({
    super.key,
    required this.controller,
    required this.columns,
    this.title,
    this.subtitle,
    this.searchHint = AppStrings.search,
    this.showSearch = true,
    this.onAdd,
    this.addLabel = AppStrings.addNew,
    this.onEdit,
    this.onDelete,
    this.itemLabel,
    this.deletedMessage,
    this.extraRowActions,
    this.extraActions = const [],
    this.onRowTap,
    this.emptyMessage = AppStrings.emptyMessage,
  });

  Future<void> _confirmDelete(BuildContext context, T item) async {
    final label = itemLabel?.call(item) ?? '';
    final confirmed = await showConfirmDialog(
      context: context,
      title: AppStrings.confirmDeleteTitle,
      message: AppStrings.confirmDeleteMessage(label),
      confirmLabel: AppStrings.delete,
      destructive: true,
    );
    if (!confirmed || !context.mounted) return;

    try {
      await onDelete!(item);
      if (!context.mounted) return;
      final message = deletedMessage?.call(item);
      if (message != null) showSuccessMessage(context, message);
      await controller.reload();
    } on Object catch (error) {
      if (context.mounted) showErrorMessage(context, error);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        final hasSearch = controller.search.isNotEmpty;

        return Padding(
          padding: AppSpacing.page,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _toolbar(context),
              Expanded(
                child: AppDataTable<T>(
                  columns: columns,
                  rows: controller.items,
                  isLoading: controller.isLoading,
                  error: controller.error,
                  onRetry: controller.reload,
                  onRowTap: onRowTap,
                  sortKey: controller.sortBy,
                  sortAscending: controller.ascending,
                  onSort: controller.sortOn,
                  paging: AppTablePaging(page: controller.page, pageSize: controller.pageSize, totalCount: controller.totalCount),
                  onPageChanged: controller.goToPage,
                  emptyTitle: hasSearch ? AppStrings.noResults : AppStrings.emptyTitle,
                  emptyMessage: hasSearch ? AppStrings.emptyForSearch(controller.search) : emptyMessage,
                  emptyAction: onAdd != null && !hasSearch
                      ? FilledButton.icon(onPressed: onAdd, icon: const Icon(Icons.add_rounded, size: 18), label: Text(addLabel))
                      : null,
                  rowActions: (onEdit == null && onDelete == null && extraRowActions == null)
                      ? null
                      : (context, item) => [
                            ...?extraRowActions?.call(item),
                            if (onEdit != null)
                              AppRowAction(icon: Icons.edit_outlined, tooltip: AppStrings.edit, onPressed: () => onEdit!(item)),
                            if (onDelete != null)
                              AppRowAction(
                                icon: Icons.delete_outline_rounded,
                                tooltip: AppStrings.delete,
                                destructive: true,
                                onPressed: () => _confirmDelete(context, item),
                              ),
                          ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _toolbar(BuildContext context) {
    final c = context.colors;

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Wrap(
        alignment: title == null ? WrapAlignment.end : WrapAlignment.spaceBetween,
        crossAxisAlignment: WrapCrossAlignment.center,
        runSpacing: AppSpacing.sm,
        spacing: AppSpacing.md,
        children: [
          if (title != null)
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(title!, style: context.text.headlineSmall),
                if (subtitle != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(subtitle!, style: context.text.bodySmall?.copyWith(color: c.textMuted)),
                  ),
              ],
            ),
          Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (showSearch) ...[
                AppSearchField(hint: searchHint, width: 280, onChanged: controller.setSearch),
                const SizedBox(width: AppSpacing.xs),
              ],
              ...extraActions,
              if (onAdd != null)
                FilledButton.icon(onPressed: onAdd, icon: const Icon(Icons.add_rounded, size: 18), label: Text(addLabel)),
            ],
          ),
        ],
      ),
    );
  }
}
