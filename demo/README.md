# Demo – Shop Business Application

Thư mục này là **ứng dụng nghiệp vụ bán hàng** được xây dựng trên nền tảng WidgetData.

---

## Kiến trúc 2 tầng

```
┌─────────────────────────────────────────────────┐
│  TẦNG 1 — PLATFORM  (src/)                      │
│                                                 │
│  WidgetData.Web  ─  Admin platform              │
│    Cấu hình widget, data source, pipeline       │
│    Thiết kế page layout, template HTML          │
│                                                 │
│  WidgetData.API  ─  API engine                  │
│    Execute widget, schedule, cache, auth        │
└──────────────────┬──────────────────────────────┘
                   │ Platform được deploy cho đơn vị nghiệp vụ
┌──────────────────▼──────────────────────────────┐
│  TẦNG 2 — BUSINESS APP  (demo/)                 │
│                                                 │
│  shop/shop-admin/  ─  Backend quản lý shop      │
│    Quản lý sản phẩm, đơn hàng, khách hàng       │
│    Báo cáo bán hàng, kho hàng, khuyến mãi       │
│                                                 │
│  shop/shop-front/  ─  Trang bán hàng public     │
│    HTML/CSS/JS thuần, zero dependency           │
│    Render UI từ WidgetEngine + dữ liệu live API  │
│    Deploy: CDN / nginx / GitHub Pages           │
└─────────────────────────────────────────────────┘
```

---

## `shop/shop-admin/` — Backend quản lý shop

Ứng dụng Blazor Server độc lập, kết nối với `WidgetData.API` để quản lý nghiệp vụ bán hàng.

```bash
# Cần chạy WidgetData.API trước
dotnet run --project src/WidgetData.API
# → http://localhost:5114/api

# Chạy shop-admin
dotnet run --project demo/shop/shop-admin
# → http://localhost:5001
```

Tài khoản mặc định:

| Email | Password |
|-------|---------|
| admin@widgetdata.com | Admin@123 |

---

## `shop/shop-front/` — Trang bán hàng public

Trang bán hàng hoàn toàn **HTML/CSS/JS**, zero dependency — triển khai độc lập lên CDN, nginx, GitHub Pages.

```bash
cd demo/shop/shop-front
python -m http.server 3000
# hoặc: npx serve .
# → http://localhost:3000
```

Trang public (`index.html`) không cần đăng nhập.  
Dashboard (`dashboard.html`) kết nối API live — cần `WidgetData.API` đang chạy:

| | Giá trị |
|-|--------|
| API URL | `http://localhost:5114/api` |
| Email | `admin@widgetdata.com` |
| Password | `Admin@123` |

Cấu trúc:

```
shop/shop-front/
├── index.html        ← landing page (home / sản phẩm / bảng giá)
├── dashboard.html    ← sales dashboard (cần đăng nhập)
├── css/              ← site.css, dashboard.css
├── lib/              ← widget-engine.js, widget-engine.css
└── pages/            ← page configs (JSON)
    ├── home.json
    ├── products.json
    ├── pricing.json
    └── dashboard.json
```

---

## Luồng vận hành

```
1. Platform admin (src/WidgetData.Web)
   → Tạo data source kết nối DB shop
   → Tạo widget: doanh thu, sản phẩm, đơn hàng...
   → Cấu hình page layout

2. Shop admin (demo/shop/shop-admin)
   → Vận hành nghiệp vụ hàng ngày
   → Quản lý sản phẩm, đơn hàng, khách hàng, báo cáo

3. Storefront (demo/shop/shop-front)
   → WidgetEngine render giao diện từ pages/*.json
   → Live data từ WidgetData.API
```

---

## WidgetEngine

`widget-engine.js` — thư viện JS zero-dependency, render giao diện từ JSON config:

```js
// Public page (không cần auth)
WidgetEngine.init({ baseUrl: '' });
WidgetEngine.page.load('pages/home.json', document.getElementById('app'));

// Dashboard (có auth, gọi API)
WidgetEngine.init({ baseUrl: 'http://localhost:5114/api' });
await WidgetEngine.auth.login('admin@widgetdata.com', 'Admin@123');
WidgetEngine.page.load('pages/dashboard.json', document.getElementById('app'));
```

Page config JSON mô tả layout và danh sách widget — dữ liệu tĩnh (`staticData`) hoặc live từ API (`id`).
