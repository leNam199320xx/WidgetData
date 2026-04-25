# Demo – Ứng dụng nghiệp vụ xây dựng trên WidgetData

Thư mục này chứa các **ứng dụng nghiệp vụ demo** được xây dựng trên nền tảng WidgetData.

---

## Danh sách demo

| Demo | Mô tả | Công nghệ |
|------|-------|-----------|
| `shop/` | Cửa hàng trực tuyến | Blazor Admin + HTML Front-end |
| `news/` | Cổng thông tin báo điện tử | HTML/JS static front-end (ASP.NET static serve) |
| `course/` | Nền tảng học trực tuyến | HTML/JS static front-end (ASP.NET static serve) |

Tất cả demo đều được đăng ký trong **WidgetData.AppHost** (Aspire) — xuất hiện trong Aspire Dashboard để monitor trạng thái, logs và health.

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

---

## `news/news-front/` — Cổng thông tin báo điện tử

Demo **VietNews** — portal tin tức với các trang tĩnh rendered bởi WidgetEngine.

```bash
# Chạy qua .NET (được đăng ký trong AppHost)
dotnet run --project demo/news/news-front
# → http://localhost:<port>

# Hoặc build và serve trực tiếp
dotnet run --project demo/news/news-front --urls http://localhost:3001
```

Cấu trúc:

```
news/news-front/
├── Program.cs                ← ASP.NET Core static file server
├── news-front.csproj
└── wwwroot/
    ├── index.html            ← Trang chủ VietNews
    ├── dashboard.html        ← News analytics dashboard (cần đăng nhập)
    ├── css/
    ├── lib/                  ← widget-engine.js, widget-engine.css
    └── pages/
        ├── home.json         ← Trang chủ (tin nổi bật, chuyên mục)
        ├── latest.json       ← Tin mới nhất (dòng thời gian)
        ├── categories.json   ← Tất cả chuyên mục
        ├── contact.json      ← Liên hệ + form
        └── dashboard.json    ← News analytics (widget API live)
```

---

## `course/course-front/` — Nền tảng học trực tuyến

Demo **EduViet** — platform khóa học online với catalog, danh sách giảng viên và analytics dashboard.

```bash
# Chạy qua .NET (được đăng ký trong AppHost)
dotnet run --project demo/course/course-front
# → http://localhost:<port>

# Hoặc chỉ định port
dotnet run --project demo/course/course-front --urls http://localhost:3002
```

Cấu trúc:

```
course/course-front/
├── Program.cs                ← ASP.NET Core static file server
├── course-front.csproj
└── wwwroot/
    ├── index.html            ← Trang chủ EduViet
    ├── dashboard.html        ← Learning analytics dashboard (cần đăng nhập)
    ├── css/
    ├── lib/                  ← widget-engine.js, widget-engine.css
    └── pages/
        ├── home.json         ← Trang chủ (khóa học nổi bật, danh mục)
        ├── courses.json      ← Catalog khóa học theo lĩnh vực
        ├── instructors.json  ← Danh sách giảng viên
        ├── contact.json      ← Liên hệ + form B2B
        └── dashboard.json    ← Learning analytics (widget API live)
```

---

## Aspire AppHost Monitoring

Tất cả 3 demo đều được đăng ký trong `WidgetData.AppHost`:

```csharp
// shop-admin: Blazor Server admin app
builder.AddProject<Projects.shop_admin>("shop-admin")
    .WithReference(api).WaitFor(api).WithExternalHttpEndpoints();

// news-front: VietNews static portal
builder.AddProject<Projects.news_front>("news-front")
    .WithReference(api).WaitFor(api).WithExternalHttpEndpoints();

// course-front: EduViet course platform
builder.AddProject<Projects.course_front>("course-front")
    .WithReference(api).WaitFor(api).WithExternalHttpEndpoints();
```

Khởi động toàn bộ hệ thống qua AppHost:

```bash
dotnet run --project src/WidgetData.AppHost
# Mở Aspire Dashboard → theo dõi tất cả services + demo webs
```
