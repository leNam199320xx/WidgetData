/**
 * WidgetEngine – Lightweight JavaScript library for building landing pages,
 * product showcase pages, and sales pages powered by the WidgetData server.
 * Zero dependencies, plain ES2020.
 *
 * Two rendering modes:
 *   Static content (no API needed):
 *     WidgetEngine.render('#hero', {
 *       staticData: { headline: 'Ship faster', cta: 'Get started' },
 *       template: '<h1>{{headline}}</h1><a href="/signup">{{cta}}</a>'
 *     });
 *
 *   API-backed live data (requires WidgetData server):
 *     WidgetEngine.init({ baseUrl: 'https://api.example.com/api' });
 *     await WidgetEngine.auth.login('user@example.com', 'password');
 *     await WidgetEngine.page.load('pages/products.json', document.getElementById('app'));
 */
(function (global) {
  'use strict';

  /* =========================================================
   *  Internal state
   * ======================================================= */
  const _state = {
    baseUrl: '',
    token: null,
    refreshToken: null,
    tokenExpiresAt: null,
    autoRefresh: true,
    refreshTimerId: null,
  };

  /* =========================================================
   *  Utilities
   * ======================================================= */
  const Utils = {
    /** Merge objects (shallow). */
    merge(...objs) {
      return Object.assign({}, ...objs);
    },

    /** Build a query-string from a plain object. */
    toQuery(params) {
      if (!params || !Object.keys(params).length) return '';
      return '?' + Object.entries(params)
        .filter(([, v]) => v !== undefined && v !== null)
        .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
        .join('&');
    },

    /** Escape HTML to prevent XSS when inserting user data. */
    escapeHtml(str) {
      if (str === null || str === undefined) return '';
      return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
    },

    /** Deep-clone a plain JSON-serialisable value. */
    clone(obj) {
      return JSON.parse(JSON.stringify(obj));
    },

    /** Format a number with thousand separators. */
    formatNumber(n, decimals = 0) {
      return Number(n).toLocaleString(undefined, {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals,
      });
    },

    /** Format a date string to locale. */
    formatDate(dateStr, options = {}) {
      if (!dateStr) return '';
      return new Date(dateStr).toLocaleString(undefined, options);
    },
  };

  /* =========================================================
   *  HTTP Client
   * ======================================================= */
  const Http = {
    /** Low-level fetch wrapper that injects auth header and handles errors. */
    async request(method, path, body, options = {}) {
      const url = _state.baseUrl + path;
      const headers = { 'Content-Type': 'application/json' };

      if (_state.token) {
        headers['Authorization'] = `Bearer ${_state.token}`;
      }

      const fetchOpts = {
        method,
        headers,
        ...options,
      };

      if (body !== undefined && body !== null) {
        fetchOpts.body = JSON.stringify(body);
      }

      let response;
      try {
        response = await fetch(url, fetchOpts);
      } catch (networkErr) {
        throw new WidgetEngineError('NETWORK_ERROR', `Network error: ${networkErr.message}`);
      }

      if (response.status === 401 && _state.autoRefresh && _state.refreshToken) {
        // Try to refresh once
        const refreshed = await Auth._tryRefresh();
        if (refreshed) {
          fetchOpts.headers['Authorization'] = `Bearer ${_state.token}`;
          response = await fetch(url, fetchOpts);
        }
      }

      if (!response.ok) {
        let errBody = null;
        try { errBody = await response.json(); } catch (_) { /* ignore */ }
        const code = errBody?.error?.code || `HTTP_${response.status}`;
        const msg = errBody?.error?.message || response.statusText;
        throw new WidgetEngineError(code, msg, errBody);
      }

      if (response.status === 204) return null;
      return response.json();
    },

    get(path, params) {
      return Http.request('GET', path + Utils.toQuery(params));
    },

    post(path, body) {
      return Http.request('POST', path, body);
    },

    put(path, body) {
      return Http.request('PUT', path, body);
    },

    del(path) {
      return Http.request('DELETE', path);
    },
  };

  /* =========================================================
   *  Custom Error
   * ======================================================= */
  class WidgetEngineError extends Error {
    constructor(code, message, raw) {
      super(message);
      this.name = 'WidgetEngineError';
      this.code = code;
      this.raw = raw || null;
    }
  }

  /* =========================================================
   *  Auth Module
   * ======================================================= */
  const Auth = {
    /**
     * Login with email + password.
     * Stores token in memory; schedules auto-refresh if enabled.
     */
    async login(email, password) {
      const data = await Http.post('/auth/login', { email, password });
      Auth._applyTokenData(data);
      return data;
    },

    /** Refresh the current access token. */
    async refresh() {
      if (!_state.refreshToken) throw new WidgetEngineError('NO_REFRESH_TOKEN', 'No refresh token available');
      const data = await Http.post('/auth/refresh', { refreshToken: _state.refreshToken });
      Auth._applyTokenData(data);
      return data;
    },

    /** Logout: clear tokens and cancel scheduled refresh. */
    logout() {
      _state.token = null;
      _state.refreshToken = null;
      _state.tokenExpiresAt = null;
      if (_state.refreshTimerId) {
        clearTimeout(_state.refreshTimerId);
        _state.refreshTimerId = null;
      }
    },

    /** Returns true if we currently have a token (not necessarily valid). */
    isAuthenticated() {
      return !!_state.token;
    },

    /** Manually set token (e.g. from localStorage). */
    setToken(token, refreshToken, expiresAt) {
      _state.token = token;
      _state.refreshToken = refreshToken || null;
      _state.tokenExpiresAt = expiresAt ? new Date(expiresAt) : null;
      if (_state.autoRefresh) Auth._scheduleRefresh();
    },

    /* Internal ------------------------------------------------ */
    _applyTokenData(data) {
      _state.token = data.token;
      _state.refreshToken = data.refreshToken || null;
      _state.tokenExpiresAt = data.expiresAt ? new Date(data.expiresAt) : null;
      if (_state.autoRefresh) Auth._scheduleRefresh();
    },

    _scheduleRefresh() {
      if (_state.refreshTimerId) clearTimeout(_state.refreshTimerId);
      if (!_state.tokenExpiresAt || !_state.refreshToken) return;

      const msUntilExpiry = _state.tokenExpiresAt - Date.now();
      const refreshIn = Math.max(msUntilExpiry - 60_000, 1_000); // at least 1 s, 1 min before expiry
      _state.refreshTimerId = setTimeout(async () => {
        try { await Auth._tryRefresh(); } catch (_) { /* swallow */ }
      }, refreshIn);
    },

    async _tryRefresh() {
      try {
        await Auth.refresh();
        return true;
      } catch (_) {
        return false;
      }
    },
  };

  /* =========================================================
   *  Widgets API Module
   * ======================================================= */
  const Widgets = {
    /** List widgets with optional filters. */
    list(params) {
      return Http.get('/widgets', params);
    },

    /** Get a single widget by ID. */
    get(id) {
      return Http.get(`/widgets/${id}`);
    },

    /**
     * Execute a widget and return data.
     * @param {number} id
     * @param {object} [parameters] – key/value pairs for the widget's parameters
     * @param {boolean} [forceRefresh]
     */
    execute(id, parameters, forceRefresh = false) {
      return Http.post(`/widgets/${id}/execute`, { parameters: parameters || {}, forceRefresh });
    },

    /**
     * Get cached data for a widget.
     * @param {number} id
     * @param {object} [params] – page, pageSize, format
     */
    getData(id, params) {
      return Http.get(`/widgets/${id}/data`, params);
    },

    /** Get execution history for a widget. */
    getHistory(id, params) {
      return Http.get(`/widgets/${id}/history`, params);
    },

    /** Create a new widget. */
    create(payload) {
      return Http.post('/widgets', payload);
    },

    /** Update an existing widget. */
    update(id, payload) {
      return Http.put(`/widgets/${id}`, payload);
    },

    /** Delete a widget. */
    delete(id) {
      return Http.del(`/widgets/${id}`);
    },
  };

  /* =========================================================
   *  Data Sources API Module
   * ======================================================= */
  const DataSources = {
    list(params) { return Http.get('/datasources', params); },
    get(id) { return Http.get(`/datasources/${id}`); },
    create(payload) { return Http.post('/datasources', payload); },
    update(id, payload) { return Http.put(`/datasources/${id}`, payload); },
    delete(id) { return Http.del(`/datasources/${id}`); },
    test(id) { return Http.post(`/datasources/${id}/test`); },
  };

  /* =========================================================
   *  Schedules API Module
   * ======================================================= */
  const Schedules = {
    list(params) { return Http.get('/schedules', params); },
    get(id) { return Http.get(`/schedules/${id}`); },
    create(payload) { return Http.post('/schedules', payload); },
    update(id, payload) { return Http.put(`/schedules/${id}`, payload); },
    delete(id) { return Http.del(`/schedules/${id}`); },
    enable(id) { return Http.post(`/schedules/${id}/enable`); },
    disable(id) { return Http.post(`/schedules/${id}/disable`); },
  };

  /* =========================================================
   *  Dashboard API Module
   * ======================================================= */
  const Dashboard = {
    stats() { return Http.get('/dashboard/stats'); },
  };

  /* =========================================================
   *  Form API Module (PR #23)
   *
   *  Public endpoints (no auth needed):
   *    Form.getSchema(id)       → GET /api/form/{id}/schema
   *    Form.submit(id, data)    → POST /api/form/{id}
   *
   *  Admin/Manager endpoints:
   *    Form.getSubmissions(id)         → GET /api/form/{id}/submissions
   *    Form.deleteSubmission(subId)    → DELETE /api/form/submissions/{subId}
   * ======================================================= */
  const Form = {
    getSchema(widgetId) {
      return Http.get(`/form/${widgetId}/schema`);
    },
    submit(widgetId, data) {
      return Http.post(`/form/${widgetId}`, data);
    },
    getSubmissions(widgetId) {
      return Http.get(`/form/${widgetId}/submissions`);
    },
    deleteSubmission(id) {
      return Http.del(`/form/submissions/${id}`);
    },
  };

  /* =========================================================
   *  Widget Activity API Module (PR #22)
   *
   *  All endpoints require Admin or Manager role.
   *    WidgetActivity.getActivity(widgetId, params)  → GET /api/widget-activity/{widgetId}
   *    WidgetActivity.getSummary(widgetId)            → GET /api/widget-activity/{widgetId}/summary
   *    WidgetActivity.getInactive(thresholdDays)      → GET /api/widget-activity/inactive
   *    WidgetActivity.getAlerts()                     → GET /api/widget-activity/alerts
   * ======================================================= */
  const WidgetActivity = {
    getActivity(widgetId, params) {
      return Http.get(`/widget-activity/${widgetId}`, params);
    },
    getSummary(widgetId) {
      return Http.get(`/widget-activity/${widgetId}/summary`);
    },
    getInactive(thresholdDays = 30) {
      return Http.get('/widget-activity/inactive', { thresholdDays });
    },
    getAlerts() {
      return Http.get('/widget-activity/alerts');
    },
  };

  /* =========================================================
   *  Template Engine
   *
   *  Supports:
   *    {{variable}}             – escape-and-replace a value
   *    {{{variable}}}           – raw (unescaped) replace
   *    {{#each rows}} … {{/each}} – loop over array
   *    {{#if expr}} … {{/if}}   – simple truthy/falsy guard
   *    {{formatNumber value 2}} – built-in helpers
   *    {{formatDate value}}     – built-in helpers
   * ======================================================= */
  const Template = {
    /**
     * Render an HTML template string with the supplied data context.
     * @param {string} tpl  – template string
     * @param {object} data – data context { rows: [...], ...extraVars }
     * @returns {string}    – rendered HTML
     */
    render(tpl, data) {
      if (!tpl) return '';
      data = data || {};
      let result = tpl;

      // 1. {{#each rows}} … {{/each}}
      result = result.replace(/\{\{#each\s+(\w+)\s*\}\}([\s\S]*?)\{\{\/each\}\}/g, (_, key, inner) => {
        const arr = data[key];
        if (!Array.isArray(arr)) return '';
        return arr.map((item, idx) => Template._renderBlock(inner, item, idx)).join('');
      });

      // 2. {{#if var}} … {{/if}}
      result = result.replace(/\{\{#if\s+(\w+)\s*\}\}([\s\S]*?)\{\{\/if\}\}/g, (_, key, inner) => {
        return data[key] ? Template._renderBlock(inner, data, 0) : '';
      });

      // 3. Top-level variable substitution
      result = Template._renderBlock(result, data, 0);

      return result;
    },

    /** Substitute variables in a single block (no nesting). */
    _renderBlock(tpl, ctx, _idx) {
      // {{{raw}}} – unescaped
      let out = tpl.replace(/\{\{\{(\w+)\}\}\}/g, (_, key) => {
        const v = ctx[key];
        return v !== undefined ? String(v) : '';
      });

      // {{formatNumber key [decimals]}}
      out = out.replace(/\{\{formatNumber\s+(\w+)(?:\s+(\d+))?\}\}/g, (_, key, dec) => {
        const v = ctx[key];
        return v !== undefined ? Utils.formatNumber(v, dec ? parseInt(dec, 10) : 0) : '';
      });

      // {{formatDate key}}
      out = out.replace(/\{\{formatDate\s+(\w+)\}\}/g, (_, key) => {
        const v = ctx[key];
        return v !== undefined ? Utils.formatDate(v) : '';
      });

      // {{key}} – escaped
      out = out.replace(/\{\{(\w+)\}\}/g, (_, key) => {
        const v = ctx[key];
        return v !== undefined ? Utils.escapeHtml(v) : '';
      });

      return out;
    },
  };

  /* =========================================================
   *  Widget Renderer
   *
   *  Responsible for fetching data for a single widget config
   *  and inserting the rendered HTML into a DOM container.
   *
   *  Widget config shape (subset used by the renderer):
   *  {
   *    id: number,            – widget server ID (mutually exclusive with template)
   *    template: string,      – inline HTML template (optional, overrides server template)
   *    parameters: {},        – parameters forwarded to execute API
   *    forceRefresh: false,
   *    containerId: string,   – DOM element id to render into (used by page.load)
   *    title: string,         – optional display title injected before widget HTML
   *    autoRefreshSeconds: 0, – 0 = no auto-refresh
   *    onData: fn,            – optional callback(data, columns, meta)
   *    onError: fn,           – optional callback(error)
   *  }
   * ======================================================= */
  const Renderer = {
    _timers: new WeakMap(),

    /**
     * Render a widget into a given DOM element.
     *
     * Supports three modes:
     *   1. Static content  – supply `staticData` (object or array) + `template` in config;
     *                        no API call is made.  Great for landing pages, product cards, etc.
     *   2. API-backed data – supply `id` to fetch data from the WidgetData server.
     *   3. Form widget     – supply `formId` to render a dynamic form from the server schema.
     *
     * @param {HTMLElement} container
     * @param {object}      widgetConfig
     */
    async render(container, widgetConfig) {
      if (!container) throw new WidgetEngineError('INVALID_CONTAINER', 'Container element not found');

      container.classList.add('we-widget');
      container.setAttribute('data-widget-id', widgetConfig.id || widgetConfig.formId || '');

      // Static-content widgets skip the loading spinner for a snappier feel
      if (!Renderer._isStatic(widgetConfig) && !Renderer._isForm(widgetConfig)) {
        Renderer._setLoading(container, true);
      }

      try {
        // 1. Resolve data + template
        let html;
        if (Renderer._isStatic(widgetConfig)) {
          html = Renderer._renderStatic(widgetConfig);
        } else if (Renderer._isForm(widgetConfig)) {
          await Renderer._renderForm(container, widgetConfig);
          return; // _renderForm manages the DOM directly
        } else {
          const result = await Renderer._fetchData(widgetConfig);
          const tpl = widgetConfig.template
            || result.htmlTemplate
            || Renderer._defaultTemplate(result.columns);
          const rows = result.data || [];
          const ctx = Utils.merge({ rows }, Renderer._flattenFirst(rows), widgetConfig.extraContext || {});
          html = Template.render(tpl, ctx);

          if (typeof widgetConfig.onData === 'function') {
            widgetConfig.onData(rows, result.columns, result);
          }
        }

        // 2. Inject into DOM
        Renderer._setContent(container, html, widgetConfig.title);

        // 3. Schedule auto-refresh (API widgets only)
        if (!Renderer._isStatic(widgetConfig)) {
          Renderer._scheduleAutoRefresh(container, widgetConfig);
        }

      } catch (err) {
        Renderer._setError(container, err);
        if (typeof widgetConfig.onError === 'function') {
          widgetConfig.onError(err);
        }
      }
    },

    /** Returns true when the widget uses local staticData instead of an API call. */
    _isStatic(cfg) {
      return cfg.staticData !== undefined;
    },

    /** Returns true when the widget is a Form widget backed by the Form API. */
    _isForm(cfg) {
      return cfg.formId !== undefined;
    },

    /**
     * Render a Form widget by fetching the schema from the Form API.
     * On submit, POST data to the Form API and show success/error message.
     * Falls back to a static placeholder if the API is unreachable.
     *
     * @param {HTMLElement} container
     * @param {object}      cfg  – must have `formId`
     */
    async _renderForm(container, cfg) {
      const title = cfg.title || 'Liên hệ';
      container.innerHTML = `<div class="we-title">${Utils.escapeHtml(title)}</div>` +
        `<div class="we-body"><div class="we-loading"><span class="we-spinner"></span> Đang tải form…</div></div>`;

      let schema;
      try {
        schema = await Form.getSchema(cfg.formId);
      } catch (_) {
        // No API available – render a static placeholder
        container.innerHTML = `<div class="we-title">${Utils.escapeHtml(title)}</div>` +
          `<div class="we-body"><div class="we-form-placeholder">` +
          `<p>📝 Form Widget demo — kết nối WidgetData.API để hiển thị form thực tế.</p>` +
          `<p style="font-size:13px;color:#718096">Endpoint: <code>GET /api/form/${cfg.formId}/schema</code></p>` +
          `</div></div>`;
        return;
      }

      const fields = schema.fields || [];
      const submitLabel = schema.submitLabel || 'Gửi';
      const successMessage = schema.successMessage || 'Cảm ơn bạn đã gửi thông tin!';

      const fieldHtml = fields.map(f => {
        const req = f.required ? ' required' : '';
        const label = `<label class="we-form-label">${Utils.escapeHtml(f.label || f.name)}${f.required ? ' <span class="we-required">*</span>' : ''}</label>`;
        let input;
        if (f.type === 'textarea') {
          input = `<textarea class="we-form-input" name="${Utils.escapeHtml(f.name)}" rows="4" placeholder="${Utils.escapeHtml(f.label || '')}"${req}></textarea>`;
        } else if (f.type === 'select' && Array.isArray(f.options)) {
          const opts = f.options.map(o => `<option value="${Utils.escapeHtml(o)}">${Utils.escapeHtml(o)}</option>`).join('');
          input = `<select class="we-form-input" name="${Utils.escapeHtml(f.name)}"${req}><option value="">-- Chọn --</option>${opts}</select>`;
        } else {
          input = `<input class="we-form-input" type="${Utils.escapeHtml(f.type || 'text')}" name="${Utils.escapeHtml(f.name)}" placeholder="${Utils.escapeHtml(f.label || '')}"${req}>`;
        }
        return `<div class="we-form-group">${label}${input}</div>`;
      }).join('');

      const formId = `we-form-${cfg.formId}-${Math.random().toString(36).slice(2, 7)}`;
      container.innerHTML = `<div class="we-title">${Utils.escapeHtml(title)}</div>` +
        `<div class="we-body">` +
        `<form id="${formId}" class="we-form" novalidate>` +
        fieldHtml +
        `<div class="we-form-msg" id="${formId}-msg" style="display:none"></div>` +
        `<button type="submit" class="we-btn we-btn-primary we-form-submit">${Utils.escapeHtml(submitLabel)}</button>` +
        `</form></div>`;

      const formEl = document.getElementById(formId);
      const msgEl = document.getElementById(`${formId}-msg`);

      formEl.addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitBtn = formEl.querySelector('.we-form-submit');
        submitBtn.disabled = true;
        msgEl.style.display = 'none';

        const data = {};
        new FormData(formEl).forEach((v, k) => { data[k] = v; });

        try {
          await Form.submit(cfg.formId, data);
          formEl.reset();
          msgEl.className = 'we-form-msg we-form-msg--success';
          msgEl.textContent = successMessage;
          msgEl.style.display = 'block';
        } catch (err) {
          msgEl.className = 'we-form-msg we-form-msg--error';
          msgEl.textContent = err.message || 'Có lỗi xảy ra. Vui lòng thử lại.';
          msgEl.style.display = 'block';
        } finally {
          submitBtn.disabled = false;
        }
      });
    },

    /**
     * Render a static widget from its inline `staticData` + `template`.
     * `staticData` can be:
     *   – an object  → used directly as the template context
     *   – an array   → passed as `{ rows: [...] }` (same as API-backed widgets)
     */
    _renderStatic(cfg) {
      const tpl = cfg.template || '';
      const raw = cfg.staticData;
      const ctx = Array.isArray(raw)
        ? Utils.merge({ rows: raw }, Renderer._flattenFirst(raw), cfg.extraContext || {})
        : Utils.merge(raw, cfg.extraContext || {});
      return Template.render(tpl, ctx);
    },

    /** Fetch widget data from the server. */
    async _fetchData(cfg) {
      const result = await Widgets.execute(cfg.id, cfg.parameters, cfg.forceRefresh || false);
      // Attach server-side htmlTemplate if the widget detail has one (optional extra call)
      // For perf, we only fetch the widget detail when we need the template
      if (!cfg.template && !result.htmlTemplate) {
        try {
          const detail = await Widgets.get(cfg.id);
          result.htmlTemplate = detail?.configuration?.htmlTemplate || null;
        } catch (_) { /* not critical */ }
      }
      return result;
    },

    /** Build a basic table template from column metadata. */
    _defaultTemplate(columns) {
      if (!columns || !columns.length) {
        return '<p class="we-empty">No data</p>';
      }
      const headers = columns.map(c => `<th>${Utils.escapeHtml(c.name)}</th>`).join('');
      const cells = columns.map(c => `<td>{{${c.name}}}</td>`).join('');
      return `
        <table class="we-table">
          <thead><tr>${headers}</tr></thead>
          <tbody>
            {{#each rows}}
            <tr>${cells}</tr>
            {{/each}}
          </tbody>
        </table>`;
    },

    /** Flatten first row values into context for single-value widgets (metrics). */
    _flattenFirst(rows) {
      if (!rows || !rows.length) return {};
      return Utils.clone(rows[0]);
    },

    _setLoading(container, on) {
      if (on) {
        container.innerHTML = '<div class="we-loading"><span class="we-spinner"></span> Loading…</div>';
      }
    },

    _setContent(container, html, title) {
      if (title) {
        container.innerHTML = `<div class="we-title">${Utils.escapeHtml(title)}</div><div class="we-body">${html}</div>`;
      } else {
        // No title wrapper – used by full-bleed landing page sections
        container.innerHTML = html;
        container.classList.add('we-widget--raw');
      }
    },

    _setError(container, err) {
      container.innerHTML = `
        <div class="we-error">
          <strong>Error</strong>: ${Utils.escapeHtml(err.message)}
          ${err.code ? `<span class="we-error-code">(${Utils.escapeHtml(err.code)})</span>` : ''}
        </div>`;
    },

    _scheduleAutoRefresh(container, cfg) {
      const existing = Renderer._timers.get(container);
      if (existing) clearInterval(existing);
      if (cfg.autoRefreshSeconds && cfg.autoRefreshSeconds > 0) {
        const id = setInterval(() => {
          Renderer.render(container, cfg);
        }, cfg.autoRefreshSeconds * 1000);
        Renderer._timers.set(container, id);
      }
    },

    /** Stop the auto-refresh timer for a container element or id string. */
    stopAutoRefresh(containerOrId) {
      const el = typeof containerOrId === 'string'
        ? document.getElementById(containerOrId)
        : containerOrId;
      if (!el) return;
      const id = Renderer._timers.get(el);
      if (id) {
        clearInterval(id);
        Renderer._timers.delete(el);
      }
    },
  };

  /* =========================================================
   *  Page Builder
   *
   *  Loads a page config (JSON) and renders all widgets into
   *  the page DOM.
   *
   *  Page config shape:
   *  {
   *    title: "Home Dashboard",
   *    layout: "grid" | "flex" | "free",
   *    columns: 3,          – for grid layout
   *    auth: { required: true },
   *    widgets: [
   *      {
   *        id: 1,
   *        containerId: "widget-revenue",   – if omitted, auto-created
   *        col: 1, row: 1, colSpan: 2,      – grid position hints
   *        title: "Monthly Revenue",
   *        autoRefreshSeconds: 60,
   *        parameters: { start_date: "2026-01-01" }
   *      }
   *    ]
   *  }
   * ======================================================= */
  const Page = {
    _currentConfig: null,

    /**
     * Load a page config and render all its widgets into `rootElement`.
     * @param {string|object} configOrUrl – URL to a JSON page config, or inline config object
     * @param {HTMLElement}   rootElement – root element to render the page into
     */
    async load(configOrUrl, rootElement) {
      if (!rootElement) throw new WidgetEngineError('INVALID_ROOT', 'Root element not found');

      // Fetch config if a URL was supplied
      let config;
      if (typeof configOrUrl === 'string') {
        const resp = await fetch(configOrUrl);
        if (!resp.ok) throw new WidgetEngineError('CONFIG_LOAD_FAILED', `Cannot load page config: ${configOrUrl}`);
        config = await resp.json();
      } else {
        config = configOrUrl;
      }

      Page._currentConfig = config;

      // Auth guard
      if (config.auth?.required && !Auth.isAuthenticated()) {
        Page._renderLoginForm(rootElement, config, configOrUrl);
        return;
      }

      // Set page title
      if (config.title) {
        document.title = config.title;
        const h1 = rootElement.querySelector('.we-page-title');
        if (h1) h1.textContent = config.title;
      }

      // Build page layout
      rootElement.innerHTML = '';
      if (config.title) {
        const titleEl = document.createElement('h1');
        titleEl.className = 'we-page-title';
        titleEl.textContent = config.title;
        rootElement.appendChild(titleEl);
      }

      const grid = Page._buildGrid(config, rootElement);

      // Render widgets (in parallel)
      const renderPromises = (config.widgets || []).map(wCfg => {
        const container = Page._resolveContainer(wCfg, grid, config);
        return Renderer.render(container, wCfg);
      });

      await Promise.allSettled(renderPromises);
    },

    /** Build the layout wrapper and return the grid element. */
    _buildGrid(config, root) {
      const wrapper = document.createElement('div');
      wrapper.className = `we-page-grid we-layout-${config.layout || 'grid'}`;
      if (config.layout === 'grid' && config.columns) {
        wrapper.style.setProperty('--we-columns', config.columns);
      }
      root.appendChild(wrapper);
      return wrapper;
    },

    /**
     * Find or create a container element for a widget.
     * If `widgetConfig.containerId` is set, look it up in the document.
     * Otherwise create a new cell inside the grid.
     */
    _resolveContainer(wCfg, grid, pageConfig) {
      if (wCfg.containerId) {
        const el = document.getElementById(wCfg.containerId);
        if (el) return el;
      }

      // Auto-create cell
      const cell = document.createElement('div');
      const autoId = `we-widget-${wCfg.id}-${Math.random().toString(36).slice(2, 7)}`;
      cell.id = wCfg.containerId || autoId;
      cell.className = 'we-cell';

      // Grid span hints
      if (pageConfig.layout === 'grid') {
        if (wCfg.colSpan) cell.style.gridColumn = `span ${wCfg.colSpan}`;
        if (wCfg.rowSpan) cell.style.gridRow = `span ${wCfg.rowSpan}`;
      }

      grid.appendChild(cell);
      return cell;
    },

    /** Show a minimal inline login form when auth is required. */
    _renderLoginForm(rootElement, config, configOrUrl) {
      rootElement.innerHTML = `
        <div class="we-login-box">
          <h2 class="we-login-title">${Utils.escapeHtml(config.title || 'Login')}</h2>
          <form class="we-login-form" id="we-login-form" novalidate>
            <label for="we-email">Email</label>
            <input id="we-email" type="email" name="email" placeholder="you@example.com" required>
            <label for="we-password">Password</label>
            <input id="we-password" type="password" name="password" placeholder="Password" required>
            <button type="submit">Sign in</button>
            <p class="we-login-error" id="we-login-error" hidden></p>
          </form>
        </div>`;

      const form = document.getElementById('we-login-form');
      form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const email = document.getElementById('we-email').value.trim();
        const pwd = document.getElementById('we-password').value;
        const errEl = document.getElementById('we-login-error');
        errEl.hidden = true;
        try {
          await Auth.login(email, pwd);
          await Page.load(configOrUrl, rootElement);
        } catch (err) {
          errEl.textContent = err.message || 'Login failed';
          errEl.hidden = false;
        }
      });
    },
  };

  /* =========================================================
   *  Public API – WidgetEngine
   * ======================================================= */
  const WidgetEngine = {
    /**
     * Initialise the engine.
     * @param {object} config
     * @param {string}  config.baseUrl        – API base URL (no trailing slash)
     * @param {boolean} [config.autoRefresh]  – auto-refresh token (default true)
     * @param {string}  [config.token]        – pre-existing JWT token
     * @param {string}  [config.refreshToken]
     * @param {string}  [config.tokenExpiresAt]
     */
    init(config = {}) {
      _state.baseUrl = (config.baseUrl || '').replace(/\/$/, '');
      _state.autoRefresh = config.autoRefresh !== false;

      if (config.token) {
        Auth.setToken(config.token, config.refreshToken, config.tokenExpiresAt);
      }

      return WidgetEngine;
    },

    auth: Auth,
    widgets: Widgets,
    dataSources: DataSources,
    schedules: Schedules,
    dashboard: Dashboard,
    form: Form,
    widgetActivity: WidgetActivity,
    template: Template,
    page: Page,
    utils: Utils,

    /**
     * Shorthand: render a single widget into a container element or CSS selector.
     * @param {string|HTMLElement} target       – element id, CSS selector, or element
     * @param {object}             widgetConfig – widget config (must have `id`)
     */
    render(target, widgetConfig) {
      const el = typeof target === 'string' ? document.querySelector(target) : target;
      return Renderer.render(el, widgetConfig);
    },

    /** Stop auto-refresh for a container (element or id string). */
    stopAutoRefresh(containerOrId) {
      Renderer.stopAutoRefresh(containerOrId);
    },

    WidgetEngineError,
  };

  /* Expose globally */
  global.WidgetEngine = WidgetEngine;

})(typeof window !== 'undefined' ? window : globalThis);
