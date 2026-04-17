/* ============================================================
 * Store SPA – Cửa hàng WidgetData
 *
 * Features:
 *   - Hash-based SPA router (#/products, #/product/:id, #/search, #/cart)
 *   - Cart persisted in localStorage
 *   - All product data fetched from GET /api/store/*
 *   - POST /api/store/orders to place real orders
 * ============================================================ */

'use strict';

/* ── Configuration ─────────────────────────────────────────── */
const API_BASE = '/api';   // same origin as the API server

/* ── Utility helpers ────────────────────────────────────────── */
const fmt = price =>
  Number(price).toLocaleString('vi-VN') + '₫';

const esc = str => {
  if (str == null) return '';
  return String(str)
    .replace(/&/g, '&amp;').replace(/</g, '&lt;')
    .replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
};

const $ = (sel, root = document) => root.querySelector(sel);
const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];

function categoryEmoji(name = '') {
  const map = {
    'Điện thoại': '📱', 'Máy tính': '💻', 'Thiết bị âm thanh': '🎧',
    'Đồng hồ': '⌚', 'Thời trang nam': '👔', 'Thời trang nữ': '👗',
    'Thực phẩm': '🍱', 'Chăm sóc sức khỏe': '💊', 'Đồ gia dụng': '🏠', 'Sách': '📚',
  };
  for (const [k, v] of Object.entries(map))
    if (name.includes(k)) return v;
  return '📦';
}

function stockBadge(stock, unit = 'cái') {
  if (stock <= 0) return `<span class="badge badge--danger">Hết hàng</span>`;
  if (stock <= 5) return `<span class="badge badge--warn">Còn ${stock} ${esc(unit)}</span>`;
  return `<span class="badge badge--ok">Còn hàng</span>`;
}

/* ── API client ─────────────────────────────────────────────── */
const Api = {
  async _get(path, params = {}) {
    const qs = new URLSearchParams(
      Object.entries(params).filter(([, v]) => v !== '' && v != null)
    ).toString();
    const url = API_BASE + path + (qs ? '?' + qs : '');
    const res = await fetch(url);
    if (!res.ok) throw new Error(`HTTP ${res.status} – ${url}`);
    return res.json();
  },

  async _post(path, body) {
    const res = await fetch(API_BASE + path, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || `HTTP ${res.status}`);
    }
    return res.json();
  },

  getCategories: ()           => Api._get('/store/categories'),
  getProducts:   (p = {})     => Api._get('/store/products', p),
  getProduct:    (id)         => Api._get(`/store/products/${id}`),
  placeOrder:    (dto)        => Api._post('/store/orders', dto),
};

/* ── Toast notifications ────────────────────────────────────── */
const Toast = {
  _t: null,
  show(msg, type = 'success', ms = 3000) {
    const el = document.getElementById('toast');
    if (!el) return;
    el.className = `toast toast--show${type === 'error' ? ' toast--error' : ''}`;
    el.textContent = msg;
    clearTimeout(this._t);
    this._t = setTimeout(() => el.classList.remove('toast--show'), ms);
  },
};

/* ── Cart (localStorage) ────────────────────────────────────── */
const Cart = {
  _KEY: 'wd_store_cart_v1',

  get() {
    try { return JSON.parse(localStorage.getItem(this._KEY) || '[]'); } catch { return []; }
  },
  _save(items) {
    localStorage.setItem(this._KEY, JSON.stringify(items));
    this._badge();
  },
  add(product, qty = 1) {
    const items = this.get();
    const existing = items.find(i => i.id === product.id);
    if (existing) {
      existing.qty = Math.min(existing.qty + qty, product.stock);
    } else {
      items.push({
        id: product.id, name: product.name, price: Number(product.price),
        stock: product.stock, unit: product.unit, categoryName: product.categoryName, qty,
      });
    }
    this._save(items);
    Toast.show(`✓ Đã thêm "${product.name}" vào giỏ hàng`);
  },
  remove(id)          { this._save(this.get().filter(i => i.id !== id)); },
  setQty(id, qty)     {
    const items = this.get();
    const item = items.find(i => i.id === id);
    if (!item) return;
    if (qty <= 0) { this._save(items.filter(i => i.id !== id)); return; }
    item.qty = Math.min(qty, item.stock);
    this._save(items);
  },
  clear()             { localStorage.removeItem(this._KEY); this._badge(); },
  total()             { return this.get().reduce((s, i) => s + i.price * i.qty, 0); },
  count()             { return this.get().reduce((s, i) => s + i.qty, 0); },
  _badge() {
    const el = document.getElementById('cart-count');
    if (!el) return;
    const n = this.count();
    el.textContent = n;
    el.style.display = n > 0 ? '' : 'none';
  },
};

