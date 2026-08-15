import { useState, useMemo } from 'react';
import { createPortal } from 'react-dom';
import { useTranslation } from 'react-i18next';
import { CHART_RANGES, rangeToParams } from '../utils/constants';
import { formatDateTime, formatNumber, formatDuration } from '../i18n/formatters';

/**
 * Hand-rolled stacked SVG bar chart (success stacked on failure). No charting
 * library. Missing buckets are normalized to zero before rendering. Tooltip is
 * portal-rendered so it escapes overflow containers. Theme-aware via CSS vars.
 */

const WIDTH = 900;
const DEFAULT_HEIGHT = 220;
const PAD = { top: 12, right: 12, bottom: 28, left: 44 };

function normalizeBuckets(summary, rangeId) {
  const range = CHART_RANGES.find((r) => r.id === rangeId) || CHART_RANGES[1];
  const params = rangeToParams(rangeId);
  const start = new Date(params.fromUtc).getTime();
  const stepMs = range.bucketMinutes * 60 * 1000;
  const count = range.buckets;

  const byStart = new Map();
  for (const bucket of summary?.buckets || []) {
    const key = Math.floor(new Date(bucket.bucketStartUtc).getTime() / stepMs) * stepMs;
    byStart.set(key, bucket);
  }

  const result = [];
  for (let i = 0; i < count; i += 1) {
    const bucketStart = start + i * stepMs;
    const key = Math.floor(bucketStart / stepMs) * stepMs;
    const existing = byStart.get(key);
    result.push(
      existing || {
        bucketStartUtc: new Date(bucketStart).toISOString(),
        bucketEndUtc: new Date(bucketStart + stepMs).toISOString(),
        successCount: 0,
        failureCount: 0,
        averageDurationMs: 0
      }
    );
  }
  return result;
}

