/**
 * WidgetData Embed Loader v1.0
 *
 * Nhúng script này vào bất kỳ trang web nào để hiển thị widget hoặc form từ WidgetData.
 *
 * Cách sử dụng – Widget Data (hiển thị dữ liệu):
 *   <div data-widget-id="5" data-api-base="https://your-api.com"></div>
 *   <script src="https://your-api.com/widget-loader.js"></script>
 *
 * Cách sử dụng – Trang (Page) chứa nhiều widget:
 *   <div data-page-slug="trang-chu" data-api-base="https://your-api.com" data-tenant-id="1"></div>
 *   <script src="https://your-api.com/widget-loader.js"></script>
 *
 * Cách sử dụng – Form widget:
 *   <div data-widget-id="8" data-widget-type="form" data-api-base="https://your-api.com"></div>
 *   <script src="https://your-api.com/widget-loader.js"></script>
 */
(function () {
    'use strict';

    var BASE_STYLE = [
        '.wd-widget{font-family:system-ui,sans-serif;border:1px solid #e0e0e0;border-radius:8px;padding:16px;margin:8px 0;background:#fff}',
        '.wd-widget-title{font-size:1rem;font-weight:600;margin-bottom:12px;color:#333}',
        '.wd-table{width:100%;border-collapse:collapse;font-size:.875rem}',
        '.wd-table th,.wd-table td{border:1px solid #e0e0e0;padding:8px 12px;text-align:left}',
        '.wd-table th{background:#f5f5f5;font-weight:600}',
        '.wd-metric-value{font-size:2rem;font-weight:700;color:#1976d2}',
        '.wd-metric-label{color:#666;font-size:.875rem}',
        '.wd-form-group{margin-bottom:12px}',
        '.wd-form-group label{display:block;margin-bottom:4px;font-size:.875rem;font-weight:500}',
        '.wd-form-group input,.wd-form-group textarea,.wd-form-group select{width:100%;padding:8px;border:1px solid #ccc;border-radius:4px;font-size:.875rem;box-sizing:border-box}',
        '.wd-btn{padding:8px 20px;background:#1976d2;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:.875rem}',
        '.wd-btn:hover{background:#1565c0}',
        '.wd-success{color:#2e7d32;background:#e8f5e9;border:1px solid #a5d6a7;padding:8px;border-radius:4px;margin-top:8px}',
        '.wd-error{color:#c62828;background:#ffebee;border:1px solid #ef9a9a;padding:8px;border-radius:4px;margin-top:8px}',
        '.wd-loading{color:#999;font-size:.875rem}',
        '.wd-page-grid{display:flex;flex-wrap:wrap;gap:12px}',
        '.wd-col-1{width:calc(8.33% - 12px)}.wd-col-2{width:calc(16.67% - 12px)}.wd-col-3{width:calc(25% - 12px)}',
        '.wd-col-4{width:calc(33.33% - 12px)}.wd-col-5{width:calc(41.67% - 12px)}.wd-col-6{width:calc(50% - 12px)}',
        '.wd-col-7{width:calc(58.33% - 12px)}.wd-col-8{width:calc(66.67% - 12px)}.wd-col-9{width:calc(75% - 12px)}',
        '.wd-col-10{width:calc(83.33% - 12px)}.wd-col-11{width:calc(91.67% - 12px)}.wd-col-12{width:100%}',
        '@media(max-width:600px){.wd-col-1,.wd-col-2,.wd-col-3,.wd-col-4,.wd-col-5,.wd-col-6,',
        '.wd-col-7,.wd-col-8,.wd-col-9,.wd-col-10,.wd-col-11,.wd-col-12{width:100%}}'
    ].join('');

    function injectStyles() {
        if (document.getElementById('wd-styles')) return;
        var s = document.createElement('style');
        s.id = 'wd-styles';
        s.textContent = BASE_STYLE;
        document.head.appendChild(s);
    }

    function apiUrl(base, path) {
        return base.replace(/\/$/, '') + path;
    }

    function fetchJson(url, method, body) {
        var opts = { method: method || 'GET', headers: { 'Content-Type': 'application/json' } };
        if (body) opts.body = JSON.stringify(body);
        return fetch(url, opts).then(function (r) {
            if (!r.ok) throw new Error('HTTP ' + r.status);
            return r.json();
        });
    }

    // ── Render template ───────────────────────────────────────────────────

    function renderTemplate(template, rows) {
        if (!template) return renderDefaultTable(rows);
        // Replace {{rows}} with a table, or interpolate simple variables
        if (template.indexOf('{{rows}}') !== -1) {
            var tbody = '';
            if (Array.isArray(rows)) {
                rows.forEach(function (row) {
                    var cells = Object.values(row).map(function (v) {
                        return '<td>' + escHtml(String(v ?? '')) + '</td>';
                    }).join('');
                    tbody += '<tr>' + cells + '</tr>';
                });
            }
            return template.replace('{{rows}}', tbody);
        }
        // Single-value interpolation: {{key}}
        if (!Array.isArray(rows) || rows.length === 0) return template;
        var row = rows[0];
        return template.replace(/\{\{(\w+)\}\}/g, function (_, key) {
            return escHtml(String(row[key] ?? ''));
        });
    }

    function renderDefaultTable(rows) {
        if (!Array.isArray(rows) || rows.length === 0) return '<p class="wd-loading">Không có dữ liệu.</p>';
        var cols = Object.keys(rows[0]);
        var th = cols.map(function (c) { return '<th>' + escHtml(c) + '</th>'; }).join('');
        var tr = rows.map(function (row) {
            var td = cols.map(function (c) { return '<td>' + escHtml(String(row[c] ?? '')) + '</td>'; }).join('');
            return '<tr>' + td + '</tr>';
        }).join('');
        return '<table class="wd-table"><thead><tr>' + th + '</tr></thead><tbody>' + tr + '</tbody></table>';
    }

    function escHtml(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // ── Render Form ───────────────────────────────────────────────────────

    function renderForm(container, widgetId, schema, apiBase, label) {
        var fields = (schema && schema.fields) ? schema.fields : [];
        var html = '<div class="wd-widget">';
        if (label) html += '<div class="wd-widget-title">' + escHtml(label) + '</div>';
        html += '<form id="wd-form-' + widgetId + '">';
        fields.forEach(function (f) {
            html += '<div class="wd-form-group"><label>' + escHtml(f.label || f.name) + '</label>';
            if (f.type === 'textarea') {
                html += '<textarea name="' + escHtml(f.name) + '" ' + (f.required ? 'required' : '') + '></textarea>';
            } else if (f.type === 'select' && f.options) {
                html += '<select name="' + escHtml(f.name) + '">' +
                    f.options.map(function (o) { return '<option value="' + escHtml(o) + '">' + escHtml(o) + '</option>'; }).join('') +
                    '</select>';
            } else {
                html += '<input type="' + escHtml(f.type || 'text') + '" name="' + escHtml(f.name) + '" ' + (f.required ? 'required' : '') + ' />';
            }
            html += '</div>';
        });
        html += '<button type="submit" class="wd-btn">' + escHtml((schema && schema.submitLabel) || 'Gửi') + '</button>';
        html += '<div id="wd-form-msg-' + widgetId + '"></div>';
        html += '</form></div>';
        container.innerHTML = html;

        var form = document.getElementById('wd-form-' + widgetId);
        var msgDiv = document.getElementById('wd-form-msg-' + widgetId);
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var data = {};
            var elements = form.elements;
            for (var i = 0; i < elements.length; i++) {
                var el = elements[i];
                if (el.name) data[el.name] = el.value;
            }
            fetchJson(apiUrl(apiBase, '/api/form/' + widgetId), 'POST', data)
                .then(function () {
                    msgDiv.innerHTML = '<div class="wd-success">' + escHtml((schema && schema.successMessage) || 'Đã gửi thành công!') + '</div>';
                    form.reset();
                })
                .catch(function (err) {
                    msgDiv.innerHTML = '<div class="wd-error">Lỗi: ' + escHtml(err.message) + '</div>';
                });
        });
    }

    // ── Render single widget ───────────────────────────────────────────────

    function loadWidget(container, widgetId, apiBase) {
        var type = (container.dataset.widgetType || '').toLowerCase();
        container.innerHTML = '<p class="wd-loading">Đang tải...</p>';

        if (type === 'form') {
            fetchJson(apiUrl(apiBase, '/api/form/' + widgetId + '/schema'))
                .then(function (schema) {
                    renderForm(container, widgetId, schema, apiBase, schema && schema.title);
                })
                .catch(function (err) {
                    container.innerHTML = '<p class="wd-error">Không thể tải form: ' + escHtml(err.message) + '</p>';
                });
            return;
        }

        fetchJson(apiUrl(apiBase, '/api/widgets/' + widgetId + '/embed'))
            .then(function (result) {
                var rows = result.data || result;
                var template = container.dataset.template || result.htmlTemplate || null;
                var label = container.dataset.label || result.friendlyLabel || result.name || '';
                var html = '<div class="wd-widget">';
                if (label) html += '<div class="wd-widget-title">' + escHtml(label) + '</div>';
                html += renderTemplate(template, rows);
                html += '</div>';
                container.innerHTML = html;
            })
            .catch(function (err) {
                container.innerHTML = '<p class="wd-error">Không thể tải widget: ' + escHtml(err.message) + '</p>';
            });
    }

    // ── Render page (slug) ─────────────────────────────────────────────────

    function loadPage(container, slug, apiBase, tenantId) {
        container.innerHTML = '<p class="wd-loading">Đang tải trang...</p>';
        var url = apiUrl(apiBase, '/api/pages/public/' + encodeURIComponent(slug));
        if (tenantId) url += '?tenantId=' + tenantId;

        fetchJson(url)
            .then(function (page) {
                var grid = document.createElement('div');
                grid.className = 'wd-page-grid';
                container.innerHTML = '';
                container.appendChild(grid);

                (page.widgets || []).forEach(function (pw) {
                    var col = document.createElement('div');
                    col.className = 'wd-col-' + (pw.width || 6);

                    var inner = document.createElement('div');
                    inner.dataset.widgetId = pw.widgetId;
                    inner.dataset.widgetType = pw.widgetType ? pw.widgetType.toLowerCase() : 'data';
                    inner.dataset.apiBase = apiBase;
                    if (pw.friendlyLabel) inner.dataset.label = pw.friendlyLabel;
                    if (pw.htmlTemplate) inner.dataset.template = pw.htmlTemplate;
                    col.appendChild(inner);
                    grid.appendChild(col);

                    if ((pw.widgetType || '').toLowerCase() === 'form') {
                        fetchJson(apiUrl(apiBase, '/api/form/' + pw.widgetId + '/schema'))
                            .then(function (schema) {
                                renderForm(inner, pw.widgetId, schema, apiBase, pw.friendlyLabel);
                            })
                            .catch(function () {
                                inner.innerHTML = '<p class="wd-error">Không thể tải form.</p>';
                            });
                    } else {
                        loadWidget(inner, pw.widgetId, apiBase);
                    }
                });
            })
            .catch(function (err) {
                container.innerHTML = '<p class="wd-error">Không thể tải trang: ' + escHtml(err.message) + '</p>';
            });
    }

    // ── Bootstrap ─────────────────────────────────────────────────────────

    function init() {
        injectStyles();

        // Load individual widgets
        var widgetEls = document.querySelectorAll('[data-widget-id]');
        for (var i = 0; i < widgetEls.length; i++) {
            var el = widgetEls[i];
            var apiBase = el.dataset.apiBase || window.WIDGETDATA_API_BASE || '';
            if (!apiBase) { console.warn('WidgetData: data-api-base is required'); continue; }
            loadWidget(el, el.dataset.widgetId, apiBase);
        }

        // Load pages
        var pageEls = document.querySelectorAll('[data-page-slug]');
        for (var j = 0; j < pageEls.length; j++) {
            var pel = pageEls[j];
            var pBase = pel.dataset.apiBase || window.WIDGETDATA_API_BASE || '';
            if (!pBase) { console.warn('WidgetData: data-api-base is required'); continue; }
            loadPage(pel, pel.dataset.pageSlug, pBase, pel.dataset.tenantId || null);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