/* ── Breadcrumb ─────────────────────────────────────────────── */
function setBreadcrumb(items) {
  const nav = document.getElementById('breadcrumb');
  if (!nav) return;
  nav.innerHTML = items.map((item, i) =>
    i === items.length - 1
      ? `<span class="bc-item bc-current">${esc(item.label)}</span>`
      : `<a class="bc-item" href="${item.href}">${esc(item.label)}</a>`
  ).join('<span class="bc-sep">›</span>');
}

/* ── Reusable product card HTML ─────────────────────────────── */
function productCardHTML(p) {
  const inStock = p.stock > 0;
  return `
    <div class="product-card">
      <a class="product-card__img" href="#/product/${p.id}">
        <span class="product-emoji">${categoryEmoji(p.categoryName)}</span>
      </a>
      <div class="product-card__body">
        <div class="product-card__category">${esc(p.categoryName)}</div>
        <a class="product-card__name" href="#/product/${p.id}">${esc(p.name)}</a>
        <div class="product-card__price">${fmt(p.price)}</div>
        <div class="product-card__stock">${stockBadge(p.stock, p.unit)}</div>
      </div>
      <div class="product-card__footer">
        <a href="#/product/${p.id}" class="btn btn--outline btn--sm">Xem chi tiết</a>
        <button class="btn btn--primary btn--sm js-add-cart" data-id="${p.id}"
                data-name="${esc(p.name)}" data-price="${p.price}"
                data-stock="${p.stock}" data-unit="${esc(p.unit)}"
                data-category="${esc(p.categoryName)}"
                ${!inStock ? 'disabled' : ''}>
          Thêm giỏ
        </button>
      </div>
    </div>`;
}

/* Bind add-to-cart buttons inside a root element */
function bindAddToCart(root) {
  $$(('.js-add-cart'), root).forEach(btn => {
    btn.addEventListener('click', () => {
      Cart.add({
        id:           Number(btn.dataset.id),
        name:         btn.dataset.name,
        price:        Number(btn.dataset.price),
        stock:        Number(btn.dataset.stock),
        unit:         btn.dataset.unit,
        categoryName: btn.dataset.category,
      });
    });
  });
}

function paginationHTML(page, totalPages, params) {
  const pages = [];
  for (let i = 1; i <= totalPages; i++) {
    const p = { ...params, page: i };
    const qs = new URLSearchParams(Object.entries(p).filter(([, v]) => v != null && v !== '')).toString();
    pages.push(`<a href="#/products?${qs}" class="page-btn ${i === page ? 'active' : ''}">${i}</a>`);
  }
  return `<div class="pagination">${pages.join('')}</div>`;
}