function ActivityChart({ summary, rangeId = 'day', onBucketClick, height = DEFAULT_HEIGHT }) {
  const { t } = useTranslation();
  const [tooltip, setTooltip] = useState(null);
  const HEIGHT = height;

  const buckets = useMemo(() => normalizeBuckets(summary, rangeId), [summary, rangeId]);

  const maxTotal = useMemo(() => {
    let max = 0;
    for (const b of buckets) max = Math.max(max, (b.successCount || 0) + (b.failureCount || 0));
    return Math.max(max, 1);
  }, [buckets]);

  const plotW = WIDTH - PAD.left - PAD.right;
  const plotH = HEIGHT - PAD.top - PAD.bottom;
  const barGap = buckets.length > 60 ? 0.5 : 1.5;
  const barW = Math.max(1, plotW / buckets.length - barGap);

  const hasActivity = buckets.some((b) => (b.successCount || 0) + (b.failureCount || 0) > 0);

  const yTicks = [0, 0.5, 1].map((frac) => ({
    value: Math.round(maxTotal * frac),
    y: PAD.top + plotH - plotH * frac
  }));

  const xLabelStep = Math.max(1, Math.floor(buckets.length / 8));

  const showTooltip = (e, bucket) => {
    setTooltip({
      x: e.clientX,
      y: e.clientY,
      bucket
    });
  };

  return (
    <div className="activity-chart">
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} className="activity-chart-svg" preserveAspectRatio="xMidYMid meet" role="img" aria-label={t('chart.total')}>
        {/* Y axis grid + ticks */}
        <g className="chart-axis">
          {yTicks.map((tick) => (
            <g key={tick.y}>
              <line x1={PAD.left} y1={tick.y} x2={WIDTH - PAD.right} y2={tick.y} className="chart-gridline" />
              <text x={PAD.left - 8} y={tick.y + 4} textAnchor="end" className="chart-tick-label">
                {formatNumber(tick.value)}
              </text>
            </g>
          ))}
        </g>

        {/* Bars */}
        <g>
          {buckets.map((bucket, i) => {
            const success = bucket.successCount || 0;
            const failure = bucket.failureCount || 0;
            const total = success + failure;
            const x = PAD.left + i * (plotW / buckets.length);
            const failureH = (failure / maxTotal) * plotH;
            const successH = (success / maxTotal) * plotH;
            const failureY = PAD.top + plotH - failureH;
            const successY = failureY - successH;
            return (
              <g
                key={bucket.bucketStartUtc}
                className={onBucketClick ? 'chart-bar clickable' : 'chart-bar'}
                onMouseEnter={(e) => showTooltip(e, bucket)}
                onMouseMove={(e) => showTooltip(e, bucket)}
                onMouseLeave={() => setTooltip(null)}
                onClick={() => onBucketClick && total > 0 && onBucketClick(bucket)}
              >
                {/* Invisible hit area for zero buckets */}
                <rect x={x} y={PAD.top} width={barW} height={plotH} fill="transparent" />
                {failure > 0 && <rect x={x} y={failureY} width={barW} height={failureH} className="bar-failure" />}
                {success > 0 && <rect x={x} y={successY} width={barW} height={successH} className="bar-success" />}
              </g>
            );
          })}
        </g>

        {/* X axis labels */}
        <g className="chart-axis">
          {buckets.map((bucket, i) => {
            if (i % xLabelStep !== 0) return null;
            const x = PAD.left + i * (plotW / buckets.length) + barW / 2;
            const d = new Date(bucket.bucketStartUtc);
            const label = new Intl.DateTimeFormat(document.documentElement.lang || 'en', {
              hour: '2-digit',
              minute: '2-digit'
            }).format(d);
            return (
              <text key={bucket.bucketStartUtc} x={x} y={HEIGHT - 8} textAnchor="middle" className="chart-tick-label">
                {label}
              </text>
            );
          })}
        </g>
      </svg>

      <div className="chart-legend">
        <span className="legend-item">
          <span className="legend-swatch bar-success" /> {t('chart.success')}
        </span>
        <span className="legend-item">
          <span className="legend-swatch bar-failure" /> {t('chart.failure')}
        </span>
      </div>

      {!hasActivity && <p className="chart-empty">{t('chart.noData')}</p>}

      {tooltip &&
        createPortal(
          <div className="chart-tooltip" style={{ left: tooltip.x + 12, top: tooltip.y + 12 }}>
            <div className="tt-range">
              {formatDateTime(tooltip.bucket.bucketStartUtc)} – {formatDateTime(tooltip.bucket.bucketEndUtc)}
            </div>
            <div className="tt-row">
              <span>{t('chart.total')}</span>
              <span>{formatNumber((tooltip.bucket.successCount || 0) + (tooltip.bucket.failureCount || 0))}</span>
            </div>
            <div className="tt-row tone-success">
              <span>{t('chart.success')}</span>
              <span>{formatNumber(tooltip.bucket.successCount || 0)}</span>
            </div>
            <div className="tt-row tone-danger">
              <span>{t('chart.failure')}</span>
              <span>{formatNumber(tooltip.bucket.failureCount || 0)}</span>
            </div>
            <div className="tt-row">
              <span>{t('chart.avgDuration')}</span>
              <span>{formatDuration(tooltip.bucket.averageDurationMs || 0)}</span>
            </div>
          </div>,
          document.body
        )}
    </div>
  );
}

/**
 * Range selector segmented control shared by Home + Request History charts.
 */
export function ChartRangeSelector({ value, onChange, onRefresh, loading }) {
  const { t } = useTranslation();
  return (
    <div className="chart-range-selector">
      <div className="segmented">
        {CHART_RANGES.map((range) => (
          <button
            key={range.id}
            type="button"
            className={`segment ${value === range.id ? 'active' : ''}`}
            onClick={() => onChange(range.id)}
          >
            {t(range.labelKey)}
          </button>
        ))}
      </div>
      {onRefresh && (
        <button type="button" className="icon-button refresh" onClick={onRefresh} disabled={loading} aria-label={t('common.refresh')} title={t('common.refresh')}>
          <span className={loading ? 'spin' : ''}>↻</span>
        </button>
      )}
    </div>
  );
}

export default ActivityChart;
