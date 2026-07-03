# Ke hoach tach module WidgetData

## 1. Muc tieu

- Giam coupling giua cac domain nghiep vu de code de bao tri hon.
- Chia nho cac service lon thanh cac use-case ro rang.
- Chuyen tu mot khoi Infrastructure lon sang mo hinh modular monolith.
- Giu nguyen hanh vi API hien tai trong qua trinh refactor.

## 2. Pham vi tach module

- Widgets module: CRUD, execute, history, config archive.
- DataSources module: CRUD, upload file, test connection.
- Pages module: CRUD, versioning, publish, rollback, widget layout.
- Delivery module: target management, dispatch da kenh.
- Identity and Tenants module: auth, permission, tenant context.
- Cross-cutting module: audit, logging, seeding, migration, shared abstractions.

## 3. Nguyen tac thuc hien

- Lam theo modular monolith truoc, khong tach microservice ngay.
- Moi phase phai build xanh va test xanh truoc khi sang phase tiep.
- Khong doi endpoint contract trong dot refactor dau tien.
- Tinh nang moi phai dat dung module, khong them vao service tong hop cu.
- Side effect startup se duoc day sang initializer pipeline.

## 4. Lo trinh de xuat theo 6 sprint

### Sprint 1: Chuan bi bien gioi module va anti-corruption layer

- Tao cau truc folder theo module ben trong solution hien tai.
- Dinh nghia interface use-case cho tung module.
- Tao module registration methods theo tung domain.
- Tach wiring DI theo module thay vi mot file dang ky lon.

**Trang thai**: Hoan thanh

Deliverables:
- Module map tai lieu hoa.
- DI extension methods rieng cho tung module.
- Build va smoke test pass.

### Sprint 2: Tach Widget module

- Tach WidgetService thanh WidgetCrudService va WidgetExecutionService.
- Tach data loading thanh strategy theo source type.
- Tach logic archive config thanh service rieng.
- Giu interface va response hien tai de khong anh huong client.

**Trang thai**: Hoan thanh

Deliverables:
- Widget module co service nho theo use-case.
- Unit test cho execute flow va archive flow.
- Regression test endpoint widgets pass.

### Sprint 3: Tach DataSources module

- Tach DataSourceService thanh 3 use-case: CRUD, Upload, ConnectivityTest.
- Tao validator rieng theo source type.
- Tach file handling sang component rieng.

**Trang thai**: Hoan thanh

Deliverables:
- DataSources module khong con service da trach nhiem.
- Test cho upload va test-connection pass.

### Sprint 4: Tach Pages module

- Tach versioning logic thanh PageVersioningService.
- Tach publish and rollback use-cases.
- Giu nguyen lifecycle state machine va snapshot format.

**Trang thai**: Hoan thanh

Deliverables:
- Pages module ro bien gioi CRUD va versioning.
- Test publish rollback va layout pass.

### Sprint 5: Tach Delivery module

- Tach target management khoi dispatch.
- Tao delivery dispatcher theo strategy cho Email, SFTP, SSH, HTTP, Telegram, Zalo, File.
- Bo direct dependency vao DbContext trong service nghiep vu, di qua repository interface.

**Trang thai**: Hoan thanh

Deliverables:
- Delivery module plugin-like cho kenh giao nhan.
- Test cho it nhat Email, HTTP, File.

### Sprint 6: Hardening va startup cleanup

- Day seed, migration, bootstrap side effects sang startup pipeline co dieu kien.
- Chuan hoa telemetry, audit, error policy theo module.
- Don dep code cu va khoa boundary module bang architecture tests.

**Trang thai**: Hoan thanh

Deliverables:
- Startup sach hon, side effect ro rang.
- Architecture tests chan vi pham module boundaries.
- Tai lieu hoa quy trinh them module moi.

## 5. Ke hoach PR nho de trien khai an toan

- PR 1: Them module folders va DI registration theo module, chua doi logic.
- PR 2: Widget service split.
- PR 3: DataSources service split.
- PR 4: Pages service split.
- PR 5: Delivery strategy split.
- PR 6: Startup side-effect cleanup.
- PR 7: Architecture tests va docs cap nhat.

## 6. Tieu chi nghiem thu

- Build solution pass.
- Unit tests va integration tests pass.
- Khong thay doi contract endpoint da public.
- Khong tang coupling cheo module.
- Do phuc tap cyclomatic cua cac service lon giam dang ke.

## 7. Chi so theo doi tien do

- So service lon tren 500 dong truoc va sau refactor.
- So dependency tiep theo giua module.
- Ty le test bao phu cac use-case da tach.
- So bug regression phat sinh sau moi phase.

## 8. Rui ro va giai phap

- Rui ro vo tenant filtering va shared data behavior.
- Giai phap: giu integration test theo tenant va bo sung test null tenant.

- Rui ro vo co che EF va JSON dual-mode.
- Giai phap: abstraction provider ro rang va test song song 2 mode.

- Rui ro startup side effects gay hanh vi bat ngo.
- Giai phap: co feature flags cho migration and seed pipeline.

## 9. Uoc luong tong quan

- Tong thoi gian: 6 sprint ngan, moi sprint 3 den 5 ngay lam viec.
- Tong effort: trung binh den cao, nhung rui ro van hanh thap neu di theo PR nho.

## 10. Trang thai hien tai

- Sprint 1–6: Hoan thanh
- Build: Infrastructure + API + Tests sach; 244 unit tests pass
- Chi con 2 loi pre-existing o Worker project (khong lien quan)
- Tat ca endpoint contracts duoc giu nguyen
- Facade services duoc giu lai de backward-compatible

## 10. Buoc tiep theo ngay

- Chot thu tu uu tien module: Widgets truoc, sau do DataSources, Pages, Delivery.
- Tao issue va milestone theo cac PR da liet ke.
- Bat dau voi PR 1 de tach DI registration theo module.
