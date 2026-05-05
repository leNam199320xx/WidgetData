-- =============================================================================
-- sales-test.sql  —  Dữ liệu test cho sales.db (Cửa hàng)
-- =============================================================================
-- Mục đích : Tạo dữ liệu tối thiểu đủ để test tất cả widget queries của
--            demo shop-admin / shop-front.
-- Cách dùng:
--   sqlite3 sales.db < sales-test.sql
--   # hoặc trong DB Browser for SQLite: File → Import → Database from SQL file
-- Lưu ý    : Script sẽ xóa và tạo lại toàn bộ bảng (idempotent).
-- =============================================================================

PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=OFF;

-- ─── Drop tables (thứ tự ngược foreign key) ──────────────────────────────────
DROP TABLE IF EXISTS payments;
DROP TABLE IF EXISTS order_items;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS employees;
DROP TABLE IF EXISTS customers;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS categories;

-- ─── Schema ───────────────────────────────────────────────────────────────────
CREATE TABLE categories (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    name        TEXT NOT NULL,
    description TEXT,
    created_at  TEXT DEFAULT (datetime('now'))
);

CREATE TABLE products (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id    INTEGER NOT NULL,
    name           TEXT NOT NULL,
    sku            TEXT NOT NULL UNIQUE,
    description    TEXT,
    unit_price     REAL NOT NULL,
    cost_price     REAL NOT NULL,
    stock_quantity INTEGER NOT NULL DEFAULT 0,
    unit           TEXT NOT NULL DEFAULT 'cái',
    is_active      INTEGER NOT NULL DEFAULT 1,
    created_at     TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

CREATE TABLE customers (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name      TEXT NOT NULL,
    phone          TEXT,
    email          TEXT,
    address        TEXT,
    city           TEXT,
    loyalty_points INTEGER NOT NULL DEFAULT 0,
    total_spent    REAL NOT NULL DEFAULT 0,
    created_at     TEXT DEFAULT (datetime('now'))
);

CREATE TABLE employees (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name  TEXT NOT NULL,
    position   TEXT NOT NULL,
    branch     TEXT NOT NULL,
    created_at TEXT DEFAULT (datetime('now'))
);

CREATE TABLE orders (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    order_code      TEXT NOT NULL UNIQUE,
    customer_id     INTEGER,
    employee_id     INTEGER NOT NULL,
    status          TEXT NOT NULL DEFAULT 'completed',
    subtotal        REAL NOT NULL,
    discount_amount REAL NOT NULL DEFAULT 0,
    tax_amount      REAL NOT NULL DEFAULT 0,
    total_amount    REAL NOT NULL,
    note            TEXT,
    created_at      TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (customer_id) REFERENCES customers(id),
    FOREIGN KEY (employee_id) REFERENCES employees(id)
);

CREATE TABLE order_items (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id         INTEGER NOT NULL,
    product_id       INTEGER NOT NULL,
    quantity         INTEGER NOT NULL,
    unit_price       REAL NOT NULL,
    discount_percent REAL NOT NULL DEFAULT 0,
    line_total       REAL NOT NULL,
    FOREIGN KEY (order_id)   REFERENCES orders(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE payments (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id        INTEGER NOT NULL,
    payment_method  TEXT NOT NULL,
    amount          REAL NOT NULL,
    status          TEXT NOT NULL DEFAULT 'success',
    transaction_ref TEXT,
    paid_at         TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (order_id) REFERENCES orders(id)
);

CREATE INDEX idx_orders_created_at    ON orders(created_at);
CREATE INDEX idx_orders_customer_id   ON orders(customer_id);
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_payments_paid_at     ON payments(paid_at);

-- ─── 1. Danh mục (categories) ────────────────────────────────────────────────
INSERT INTO categories (name, description) VALUES
    ('Điện thoại & Phụ kiện', 'Điện thoại, tai nghe, sạc, phụ kiện di động'),
    ('Máy tính & Laptop',     'Laptop, máy tính để bàn, linh kiện'),
    ('Thiết bị âm thanh',     'Loa, tai nghe không dây, soundbar'),
    ('Thời trang nam',        'Áo, quần, giày, túi xách dành cho nam'),
    ('Thực phẩm & Đồ uống',  'Cà phê, trà, bánh kẹo, nước uống'),
    ('Sách & Văn phòng phẩm', 'Sách, bút, sổ tay, máy tính cầm tay');

-- ─── 2. Sản phẩm (products) — bao gồm sản phẩm tồn kho thấp (<50) ────────────
INSERT INTO products (category_id, name, sku, description, unit_price, cost_price, stock_quantity, unit) VALUES
    -- Điện thoại (cat=1)
    (1, 'iPhone 15 Pro Max 256GB',      'IPH-15PM-256',   'iPhone 15 Pro Max',   28990000, 22000000, 50,  'máy'),
    (1, 'Samsung Galaxy S24 Ultra',     'SAM-S24U',       'Samsung S24 Ultra',   25990000, 19000000, 45,  'máy'),
    (1, 'Ốp lưng iPhone 15',            'ACC-CASE-I15',   'Ốp lưng bảo vệ',        199000,    80000, 500, 'cái'),
    (1, 'Cáp sạc USB-C 1m',             'ACC-CABLE-C1',   'Cáp sạc nhanh',          149000,    50000, 12,  'sợi'), -- tồn kho thấp
    (1, 'Kính cường lực iPhone 15',     'ACC-GLASS-I15',  'Kính cường lực 9H',       99000,    30000, 30,  'tấm'), -- tồn kho thấp
    -- Laptop (cat=2)
    (2, 'MacBook Air M3 8GB 256GB',     'MAC-AIR-M3',     'MacBook Air M3',      28490000, 21000000, 30,  'máy'),
    (2, 'Dell XPS 15 i7-13th Gen',      'DEL-XPS15',      'Dell XPS 15 inch',    35990000, 27000000, 8,   'máy'), -- tồn kho thấp
    (2, 'Bàn phím cơ Keychron K2',      'KEY-K2-BT',      'Bàn phím cơ Bluetooth', 1890000, 1100000, 100, 'cái'),
    -- Âm thanh (cat=3)
    (3, 'Loa Bluetooth JBL Charge 5',   'JBL-CHG5',       'Loa JBL không dây',    3490000,  2300000, 60,  'cái'),
    (3, 'Tai nghe Sony WH-1000XM5',     'SON-WH1000XM5',  'Tai nghe chống ồn',    7490000,  5200000, 20,  'cái'), -- tồn kho thấp
    -- Thời trang (cat=4)
    (4, 'Áo polo nam basic',            'CLO-POLO-M',     'Áo polo vải cotton',     249000,   120000, 300, 'cái'),
    (4, 'Quần jean nam slim fit',        'CLO-JEAN-M',     'Quần jean co giãn',      399000,   180000, 5,   'cái'), -- tồn kho thấp
    -- Thực phẩm (cat=5)
    (5, 'Cà phê rang xay Highlands 500g','FOOD-CF-HG500', 'Cà phê nguyên chất',     119000,    60000, 500, 'gói'),
    (5, 'Chocolate Ferrero Rocher 24v',  'FOOD-CHOC-FER24','Socola Ferrero hộp 24v', 249000,   140000, 300, 'hộp'),
    -- Sách (cat=6)
    (6, 'Sách Đắc Nhân Tâm',            'BOOK-DNT',       'Dale Carnegie tiếng Việt', 89000,  40000, 500, 'quyển'),
    (6, 'Bút Parker IM Black',          'PEN-PAR-IMB',    'Bút ký cao cấp',          690000,  450000, 40,  'cái'); -- tồn kho thấp

-- ─── 3. Khách hàng (customers) ───────────────────────────────────────────────
INSERT INTO customers (full_name, phone, email, address, city, loyalty_points, total_spent) VALUES
    ('Nguyễn Văn An',   '0912345678', 'an.nguyen@gmail.com',   'Số 12, Đường Láng, Đống Đa',     'Hà Nội',          1200, 5850000),
    ('Trần Thị Mai',    '0987654321', 'mai.tran@gmail.com',    'Số 45, Nguyễn Huệ, Q1',          'TP. Hồ Chí Minh', 880,  3250000),
    ('Lê Văn Đức',      '0901234567', 'duc.le@company.vn',     'Số 8, Trần Phú, Hải Châu',       'Đà Nẵng',         450,  1980000),
    ('Phạm Thị Hoa',    '0345678901', 'hoa.pham@gmail.com',    'Số 22, Lê Lợi, Ninh Kiều',       'Cần Thơ',         300,  1200000),
    ('Hoàng Văn Minh',  '0356789012', 'minh.hoang@gmail.com',  'Số 100, Hùng Vương, Hồng Bàng',  'Hải Phòng',       650,  2870000),
    ('Vũ Thị Lan',      '0367890123', 'lan.vu@email.com',      'Số 55, Bạch Đằng, Việt Trì',     'Phú Thọ',         120,   560000),
    ('Đặng Quốc Hùng',  '0378901234', 'hung.dang@gmail.com',   'Số 33, Đinh Tiên Hoàng, TP Huế', 'Huế',             950,  4320000),
    ('Ngô Thị Phương',  '0389012345', 'phuong.ngo@gmail.com',  'Số 7, Trần Hưng Đạo, TP Nha Trang', 'Nha Trang',   200,   780000);

-- ─── 4. Nhân viên (employees) ────────────────────────────────────────────────
INSERT INTO employees (full_name, position, branch) VALUES
    ('Nguyễn Văn Hùng',   'Trưởng cửa hàng',    'Chi nhánh Hà Nội'),
    ('Trần Thị Mai',       'Nhân viên bán hàng', 'Chi nhánh Hà Nội'),
    ('Lê Văn Đức',         'Thu ngân',           'Chi nhánh Hà Nội'),
    ('Vũ Thị Hoa',         'Trưởng cửa hàng',    'Chi nhánh TP.HCM'),
    ('Đặng Văn Tùng',      'Nhân viên bán hàng', 'Chi nhánh TP.HCM'),
    ('Dương Văn Quân',     'Trưởng cửa hàng',    'Chi nhánh Đà Nẵng');

-- ─── 5. Đơn hàng + chi tiết + thanh toán ─────────────────────────────────────
-- Tháng này (current month)
INSERT INTO orders (order_code, customer_id, employee_id, status, subtotal, discount_amount, tax_amount, total_amount, created_at) VALUES
    ('ORD-2026-001', 1, 1, 'completed',  28990000,      0, 0, 28990000, datetime('now', '-2 days')),
    ('ORD-2026-002', 2, 4, 'completed',  3640000,       0, 0,  3640000, datetime('now', '-2 days')),
    ('ORD-2026-003', 3, 6, 'completed',  7490000,       0, 0,  7490000, datetime('now', '-3 days')),
    ('ORD-2026-004', 1, 2, 'completed',  28490000,      0, 0, 28490000, datetime('now', '-4 days')),
    ('ORD-2026-005', 4, 5, 'cancelled',   249000,       0, 0,   249000, datetime('now', '-5 days')),
    ('ORD-2026-006', 5, 1, 'completed',  25990000, 500000, 0, 25490000, datetime('now', '-6 days')),
    ('ORD-2026-007', 2, 2, 'completed',   597000,       0, 0,   597000, datetime('now', '-7 days')),
    ('ORD-2026-008', NULL,3, 'completed',  199000,       0, 0,   199000, datetime('now', '-1 day')),  -- khách lẻ
    ('ORD-2026-009', 6, 4, 'refunded',   1890000,       0, 0,  1890000, datetime('now', '-8 days')),
    ('ORD-2026-010', 7, 6, 'completed',  35990000,      0, 0, 35990000, datetime('now', '-9 days'));

-- Tháng trước (last month -35 ngày)
INSERT INTO orders (order_code, customer_id, employee_id, status, subtotal, discount_amount, tax_amount, total_amount, created_at) VALUES
    ('ORD-2026-011', 3, 6, 'completed',  3490000,       0, 0,  3490000, datetime('now', '-35 days')),
    ('ORD-2026-012', 8, 1, 'completed',  28990000,      0, 0, 28990000, datetime('now', '-38 days')),
    ('ORD-2026-013', 1, 2, 'completed',  7490000,       0, 0,  7490000, datetime('now', '-40 days')),
    ('ORD-2026-014', 5, 5, 'completed',  199000,        0, 0,   199000, datetime('now', '-42 days'));

-- Hai tháng trước
INSERT INTO orders (order_code, customer_id, employee_id, status, subtotal, discount_amount, tax_amount, total_amount, created_at) VALUES
    ('ORD-2026-015', 2, 4, 'completed',  25990000,      0, 0, 25990000, datetime('now', '-65 days')),
    ('ORD-2026-016', 4, 2, 'completed',  1890000,       0, 0,  1890000, datetime('now', '-70 days')),
    ('ORD-2026-017', 7, 1, 'completed',  89000,         0, 0,    89000, datetime('now', '-72 days'));

-- Ba tháng trước
INSERT INTO orders (order_code, customer_id, employee_id, status, subtotal, discount_amount, tax_amount, total_amount, created_at) VALUES
    ('ORD-2026-018', 1, 3, 'completed',  28490000,      0, 0, 28490000, datetime('now', '-95 days')),
    ('ORD-2026-019', 3, 6, 'completed',  7490000,       0, 0,  7490000, datetime('now','-100 days')),
    ('ORD-2026-020', 6, 4, 'completed',   597000,       0, 0,   597000, datetime('now','-105 days'));

-- ─── 6. Chi tiết đơn hàng (order_items) ──────────────────────────────────────
INSERT INTO order_items (order_id, product_id, quantity, unit_price, discount_percent, line_total) VALUES
    (1,  1,  1, 28990000, 0,   28990000),  -- ORD-001: iPhone
    (2,  9,  1,  3490000, 0,    3490000),  -- ORD-002: JBL speaker
    (2,  3,  1,   199000, 0,     199000),  -- ORD-002: Ốp lưng (extra item)  -- fixed: was sum 3689000 → use individual adds
    (3, 10,  1,  7490000, 0,    7490000),  -- ORD-003: Sony WH-1000XM5
    (4,  6,  1, 28490000, 0,   28490000),  -- ORD-004: MacBook Air M3
    (5, 13,  1,   249000, 0,     249000),  -- ORD-005: Chocolate (cancelled)
    (6,  2,  1, 25990000, 0,   25990000),  -- ORD-006: Samsung S24 Ultra (with discount)
    (7, 11,  1,   249000, 0,     249000),  -- ORD-007: Áo polo
    (7, 12,  1,   399000, 0,     399000),  -- ORD-007: Quần jean
    (8,  3,  1,   199000, 0,     199000),  -- ORD-008: Ốp lưng (walk-in)
    (9,  8,  1,  1890000, 0,    1890000),  -- ORD-009: Keychron K2 (refunded)
    (10, 7,  1, 35990000, 0,   35990000), -- ORD-010: Dell XPS 15
    (11, 9,  1,  3490000, 0,    3490000), -- ORD-011
    (12, 1,  1, 28990000, 0,   28990000), -- ORD-012
    (13,10,  1,  7490000, 0,    7490000), -- ORD-013
    (14, 3,  1,   199000, 0,     199000), -- ORD-014
    (15, 2,  1, 25990000, 0,   25990000), -- ORD-015
    (16, 8,  1,  1890000, 0,    1890000), -- ORD-016
    (17,15,  1,    89000, 0,      89000), -- ORD-017: Sách
    (18, 6,  1, 28490000, 0,   28490000), -- ORD-018
    (19,10,  1,  7490000, 0,    7490000), -- ORD-019
    (20, 8,  1,   597000, 0,     597000); -- ORD-020: 3 × bút Parker (mocked as 1 line)

-- ─── 7. Thanh toán (payments) ─────────────────────────────────────────────────
INSERT INTO payments (order_id, payment_method, amount, status, transaction_ref, paid_at) VALUES
    (1,  'Chuyển khoản',        28990000, 'success',  'TXN-VCB-0001', datetime('now', '-2 days')),
    (2,  'Ví điện tử MoMo',      3640000, 'success',  'TXN-MMO-0002', datetime('now', '-2 days')),
    (3,  'Thẻ tín dụng',         7490000, 'success',  'TXN-VIS-0003', datetime('now', '-3 days')),
    (4,  'Chuyển khoản',        28490000, 'success',  'TXN-VCB-0004', datetime('now', '-4 days')),
    (5,  'Tiền mặt',               249000, 'failed',   'TXN-CSH-0005', datetime('now', '-5 days')),
    (6,  'QR Code',             25490000, 'success',  'TXN-QRC-0006', datetime('now', '-6 days')),
    (7,  'Tiền mặt',               597000, 'success',  'TXN-CSH-0007', datetime('now', '-7 days')),
    (8,  'Tiền mặt',               199000, 'success',  'TXN-CSH-0008', datetime('now', '-1 day')),
    (9,  'Ví ZaloPay',           1890000, 'refunded', 'TXN-ZLP-0009', datetime('now', '-8 days')),
    (10, 'Thẻ tín dụng',        35990000, 'success',  'TXN-VIS-0010', datetime('now', '-9 days')),
    (11, 'Chuyển khoản',         3490000, 'success',  'TXN-VCB-0011', datetime('now', '-35 days')),
    (12, 'Ví điện tử MoMo',     28990000, 'success',  'TXN-MMO-0012', datetime('now', '-38 days')),
    (13, 'Thẻ tín dụng',         7490000, 'success',  'TXN-VIS-0013', datetime('now', '-40 days')),
    (14, 'Tiền mặt',               199000, 'success',  'TXN-CSH-0014', datetime('now', '-42 days')),
    (15, 'QR Code',             25990000, 'success',  'TXN-QRC-0015', datetime('now', '-65 days')),
    (16, 'Chuyển khoản',         1890000, 'success',  'TXN-VCB-0016', datetime('now', '-70 days')),
    (17, 'Tiền mặt',                89000, 'success',  'TXN-CSH-0017', datetime('now', '-72 days')),
    (18, 'Thẻ tín dụng',        28490000, 'success',  'TXN-VIS-0018', datetime('now', '-95 days')),
    (19, 'Ví ZaloPay',           7490000, 'success',  'TXN-ZLP-0019', datetime('now','-100 days')),
    (20, 'Ví điện tử MoMo',       597000, 'success',  'TXN-MMO-0020', datetime('now','-105 days'));

-- ─── 8. Cập nhật total_spent cho khách hàng ──────────────────────────────────
UPDATE customers SET total_spent = (
    SELECT COALESCE(SUM(o.total_amount), 0)
    FROM orders o
    WHERE o.customer_id = customers.id AND o.status = 'completed'
);

-- ─── Kiểm tra nhanh ──────────────────────────────────────────────────────────
-- Chạy các câu này để xác nhận dữ liệu đúng:
--
-- SELECT COUNT(*) as total_orders FROM orders;                        → 20
-- SELECT COUNT(*) as completed FROM orders WHERE status='completed';  → 17
-- SELECT COUNT(*) as customers FROM customers;                        → 8
-- SELECT COUNT(*) as low_stock FROM products WHERE stock_quantity<50; → 6
-- SELECT payment_method, COUNT(*) FROM payments WHERE status='success' GROUP BY payment_method;
-- SELECT strftime('%Y-%m',created_at) as m, SUM(total_amount) FROM orders WHERE status='completed' GROUP BY m;