/* ── View: Product list ─────────────────────────────────────── */
async function viewProducts(params = {}) {
  const app = document.getElementById('app');
  app.innerHTML = '<div class="loading">⏳ Đang tải sản phẩm…</div>';

  setBreadcrumb([
    { label: 'Trang chủ', href: '#/' },
    { label: 'Sản phẩm' },
  ]);

  const [cats, data] = await Promise.all([
    Api.getCategories(),
    Api.getProducts({ categoryId: params.category || '', page: params.page || 1, pageSize: 12 }),
  ]);

  const selectedCat = cats.find(c => c.id === Number(params.category));

  app.innerHTML = `
    <div class="product-layout">
      <aside class="category-sidebar">
        <h3 class="sidebar-title">Danh mục</h3>
        <ul class="category-list">
          <li><a href="#/products" class="${!params.category ? 'active' : ''}">🗂 Tất cả</a></li>
          ${cats.map(c => `<li><a href="#/products?category=${c.id}"
            class="${Number(params.category) === c.id ? 'active' : ''}">
            ${categoryEmoji(c.name)} ${esc(c.name)}
          </a></li>`).join('')}
        </ul>
      </aside>
      <div class="product-main">
        <div class="product-main__header">
          <h2>${selectedCat ? esc(selectedCat.name) : 'Tất cả sản phẩm'}</h2>
          <span class="result-count">${data.total} sản phẩm</span>
        </div>
        <div class="product-grid">
          ${data.items.length
            ? data.items.map(productCardHTML).join('')
            : '<p style="color:var(--text-muted)">Không có sản phẩm nào.</p>'}
        </div>
        ${data.totalPages > 1 ? paginationHTML(data.page, data.totalPages, params) : ''}
      </div>
    </div>`;

  bindAddToCart(app);
}

/* ── View: Product detail ───────────────────────────────────── */
async function viewProductDetail({ id }) {
  const app = document.getElementById('app');
  app.innerHTML = '<div class="loading">⏳ Đang tải sản phẩm…</div>';

  const product = await Api.getProduct(id);

  setBreadcrumb([
    { label: 'Trang chủ', href: '#/' },
    { label: 'Sản phẩm', href: '#/products' },
    { label: product.categoryName, href: `#/products?category=${product.categoryId}` },
    { label: product.name },
  ]);

  const inStock = product.stock > 0;
  const relData  = await Api.getProducts({ categoryId: product.categoryId, pageSize: 5 });
  const related  = relData.items.filter(p => p.id !== product.id).slice(0, 4);

  app.innerHTML = `
    <div class="product-detail">
      <div class="product-detail__img">
        <span class="detail-emoji">${categoryEmoji(product.categoryName)}</span>
      </div>
      <div class="product-detail__info">
        <div class="detail-category">${esc(product.categoryName)}</div>
        <h1 class="detail-name">${esc(product.name)}</h1>
        <div class="detail-sku">SKU: ${esc(product.sku)}</div>
        <div class="detail-price">${fmt(product.price)}</div>
        <div class="detail-stock">${stockBadge(product.stock, product.unit)}</div>
        ${product.description ? `<p class="detail-desc">${esc(product.description)}</p>` : ''}

        <div class="qty-row">
          <label for="qty-input">Số lượng:</label>
          <div class="qty-control">
            <button class="qty-btn" id="qty-dec" type="button">−</button>
            <input id="qty-input" type="number" value="1" min="1" max="${product.stock}"
                   class="qty-input" ${!inStock ? 'disabled' : ''}>
            <button class="qty-btn" id="qty-inc" type="button">+</button>
          </div>
        </div>

        <div class="detail-actions">
          <button class="btn btn--primary btn--lg" id="add-cart-btn" ${!inStock ? 'disabled' : ''}>
            🛒 Thêm vào giỏ hàng
          </button>
          <a href="#/cart" class="btn btn--outline btn--lg">Xem giỏ hàng</a>
        </div>
      </div>
    </div>
    ${related.length ? `
      <section class="related-section">
        <h3>Sản phẩm cùng danh mục</h3>
        <div class="product-grid">${related.map(productCardHTML).join('')}</div>
      </section>` : ''}`;

  // Qty stepper
  const qtyInput = document.getElementById('qty-input');
  document.getElementById('qty-dec')?.addEventListener('click', () => {
    qtyInput.value = Math.max(1, Number(qtyInput.value) - 1);
  });
  document.getElementById('qty-inc')?.addEventListener('click', () => {
    qtyInput.value = Math.min(product.stock, Number(qtyInput.value) + 1);
  });

  document.getElementById('add-cart-btn')?.addEventListener('click', () => {
    const qty = Math.max(1, Math.min(Number(qtyInput.value) || 1, product.stock));
    Cart.add(product, qty);
  });

  bindAddToCart(app);
}

