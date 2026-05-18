# CSV Small System Test Dataset

Bộ dữ liệu test nhỏ, chỉ dùng file CSV (không dùng database cho dữ liệu nghiệp vụ).

## Thành phần

- `users.csv`: tài khoản mẫu gồm admin + customer
- `datasources.csv`: định nghĩa nguồn dữ liệu CSV cho widget
- `widgets.csv`: định nghĩa widget mẫu cho hệ thống nhỏ
- `data/orders.csv`: dữ liệu đơn hàng
- `data/customers.csv`: dữ liệu khách hàng
- `data/payments.csv`: dữ liệu thanh toán

## Gợi ý sử dụng

1. Bật file-based business data:
   - `Storage:BusinessDataProvider=json`
2. Tạo DataSource theo `datasources.csv` (loại `Csv`) và map `connection_string` tới file trong thư mục `data/`.
3. Tạo Widget theo `widgets.csv` và gán `data_source_key` tương ứng.
4. Với luồng auth thực tế, hệ thống vẫn dùng ASP.NET Identity + SQLite; file `users.csv` là bộ user test để import/mock cho môi trường test CSV-only.

