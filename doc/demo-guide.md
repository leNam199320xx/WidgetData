# Hướng dẫn tạo 1 demo end-to-end

Tài liệu này hướng dẫn chạy hệ thống và dựng nhanh 1 demo hoàn chỉnh với luồng:
**WidgetData.Web (admin) → WidgetData.API (data/execute) → WidgetData.Worker (schedule) → front-end demo**.

## 1) Chuẩn bị môi trường

Yêu cầu:
- .NET SDK 10
- Docker Desktop (để chạy AppHost thuận tiện)

Mở terminal tại:

```bash
cd /home/runner/work/WidgetData/WidgetData
```

Khôi phục package:

```bash
dotnet restore
```

## 2) Khởi động hệ thống demo

### Cách nhanh nhất (khuyến nghị): chạy AppHost

```bash
dotnet run --project /home/runner/work/WidgetData/WidgetData/src/WidgetData.AppHost
```

AppHost hiện orchestration các service:
- `widgetdata-api`
- `widgetdata-worker`
- `widgetdata-gateway`
- `widgetdata-web`

### Cách tách riêng từng service

Mở 3 terminal:

```bash
dotnet run --project /home/runner/work/WidgetData/WidgetData/src/WidgetData.API
dotnet run --project /home/runner/work/WidgetData/WidgetData/src/WidgetData.Web
dotnet run --project /home/runner/work/WidgetData/WidgetData/src/WidgetData.Worker
```

### Xác nhận service đã lên

- API docs (Development): `http://localhost:5114/scalar`
- Health endpoint: `http://localhost:5114/health`
- Web admin: `http://localhost:5282`

Tài khoản mặc định (seed):
- Email: `admin@widgetdata.com`
- Password: `Admin@123!`

## 3) Đăng nhập admin và tạo dữ liệu demo

Truy cập Web admin:

```text
http://localhost:5282
```

Thực hiện theo thứ tự:
1. Tạo hoặc chọn tenant demo.
2. Tạo **Data Source** (Database/CSV/JSON/RestApi).
3. Tạo **Widget** và cấu hình lấy dữ liệu từ Data Source vừa tạo.
4. Tạo **Dashboard Page** và gắn các widget vào page.
5. Mở preview để kiểm tra widget đã trả dữ liệu đúng.

## 4) Tạo front-end demo

Chọn 1 front-end có sẵn:
- `/home/runner/work/WidgetData/WidgetData/demo/shop/shop-front`
- `/home/runner/work/WidgetData/WidgetData/demo/news/news-front`
- `/home/runner/work/WidgetData/WidgetData/demo/course/course-front`

Ví dụ chạy `shop-front`:

```bash
cd /home/runner/work/WidgetData/WidgetData/demo/shop/shop-front
python -m http.server 3000
```

Mở:
- `http://localhost:3000/index.html` (public page)
- `http://localhost:3000/dashboard.html` (dashboard có auth)

Cấu hình trong front-end:
- API URL: `http://localhost:5114/api`
- Trỏ widget/page ID đúng với dữ liệu bạn vừa tạo ở Web admin.

## 5) Kiểm thử luồng end-to-end

Checklist kiểm thử:
1. Đăng nhập dashboard demo thành công.
2. Widget đọc dữ liệu thành công từ Data Source.
3. Execute widget trả dữ liệu đúng.
4. Dashboard page hiển thị đúng layout và nội dung.
5. Nếu cần chạy định kỳ: tạo Schedule cho widget, bật `IsEnabled`.
6. Xác nhận Worker đã thực thi schedule (xem log runtime và trạng thái lần chạy gần nhất).

## 6) Gợi ý xử lý lỗi nhanh

- Không đăng nhập được: kiểm tra API có chạy ở `http://localhost:5114` và user seed còn tồn tại.
- Front-end bị CORS: chạy API trong môi trường Development hoặc cấu hình lại `CorsSettings`.
- Widget không có dữ liệu: kiểm tra lại query/path file/jsonPath trong Data Source.
- Schedule không chạy: đảm bảo `WidgetData.Worker` đang chạy và schedule đã `IsEnabled=true`.
