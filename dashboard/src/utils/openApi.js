/**
 * OpenAPI helpers for the API Explorer. Pure functions with no React
 * dependency so they can be unit-tested in isolation.
 */

const METHODS = ['get', 'post', 'put', 'delete', 'patch', 'head', 'options'];

/**
 * Flatten an OpenAPI spec into a flat list of operations.
 * Each: { id, tag, method, path, summary, parameters, requestBody, responses }
 */
export function flattenOpenApiSpec(spec) {
  if (!spec || !spec.paths) return [];
  const operations = [];
  for (const [path, pathItem] of Object.entries(spec.paths)) {
    if (!pathItem) continue;
    const sharedParams = pathItem.parameters || [];
    for (const method of METHODS) {
      const op = pathItem[method];
      if (!op) continue;
      const parameters = [...sharedParams, ...(op.parameters || [])];
      operations.push({
        id: op.operationId || `${method.toUpperCase()} ${path}`,
        tag: (op.tags && op.tags[0]) || 'default',
        method: method.toUpperCase(),
        path,
        summary: op.summary || op.description || '',
        parameters,
        requestBody: op.requestBody || null,
        responses: op.responses || {}
      });
    }
  }
  return operations;
}

/** Group operations by tag for the picker. */
export function groupOperationsByTag(operations) {
  const groups = {};
  for (const op of operations) {
    if (!groups[op.tag]) groups[op.tag] = [];
    groups[op.tag].push(op);
  }
  return Object.entries(groups)
    .map(([tag, ops]) => ({ tag, ops }))
    .sort((a, b) => a.tag.localeCompare(b.tag));
}

/** Resolve a $ref against the spec's components. */
function resolveRef(ref, spec) {
  if (!ref || !ref.startsWith('#/')) return null;
  const segments = ref.slice(2).split('/');
  let current = spec;
  for (const segment of segments) {
    if (!current) return null;
    current = current[segment];
  }
  return current || null;
}

/** Merge allOf/resolve $ref down to a plain schema. */
function resolveSchema(schema, spec, depth = 0) {
  if (!schema || depth > 8) return schema || {};
  if (schema.$ref) return resolveSchema(resolveRef(schema.$ref, spec), spec, depth + 1);
  if (schema.allOf) {
    const merged = { type: 'object', properties: {} };
    for (const part of schema.allOf) {
      const resolved = resolveSchema(part, spec, depth + 1);
      Object.assign(merged.properties, resolved.properties || {});
    }
    return merged;
  }
  return schema;
}

/** Default value for a single parameter. */
export function getParameterDefault(parameter) {
  const schema = parameter.schema || {};
  if (parameter.example !== undefined) return parameter.example;
  if (schema.example !== undefined) return schema.example;
  if (schema.default !== undefined) return schema.default;
  if (Array.isArray(schema.enum) && schema.enum.length > 0) return schema.enum[0];
  switch (schema.type) {
    case 'integer':
    case 'number':
      return '';
    case 'boolean':
      return 'false';
    default:
      return '';
  }
}

/** Build an example JSON body string from a requestBody schema. */
export function getRequestBodyTemplate(requestBody, spec) {
  if (!requestBody) return '';
  const content = requestBody.content || {};
  const json = content['application/json'];
  if (!json || !json.schema) return '';
  const schema = resolveSchema(json.schema, spec);
  const example = buildExample(schema, spec);
  return example === undefined ? '' : JSON.stringify(example, null, 2);
}

function buildExample(schema, spec, depth = 0) {
  if (!schema || depth > 6) return null;
  const resolved = resolveSchema(schema, spec, depth);
  if (resolved.example !== undefined) return resolved.example;
  if (resolved.default !== undefined) return resolved.default;
  if (Array.isArray(resolved.enum) && resolved.enum.length > 0) return resolved.enum[0];
  switch (resolved.type) {
    case 'object': {
      const obj = {};
      const props = resolved.properties || {};
      for (const [key, propSchema] of Object.entries(props)) {
        obj[key] = buildExample(propSchema, spec, depth + 1);
      }
      return obj;
    }
    case 'array':
      return [buildExample(resolved.items, spec, depth + 1)].filter((v) => v !== null);
    case 'integer':
    case 'number':
      return 0;
    case 'boolean':
      return false;
    case 'string':
      return resolved.format === 'date-time' ? new Date().toISOString() : '';
    default:
      return null;
  }
}

