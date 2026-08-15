/**
 * Hand-rolled fetch-based API client for the CodeHub backend. No axios: keeps
 * the bundle small and gives the API Explorer raw Response access.
 *
 * Auth model: single static key sent as `Authorization: Bearer <apiKey>` on
 * every request. On any 401 the client dispatches `auth:unauthorized` so the
 * AuthContext can log the operator out.
 */

/**
 * Base64url-encode a UTF-8 string (safe for use as a query-parameter value).
 */
function base64UrlEncode(value) {
  const b64 = btoa(unescape(encodeURIComponent(value)));
  return b64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

class ApiError extends Error {
  constructor(status, body) {
    super(`HTTP ${status}: ${body}`);
    this.name = 'ApiError';
    this.status = status;
    this.body = body;
  }
}

class ApiClient {
  /**
   * @param {string} baseUrl - Absolute server URL captured at login.
   * @param {string|null} apiKey - Static bearer key.
   */
  constructor(baseUrl, apiKey = null) {
    this.baseUrl = (baseUrl || '').replace(/\/$/, '');
    this.apiKey = apiKey;
  }

  _headers(extra = {}) {
    const headers = { 'Content-Type': 'application/json', ...extra };
    if (this.apiKey) headers['Authorization'] = `Bearer ${this.apiKey}`;
    return headers;
  }

  async _request(method, path, { query = null, body = null, headers = {} } = {}) {
    const url = new URL(this.baseUrl + path);
    if (query) {
      for (const [k, v] of Object.entries(query)) {
        if (v !== undefined && v !== null && v !== '') url.searchParams.append(k, v);
      }
    }

    const init = { method, headers: this._headers(headers) };
    if (body !== null && body !== undefined) init.body = JSON.stringify(body);

    const response = await fetch(url.toString(), init);

    if (response.status === 401) {
      window.dispatchEvent(new CustomEvent('auth:unauthorized'));
    }

    if (!response.ok) {
      const errorBody = await response.text().catch(() => '');
      throw new ApiError(response.status, errorBody || response.statusText);
    }

    if (response.status === 204) return null;
    const text = await response.text();
    if (!text) return null;
    try {
      return JSON.parse(text);
    } catch {
      return text;
    }
  }

  // ---------------------------------------------------------------- Auth + health

  /** Validate the static key. GET /v1.0/api/token → { authenticated: true }. */
  async validateToken() {
    const result = await this._request('GET', '/v1.0/api/token');
    if (!result || result.authenticated !== true) {
      throw new ApiError(401, 'Authentication failed');
    }
    return true;
  }

  async health() {
    return this._request('GET', '/v1.0/api/health');
  }

  // ---------------------------------------------------------------- Overview

  async getOverview() {
    return this._request('GET', '/v1.0/api/overview');
  }

  // ---------------------------------------------------------------- Repositories

  async getRepositories(filters = {}) {
    return this._request('GET', '/v1.0/api/repositories', { query: filters });
  }

  async getRepository(id) {
    return this._request('GET', `/v1.0/api/repositories/${encodeURIComponent(id)}`);
  }

  async includeRepository(id) {
    return this._request('POST', `/v1.0/api/repositories/${encodeURIComponent(id)}/include`);
  }

  async excludeRepository(id) {
    return this._request('POST', `/v1.0/api/repositories/${encodeURIComponent(id)}/exclude`);
  }

  async openRepository(id, target, dangerous = false) {
    return this._request('POST', `/v1.0/api/repositories/${encodeURIComponent(id)}/open`, {
      body: { target, dangerous }
    });
  }

  async getProject(id) {
    return this._request('GET', `/v1.0/api/projects/${encodeURIComponent(id)}`);
  }

  // ---------------------------------------------------------------- Scans

  async startScan(repositoryId = null) {
    return this._request('POST', '/v1.0/api/scan', { body: { repositoryId } });
  }

  async getScanStatus() {
    return this._request('GET', '/v1.0/api/scan/status');
  }

  async getScanRuns(limit = 50) {
    return this._request('GET', '/v1.0/api/scan/runs', { query: { limit } });
  }

  // ------------------------------------------------------------------
  // Directory picker (scan selection)
  // ------------------------------------------------------------------

  async browseDirectory(path = null) {
    // Base64url-encode the path so it survives the query string (paths contain '/', '\\', ':').
    const query = path ? { path: base64UrlEncode(path) } : {};
    return this._request('GET', '/v1.0/api/fs/browse', { query });
  }

  async setSelection(path, selected) {
    return this._request('POST', '/v1.0/api/scan/selection', { body: { path, selected } });
  }

  async getSelection() {
    return this._request('GET', '/v1.0/api/scan/selection');
  }

  // ---------------------------------------------------------------- Settings

  async getSettings() {
    return this._request('GET', '/v1.0/api/settings');
  }

  async getEditableSettings() {
    return this._request('GET', '/v1.0/api/settings/editable');
  }

  async updateSettings(settings) {
    return this._request('PUT', '/v1.0/api/settings', { body: settings });
  }

  // ---------------------------------------------------------------- Request history

  async getRequestHistory(filters = {}) {
    return this._request('GET', '/v1.0/api/request-history', { query: filters });
  }

  async getRequestHistorySummary(filters = {}) {
    return this._request('GET', '/v1.0/api/request-history/summary', { query: filters });
  }

  async getRequestHistoryEntry(id) {
    return this._request('GET', `/v1.0/api/request-history/${encodeURIComponent(id)}`);
  }

  async deleteRequestHistoryEntry(id) {
    return this._request('DELETE', `/v1.0/api/request-history/${encodeURIComponent(id)}`);
  }

  // ---------------------------------------------------------------- OpenAPI / Explorer

  async getOpenApiSpec() {
    return this._request('GET', '/openapi.json');
  }

  /**
   * Execute an arbitrary request built by the API Explorer. Returns the raw
   * Response so the caller can inspect status, headers, and streaming bodies.
   */
  async executeExplorer({ method, path, query, headers, body }) {
    const url = new URL(this.baseUrl + path);
    if (query) {
      for (const [k, v] of Object.entries(query)) {
        if (v !== undefined && v !== null && v !== '') url.searchParams.append(k, v);
      }
    }
    return fetch(url.toString(), {
      method,
      headers: this._headers(headers || {}),
      body: body !== null && body !== undefined && body !== '' ? body : undefined
    });
  }
}

export default ApiClient;
export { ApiError };
