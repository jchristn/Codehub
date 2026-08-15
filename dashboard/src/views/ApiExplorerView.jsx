import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import PageHeader from '../components/PageHeader';
import CopyButton from './../components/CopyButton';
import JsonBlock from '../components/JsonBlock';
import ConfirmModal from '../components/ConfirmModal';
import useApiExplorer from '../hooks/useApiExplorer';
import { groupOperationsByTag, getRequestBodyTemplate, buildCodeSnippets } from '../utils/openApi';
import { formatDuration, formatBytes } from '../i18n/formatters';

/**
 * OpenAPI-driven API Explorer. Auth is inherited from the logged-in ApiClient.
 * DELETE (and other destructive) operations go through a confirm modal.
 */
function ApiExplorerView({ apiClient }) {
  const { t } = useTranslation();
  const explorer = useApiExplorer(apiClient);
  const {
    spec,
    usingFallback,
    specLoading,
    operations,
    operation,
    operationId,
    selectOperation,
    pathParams,
    setPathParams,
    queryParams,
    setQueryParams,
    headers,
    setHeaders,
    body,
    setBody,
    resolvedUrl,
    execute,
    executing,
    response
  } = explorer;

  const [snippetTab, setSnippetTab] = useState('curl');
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [bodyError, setBodyError] = useState('');

  const groups = useMemo(() => groupOperationsByTag(operations), [operations]);

  const isDestructive = operation && (operation.method === 'DELETE' || operation.path.includes('/bulk'));

  const validateBody = () => {
    if (!body || !body.trim()) return true;
    try {
      JSON.parse(body);
      setBodyError('');
      return true;
    } catch {
      setBodyError(t('explorer.invalidJson'));
      return false;
    }
  };

  const runRequest = () => {
    if (!validateBody()) return;
    if (isDestructive) {
      setConfirmOpen(true);
    } else {
      execute();
    }
  };

  const confirmRun = () => {
    setConfirmOpen(false);
    execute();
  };

  const fillBodyTemplate = () => {
    const template = getRequestBodyTemplate(operation.requestBody, spec);
    if (template) setBody(template);
  };

  const snippets = useMemo(() => {
    if (!operation) return null;
    return buildCodeSnippets({ method: operation.method, url: resolvedUrl, headers, body });
  }, [operation, resolvedUrl, headers, body]);

  return (
    <div className="view api-explorer-view">
      <PageHeader title={t('explorer.title')} subtitle={t('explorer.subtitle')} />

      {usingFallback && <div className="banner banner-warning">{t('explorer.specMissing')}</div>}

      <div className="explorer-stack">
        {/* Operation picker — full-width dropdown */}
        <div className="panel explorer-op-picker">
          <label className="explorer-field-label" htmlFor="explorer-op-select">{t('explorer.operations')}</label>
          <select
            id="explorer-op-select"
            className="explorer-op-select mono"
            value={operationId || ''}
            onChange={(e) => selectOperation(e.target.value)}
            disabled={specLoading}
          >
            <option value="">{specLoading ? t('explorer.specLoading') : t('explorer.selectOp')}</option>
            {groups.map((group) => (
              <optgroup key={group.tag} label={group.tag}>
                {group.ops.map((op) => (
                  <option key={op.id} value={op.id}>
                    {op.method}  {op.path}
                  </option>
                ))}
              </optgroup>
            ))}
          </select>
        </div>

        {/* Request builder + response, stacked full width */}
        <section className="explorer-main panel">
          {!operation ? (
            <p className="muted explorer-placeholder">{t('explorer.selectOp')}</p>
          ) : (
            <>
              <div className="explorer-op-header">
                <span className={`method-badge method-${operation.method.toLowerCase()}`}>{operation.method}</span>
                <span className="mono explorer-op-title">{operation.path}</span>
              </div>
              {operation.summary && <p className="explorer-op-summary">{operation.summary}</p>}

              {/* Path params */}
              {Object.keys(pathParams).length > 0 && (
                <div className="explorer-field-group">
                  <h4 className="explorer-field-title">{t('explorer.pathParams')}</h4>
                  {Object.keys(pathParams).map((name) => (
                    <label className="explorer-field" key={name}>
                      <span className="explorer-field-label">{name} *</span>
                      <input value={pathParams[name]} onChange={(e) => setPathParams({ ...pathParams, [name]: e.target.value })} />
                    </label>
                  ))}
                </div>
              )}

              {/* Query params */}
              {Object.keys(queryParams).length > 0 && (
                <div className="explorer-field-group">
                  <h4 className="explorer-field-title">{t('explorer.queryParams')}</h4>
                  {Object.keys(queryParams).map((name) => (
                    <label className="explorer-field" key={name}>
                      <span className="explorer-field-label">{name}</span>
                      <input value={queryParams[name]} onChange={(e) => setQueryParams({ ...queryParams, [name]: e.target.value })} />
                    </label>
                  ))}
                </div>
              )}

              {/* Headers */}
              <div className="explorer-field-group">
                <h4 className="explorer-field-title">{t('explorer.headers')}</h4>
                <HeaderEditor headers={headers} onChange={setHeaders} />
              </div>

              {/* Body */}
              {['POST', 'PUT', 'PATCH', 'DELETE'].includes(operation.method) && (
                <div className="explorer-field-group">
                  <div className="explorer-field-title-row">
                    <h4 className="explorer-field-title">{t('explorer.body')}</h4>
                    {operation.requestBody && (
                      <button type="button" className="button-secondary tiny" onClick={fillBodyTemplate}>
                        Template
                      </button>
                    )}
                  </div>
                  <textarea
                    className="explorer-body mono"
                    rows={8}
                    value={body}
                    onChange={(e) => setBody(e.target.value)}
                    onBlur={validateBody}
                    placeholder="{ }"
                  />
                  {bodyError && <p className="error-message small">{bodyError}</p>}
                </div>
              )}

              {/* Resolved URL */}
              <div className="explorer-resolved">
                <span className="explorer-field-label">{t('explorer.resolvedUrl')}</span>
                <div className="explorer-url-row">
                  <code className="mono explorer-url">{resolvedUrl}</code>
                  <CopyButton value={resolvedUrl} label="URL" size="sm" />
                </div>
              </div>

              <button type="button" className="button-primary explorer-execute" onClick={runRequest} disabled={executing}>
                {executing ? t('explorer.executing') : t('explorer.execute')}
              </button>

              {/* Response */}
              {response && (
                <div className="explorer-response">
                  <div className="explorer-response-status">
                    <span className={`status-pill ${response.status >= 400 || response.status === 0 ? 'tone-danger' : 'tone-success'}`}>
                      {t('explorer.status')}: {response.status} {response.statusText}
                    </span>
                    <span className="muted">{t('explorer.duration')}: {formatDuration(response.durationMs)}</span>
                    <span className="muted">{formatBytes(response.byteLength)}</span>
                  </div>
                  <JsonBlock value={formatResponseBody(response.body)} label={t('explorer.response')} />
                </div>
              )}

              {/* Code snippets */}
              {snippets && (
                <div className="explorer-snippets">
                  <div className="explorer-field-title-row">
                    <h4 className="explorer-field-title">{t('explorer.codeSnippets')}</h4>
                    <div className="segmented tiny">
                      {['curl', 'fetch', 'csharp'].map((tab) => (
                        <button key={tab} type="button" className={`segment ${snippetTab === tab ? 'active' : ''}`} onClick={() => setSnippetTab(tab)}>
                          {tab}
                        </button>
                      ))}
                    </div>
                  </div>
                  <JsonBlock value={snippets[snippetTab]} label={snippetTab} />
                </div>
              )}
            </>
          )}
        </section>
      </div>

      <ConfirmModal
        open={confirmOpen}
        onConfirm={confirmRun}
        onCancel={() => setConfirmOpen(false)}
        title={t('explorer.confirmTitle')}
        body={t('explorer.confirmBody', { method: operation?.method })}
        confirmLabel={t('explorer.execute')}
      />
    </div>
  );
}

function HeaderEditor({ headers, onChange }) {
  const entries = Object.entries(headers);
  const update = (index, key, value) => {
    const next = {};
    entries.forEach(([k, v], i) => {
      if (i === index) next[key] = value;
      else next[k] = v;
    });
    onChange(next);
  };
  const add = () => onChange({ ...headers, '': '' });
  const remove = (key) => {
    const next = { ...headers };
    delete next[key];
    onChange(next);
  };
  return (
    <div className="header-editor">
      {entries.map(([key, value], i) => (
        <div className="header-editor-row" key={i}>
          <input className="mono" placeholder="Header" value={key} onChange={(e) => update(i, e.target.value, value)} />
          <input className="mono" placeholder="Value" value={value} onChange={(e) => update(i, key, e.target.value)} />
          <button type="button" className="icon-button" onClick={() => remove(key)} aria-label="Remove header">×</button>
        </div>
      ))}
      <button type="button" className="button-secondary tiny" onClick={add}>+ Header</button>
    </div>
  );
}

function formatResponseBody(text) {
  if (!text) return '';
  try {
    return JSON.stringify(JSON.parse(text), null, 2);
  } catch {
    return text;
  }
}

export default ApiExplorerView;