/* ── View: Search ───────────────────────────────────────────── */
async function viewSearch({ q } = {}) {
  const app = document.getElementById('app');

  // Pre-fill the search input
  const si = document.getElementById('search-input');
  if (si && q && !si.value) si.value = q;

  setBreadcrumb([
    { label: 'Trang chủ', href: '#/' },
    { label: q ? `Tìm kiếm: "${q}"` : 'Tìm kiếm' },
  ]);

  if (!q) {
    app.innerHTML = `
      <div class="search-hero">
        <h2>🔍 Tìm kiếm sản phẩm</h2>
        <p>Nhập tên sản phẩm, SKU hoặc tên danh mục vào ô tìm kiếm phía trên.</p>
        <a href="#/products" class="btn btn--outline">Xem tất cả sản phẩm →</a>
      </div>`;
    return;
  }

  app.innerHTML = '<div class="loading">⏳ Đang tìm kiếm…</div>';
  const data = await Api.getProducts({ search: q, pageSize: 48 });

  app.innerHTML = `
    <div class="search-results">
      <h2>Kết quả: <em>"${esc(q)}"</em></h2>
      <p class="result-count" style="margin-bottom:20px">Tìm thấy ${data.total} sản phẩm</p>
      ${data.items.length
        ? `<div class="product-grid">${data.items.map(productCardHTML).join('')}</div>`
        : `<div class="empty-state">
             <div class="empty-icon">🔍</div>
             <h3>Không tìm thấy kết quả</h3>
             <p>Thử từ khóa khác hoặc duyệt theo danh mục.</p>
             <a href="#/products" class="btn btn--primary">Xem tất cả sản phẩm</a>
           </div>`}
    </div>`;

  bindAddToCart(app);
}

