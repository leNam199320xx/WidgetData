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
│  shop-admin/     ─  Backend quản lý shop        │
│    Platform cấu hình sẵn cho nghiệp vụ bán hàng│
│    Quản lý sản phẩm, đơn hàng, khách hàng       │
│    Xuất page config JSON cho storefront         │
│                                                 │
│  shop-front/     ─  Trang bán hàng public       │
│    HTML/CSS/JS thuần, zero dependency           │
│    Render UI từ page config của shop-admin      │
│    Deploy: CDN / nginx / GitHub Pages           │
└─────────────────────────────────────────────────┘
```

---

## `shop-admin/` — Backend quản lý shop

Instance của WidgetData.Web được **cấu hình sẵn cho nghiệp vụ bán hàng**.  
Đơn vị vận hành dùng đây để quản lý toàn bộ shop: sản phẩm, đơn hàng, báo cáo, khách hàng, khuyến mãi.

```bash
dotnet run --project demo/shop-admin
# → http://localhost:5001
# → /widget-engine  (WidgetEngine pages)
```

Tài khoản mặc định:
| Email | Password |
|-------|---------|
| admin@widgetdata.com | Admin@123 |

---

## `shop-front/` — Trang bán hàng public

Trang bán hàng public được **generate từ cấu hình** của shop-admin.  
Hoàn toàn HTML/CSS/JS — không cần .NET runtime.

```bash
cd demo/storefront
npx serve .
# hoặc: python -m http.server 3000
# → http://localhost:3000
```

Cần `WidgetData.API` đang chạy để dashboard hoạt động:

```bash
dotnet run --project src/WidgetData.API
# → http://localhost:5114/api
```

Cấu trúc:
```
shop-front/
├── index.html        ← landing page (home / sản phẩm / bảng giá)
├── dashboard.html    ← sales dashboard (cần đăng nhập)
├── css/              ← site.css, dashboard.css
├── lib/              ← widget-engine.js, widget-engine.css
└── pages/            ← page configs (JSON) từ shop-admin
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

