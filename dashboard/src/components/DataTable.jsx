import { useTranslation } from 'react-i18next';

/**
 * Presentational data table with loading/empty/error states, sortable headers,
 * and row-click guards. Sorting and pagination are backend-driven by the
 * parent — this component only renders and reports sort clicks.
 *
 * columns: [{ key, label, className, sortKey, render(row) }]
 */
function DataTable({
  columns,
  rows,
  rowKey = (row, i) => row.id || i,
  onRowClick,
  loading = false,
  error = null,
  emptyMessage,
  sort = null,
  dir = 'asc',
  onSortChange
}) {
  const { t } = useTranslation();

  const handleHeaderClick = (col) => {
    if (!col.sortKey || !onSortChange) return;
    if (sort === col.sortKey) {
      onSortChange(col.sortKey, dir === 'asc' ? 'desc' : 'asc');
    } else {
      onSortChange(col.sortKey, 'asc');
    }
  };

  // Guard row clicks originating from interactive controls.
  const handleRowClick = (e, row) => {
    if (!onRowClick) return;
    const interactive = e.target.closest('button, a, input, select, textarea, label, [role="menu"], [data-no-row-click]');
    if (interactive) return;
    onRowClick(row);
  };

  return (
    <div className="data-table-frame">
      <div className="data-table-scroll">
        <table className="data-table">
          <thead>
            <tr>
              {columns.map((col) => {
                const sortable = Boolean(col.sortKey && onSortChange);
                const active = sort === col.sortKey;
                return (
                  <th
                    key={col.key}
                    scope="col"
                    className={`${col.className || ''} ${sortable ? 'sortable' : ''}`}
                    aria-sort={active ? (dir === 'asc' ? 'ascending' : 'descending') : undefined}
                    onClick={() => handleHeaderClick(col)}
                  >
                    <span className="th-content">
                      {col.label}
                      {sortable && <span className="sort-indicator">{active ? (dir === 'asc' ? '▲' : '▼') : '⇅'}</span>}
                    </span>
                  </th>
                );
              })}
            </tr>
            {columns.some((c) => c.renderFilter) && (
              <tr className="filter-row">
                {columns.map((col) => (
                  <th key={col.key} className="filter-cell">
                    {col.renderFilter ? col.renderFilter() : null}
                  </th>
                ))}
              </tr>
            )}
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={columns.length} className="table-state">
                  <div className="loading-spinner" />
                  <span>{t('common.loading')}</span>
                </td>
              </tr>
            ) : error ? (
              <tr>
                <td colSpan={columns.length} className="table-state error">
                  {error}
                </td>
              </tr>
            ) : !rows || rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="table-state">
                  {emptyMessage || t('common.noData')}
                </td>
              </tr>
            ) : (
              rows.map((row, i) => (
                <tr
                  key={rowKey(row, i)}
                  onClick={(e) => handleRowClick(e, row)}
                  className={onRowClick ? 'clickable' : ''}
                >
                  {columns.map((col) => (
                    <td key={col.key} className={col.className || ''}>
                      {col.render ? col.render(row) : row[col.key]}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default DataTable;
