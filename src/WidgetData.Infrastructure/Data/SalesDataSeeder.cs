using Microsoft.Data.Sqlite;

namespace WidgetData.Infrastructure.Data;

/// <summary>
/// Generates a large SQLite sales database to simulate a store payment system.
/// Tables: categories, products, customers, employees, orders, order_items, payments
/// </summary>
public static class SalesDataSeeder
{
    private static readonly string[] FirstNames = [
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng",
        "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đinh", "Tô", "Mai", "Cao"
    ];
    private static readonly string[] MiddleNames = [
        "Văn", "Thị", "Hữu", "Quốc", "Minh", "Thanh", "Tuấn", "Anh", "Thu", "Hoài"
    ];
    private static readonly string[] LastNames = [
        "An", "Bình", "Cường", "Dũng", "Đức", "Giang", "Hoa", "Hùng", "Lan", "Long",
        "Mai", "Nam", "Ngọc", "Phúc", "Quân", "Sơn", "Thành", "Tùng", "Việt", "Xuân"
    ];
    private static readonly string[] Cities = [
        "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Cần Thơ", "Hải Phòng",
        "Biên Hòa", "Huế", "Nha Trang", "Vũng Tàu", "Quy Nhơn"
    ];

    public static void EnsureSalesDatabase(string dbPath)
    {
        if (File.Exists(dbPath)) return;

        var connectionString = $"Data Source={dbPath}";
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        CreateSchema(conn);
        SeedCategories(conn);
        SeedProducts(conn);
        SeedCustomers(conn);
        SeedEmployees(conn);
        SeedOrders(conn);
        SeedPayments(conn);
    }

    private static void CreateSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
PRAGMA journal_mode=WAL;

CREATE TABLE IF NOT EXISTS categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT,
    created_at TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    sku TEXT NOT NULL UNIQUE,
    description TEXT,
    unit_price REAL NOT NULL,
    cost_price REAL NOT NULL,
    stock_quantity INTEGER NOT NULL DEFAULT 0,
    unit TEXT NOT NULL DEFAULT 'cái',
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

CREATE TABLE IF NOT EXISTS customers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name TEXT NOT NULL,
    phone TEXT,
    email TEXT,
    address TEXT,
    city TEXT,
    loyalty_points INTEGER NOT NULL DEFAULT 0,
    total_spent REAL NOT NULL DEFAULT 0,
    created_at TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS employees (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name TEXT NOT NULL,
    position TEXT NOT NULL,
    branch TEXT NOT NULL,
    created_at TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    order_code TEXT NOT NULL UNIQUE,
    customer_id INTEGER,
    employee_id INTEGER NOT NULL,
    status TEXT NOT NULL DEFAULT 'completed',
    subtotal REAL NOT NULL,
    discount_amount REAL NOT NULL DEFAULT 0,
    tax_amount REAL NOT NULL DEFAULT 0,
    total_amount REAL NOT NULL,
    note TEXT,
    created_at TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (customer_id) REFERENCES customers(id),
    FOREIGN KEY (employee_id) REFERENCES employees(id)
);

CREATE TABLE IF NOT EXISTS order_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    unit_price REAL NOT NULL,
    discount_percent REAL NOT NULL DEFAULT 0,
    line_total REAL NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS payments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id INTEGER NOT NULL,
    payment_method TEXT NOT NULL,
    amount REAL NOT NULL,
    status TEXT NOT NULL DEFAULT 'success',
    transaction_ref TEXT,
    paid_at TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (order_id) REFERENCES orders(id)
);

