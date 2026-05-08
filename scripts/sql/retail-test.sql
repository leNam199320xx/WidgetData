-- =============================================================================
-- retail-test.sql  —  Dữ liệu test cho retail.db (Retail Analytics)
-- =============================================================================
-- Mục đích : Tạo dữ liệu mẫu cho tenant retail để test widget inventory/sales/PO.
-- Cách dùng:
--   sqlite3 retail.db < retail-test.sql
--   # hoặc trong DB Browser for SQLite: File → Import → Database from SQL file
-- Lưu ý    : Script sẽ xóa và tạo lại toàn bộ bảng (idempotent).
-- =============================================================================

PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=OFF;

-- ─── Drop tables ─────────────────────────────────────────────────────────────
DROP TABLE IF EXISTS sales_items;
DROP TABLE IF EXISTS sales_receipts;
DROP TABLE IF EXISTS stock_movements;
DROP TABLE IF EXISTS purchase_order_items;
DROP TABLE IF EXISTS purchase_orders;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS suppliers;
DROP TABLE IF EXISTS stores;
DROP TABLE IF EXISTS categories;

-- ─── Schema ──────────────────────────────────────────────────────────────────
CREATE TABLE categories (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    name        TEXT NOT NULL,
    slug        TEXT NOT NULL UNIQUE,
    description TEXT,
    created_at  TEXT DEFAULT (datetime('now'))
);

CREATE TABLE stores (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    code        TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL,
    city        TEXT NOT NULL,
    is_active   INTEGER NOT NULL DEFAULT 1,
    opened_at   TEXT DEFAULT (datetime('now'))
);

CREATE TABLE suppliers (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    name          TEXT NOT NULL,
    contact_email TEXT,
    phone         TEXT,
    city          TEXT,
    rating        REAL NOT NULL DEFAULT 4.5,
    created_at    TEXT DEFAULT (datetime('now'))
);

CREATE TABLE products (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id       INTEGER NOT NULL,
    supplier_id       INTEGER NOT NULL,
    sku               TEXT NOT NULL UNIQUE,
    name              TEXT NOT NULL,
    unit              TEXT NOT NULL DEFAULT 'pcs',
    cost_price        REAL NOT NULL DEFAULT 0,
    sale_price        REAL NOT NULL DEFAULT 0,
    reorder_level     INTEGER NOT NULL DEFAULT 20,
    current_stock     INTEGER NOT NULL DEFAULT 0,
    is_active         INTEGER NOT NULL DEFAULT 1,
    created_at        TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (category_id) REFERENCES categories(id),
    FOREIGN KEY (supplier_id) REFERENCES suppliers(id)
);

CREATE TABLE purchase_orders (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    supplier_id     INTEGER NOT NULL,
    store_id        INTEGER NOT NULL,
    po_number       TEXT NOT NULL UNIQUE,
    status          TEXT NOT NULL DEFAULT 'received',
    total_amount    REAL NOT NULL DEFAULT 0,
    ordered_at      TEXT DEFAULT (datetime('now')),
    received_at     TEXT,
    FOREIGN KEY (supplier_id) REFERENCES suppliers(id),
    FOREIGN KEY (store_id) REFERENCES stores(id)
);

