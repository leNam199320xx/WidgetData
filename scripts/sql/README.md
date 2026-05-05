# SQL Test Data Scripts

Thư mục này chứa các file SQL với lệnh `INSERT` sẵn có để nhanh chóng tạo dữ liệu test cho các SQLite database của demo apps.

## Các file

| File | Database | Demo app |
|---|---|---|
| `sales-test.sql`  | `sales.db`  | shop-admin / shop-front |
| `course-test.sql` | `course.db` | course-front (EduViet) |
| `news-test.sql`   | `news.db`   | news-front (VietNews) |

## Cách chạy

### Bằng sqlite3 CLI

```bash
# Tạo database mới từ script
sqlite3 sales.db  < scripts/sql/sales-test.sql
sqlite3 course.db < scripts/sql/course-test.sql
sqlite3 news.db   < scripts/sql/news-test.sql

# Hoặc chạy trong thư mục output của API
cd src/WidgetData.API/bin/Debug/net10.0
sqlite3 sales.db  < ../../../../../scripts/sql/sales-test.sql
sqlite3 course.db < ../../../../../scripts/sql/course-test.sql
sqlite3 news.db   < ../../../../../scripts/sql/news-test.sql
```

### Bằng DB Browser for SQLite

1. Tạo/mở database file (ví dụ `sales.db`)
2. Menu **File → Import → Database from SQL file...**
3. Chọn file `.sql` tương ứng
4. Nhấn **OK**

### Bằng PowerShell (Windows)

```powershell
# Cài sqlite3 nếu chưa có: winget install SQLite.SQLite
sqlite3 sales.db  (Get-Content scripts\sql\sales-test.sql -Raw)
sqlite3 course.db (Get-Content scripts\sql\course-test.sql -Raw)
sqlite3 news.db   (Get-Content scripts\sql\news-test.sql -Raw)
```

---

## Nội dung dữ liệu

### sales-test.sql — Cửa hàng điện tử

| Bảng | Số dòng | Ghi chú |
|---|---|---|
| categories | 6 | Điện thoại, Laptop, Âm thanh, Thời trang, Thực phẩm, Sách |
| products | 16 | Bao gồm 6 sản phẩm có tồn kho thấp (<50) |
| customers | 8 | Phân bổ các tỉnh thành |
| employees | 6 | 3 chi nhánh: Hà Nội, TP.HCM, Đà Nẵng |
| orders | 20 | Trải dài 4 tháng; includes completed/cancelled/refunded |
| order_items | 21 | |
| payments | 20 | Các phương thức: tiền mặt, chuyển khoản, thẻ, MoMo, ZaloPay, QR |

**Widget queries có thể test:**
- Tổng doanh thu, Tổng đơn hàng, Giá trị đơn TB, Khách hàng
- Xu hướng doanh thu theo tháng (4 tháng dữ liệu)
- Doanh thu theo danh mục
- Đơn hàng gần đây
- Trạng thái đơn hàng (completed / cancelled / refunded)
- Phương thức thanh toán
- Tồn kho thấp

---

### course-test.sql — EduViet (học trực tuyến)

| Bảng | Số dòng | Ghi chú |
|---|---|---|
| categories | 4 | Lập trình, Kinh doanh, Marketing, Ngoại ngữ |
| instructors | 5 | |
| courses | 10 | |
| students | 15 | 2 học viên đăng ký hôm nay |
| enrollments | 25 | 3 đăng ký hôm nay; completed/active/paused |
| lessons | 10 | 5 bài cho 2 khóa |
| course_payments | 17 | 3 thanh toán hôm nay |
| reviews | 7 | Rating 4-5 sao |

**Widget queries có thể test:**
- Đăng ký mới hôm nay (→ 3)
- Khóa học đang hoạt động (→ 10)
- Tỷ lệ hoàn thành (→ ~28%)
- Doanh thu hôm nay (→ 1,847,000₫)
- Đăng ký theo danh mục (bar chart)
- Khóa học phổ biến nhất (top 10)
- Tiến độ hoàn thành (donut chart)
- Hoạt động học viên gần đây

---

### news-test.sql — VietNews (cổng tin tức)

| Bảng | Số dòng | Ghi chú |
|---|---|---|
| categories | 6 | Công nghệ, Kinh tế, Thể thao, Giải trí, Xã hội, Sức khỏe |
| authors | 5 | |
| articles | 23 | 3 bài đăng hôm nay |
| readers | 13 | 3 độc giả mới hôm nay |
| article_views | 65+ | 20 lượt xem hôm nay; phân bổ 4 tháng |
| comments | 13 | |

**Widget queries có thể test:**
- Tổng lượt xem hôm nay (→ 20)
- Bài đăng hôm nay (→ 3)
- Độc giả mới (→ 3)
- Tỷ lệ đọc trọn bài trung bình
- Lượt xem theo chuyên mục (bar chart)
- Bài viết phổ biến nhất trong tuần
- Nguồn truy cập (pie chart: google/social/direct/email/referral)
- Hoạt động độc giả gần đây
- Xu hướng lượt xem theo tháng (4 tháng)

---

## Lưu ý

- Mỗi script **xóa và tạo lại** toàn bộ bảng (`DROP TABLE IF EXISTS`) → **idempotent**, có thể chạy nhiều lần.
- Dữ liệu sử dụng `datetime('now', '-N days')` để đảm bảo các query "hôm nay" / "tuần này" / "tháng này" luôn trả về kết quả đúng.
- Nếu API đang chạy và đã có `.db` file, hãy dừng API trước khi chạy script để tránh lock file.
