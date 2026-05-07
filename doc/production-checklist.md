# Production checklist trước release

## CI/CD & Chất lượng
- [ ] Build `WidgetData.sln` thành công ở cấu hình Release.
- [ ] Unit + integration smoke tests pass.
- [ ] Coverage đạt ngưỡng tối thiểu của pipeline.
- [ ] Dependency scan không có vulnerability mức cao/critical.
- [ ] CodeQL scan không có issue nghiêm trọng chưa xử lý.

## Bảo mật
- [ ] `JwtSettings:Secret` được quản lý bằng secret manager, không hard-code.
- [ ] `CorsSettings:AllowedOrigins` và `CorsSettings:WidgetEmbedAllowedOrigins` được cấu hình đúng môi trường.
- [ ] HTTPS/TLS được bật toàn bộ ingress.
- [ ] Rotation plan cho JWT secret đã được xác nhận.

## Vận hành
- [ ] Có dashboard theo dõi health, lỗi, latency, scheduler failure.
- [ ] Alerting đã bật cho API error rate và worker backlog.
- [ ] Runbook sự cố đã cập nhật theo release hiện tại.
- [ ] Kế hoạch rollback release đã kiểm tra.

## Dữ liệu & hiệu năng
- [ ] Migration mới (nếu có) đã apply và verify.
- [ ] Index cho truy vấn nóng đã được kiểm tra.
- [ ] Kiểm tra endpoint payload lớn có phân trang/giới hạn hợp lý.

## Nghiệm thu nghiệp vụ
- [ ] Smoke flow: login -> execute widget -> xem dashboard hoạt động.
- [ ] Flow public form submit hoạt động và lưu dữ liệu đúng tenant.
- [ ] Worker schedule chạy đúng timezone và ghi execution log.
