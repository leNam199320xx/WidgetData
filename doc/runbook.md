# Runbook vận hành sự cố

## Mục tiêu
- Giảm MTTR cho các lỗi production thường gặp.
- Chuẩn hóa bước kiểm tra giữa API, Worker, DB và Gateway.

## 1) API down / trả lỗi 5xx tăng đột biến
1. Kiểm tra health endpoint: `/health`.
2. Kiểm tra log lỗi gần nhất (Serilog) theo `RequestPath`, `StatusCode`, `Exception`.
3. Xác nhận cấu hình bắt buộc còn hợp lệ:
   - `JwtSettings:Secret`
   - `CorsSettings:AllowedOrigins`
4. Nếu lỗi liên quan DB: chuyển sang mục **DB lock / DB unavailable**.
5. Nếu chỉ lỗi một nhóm endpoint, rollback về release tag gần nhất đã ổn định.

## 2) Worker bị stuck / schedule không chạy
1. Kiểm tra service `WidgetData.Worker` còn chạy.
2. Kiểm tra `SchedulerWorker:PollingIntervalSeconds` và timezone schedule.
3. Truy vấn các schedule có `NextRunAt <= now` nhưng không phát sinh `WidgetExecution`.
4. Kiểm tra retry (`RetryOnFailure`, `MaxRetries`) và log lỗi downstream.
5. Nếu backlog tăng liên tục, tạm giảm tải bằng cách disable schedule không critical.

## 3) SQLite DB lock / DB unavailable
1. Xác nhận file DB còn dung lượng, quyền ghi và không bị mount read-only.
2. Kiểm tra số lượng request ghi đồng thời tăng đột biến.
3. Bật chế độ bảo trì ngắn hạn cho các endpoint ghi nếu cần.
4. Khởi động lại service theo thứ tự: API -> Worker -> Gateway.
5. Nếu lock lặp lại, thực hiện backup + vacuum + tối ưu index.

## 4) CORS/JWT lỗi sau deploy
1. Xác nhận `CorsSettings:AllowedOrigins` và `CorsSettings:WidgetEmbedAllowedOrigins` đúng môi trường.
2. Xác nhận issuer/audience khớp giữa API và client.
3. Kiểm tra secret rotation: secret mới đã được rollout đồng bộ chưa.
4. Kiểm tra đồng hồ máy chủ (ClockSkew thấp có thể làm token hết hạn sớm).

## 5) Quy trình rollback tiêu chuẩn
1. Xác định release tag ổn định gần nhất.
2. Rollback artifact API/Worker/Web đồng bộ.
3. Xác nhận `/health`, login, execute widget, scheduler chạy lại bình thường.
4. Lập postmortem: nguyên nhân gốc, tác động, hành động phòng ngừa.