2. Shop admin (demo/shop-admin)
   → Vận hành nghiệp vụ hàng ngày
   → Xuất / cập nhật pages/*.json

3. Storefront (demo/storefront)
   → Đọc pages/*.json
   → WidgetEngine render giao diện tự động
   → Live data từ WidgetData.API
```


Thư mục này chứa **storefront** — trang bán hàng public được generate/cấu hình từ **WidgetData.Web** (Blazor admin shop).

## Kiến trúc

```
WidgetData.Web (Blazor)   ← Admin quản lý shop
  Tạo / cấu hình Widget
  Xuất page config JSON
        ↓
  demo/shop-front/         ← Trang bán hàng public
    Đọc page config JSON
    WidgetEngine render UI (HTML/CSS/JS thuần)
        ↓
  WidgetData.API           ← Cung cấp dữ liệu live
```

---

## `shop-front/` — Trang bán hàng public

Hoàn toàn **HTML/CSS/JS**, zero dependency — triển khai độc lập lên CDN, nginx, GitHub Pages.

```bash
cd demo/storefront
python -m http.server 3000
# hoặc: npx serve .
# → http://localhost:3000
```

Trang public (`index.html`) không cần đăng nhập.  
Dashboard (`dashboard.html`) kết nối API live:

| | Giá trị |
|-|--------|
| API URL | `http://localhost:5114/api` |
| Email | `admin@widgetdata.com` |
| Password | `Admin@123` |

Cấu trúc:

```
shop-front/
├── index.html        ← landing page (home / sản phẩm / bảng giá)
├── dashboard.html    ← dashboard live có đăng nhập
├── css/              ← site.css, dashboard.css
├── lib/              ← widget-engine.js, widget-engine.css
└── pages/            ← home.json, products.json, pricing.json, dashboard.json
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


Thư mục này minh họa ứng dụng công nghệ **WidgetEngine** trong bài toán shop bán hàng.

## Kiến trúc

```
demo/
├── admin/          ← Backend quản lý shop (Blazor)
│                      Quản lý widget, data source, cấu hình page
│                      Xuất ra page config JSON cho storefront
│
└── shop-front/     ← Trang bán hàng public (HTML / CSS / JS thuần)
                       Đọc page config JSON từ backend
                       Render giao diện hoàn toàn phía client
```

**Luồng hoạt động:**

```
Admin (Blazor) → tạo / cấu hình Widget → lưu vào API
                                        ↓
                              page config JSON (home, products, pricing…)
                                        ↓
Storefront (HTML/JS) → đọc JSON → WidgetEngine.page.load() → render UI
```

---

## `admin/` — Backend quản lý shop

Là tập hợp các trang demo WidgetEngine được nhúng trong **WidgetData.Web** (Blazor Server).  
Được serve tại `/widget-engine` khi Blazor app chạy.

```bash
dotnet run --project src/WidgetData.Web
# → http://localhost:5001/widget-engine/
```

Nội dung:
- `index.html` — entry point, điều hướng giữa các page
- `pages/*.json` — cấu hình trang: home, products, pricing, dashboard
- `widget-engine.js` / `.css` — thư viện WidgetEngine

---

## `shop-front/` — Trang bán hàng public

Trang bán hàng hoàn toàn **HTML/CSS/JS**, zero dependency — được generate/cấu hình từ backend shop.  
Có thể triển khai độc lập: CDN, nginx, GitHub Pages.

```bash
cd demo/storefront
python -m http.server 3000
# hoặc: npx serve .
# → http://localhost:3000
```

Trang public (`index.html`) không cần đăng nhập. Dashboard (`dashboard.html`) kết nối API live:

| | Giá trị |
|-|---------|
| API URL | `http://localhost:5114/api` |
| Email | `admin@widgetdata.com` |
| Password | `Admin@123` |

Nội dung:
```
shop-front/
├── index.html        ← landing page (home / sản phẩm / bảng giá)
├── dashboard.html    ← dashboard live có đăng nhập
├── css/              ← site.css, dashboard.css
├── lib/              ← widget-engine.js, widget-engine.css
└── pages/            ← home.json, products.json, pricing.json, dashboard.json
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


Thư mục này chứa hai bản demo shop ứng dụng công nghệ **WidgetEngine** để hiển thị dữ liệu động theo cấu hình JSON.

## Hai phiên bản

| Phiên bản | Thư mục | Mô tả |
|-----------|---------|-------|
| **Standalone** | `standalone/` | Hoàn toàn HTML/CSS/JS — không cần server .NET. Mở file là chạy được. Triển khai lên nginx, CDN, GitHub Pages. |
| **Blazor-embedded** | `blazor-web/` | Nhúng trong `WidgetData.Web`. Được serve tại `/widget-engine` khi Blazor chạy. Dùng chung asset với app. |

---

## Chạy bản Standalone

```bash
# Với Python
cd demo/standalone
python -m http.server 3000
# → http://localhost:3000

# Với Node.js
npx serve demo/standalone
```

Hoặc mở thẳng `demo/standalone/index.html` trong trình duyệt (Chrome/Edge hỗ trợ file:// cho fetch JSON).

**Thông tin đăng nhập dashboard:**
- URL: `http://localhost:5114/api`
- Email: `admin@widgetdata.com`
- Password: `Admin@123`

---

## Chạy bản Blazor-embedded

```bash
dotnet run --project src/WidgetData.Web
# → http://localhost:5001/widget-engine/
```

`WidgetData.Web` tự phục vụ thư mục `blazor-web/` tại đường dẫn `/widget-engine`.

---

## Cấu trúc

```
demo/
├── README.md              ← file này
│
├── standalone/            ← bản độc lập HTML/JS/CSS
│   ├── index.html         ← landing page (home / sản phẩm / bảng giá)
│   ├── dashboard.html     ← dashboard có đăng nhập, gọi API live
│   ├── css/
│   │   ├── site.css       ← header, nav, footer
│   │   └── dashboard.css  ← login screen, sidebar, layout
│   ├── lib/
│   │   ├── widget-engine.js   ← WidgetEngine library
│   │   └── widget-engine.css  ← base widget styles
│   └── pages/             ← page configs (JSON)
│       ├── home.json
│       ├── products.json
│       ├── pricing.json
│       └── dashboard.json
│
└── blazor-web/            ← bản nhúng trong Blazor Web
    ├── index.html         ← entry point (served tại /widget-engine/)
    ├── index.css          ← page chrome styles
    ├── widget-engine.js   ← WidgetEngine library
    ├── widget-engine.css  ← base widget styles
    └── pages/             ← page configs
        ├── home.json
        ├── products.json
        ├── pricing.json
        └── dashboard.json
```

---

## WidgetEngine là gì?

**WidgetEngine** (`widget-engine.js`) là thư viện JavaScript zero-dependency cho phép render giao diện động từ cấu hình JSON:

```js
// Khởi tạo
WidgetEngine.init({ baseUrl: 'http://localhost:5114/api' });

// Load cả trang từ config
WidgetEngine.page.load('pages/home.json', document.getElementById('app'));

// Render widget đơn (có auth)
await WidgetEngine.auth.login('admin@widgetdata.com', 'Admin@123');
await WidgetEngine.render('#my-widget', { id: 1 });
```

Page config JSON mô tả layout và danh sách widget — dữ liệu tĩnh (`staticData`) hoặc live từ API (`id`).
