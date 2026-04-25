# widgetdata-frontend

Standalone frontend project — **pure HTML / CSS / JavaScript**, không phụ thuộc vào bất kỳ framework nào.

Đây là phần giao diện người dùng được generate từ WidgetData backend, tách biệt hoàn toàn khỏi WidgetData.Web (Blazor).

---

## Cấu trúc thư mục

```
widgetdata-frontend/
├── index.html          ← Trang public: landing / sản phẩm / bảng giá / liên hệ
├── dashboard.html      ← Dashboard có đăng nhập, gọi API live
│
├── lib/
│   ├── widget-engine.js    ← WidgetEngine library (zero-dependency ES2020)
│   └── widget-engine.css   ← Base styles cho widget components
│
├── css/
│   ├── site.css        ← Header, nav, footer cho trang public
│   └── dashboard.css   ← Login screen, sidebar, layout cho dashboard
│
└── pages/
    ├── home.json       ← Landing page (static data, no auth)
    ├── products.json   ← Trang sản phẩm (static data)
    ├── pricing.json    ← Bảng giá (static data)
    ├── contact.json    ← Form liên hệ (Form widget demo)
    └── dashboard.json  ← Sales dashboard (API-backed, requires auth)
```

---

## Chạy thử

Chỉ cần một web server tĩnh — không cần Node.js, không cần build.

### Với VS Code Live Server
1. Cài extension **Live Server** (ritwickdey.LiveServer)
2. Chuột phải `index.html` → **Open with Live Server**

### Với Python
```bash
cd widgetdata-frontend
python -m http.server 3000
# → http://localhost:3000
```

### Với Node.js (npx)
```bash
cd widgetdata-frontend
npx serve .
# → http://localhost:3000
```

---

## Hai chế độ

### 1. Trang public (`index.html`)
- Không yêu cầu đăng nhập
- Tất cả dữ liệu là `staticData` — không cần API
- Gồm: Trang chủ · Sản phẩm · Bảng giá · Liên hệ (Form widget demo)

### 2. Dashboard (`dashboard.html`)
- Yêu cầu đăng nhập → gọi `POST /api/auth/login`
- Token lưu `localStorage`, tự refresh
- Gọi API live từ **WidgetData.API** (`http://localhost:5114/api`)
- Chức năng demo:
  - 📈 Sales Dashboard — page config từ `pages/dashboard.json`
  - 📡 Widget Activity — xem activity log + summary cho bất kỳ widget ID
  - 🔔 Inactivity Alerts — danh sách widget không hoạt động + alert log
  - ⚡ Chạy widget đơn — nhập Widget ID
  - 🔌 Kiểm tra kết nối — test các API endpoint (bao gồm Form và Activity)

---

## Kết nối API

Dashboard cần WidgetData.API đang chạy:

```bash
cd WidgetData
dotnet run --project src/WidgetData.API
# API chạy tại http://localhost:5114
```

Thông tin đăng nhập mặc định:
| Field    | Giá trị                  |
|----------|--------------------------|
| Email    | admin@widgetdata.com     |
| Password | Admin@123                |
| API URL  | http://localhost:5114/api |

---

## Tích hợp WidgetEngine

### Trang tĩnh (không cần API)
```html
<script src="lib/widget-engine.js"></script>
<script>
  WidgetEngine.init({ baseUrl: '' });
  WidgetEngine.page.load('pages/home.json', document.getElementById('app'));
</script>
```

### Widget có API
```html
<script>
  WidgetEngine.init({ baseUrl: 'http://localhost:5114/api' });
  await WidgetEngine.auth.login('admin@widgetdata.com', 'Admin@123');
  await WidgetEngine.render('#my-widget', { id: 1, title: 'Widget #1' });
</script>
```

### Form Widget (PR #23)
```html
<!-- Render form từ schema API, tự submit qua POST /api/form/{id} -->
<script>
  WidgetEngine.init({ baseUrl: 'http://localhost:5114/api' });
  await WidgetEngine.render('#contact-form', {
    formId: 1,
    title: 'Gửi tin nhắn'
  });
</script>
```

Trong page config JSON:
```json
{
  "formId": 1,
  "title": "Gửi tin nhắn"
}
```

### Widget Activity API (PR #22)
```javascript
// Xem activity log (yêu cầu Admin/Manager)
const log = await WidgetEngine.widgetActivity.getActivity(widgetId, { page: 1, pageSize: 20 });
const summary = await WidgetEngine.widgetActivity.getSummary(widgetId);
const inactive = await WidgetEngine.widgetActivity.getInactive(30); // Admin only
const alerts = await WidgetEngine.widgetActivity.getAlerts();        // Admin only
```

### Cấu trúc page config (`pages/*.json`)
```json
{
  "title": "Tên trang",
  "layout": "grid",
  "columns": 3,
  "auth": { "required": false },
  "widgets": [
    {
      "staticData": { "headline": "Hello" },
      "template": "<h1>{{headline}}</h1>"
    },
    {
      "id": 1,
      "title": "Live widget từ API",
      "colSpan": 2,
      "autoRefreshSeconds": 60
    },
    {
      "formId": 1,
      "title": "Form liên hệ"
    }
  ]
}
```

---

## CORS

Khi chạy `dashboard.html` từ một origin khác với API (ví dụ `localhost:3000` vs `localhost:5114`), đảm bảo API cho phép CORS.  
Trong `WidgetData.API/Program.cs` đã có `app.UseCors("AllowAll")`.

---

## Thay đổi gần nhất

| Thay đổi | PR |
|---|---|
| Multi-source data: CSV, JSON, Excel, REST API | #23 |
| Form Widget: schema API + submit + `formId` config | #23 |
| `WidgetEngine.form.*` API module | #23 |
| `WidgetEngine.widgetActivity.*` API module | #22 |
| Dashboard Activity Monitoring panel | #22 |
| Dashboard Inactivity Alerts panel | #22 |
| Trang Liên hệ (`pages/contact.json`) | — |
