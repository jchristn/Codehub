import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { PAGE_SIZE_OPTIONS } from '../utils/constants';

/**
 * Above-table pagination toolbar: total count, visible range, page size,
 * first/prev/jump/next/last, and refresh.
 */
function Pagination({
  pageNumber,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  onRefresh,
  loading = false,
  extraControls = null
}) {
  const { t } = useTranslation();
  const [jump, setJump] = useState('');
  const totalPages = Math.max(1, Math.ceil((totalCount || 0) / pageSize));
  const from = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const to = Math.min(pageNumber * pageSize, totalCount || 0);

  const go = (page) => {
    const clamped = Math.min(Math.max(1, page), totalPages);
    onPageChange(clamped);
  };

  const submitJump = (e) => {
    e.preventDefault();
    const parsed = parseInt(jump, 10);
    if (!Number.isNaN(parsed)) go(parsed);
    setJump('');
  };

  return (
    <div className="pagination-bar">
      <div className="pagination-info">
        <span>{t('pagination.showing', { from, to, total: totalCount ?? 0 })}</span>
        <span className="pagination-page">{t('pagination.page', { page: pageNumber, pages: totalPages })}</span>
      </div>

      <div className="pagination-controls">
        {extraControls}

        <label className="pagination-size">
          <span className="sr-only">{t('pagination.pageSize')}</span>
          <select
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            aria-label={t('pagination.pageSize')}
          >
            {PAGE_SIZE_OPTIONS.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>

        <div className="pagination-buttons">
          <button type="button" className="icon-button" onClick={() => go(1)} disabled={pageNumber <= 1} aria-label={t('pagination.first')} title={t('pagination.first')}>
            «
          </button>
          <button type="button" className="icon-button" onClick={() => go(pageNumber - 1)} disabled={pageNumber <= 1} aria-label={t('pagination.prev')} title={t('pagination.prev')}>
            ‹
          </button>
          <form className="pagination-jump" onSubmit={submitJump}>
            <input
              type="number"
              min="1"
              max={totalPages}
              value={jump}
              onChange={(e) => setJump(e.target.value)}
              placeholder={String(pageNumber)}
              aria-label={t('pagination.jump')}
            />
          </form>
          <button type="button" className="icon-button" onClick={() => go(pageNumber + 1)} disabled={pageNumber >= totalPages} aria-label={t('pagination.next')} title={t('pagination.next')}>
            ›
          </button>
          <button type="button" className="icon-button" onClick={() => go(totalPages)} disabled={pageNumber >= totalPages} aria-label={t('pagination.last')} title={t('pagination.last')}>
            »
          </button>
        </div>

        {onRefresh && (
          <button type="button" className="icon-button refresh" onClick={onRefresh} disabled={loading} aria-label={t('common.refresh')} title={t('common.refresh')}>
            <span className={loading ? 'spin' : ''}>↻</span>
          </button>
        )}
      </div>
    </div>
  );
}

export default Pagination;
