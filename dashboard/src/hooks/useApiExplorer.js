import { useState, useEffect, useCallback, useMemo } from 'react';
import {
  flattenOpenApiSpec,
  substitutePathParams,
  FALLBACK_OPERATIONS
} from '../utils/openApi';
import { STORAGE } from '../utils/constants';

const HISTORY_CAP = 12;

function loadHistory() {
  try {
    return JSON.parse(localStorage.getItem(STORAGE.explorerHistory) || '[]');
  } catch {
    return [];
  }
}

function headersToObject(headers) {
  const out = {};
  headers.forEach((value, key) => {
    out[key] = value;
  });
  return out;
}

/**
 * API Explorer state: loads the live OpenAPI spec, builds operation forms, and
 * executes requests through the authenticated ApiClient. Falls back to a
 * curated operation list when the spec is missing or empty.
 */
export function useApiExplorer(apiClient) {
  const [spec, setSpec] = useState(null);
  const [usingFallback, setUsingFallback] = useState(false);
  const [specLoading, setSpecLoading] = useState(true);
  const [operations, setOperations] = useState([]);
  const [operationId, setOperationId] = useState(null);
  const [pathParams, setPathParams] = useState({});
  const [queryParams, setQueryParams] = useState({});
  const [headers, setHeaders] = useState({});
  const [body, setBody] = useState('');
  const [response, setResponse] = useState(null);
  const [executing, setExecuting] = useState(false);
  const [history, setHistory] = useState(loadHistory);

  useEffect(() => {
    if (!apiClient) return;
    let cancelled = false;
    setSpecLoading(true);
    apiClient
      .getOpenApiSpec()
      .then((loaded) => {
        if (cancelled) return;
        const ops = flattenOpenApiSpec(loaded);
        if (ops.length > 0) {
          setSpec(loaded);
          setOperations(ops);
          setUsingFallback(false);
        } else {
          setOperations(FALLBACK_OPERATIONS);
          setUsingFallback(true);
        }
      })
      .catch(() => {
        if (cancelled) return;
        setOperations(FALLBACK_OPERATIONS);
        setUsingFallback(true);
      })
      .finally(() => {
        if (!cancelled) setSpecLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [apiClient]);

  const operation = useMemo(
    () => operations.find((op) => op.id === operationId) || null,
    [operations, operationId]
  );

  const persistHistory = useCallback((next) => {
    const trimmed = next.slice(0, HISTORY_CAP);
    setHistory(trimmed);
    try {
      localStorage.setItem(STORAGE.explorerHistory, JSON.stringify(trimmed));
    } catch {
      /* ignore */
    }
  }, []);

  const selectOperation = useCallback(
    (id) => {
      setOperationId(id);
      setResponse(null);
      const op = operations.find((o) => o.id === id);
      if (!op) return;
      const nextPath = {};
      const nextQuery = {};
      for (const param of op.parameters || []) {
        if (param.in === 'path') nextPath[param.name] = '';
        else if (param.in === 'query') nextQuery[param.name] = '';
      }
      setPathParams(nextPath);
      setQueryParams(nextQuery);
      setHeaders({});
      setBody('');
    },
    [operations]
  );

  const resolvedPath = useMemo(() => {
    if (!operation) return '';
    return substitutePathParams(operation.path, pathParams);
  }, [operation, pathParams]);

  const resolvedUrl = useMemo(() => {
    if (!operation || !apiClient) return '';
    const base = apiClient.baseUrl + resolvedPath;
    const entries = Object.entries(queryParams).filter(([, v]) => v !== '' && v !== undefined && v !== null);
    if (entries.length === 0) return base;
    const qs = entries.map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`).join('&');
    return `${base}?${qs}`;
  }, [operation, apiClient, resolvedPath, queryParams]);

  const execute = useCallback(async () => {
    if (!operation || !apiClient) return;
    setExecuting(true);
    const start = performance.now();
    try {
      const raw = await apiClient.executeExplorer({
        method: operation.method,
        path: resolvedPath,
        query: queryParams,
        headers,
        body
      });
      const text = await raw.text();
      const durationMs = performance.now() - start;
      const parsed = {
        status: raw.status,
        statusText: raw.statusText,
        headers: headersToObject(raw.headers),
        body: text,
        durationMs,
        byteLength: new Blob([text]).size
      };
      setResponse(parsed);
      persistHistory([
        {
          operationId: operation.id,
          method: operation.method,
          path: operation.path,
          pathParams,
          queryParams,
          headers,
          body,
          status: parsed.status,
          at: new Date().toISOString()
        },
        ...history
      ]);
    } catch (err) {
      setResponse({ status: 0, statusText: 'Network error', headers: {}, body: String(err), durationMs: performance.now() - start, byteLength: 0 });
    } finally {
      setExecuting(false);
    }
  }, [operation, apiClient, resolvedPath, queryParams, headers, body, pathParams, history, persistHistory]);

  const loadFromHistory = useCallback(
    (entry) => {
      setOperationId(entry.operationId);
      setPathParams(entry.pathParams || {});
      setQueryParams(entry.queryParams || {});
      setHeaders(entry.headers || {});
      setBody(entry.body || '');
      setResponse(null);
    },
    []
  );

  const deleteFromHistory = useCallback(
    (index) => {
      persistHistory(history.filter((_, i) => i !== index));
    },
    [history, persistHistory]
  );

  return {
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
    response,
    history,
    loadFromHistory,
    deleteFromHistory
  };
}

export default useApiExplorer;