CREATE INDEX IF NOT EXISTS idx_orders_created_at ON orders(created_at);
CREATE INDEX IF NOT EXISTS idx_orders_customer_id ON orders(customer_id);
CREATE INDEX IF NOT EXISTS idx_order_items_order_id ON order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_order_items_product_id ON order_items(product_id);
CREATE INDEX IF NOT EXISTS idx_payments_order_id ON payments(order_id);
CREATE INDEX IF NOT EXISTS idx_payments_paid_at ON payments(paid_at);
";
        cmd.ExecuteNonQuery();
    }

    private static void SeedCategories(SqliteConnection conn)
    {
        string[] categories = [
            "Điện thoại & Phụ kiện",
            "Máy tính & Laptop",
            "Thiết bị âm thanh",
            "Đồng hồ & Trang sức",
            "Thời trang nam",
            "Thời trang nữ",
            "Thực phẩm & Đồ uống",
            "Chăm sóc sức khỏe",
            "Đồ gia dụng",
            "Sách & Văn phòng phẩm"
        ];

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO categories (name, description) VALUES (@name, @desc)";
        var nameParam = cmd.Parameters.Add("@name", SqliteType.Text);
        var descParam = cmd.Parameters.Add("@desc", SqliteType.Text);

        foreach (var cat in categories)
        {
            nameParam.Value = cat;
            descParam.Value = $"Danh mục {cat}";
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedProducts(SqliteConnection conn)
    {
        var rand = new Random(42);
        var products = new List<(int catId, string name, string sku, double price, double cost, int stock, string unit)>
        {
            // Điện thoại & Phụ kiện (catId=1)
            (1, "iPhone 15 Pro Max 256GB", "IPH-15PM-256", 28990000, 22000000, 50, "máy"),
            (1, "Samsung Galaxy S24 Ultra", "SAM-S24U", 25990000, 19000000, 45, "máy"),
            (1, "Xiaomi 14 Ultra", "XIA-14U", 18990000, 14000000, 60, "máy"),
            (1, "OPPO Find X7 Pro", "OPP-FX7P", 16990000, 12500000, 40, "máy"),
            (1, "Ốp lưng iPhone 15", "ACC-CASE-I15", 199000, 80000, 500, "cái"),
            (1, "Cáp sạc USB-C 1m", "ACC-CABLE-C1", 149000, 50000, 1000, "sợi"),
            (1, "Sạc nhanh 65W", "ACC-CHG-65W", 399000, 150000, 300, "cái"),
            (1, "Kính cường lực iPhone 15", "ACC-GLASS-I15", 99000, 30000, 800, "tấm"),
            (1, "Tai nghe không dây AirPods Pro", "APP-AIRP-PRO", 5490000, 3800000, 80, "hộp"),
            (1, "Pin dự phòng 20000mAh", "ACC-PB-20K", 599000, 280000, 200, "cái"),
            // Máy tính & Laptop (catId=2)
            (2, "MacBook Air M3 8GB 256GB", "MAC-AIR-M3", 28490000, 21000000, 30, "máy"),
            (2, "Dell XPS 15 i7-13th Gen", "DEL-XPS15", 35990000, 27000000, 20, "máy"),
            (2, "Laptop Asus VivoBook 15", "ASU-VB15", 14990000, 11000000, 40, "máy"),
            (2, "Laptop HP Pavilion 15", "HP-PAV15", 13490000, 10000000, 35, "máy"),
            (2, "iPad Pro M4 11inch WiFi", "APP-IPAD-P4", 22990000, 17000000, 25, "máy"),
            (2, "Bàn phím cơ Keychron K2", "KEY-K2-BT", 1890000, 1100000, 100, "cái"),
            (2, "Chuột Logitech MX Master 3S", "LOG-MXM3S", 1990000, 1300000, 80, "cái"),
            (2, "Màn hình LG 27inch 4K", "LG-27UK", 7990000, 5500000, 20, "cái"),
            (2, "SSD Samsung 1TB NVMe", "SAM-SSD-1T", 1990000, 1400000, 150, "cái"),
            (2, "RAM Kingston 16GB DDR5", "KIN-RAM-16D5", 1590000, 1100000, 120, "thanh"),
            // Thiết bị âm thanh (catId=3)
            (3, "Loa Bluetooth JBL Charge 5", "JBL-CHG5", 3490000, 2300000, 60, "cái"),
            (3, "Tai nghe Sony WH-1000XM5", "SON-WH1000XM5", 7490000, 5200000, 40, "cái"),
            (3, "Tai nghe Bose QuietComfort 45", "BOS-QC45", 7990000, 5500000, 30, "cái"),
            (3, "Loa soundbar Samsung HW-Q600C", "SAM-HWQ600", 5990000, 4200000, 15, "cái"),
            (3, "Micro thu âm Blue Yeti", "BLU-YETI", 3190000, 2200000, 25, "cái"),
            // Đồng hồ & Trang sức (catId=4)
            (4, "Đồng hồ Apple Watch Series 9 GPS", "APP-WS9-GPS", 9990000, 7000000, 35, "cái"),
            (4, "Đồng hồ Samsung Galaxy Watch 6", "SAM-GW6", 5490000, 3800000, 40, "cái"),
            (4, "Đồng hồ cơ Citizen BM8470", "CIT-BM8470", 3490000, 2200000, 20, "cái"),
            (4, "Vòng tay Garmin Vivosmart 5", "GAR-VS5", 2990000, 1900000, 30, "cái"),
            (4, "Đồng hồ Casio G-Shock GA-2100", "CAS-GA2100", 2290000, 1500000, 50, "cái"),
            // Thời trang nam (catId=5)
            (5, "Áo polo nam basic", "CLO-POLO-M", 249000, 120000, 300, "cái"),
            (5, "Quần jean nam slim fit", "CLO-JEAN-M", 399000, 180000, 250, "cái"),
            (5, "Áo khoác nam bomber", "CLO-BOM-M", 599000, 280000, 150, "cái"),
            (5, "Giày thể thao nam Nike Air Max", "SHO-NAM-AIR", 2490000, 1700000, 80, "đôi"),
            (5, "Túi xách nam da", "BAG-NAM-LTH", 1290000, 750000, 60, "cái"),
            // Thời trang nữ (catId=6)
            (6, "Váy maxi floral", "CLO-MAXI-F", 449000, 200000, 200, "cái"),
            (6, "Áo blouse nữ cổ V", "CLO-BLOUSE-V", 299000, 140000, 300, "cái"),
            (6, "Quần culottes nữ", "CLO-CUL-F", 349000, 160000, 250, "cái"),
            (6, "Giày cao gót nữ 7cm", "SHO-NUF-7", 890000, 500000, 100, "đôi"),
            (6, "Túi xách nữ da PU", "BAG-NUF-PU", 690000, 380000, 120, "cái"),
            // Thực phẩm & Đồ uống (catId=7)
            (7, "Cà phê rang xay Highlands 500g", "FOOD-CF-HG500", 119000, 60000, 500, "gói"),
            (7, "Trà xanh Lipton hộp 50 túi", "FOOD-TEA-LIP50", 79000, 35000, 800, "hộp"),
            (7, "Chocolate Ferrero Rocher 24 viên", "FOOD-CHOC-FER24", 249000, 140000, 300, "hộp"),
            (7, "Nước ép cam nguyên chất 1L", "FOOD-JUICE-1L", 49000, 22000, 1000, "chai"),
            (7, "Bánh quy Oreo 432g", "FOOD-OREO-432", 89000, 45000, 600, "gói"),
            // Chăm sóc sức khỏe (catId=8)
            (8, "Vitamin C 1000mg 60 viên", "HLTH-VITC-60", 199000, 100000, 400, "hộp"),
            (8, "Máy đo huyết áp Omron HEM-7361T", "HLTH-BP-OM7361", 1890000, 1300000, 30, "máy"),
            (8, "Khẩu trang y tế 3M N95", "HLTH-MASK-3MN95", 89000, 45000, 2000, "hộp"),
            (8, "Nước muối sinh lý 0.9% 500ml", "HLTH-SALT-500", 35000, 15000, 1500, "chai"),
            (8, "Gel rửa tay sát khuẩn 500ml", "HLTH-HAND-500", 65000, 30000, 1200, "chai"),
            // Đồ gia dụng (catId=9)
            (9, "Nồi chiên không dầu Philips 4.1L", "HOME-AF-PHI41", 2490000, 1700000, 40, "cái"),
            (9, "Máy xay sinh tố Sunhouse SHB5120", "HOME-BLD-SH", 890000, 580000, 60, "cái"),
            (9, "Bộ nồi inox 5 chiếc", "HOME-POT-5PC", 1190000, 750000, 50, "bộ"),
            (9, "Chảo chống dính Tefal 28cm", "HOME-PAN-TF28", 590000, 380000, 80, "cái"),
            (9, "Máy lọc không khí Xiaomi 4 Pro", "HOME-AIR-XI4P", 2990000, 2100000, 25, "máy"),
            // Sách & Văn phòng phẩm (catId=10)
            (10, "Sách Đắc Nhân Tâm", "BOOK-DNT", 89000, 40000, 500, "quyển"),
            (10, "Sách Nhà Giả Kim", "BOOK-NGK", 79000, 35000, 500, "quyển"),
            (10, "Sổ tay Leuchtturm1917 A5", "STN-LEUCH-A5", 299000, 180000, 200, "quyển"),
            (10, "Bút Parker IM Black", "PEN-PAR-IMB", 690000, 450000, 100, "cái"),
            (10, "Máy tính bỏ túi Casio FX-580", "CALC-CAS-FX580", 489000, 320000, 80, "cái"),
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO products (category_id, name, sku, description, unit_price, cost_price, stock_quantity, unit, is_active)
                            VALUES (@cat, @name, @sku, @desc, @price, @cost, @stock, @unit, 1)";
        var catParam = cmd.Parameters.Add("@cat", SqliteType.Integer);
        var nameParam = cmd.Parameters.Add("@name", SqliteType.Text);
        var skuParam = cmd.Parameters.Add("@sku", SqliteType.Text);
        var descParam = cmd.Parameters.Add("@desc", SqliteType.Text);
        var priceParam = cmd.Parameters.Add("@price", SqliteType.Real);
        var costParam = cmd.Parameters.Add("@cost", SqliteType.Real);
        var stockParam = cmd.Parameters.Add("@stock", SqliteType.Integer);
        var unitParam = cmd.Parameters.Add("@unit", SqliteType.Text);

        foreach (var (catId, name, sku, price, cost, stock, unit) in products)
        {
            catParam.Value = catId;
            nameParam.Value = name;
            skuParam.Value = sku;
            descParam.Value = $"Sản phẩm {name}";
            priceParam.Value = price;
            costParam.Value = cost;
            stockParam.Value = stock;
            unitParam.Value = unit;
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedCustomers(SqliteConnection conn)
    {
        var rand = new Random(42);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO customers (full_name, phone, email, address, city, loyalty_points, total_spent)
                            VALUES (@name, @phone, @email, @addr, @city, @points, @spent)";
        var nameParam = cmd.Parameters.Add("@name", SqliteType.Text);
        var phoneParam = cmd.Parameters.Add("@phone", SqliteType.Text);
        var emailParam = cmd.Parameters.Add("@email", SqliteType.Text);
        var addrParam = cmd.Parameters.Add("@addr", SqliteType.Text);
        var cityParam = cmd.Parameters.Add("@city", SqliteType.Text);
        var pointsParam = cmd.Parameters.Add("@points", SqliteType.Integer);
        var spentParam = cmd.Parameters.Add("@spent", SqliteType.Real);

        for (int i = 1; i <= 500; i++)
        {
            var firstName = FirstNames[rand.Next(FirstNames.Length)];
            var middleName = MiddleNames[rand.Next(MiddleNames.Length)];
            var lastName = LastNames[rand.Next(LastNames.Length)];
            var fullName = $"{firstName} {middleName} {lastName}";
            var city = Cities[rand.Next(Cities.Length)];

            nameParam.Value = fullName;
            phoneParam.Value = $"0{rand.Next(300000000, 999999999)}";
            emailParam.Value = $"customer{i}@example.com";
            addrParam.Value = $"Số {rand.Next(1, 200)}, Đường {rand.Next(1, 50)}, Quận {rand.Next(1, 12)}";
            cityParam.Value = city;
            pointsParam.Value = rand.Next(0, 5000);
            spentParam.Value = 0;
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedEmployees(SqliteConnection conn)
    {
        var employees = new (string name, string position, string branch)[]
        {
            ("Nguyễn Văn Hùng", "Trưởng cửa hàng", "Chi nhánh Hà Nội"),
            ("Trần Thị Mai", "Nhân viên bán hàng", "Chi nhánh Hà Nội"),
            ("Lê Văn Đức", "Nhân viên bán hàng", "Chi nhánh Hà Nội"),
            ("Phạm Thị Lan", "Thu ngân", "Chi nhánh Hà Nội"),
            ("Hoàng Văn Nam", "Nhân viên bán hàng", "Chi nhánh Hà Nội"),
            ("Vũ Thị Hoa", "Trưởng cửa hàng", "Chi nhánh TP.HCM"),
            ("Đặng Văn Tùng", "Nhân viên bán hàng", "Chi nhánh TP.HCM"),
            ("Bùi Thị Ngọc", "Nhân viên bán hàng", "Chi nhánh TP.HCM"),
            ("Đỗ Văn Minh", "Thu ngân", "Chi nhánh TP.HCM"),
            ("Ngô Thị Phương", "Nhân viên bán hàng", "Chi nhánh TP.HCM"),
            ("Dương Văn Quân", "Trưởng cửa hàng", "Chi nhánh Đà Nẵng"),
            ("Lý Thị Thu", "Nhân viên bán hàng", "Chi nhánh Đà Nẵng"),
            ("Đinh Văn Bình", "Nhân viên bán hàng", "Chi nhánh Đà Nẵng"),
            ("Tô Thị Thảo", "Thu ngân", "Chi nhánh Đà Nẵng"),
            ("Mai Văn Sơn", "Nhân viên bán hàng", "Chi nhánh Đà Nẵng"),
            ("Cao Thị Linh", "Trưởng cửa hàng", "Chi nhánh Cần Thơ"),
            ("Nguyễn Hữu Phúc", "Nhân viên bán hàng", "Chi nhánh Cần Thơ"),
            ("Trần Quốc Việt", "Nhân viên bán hàng", "Chi nhánh Cần Thơ"),
            ("Lê Thị Giang", "Thu ngân", "Chi nhánh Cần Thơ"),
            ("Phạm Minh Thành", "Nhân viên bán hàng", "Chi nhánh Cần Thơ"),
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO employees (full_name, position, branch) VALUES (@name, @pos, @branch)";
        var nameParam = cmd.Parameters.Add("@name", SqliteType.Text);
        var posParam = cmd.Parameters.Add("@pos", SqliteType.Text);
        var branchParam = cmd.Parameters.Add("@branch", SqliteType.Text);

        foreach (var (name, position, branch) in employees)
        {
            nameParam.Value = name;
            posParam.Value = position;
            branchParam.Value = branch;
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedOrders(SqliteConnection conn)
    {
        var rand = new Random(42);
        var baseDate = DateTime.UtcNow.AddDays(-365);

        // Get product info
        var products = new List<(int id, double price, int catId)>();
        using (var readCmd = conn.CreateCommand())
        {
            readCmd.CommandText = "SELECT id, unit_price, category_id FROM products WHERE is_active = 1";
            using var reader = readCmd.ExecuteReader();
            while (reader.Read())
                products.Add((reader.GetInt32(0), reader.GetDouble(1), reader.GetInt32(2)));
        }

        string[] paymentMethods = ["Tiền mặt", "Thẻ tín dụng", "Chuyển khoản", "Ví điện tử MoMo", "Ví ZaloPay", "QR Code"];
        string[] orderStatuses = ["completed", "completed", "completed", "completed", "cancelled", "refunded"];

        using var orderCmd = conn.CreateCommand();
        orderCmd.CommandText = @"INSERT INTO orders (order_code, customer_id, employee_id, status, subtotal, discount_amount, tax_amount, total_amount, created_at)
                                  VALUES (@code, @cust, @emp, @status, @sub, @disc, @tax, @total, @date)";
        var codeParam = orderCmd.Parameters.Add("@code", SqliteType.Text);
        var custParam = orderCmd.Parameters.Add("@cust", SqliteType.Integer);
        var empParam = orderCmd.Parameters.Add("@emp", SqliteType.Integer);
        var statusParam = orderCmd.Parameters.Add("@status", SqliteType.Text);
        var subParam = orderCmd.Parameters.Add("@sub", SqliteType.Real);
        var discParam = orderCmd.Parameters.Add("@disc", SqliteType.Real);
        var taxParam = orderCmd.Parameters.Add("@tax", SqliteType.Real);
        var totalParam = orderCmd.Parameters.Add("@total", SqliteType.Real);
        var dateParam = orderCmd.Parameters.Add("@date", SqliteType.Text);

        using var itemCmd = conn.CreateCommand();
        itemCmd.CommandText = @"INSERT INTO order_items (order_id, product_id, quantity, unit_price, discount_percent, line_total)
                                 VALUES (@oid, @pid, @qty, @price, @disc, @total)";
        var iOidParam = itemCmd.Parameters.Add("@oid", SqliteType.Integer);
        var iPidParam = itemCmd.Parameters.Add("@pid", SqliteType.Integer);
        var iQtyParam = itemCmd.Parameters.Add("@qty", SqliteType.Integer);
        var iPriceParam = itemCmd.Parameters.Add("@price", SqliteType.Real);
        var iDiscParam = itemCmd.Parameters.Add("@disc", SqliteType.Real);
        var iTotalParam = itemCmd.Parameters.Add("@total", SqliteType.Real);

        using var payCmd = conn.CreateCommand();
        payCmd.CommandText = @"INSERT INTO payments (order_id, payment_method, amount, status, transaction_ref, paid_at)
                                VALUES (@oid, @method, @amount, @status, @ref, @paidat)";
        var pOidParam = payCmd.Parameters.Add("@oid", SqliteType.Integer);
        var pMethodParam = payCmd.Parameters.Add("@method", SqliteType.Text);
        var pAmountParam = payCmd.Parameters.Add("@amount", SqliteType.Real);
        var pStatusParam = payCmd.Parameters.Add("@status", SqliteType.Text);
        var pRefParam = payCmd.Parameters.Add("@ref", SqliteType.Text);
        var pPaidAtParam = payCmd.Parameters.Add("@paidat", SqliteType.Text);

        using var updateCustomerCmd = conn.CreateCommand();
        updateCustomerCmd.CommandText = "UPDATE customers SET total_spent = total_spent + @amt, loyalty_points = loyalty_points + @pts WHERE id = @id";
        var ucAmtParam = updateCustomerCmd.Parameters.Add("@amt", SqliteType.Real);
        var ucPtsParam = updateCustomerCmd.Parameters.Add("@pts", SqliteType.Integer);
        var ucIdParam = updateCustomerCmd.Parameters.Add("@id", SqliteType.Integer);

        using var getLastIdCmd = conn.CreateCommand();
        getLastIdCmd.CommandText = "SELECT last_insert_rowid()";

        for (int i = 1; i <= 2500; i++)
        {
            var orderDate = baseDate.AddDays(rand.NextDouble() * 365).AddHours(rand.Next(8, 21));
            var customerId = rand.Next(0, 10) < 2 ? (int?)null : rand.Next(1, 501); // 20% guest
            var employeeId = rand.Next(1, 21);
            var status = orderStatuses[rand.Next(orderStatuses.Length)];

            // Build order items
            int itemCount = rand.Next(1, 6);
            var selectedProducts = new List<int>();
            for (int j = 0; j < itemCount; j++)
            {
                int pidx = rand.Next(products.Count);
                if (!selectedProducts.Contains(products[pidx].id))
                    selectedProducts.Add(products[pidx].id);
            }

            double subtotal = 0;
            var itemsData = new List<(int pid, int qty, double price, double discPct, double lineTotal)>();
            foreach (var pid in selectedProducts)
            {
                var product = products.First(p => p.id == pid);
                int qty = rand.Next(1, 4);
                double discPct = rand.Next(0, 5) == 0 ? rand.Next(5, 21) : 0;
                double lineTotal = Math.Round(product.price * qty * (1 - discPct / 100), 0);
                subtotal += lineTotal;
                itemsData.Add((pid, qty, product.price, discPct, lineTotal));
            }

            double discountAmount = status == "completed" && rand.Next(0, 8) == 0 ? Math.Round(subtotal * 0.05, 0) : 0;
            double taxAmount = Math.Round((subtotal - discountAmount) * 0.1, 0);
            double totalAmount = subtotal - discountAmount + taxAmount;

            var orderDateStr = orderDate.ToString("yyyy-MM-dd HH:mm:ss");
            codeParam.Value = $"ORD{i:D6}";
            custParam.Value = customerId.HasValue ? (object)customerId.Value : DBNull.Value;
            empParam.Value = employeeId;
            statusParam.Value = status;
            subParam.Value = subtotal;
            discParam.Value = discountAmount;
            taxParam.Value = taxAmount;
            totalParam.Value = totalAmount;
            dateParam.Value = orderDateStr;
            orderCmd.ExecuteNonQuery();

            var orderId = (long)getLastIdCmd.ExecuteScalar()!;

            foreach (var (pid, qty, price, discPct, lineTotal) in itemsData)
            {
                iOidParam.Value = orderId;
                iPidParam.Value = pid;
                iQtyParam.Value = qty;
                iPriceParam.Value = price;
                iDiscParam.Value = discPct;
                iTotalParam.Value = lineTotal;
                itemCmd.ExecuteNonQuery();
            }

            // Payment (skip for cancelled)
            if (status != "cancelled")
            {
                string payMethod = paymentMethods[rand.Next(paymentMethods.Length)];
                string payStatus = status == "refunded" ? "refunded" : "success";

                pOidParam.Value = orderId;
                pMethodParam.Value = payMethod;
                pAmountParam.Value = totalAmount;
                pStatusParam.Value = payStatus;
                pRefParam.Value = $"TXN{rand.Next(10000000, 99999999)}";
                pPaidAtParam.Value = orderDate.AddMinutes(rand.Next(1, 15)).ToString("yyyy-MM-dd HH:mm:ss");
                payCmd.ExecuteNonQuery();
            }

            // Update customer total_spent
            if (customerId.HasValue && status == "completed")
            {
                ucAmtParam.Value = totalAmount;
                ucPtsParam.Value = (int)(totalAmount / 10000);
                ucIdParam.Value = customerId.Value;
                updateCustomerCmd.ExecuteNonQuery();
            }
        }
    }

    private static void SeedPayments(SqliteConnection conn)
    {
        // Already seeded as part of SeedOrders
    }
}