/* ── View: Cart + Checkout ──────────────────────────────────── */
function viewCart() {
  const app  = document.getElementById('app');
  const items = Cart.get();

  setBreadcrumb([
    { label: 'Trang chủ', href: '#/' },
    { label: 'Giỏ hàng' },
  ]);

  if (!items.length) {
    app.innerHTML = `
      <div class="empty-state">
        <div class="empty-icon">🛒</div>
        <h3>Giỏ hàng trống</h3>
        <p>Hãy thêm sản phẩm vào giỏ hàng để tiếp tục.</p>
        <a href="#/products" class="btn btn--primary">Xem sản phẩm</a>
      </div>`;
    return;
  }

  app.innerHTML = `
    <div class="cart-layout">
      <div class="cart-items">
        <h2>Giỏ hàng (${Cart.count()} sản phẩm)</h2>
        <div class="cart-table">
          <div class="cart-header">
            <span>Sản phẩm</span>
            <span>Đơn giá</span>
            <span>Số lượng</span>
            <span>Thành tiền</span>
            <span></span>
          </div>
          ${items.map(item => `
            <div class="cart-row" data-id="${item.id}">
              <div class="cart-row__product">
                <span class="cart-emoji">${categoryEmoji(item.categoryName)}</span>
                <div>
                  <a class="cart-row__name" href="#/product/${item.id}">${esc(item.name)}</a>
                  <div class="cart-row__cat">${esc(item.categoryName)}</div>
                </div>
              </div>
              <div class="cart-row__price">${fmt(item.price)}</div>
              <div class="cart-row__qty">
                <button class="qty-btn js-dec" data-id="${item.id}" type="button">−</button>
                <span class="qty-val">${item.qty}</span>
                <button class="qty-btn js-inc" data-id="${item.id}" type="button">+</button>
              </div>
              <div class="cart-row__total">${fmt(item.price * item.qty)}</div>
              <div>
                <button class="btn-remove js-remove" data-id="${item.id}" title="Xóa khỏi giỏ">✕</button>
              </div>
            </div>`).join('')}
        </div>
      </div>

      <div class="cart-summary">
        <h3>Tóm tắt</h3>
        <div class="summary-row"><span>Sản phẩm</span><span>${items.length} loại</span></div>
        <div class="summary-row"><span>Số lượng</span><span>${Cart.count()} cái</span></div>
        <div class="summary-row summary-row--total"><span>Tổng tiền</span><span>${fmt(Cart.total())}</span></div>
        <a href="#/products" class="btn btn--outline btn--full" style="margin-top:14px">← Mua thêm</a>
        <button class="btn btn--primary btn--full" id="checkout-btn" style="margin-top:8px">Đặt hàng →</button>
      </div>
    </div>

    <!-- Checkout form (hidden until "Đặt hàng" clicked) -->
    <div id="checkout-section" style="display:none">
      <div class="checkout-form">
        <h2>📋 Thông tin đặt hàng</h2>
        <form id="order-form" novalidate>
          <div class="form-row">
            <label>Họ và tên *</label>
            <input name="customerName" type="text" placeholder="Nguyễn Văn A" required>
          </div>
          <div class="form-row">
            <label>Số điện thoại *</label>
            <input name="customerPhone" type="tel" placeholder="0901 234 567" required>
          </div>
          <div class="form-row">
            <label>Địa chỉ giao hàng</label>
            <input name="customerAddress" type="text" placeholder="123 Đường ABC, Quận 1, TP.HCM">
          </div>
          <div class="form-row">
            <label>Ghi chú</label>
            <textarea name="note" placeholder="Ghi chú cho đơn hàng…"></textarea>
          </div>
          <div class="form-row">
            <label>Phương thức thanh toán</label>
            <div class="payment-options">
              <label class="radio-option">
                <input type="radio" name="paymentMethod" value="cash" checked>
                💵 Tiền mặt khi nhận hàng
              </label>
              <label class="radio-option">
                <input type="radio" name="paymentMethod" value="bank_transfer">
                🏦 Chuyển khoản ngân hàng
              </label>
              <label class="radio-option">
                <input type="radio" name="paymentMethod" value="credit_card">
                💳 Thẻ tín dụng / thẻ ghi nợ
              </label>
            </div>
          </div>
          <div class="form-actions">
            <button type="button" class="btn btn--outline" id="cancel-checkout">Hủy</button>
            <button type="submit" class="btn btn--primary" id="submit-btn">✓ Xác nhận đặt hàng</button>
          </div>
          <p id="form-error" class="form-error" style="display:none"></p>
        </form>
      </div>
    </div>`;

  /* Cart interactions */
  $$('.js-dec', app).forEach(btn =>
    btn.addEventListener('click', () => { Cart.setQty(Number(btn.dataset.id), Cart.get().find(i => i.id === Number(btn.dataset.id))?.qty - 1); viewCart(); })
  );
  $$('.js-inc', app).forEach(btn =>
    btn.addEventListener('click', () => { Cart.setQty(Number(btn.dataset.id), Cart.get().find(i => i.id === Number(btn.dataset.id))?.qty + 1); viewCart(); })
  );
  $$('.js-remove', app).forEach(btn =>
    btn.addEventListener('click', () => { Cart.remove(Number(btn.dataset.id)); viewCart(); })
  );

  /* Show / hide checkout form */
  document.getElementById('checkout-btn')?.addEventListener('click', () => {
    const section = document.getElementById('checkout-section');
    section.style.display = '';
    section.scrollIntoView({ behavior: 'smooth', block: 'start' });
  });
  document.getElementById('cancel-checkout')?.addEventListener('click', () => {
    document.getElementById('checkout-section').style.display = 'none';
  });

  /* Submit order */
  document.getElementById('order-form')?.addEventListener('submit', async e => {
    e.preventDefault();
    const form     = e.target;
    const errEl    = document.getElementById('form-error');
    const submitBtn = document.getElementById('submit-btn');

    const name    = form.customerName.value.trim();
    const phone   = form.customerPhone.value.trim();
    const address = form.customerAddress.value.trim();
    const note    = form.note.value.trim();
    const payment = form.paymentMethod.value;

    if (!name)  { errEl.textContent = 'Vui lòng nhập họ và tên'; errEl.style.display = ''; return; }
    if (!phone) { errEl.textContent = 'Vui lòng nhập số điện thoại'; errEl.style.display = ''; return; }

    submitBtn.disabled   = true;
    submitBtn.textContent = '⏳ Đang xử lý…';
    errEl.style.display  = 'none';

    try {
      const result = await Api.placeOrder({
        customerName:    name,
        customerPhone:   phone,
        customerAddress: address,
        note,
        paymentMethod:   payment,
        items: Cart.get().map(i => ({ productId: i.id, quantity: i.qty })),
      });
      Cart.clear();
      viewOrderSuccess(result);
    } catch (err) {
      errEl.textContent    = err.message || 'Đặt hàng thất bại. Vui lòng thử lại.';
      errEl.style.display  = '';
      submitBtn.disabled   = false;
      submitBtn.textContent = '✓ Xác nhận đặt hàng';
    }
  });
}