CREATE TABLE purchase_order_items (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    purchase_order_id   INTEGER NOT NULL,
    product_id          INTEGER NOT NULL,
    quantity            INTEGER NOT NULL DEFAULT 1,
    unit_cost           REAL NOT NULL DEFAULT 0,
    line_total          REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE stock_movements (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    store_id        INTEGER NOT NULL,
    product_id      INTEGER NOT NULL,
    movement_type   TEXT NOT NULL, -- in, out, adjustment
    quantity        INTEGER NOT NULL,
    ref_type        TEXT,
    ref_id          INTEGER,
    note            TEXT,
    moved_at        TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (store_id) REFERENCES stores(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE sales_receipts (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    store_id        INTEGER NOT NULL,
    receipt_number  TEXT NOT NULL UNIQUE,
    status          TEXT NOT NULL DEFAULT 'completed',
    payment_method  TEXT NOT NULL DEFAULT 'card',
    gross_amount    REAL NOT NULL DEFAULT 0,
    discount_amount REAL NOT NULL DEFAULT 0,
    net_amount      REAL NOT NULL DEFAULT 0,
    sold_at         TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (store_id) REFERENCES stores(id)
);

CREATE TABLE sales_items (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    sales_receipt_id INTEGER NOT NULL,
    product_id      INTEGER NOT NULL,
    quantity        INTEGER NOT NULL DEFAULT 1,
    unit_price      REAL NOT NULL DEFAULT 0,
    line_total      REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (sales_receipt_id) REFERENCES sales_receipts(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE INDEX idx_products_category        ON products(category_id);
CREATE INDEX idx_products_stock           ON products(current_stock);
CREATE INDEX idx_purchase_orders_ordered  ON purchase_orders(ordered_at);
CREATE INDEX idx_stock_movements_moved_at ON stock_movements(moved_at);
CREATE INDEX idx_sales_receipts_sold_at   ON sales_receipts(sold_at);

-- ─── Seed data ────────────────────────────────────────────────────────────────
INSERT INTO categories (name, slug, description) VALUES
    ('Beverage', 'beverage', 'Nước giải khát và đồ uống đóng chai'),
    ('Household', 'household', 'Đồ gia dụng và tiêu dùng nhanh'),
    ('Personal Care', 'personal-care', 'Sản phẩm chăm sóc cá nhân'),
    ('Snack', 'snack', 'Đồ ăn nhanh, bánh kẹo');

INSERT INTO stores (code, name, city, opened_at) VALUES
    ('HN-01', 'Retail Hà Nội Center', 'Hà Nội', datetime('now', '-36 months')),
    ('HCM-01', 'Retail Saigon Hub', 'TP. Hồ Chí Minh', datetime('now', '-30 months')),
    ('DN-01', 'Retail Đà Nẵng', 'Đà Nẵng', datetime('now', '-24 months'));

INSERT INTO suppliers (name, contact_email, phone, city, rating) VALUES
    ('Fresh Drink Co.', 'ops@freshdrink.vn', '0909000001', 'Hà Nội', 4.8),
    ('Home Plus JSC', 'sale@homeplus.vn', '0909000002', 'TP. Hồ Chí Minh', 4.6),
    ('CareViet Ltd.', 'contact@careviet.vn', '0909000003', 'Đà Nẵng', 4.7),
    ('Snacky Foods', 'biz@snacky.vn', '0909000004', 'Bình Dương', 4.5);

INSERT INTO products (category_id, supplier_id, sku, name, unit, cost_price, sale_price, reorder_level, current_stock) VALUES
    (1, 1, 'BEV-0001', 'Nước suối 500ml', 'bottle', 3500, 7000, 120, 450),
    (1, 1, 'BEV-0002', 'Trà xanh 330ml', 'can', 4500, 9500, 100, 390),
    (2, 2, 'HOU-0001', 'Nước rửa chén 750ml', 'bottle', 18000, 32000, 60, 140),
    (2, 2, 'HOU-0002', 'Khăn giấy hộp', 'box', 12000, 22000, 80, 175),
    (3, 3, 'PER-0001', 'Dầu gội 650g', 'bottle', 52000, 89000, 40, 85),
    (3, 3, 'PER-0002', 'Sữa tắm 900g', 'bottle', 61000, 102000, 35, 66),
    (4, 4, 'SNK-0001', 'Khoai tây lát 95g', 'pack', 12000, 22000, 100, 72),
    (4, 4, 'SNK-0002', 'Bánh quy bơ 200g', 'box', 18000, 34000, 70, 58);

INSERT INTO purchase_orders (supplier_id, store_id, po_number, status, total_amount, ordered_at, received_at) VALUES
    (1, 1, 'PO-2026-0001', 'received', 14500000, datetime('now', '-42 days'), datetime('now', '-39 days')),
    (2, 2, 'PO-2026-0002', 'received', 9200000,  datetime('now', '-35 days'), datetime('now', '-31 days')),
    (3, 3, 'PO-2026-0003', 'received', 7800000,  datetime('now', '-28 days'), datetime('now', '-24 days')),
    (4, 1, 'PO-2026-0004', 'partial',  5600000,  datetime('now', '-10 days'), NULL);

INSERT INTO purchase_order_items (purchase_order_id, product_id, quantity, unit_cost, line_total) VALUES
    (1, 1, 1200, 3500, 4200000),
    (1, 2, 900, 4500, 4050000),
    (2, 3, 260, 18000, 4680000),
    (2, 4, 210, 12000, 2520000),
    (3, 5, 90, 52000, 4680000),
    (3, 6, 60, 61000, 3660000),
    (4, 7, 220, 12000, 2640000),
    (4, 8, 120, 18000, 2160000);

INSERT INTO sales_receipts (store_id, receipt_number, status, payment_method, gross_amount, discount_amount, net_amount, sold_at) VALUES
    (1, 'RC-2026-0001', 'completed', 'cash',  840000, 20000, 820000, datetime('now', '-7 days')),
    (2, 'RC-2026-0002', 'completed', 'card', 1260000, 40000, 1220000, datetime('now', '-6 days')),
    (1, 'RC-2026-0003', 'completed', 'qr',   950000, 10000, 940000, datetime('now', '-5 days')),
    (3, 'RC-2026-0004', 'completed', 'card', 720000,  0,     720000, datetime('now', '-4 days')),
    (2, 'RC-2026-0005', 'completed', 'cash', 1050000, 30000, 1020000, datetime('now', '-3 days')),
    (1, 'RC-2026-0006', 'completed', 'qr',   1320000, 50000, 1270000, datetime('now', '-2 days')),
    (3, 'RC-2026-0007', 'completed', 'card', 680000,  0,     680000, datetime('now', '-1 day')),
    (1, 'RC-2026-0008', 'completed', 'card', 410000,  10000, 400000, datetime('now'));

INSERT INTO sales_items (sales_receipt_id, product_id, quantity, unit_price, line_total) VALUES
    (1, 1, 30, 7000, 210000), (1, 2, 24, 9500, 228000), (1, 7, 12, 22000, 264000),
    (2, 3, 18, 32000, 576000), (2, 4, 18, 22000, 396000), (2, 8, 8, 34000, 272000),
    (3, 1, 40, 7000, 280000), (3, 5, 5, 89000, 445000), (3, 7, 10, 22000, 220000),
    (4, 6, 4, 102000, 408000), (4, 8, 8, 34000, 272000),
    (5, 2, 28, 9500, 266000), (5, 3, 16, 32000, 512000), (5, 4, 12, 22000, 264000),
    (6, 5, 7, 89000, 623000), (6, 6, 4, 102000, 408000), (6, 7, 13, 22000, 286000),
    (7, 1, 20, 7000, 140000), (7, 8, 10, 34000, 340000), (7, 2, 21, 9500, 199500),
    (8, 1, 12, 7000, 84000), (8, 4, 8, 22000, 176000), (8, 7, 6, 22000, 132000);

INSERT INTO stock_movements (store_id, product_id, movement_type, quantity, ref_type, ref_id, note, moved_at) VALUES
    (1, 1, 'in', 1200, 'po', 1, 'Nhập hàng đợt 1', datetime('now', '-39 days')),
    (1, 2, 'in', 900, 'po', 1, 'Nhập hàng đợt 1', datetime('now', '-39 days')),
    (2, 3, 'in', 260, 'po', 2, 'Nhập hàng đợt 2', datetime('now', '-31 days')),
    (2, 4, 'in', 210, 'po', 2, 'Nhập hàng đợt 2', datetime('now', '-31 days')),
    (3, 5, 'in', 90, 'po', 3, 'Nhập hàng đợt 3', datetime('now', '-24 days')),
    (3, 6, 'in', 60, 'po', 3, 'Nhập hàng đợt 3', datetime('now', '-24 days')),
    (1, 7, 'in', 220, 'po', 4, 'Nhập hàng snack', datetime('now', '-9 days')),
    (1, 8, 'in', 120, 'po', 4, 'Nhập hàng snack', datetime('now', '-9 days')),
    (1, 1, 'out', 82, 'sale', 0, 'Xuất bán tuần gần nhất', datetime('now', '-1 day')),
    (2, 3, 'out', 34, 'sale', 0, 'Xuất bán tuần gần nhất', datetime('now', '-1 day')),
    (3, 6, 'out', 8, 'sale', 0, 'Xuất bán tuần gần nhất', datetime('now', '-1 day')),
    (1, 8, 'adjustment', -2, 'audit', 0, 'Hao hụt kiểm kê', datetime('now', '-12 hours'));

PRAGMA foreign_keys=ON;