/** Replace {param} placeholders in a path with encoded values. */
export function substitutePathParams(path, pathParams) {
  return path.replace(/\{([^}]+)\}/g, (_, name) => {
    const value = pathParams[name];
    return value !== undefined && value !== '' ? encodeURIComponent(value) : `{${name}}`;
  });
}

/** Build curl / fetch / C# snippets for the composed request. */
export function buildCodeSnippets({ method, url, headers, body }) {
  const headerEntries = Object.entries(headers || {}).filter(([, v]) => v);

  const curlLines = [`curl -X ${method} '${url}'`];
  for (const [k, v] of headerEntries) curlLines.push(`  -H '${k}: ${v}'`);
  if (body) curlLines.push(`  -d '${body.replace(/'/g, "'\\''")}'`);
  const curl = curlLines.join(' \\\n');

  const fetchHeaders = JSON.stringify(Object.fromEntries(headerEntries), null, 2);
  const fetchLines = [
    `await fetch('${url}', {`,
    `  method: '${method}',`,
    `  headers: ${fetchHeaders}${body ? ',' : ''}`
  ];
  if (body) fetchLines.push(`  body: ${JSON.stringify(body)}`);
  fetchLines.push('});');
  const fetchSnippet = fetchLines.join('\n');

  const csLines = [
    'using System.Net.Http;',
    'HttpClient client = new HttpClient();',
    `HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("${method}"), "${url}");`
  ];
  for (const [k, v] of headerEntries) {
    if (k.toLowerCase() === 'content-type') continue;
    csLines.push(`request.Headers.TryAddWithoutValidation("${k}", "${v}");`);
  }
  if (body) {
    csLines.push(`request.Content = new StringContent(${JSON.stringify(body)}, System.Text.Encoding.UTF8, "application/json");`);
  }
  csLines.push('HttpResponseMessage response = await client.SendAsync(request);');
  const csharp = csLines.join('\n');

  return { curl, fetch: fetchSnippet, csharp };
}

/**
 * Curated fallback operations used when /openapi.json is missing or empty.
 */
export const FALLBACK_OPERATIONS = [
  { id: 'getHealth', tag: 'System', method: 'GET', path: '/v1.0/api/health', summary: 'Health check', parameters: [], requestBody: null, responses: {} },
  { id: 'getToken', tag: 'System', method: 'GET', path: '/v1.0/api/token', summary: 'Validate the static key', parameters: [], requestBody: null, responses: {} },
  { id: 'getOverview', tag: 'Repositories', method: 'GET', path: '/v1.0/api/overview', summary: 'Aggregate overview', parameters: [], requestBody: null, responses: {} },
  { id: 'getRepositories', tag: 'Repositories', method: 'GET', path: '/v1.0/api/repositories', summary: 'List repositories', parameters: [{ name: 'q', in: 'query', schema: { type: 'string' } }, { name: 'health', in: 'query', schema: { type: 'string' } }, { name: 'pageSize', in: 'query', schema: { type: 'integer' } }], requestBody: null, responses: {} },
  { id: 'getRepository', tag: 'Repositories', method: 'GET', path: '/v1.0/api/repositories/{id}', summary: 'Repository detail', parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'string' } }], requestBody: null, responses: {} },
  { id: 'startScan', tag: 'Scans', method: 'POST', path: '/v1.0/api/scan', summary: 'Start a scan', parameters: [], requestBody: { content: { 'application/json': { schema: { type: 'object', properties: { repositoryId: { type: 'string' } } } } } }, responses: {} },
  { id: 'getScanStatus', tag: 'Scans', method: 'GET', path: '/v1.0/api/scan/status', summary: 'Scan status', parameters: [], requestBody: null, responses: {} },
  { id: 'getScanRuns', tag: 'Scans', method: 'GET', path: '/v1.0/api/scan/runs', summary: 'Scan run history', parameters: [{ name: 'limit', in: 'query', schema: { type: 'integer' } }], requestBody: null, responses: {} },
  { id: 'getSettings', tag: 'System', method: 'GET', path: '/v1.0/api/settings', summary: 'Server settings', parameters: [], requestBody: null, responses: {} },
  { id: 'getRequestHistory', tag: 'Request History', method: 'GET', path: '/v1.0/api/request-history', summary: 'List request history', parameters: [{ name: 'method', in: 'query', schema: { type: 'string' } }, { name: 'pageSize', in: 'query', schema: { type: 'integer' } }], requestBody: null, responses: {} },
  { id: 'deleteRequestHistory', tag: 'Request History', method: 'DELETE', path: '/v1.0/api/request-history/{id}', summary: 'Delete a request', parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'string' } }], requestBody: null, responses: {} }
];