/* ── View: Order success ────────────────────────────────────── */
function viewOrderSuccess(result) {
  document.getElementById('breadcrumb').innerHTML = '';
  document.getElementById('app').innerHTML = `
    <div class="order-success">
      <div class="success-icon">🎉</div>
      <h2>Đặt hàng thành công!</h2>
      <p>Cảm ơn bạn đã mua hàng. Đơn hàng đang được xử lý.</p>
      <div class="success-details">
        <div><strong>Mã đơn hàng:</strong> ${esc(result.orderCode)}</div>
        <div><strong>Số sản phẩm:</strong> ${result.itemCount}</div>
        <div><strong>Tổng tiền:</strong> ${fmt(result.totalAmount)}</div>
        <div><strong>Trạng thái:</strong> Chờ xác nhận</div>
      </div>
      <div class="success-actions">
        <a href="#/products" class="btn btn--primary">Tiếp tục mua sắm</a>
        <a href="#/" class="btn btn--outline">Về trang chủ</a>
      </div>
    </div>`;
}

/* ── Router ─────────────────────────────────────────────────── */
function parseHash(hash) {
  const raw    = (hash || '').replace(/^#/, '') || '/';
  const [path, qs] = raw.split('?');
  const params = Object.fromEntries(new URLSearchParams(qs || ''));
  return { path, params };
}

function navigate(hash) {
  const { path, params } = parseHash(hash);
  window.scrollTo({ top: 0 });

  let promise;
  if (path === '/' || path === '/products') {
    promise = viewProducts(params);
  } else {
    const mDetail = path.match(/^\/product\/(\d+)$/);
    if (mDetail) {
      promise = viewProductDetail({ id: Number(mDetail[1]), ...params });
    } else if (path === '/search') {
      promise = viewSearch(params);
    } else if (path === '/cart') {
      viewCart();
      return;
    } else {
      document.getElementById('app').innerHTML = `
        <div class="empty-state">
          <div class="empty-icon">🔍</div>
          <h3>Trang không tìm thấy</h3>
          <a href="#/" class="btn btn--primary">Về trang chủ</a>
        </div>`;
      return;
    }
  }

  promise?.catch(err => {
    document.getElementById('app').innerHTML = `
      <div class="error-state">
        <h2>⚠️ Có lỗi xảy ra</h2>
        <p>${esc(err.message)}</p>
        <a href="#/" class="btn btn--primary">Về trang chủ</a>
      </div>`;
  });
}

/* ── Bootstrap ──────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
  // Search bar
  const searchInput = document.getElementById('search-input');
  const searchBtn   = document.getElementById('search-btn');

  const doSearch = () => {
    const q = searchInput?.value.trim();
    if (q) window.location.hash = `#/search?q=${encodeURIComponent(q)}`;
    else window.location.hash = '#/search';
  };

  searchBtn?.addEventListener('click', doSearch);
  searchInput?.addEventListener('keydown', e => { if (e.key === 'Enter') doSearch(); });

  // Hash router
  window.addEventListener('hashchange', () => navigate(window.location.hash));

  // Initial cart badge
  Cart._badge();

  // Initial page
  navigate(window.location.hash || '#/');
});
