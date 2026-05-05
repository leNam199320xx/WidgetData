using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Data;

public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DataSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        string[] roles = { "Admin", "Manager", "Developer", "Viewer", "SuperAdmin", "TenantAdmin", "TenantUser" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        // Ensure demo tenant exists
        Tenant demoTenant;
        var existingDemo = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == "demo");
        if (existingDemo == null)
        {
            demoTenant = new Tenant
            {
                Name = "Demo Tenant",
                Slug = "demo",
                IsActive = true,
                Plan = "free",
                ContactEmail = "demo@widgetdata.com",
                CreatedAt = DateTime.UtcNow
            };
            _context.Tenants.Add(demoTenant);
            await _context.SaveChangesAsync();
        }
        else
        {
            demoTenant = existingDemo;
        }

        if (!await _userManager.Users.AnyAsync())
        {
            var superAdmin = new ApplicationUser
            {
                UserName = "superadmin@widgetdata.com",
                Email = "superadmin@widgetdata.com",
                DisplayName = "Super Admin",
                EmailConfirmed = true,
                IsActive = true,
                TenantId = null  // SuperAdmin không thuộc tenant nào
            };
            var saResult = await _userManager.CreateAsync(superAdmin, "SuperAdmin@123!");
            if (saResult.Succeeded)
                await _userManager.AddToRoleAsync(superAdmin, "SuperAdmin");

            var admin = new ApplicationUser
            {
                UserName = "admin@widgetdata.com",
                Email = "admin@widgetdata.com",
                DisplayName = "Admin User",
                EmailConfirmed = true,
                IsActive = true
            };
            var adminResult = await _userManager.CreateAsync(admin, "Admin@123!");
            if (adminResult.Succeeded)
                await _userManager.AddToRoleAsync(admin, "Admin");

            var manager = new ApplicationUser
            {
                UserName = "manager@widgetdata.com",
                Email = "manager@widgetdata.com",
                DisplayName = "Manager User",
                EmailConfirmed = true,
                IsActive = true
            };
            var mgResult = await _userManager.CreateAsync(manager, "Manager@123!");
            if (mgResult.Succeeded)
                await _userManager.AddToRoleAsync(manager, "Manager");

            var dev = new ApplicationUser
            {
                UserName = "dev@widgetdata.com",
                Email = "dev@widgetdata.com",
                DisplayName = "Developer User",
                EmailConfirmed = true,
                IsActive = true
            };
            var devResult = await _userManager.CreateAsync(dev, "Developer@123!");
            if (devResult.Succeeded)
                await _userManager.AddToRoleAsync(dev, "Developer");
        }

        // Ensure SQLite demo databases exist
        var salesDbPath  = Path.Combine(AppContext.BaseDirectory, "sales.db");
        var courseDbPath = Path.Combine(AppContext.BaseDirectory, "course.db");
        var newsDbPath   = Path.Combine(AppContext.BaseDirectory, "news.db");
        SalesDataSeeder.EnsureSalesDatabase(salesDbPath);
        CourseDataSeeder.EnsureCourseDatabase(courseDbPath);
        NewsDataSeeder.EnsureNewsDatabase(newsDbPath);

        if (!await _context.DataSources.AnyAsync())
        {
            var dsSales = new DataSource
            {
                Name = "Cửa hàng - Sales DB",
                SourceType = DataSourceType.SQLite,
                Description = "Cơ sở dữ liệu SQLite chứa dữ liệu bán hàng, khách hàng, sản phẩm và thanh toán",
                ConnectionString = $"Data Source={salesDbPath}",
                IsActive = true,
                CreatedBy = "system",
                LastTestedAt = DateTime.UtcNow,
                LastTestResult = "Connection successful",
                TenantId = demoTenant.Id
            };
            var dsApi = new DataSource
            {
                Name = "Analytics REST API",
                SourceType = DataSourceType.RestApi,
                Description = "REST API endpoint for analytics data",
                ApiEndpoint = "https://api.example.com/analytics",
                ApiKey = "demo-api-key-12345",
                IsActive = true,
                CreatedBy = "system",
                LastTestedAt = DateTime.UtcNow.AddHours(-1),
                LastTestResult = "Connection successful",
                TenantId = demoTenant.Id
            };
            var dsCourse = new DataSource
            {
                Name = "EduViet - Course DB",
                SourceType = DataSourceType.SQLite,
                Description = "Cơ sở dữ liệu SQLite cho nền tảng học trực tuyến EduViet: khóa học, học viên, đăng ký, thanh toán",
                ConnectionString = $"Data Source={courseDbPath}",
                IsActive = true,
                CreatedBy = "system",
                LastTestedAt = DateTime.UtcNow,
                LastTestResult = "Connection successful",
                TenantId = demoTenant.Id
            };
            var dsNews = new DataSource
            {
                Name = "VietNews - News DB",
                SourceType = DataSourceType.SQLite,
                Description = "Cơ sở dữ liệu SQLite cho cổng tin tức VietNews: bài viết, độc giả, lượt xem, bình luận",
                ConnectionString = $"Data Source={newsDbPath}",
                IsActive = true,
                CreatedBy = "system",
                LastTestedAt = DateTime.UtcNow,
                LastTestResult = "Connection successful",
                TenantId = demoTenant.Id
            };
            _context.DataSources.AddRange(dsSales, dsApi, dsCourse, dsNews);
            await _context.SaveChangesAsync();

            // --- Widget Groups (Report Pages) ---
            var grpOverview = new WidgetGroup
            {
                Name = "Tổng quan doanh thu",
                Description = "Dashboard tổng quan về doanh thu, đơn hàng và hiệu suất kinh doanh",
                IsActive = true,
                CreatedBy = "system",
                TenantId = demoTenant.Id
            };
            var grpProducts = new WidgetGroup
            {
                Name = "Báo cáo sản phẩm",
                Description = "Phân tích doanh số theo sản phẩm và danh mục",
                IsActive = true,
                CreatedBy = "system",
                TenantId = demoTenant.Id
            };
            var grpCustomers = new WidgetGroup
            {
                Name = "Báo cáo khách hàng",
                Description = "Thống kê khách hàng, doanh thu theo khách hàng",
                IsActive = true,
                CreatedBy = "system",
                TenantId = demoTenant.Id
            };
            var grpPayments = new WidgetGroup
            {
                Name = "Báo cáo thanh toán",
                Description = "Phân tích phương thức thanh toán và trạng thái giao dịch",
                IsActive = true,
                CreatedBy = "system",
                TenantId = demoTenant.Id
            };
            _context.WidgetGroups.AddRange(grpOverview, grpProducts, grpCustomers, grpPayments);
            await _context.SaveChangesAsync();

            // --- Overview Widgets ---
            var wTotalRevenue = new Widget
            {
                Name = "total_revenue_metric",
                FriendlyLabel = "Tổng doanh thu",
                HelpText = "Tổng doanh thu từ các đơn hàng hoàn thành",
                WidgetType = WidgetType.Metric,
                Description = "KPI: tổng doanh thu từ đơn hàng completed",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT ROUND(SUM(total_amount),0) as value FROM orders WHERE status='completed'\", \"label\": \"Tổng doanh thu (VNĐ)\", \"format\": \"currency\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 1
            };
            var wTotalOrders = new Widget
            {
                Name = "total_orders_metric",
                FriendlyLabel = "Tổng đơn hàng",
                HelpText = "Tổng số đơn hàng trong hệ thống",
                WidgetType = WidgetType.Metric,
                Description = "KPI: tổng số đơn hàng",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT COUNT(*) as value FROM orders WHERE status='completed'\", \"label\": \"Đơn hàng hoàn thành\", \"format\": \"number\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 15, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 1
            };
            var wAvgOrder = new Widget
            {
                Name = "avg_order_value_metric",
                FriendlyLabel = "Giá trị đơn TB",
                HelpText = "Giá trị trung bình mỗi đơn hàng",
                WidgetType = WidgetType.Metric,
                Description = "KPI: giá trị đơn hàng trung bình",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT ROUND(AVG(total_amount),0) as value FROM orders WHERE status='completed'\", \"label\": \"Giá trị đơn TB (VNĐ)\", \"format\": \"currency\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 1
            };
            var wTotalCustomers = new Widget
            {
                Name = "total_customers_metric",
                FriendlyLabel = "Tổng khách hàng",
                HelpText = "Tổng số khách hàng đã mua hàng",
                WidgetType = WidgetType.Metric,
                Description = "KPI: tổng số khách hàng",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT COUNT(*) as value FROM customers WHERE total_spent > 0\", \"label\": \"Khách hàng có mua hàng\", \"format\": \"number\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 1
            };
            var wMonthlyRevenueTrend = new Widget
            {
                Name = "monthly_revenue_trend",
                FriendlyLabel = "Xu hướng doanh thu theo tháng",
                HelpText = "Biểu đồ đường thể hiện doanh thu 12 tháng gần nhất",
                WidgetType = WidgetType.Chart,
                Description = "Line chart doanh thu theo tháng",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT strftime('%Y-%m', created_at) as month, ROUND(SUM(total_amount),0) as revenue FROM orders WHERE status='completed' AND created_at >= date('now','-12 months') GROUP BY strftime('%Y-%m', created_at) ORDER BY month ASC\", \"xAxis\": \"month\", \"yAxis\": \"revenue\"}",
                ChartConfig = "{\"type\": \"Line\", \"xAxis\": \"month\", \"yAxis\": \"revenue\", \"seriesLabel\": \"Doanh thu\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 12
            };
            var wRevenueByCategoryChart = new Widget
            {
                Name = "revenue_by_category_chart",
                FriendlyLabel = "Doanh thu theo danh mục",
                HelpText = "Biểu đồ cột so sánh doanh thu theo danh mục sản phẩm",
                WidgetType = WidgetType.Chart,
                Description = "Bar chart doanh thu theo danh mục",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT c.name as category, ROUND(SUM(oi.line_total),0) as revenue FROM order_items oi JOIN products p ON oi.product_id=p.id JOIN categories c ON p.category_id=c.id JOIN orders o ON oi.order_id=o.id WHERE o.status='completed' GROUP BY c.name ORDER BY revenue DESC\", \"xAxis\": \"category\", \"yAxis\": \"revenue\"}",
                ChartConfig = "{\"type\": \"Bar\", \"xAxis\": \"category\", \"yAxis\": \"revenue\", \"seriesLabel\": \"Doanh thu\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };
            var wOrderStatusTable = new Widget
            {
                Name = "order_status_summary",
                FriendlyLabel = "Tóm tắt trạng thái đơn hàng",
                HelpText = "Bảng thống kê số lượng và doanh thu theo trạng thái đơn hàng",
                WidgetType = WidgetType.Table,
                Description = "Bảng tóm tắt đơn hàng theo trạng thái",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT status as 'Trạng thái', COUNT(*) as 'Số đơn', ROUND(SUM(total_amount),0) as 'Tổng tiền' FROM orders GROUP BY status ORDER BY COUNT(*) DESC\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 15, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 3
            };

            // --- Product Widgets ---
            var wTopProducts = new Widget
            {
                Name = "top_products_by_revenue",
                FriendlyLabel = "Top 10 sản phẩm doanh thu cao nhất",
                HelpText = "Danh sách 10 sản phẩm có doanh thu cao nhất",
                WidgetType = WidgetType.Table,
                Description = "Top sản phẩm bán chạy theo doanh thu",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT p.name as 'Sản phẩm', c.name as 'Danh mục', SUM(oi.quantity) as 'SL bán', ROUND(SUM(oi.line_total),0) as 'Doanh thu' FROM order_items oi JOIN products p ON oi.product_id=p.id JOIN categories c ON p.category_id=c.id JOIN orders o ON oi.order_id=o.id WHERE o.status='completed' GROUP BY p.id ORDER BY SUM(oi.line_total) DESC LIMIT 10\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };
            var wProductSalesByCategory = new Widget
            {
                Name = "product_sales_by_category_chart",
                FriendlyLabel = "Sản lượng bán theo danh mục",
                HelpText = "Biểu đồ cột số lượng sản phẩm bán theo từng danh mục",
                WidgetType = WidgetType.Chart,
                Description = "Bar chart số lượng bán theo danh mục",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT c.name as category, SUM(oi.quantity) as quantity FROM order_items oi JOIN products p ON oi.product_id=p.id JOIN categories c ON p.category_id=c.id JOIN orders o ON oi.order_id=o.id WHERE o.status='completed' GROUP BY c.name ORDER BY quantity DESC\", \"xAxis\": \"category\", \"yAxis\": \"quantity\"}",
                ChartConfig = "{\"type\": \"Bar\", \"xAxis\": \"category\", \"yAxis\": \"quantity\", \"seriesLabel\": \"Số lượng bán\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };
            var wLowStock = new Widget
            {
                Name = "low_stock_products",
                FriendlyLabel = "Sản phẩm sắp hết hàng",
                HelpText = "Các sản phẩm có tồn kho dưới 50",
                WidgetType = WidgetType.Table,
                Description = "Danh sách sản phẩm tồn kho thấp",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT p.sku as 'SKU', p.name as 'Sản phẩm', c.name as 'Danh mục', p.stock_quantity as 'Tồn kho', p.unit as 'ĐVT' FROM products p JOIN categories c ON p.category_id=c.id WHERE p.stock_quantity < 50 AND p.is_active=1 ORDER BY p.stock_quantity ASC LIMIT 20\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 5, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 15
            };
            var wDailyOrders = new Widget
            {
                Name = "daily_orders_last30",
                FriendlyLabel = "Đơn hàng 30 ngày gần nhất",
                HelpText = "Biểu đồ số đơn hàng theo ngày trong 30 ngày qua",
                WidgetType = WidgetType.Chart,
                Description = "Line chart đơn hàng hằng ngày",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT strftime('%m/%d', created_at) as day, COUNT(*) as orders FROM orders WHERE created_at >= date('now','-30 days') GROUP BY strftime('%Y-%m-%d', created_at) ORDER BY created_at ASC\", \"xAxis\": \"day\", \"yAxis\": \"orders\"}",
                ChartConfig = "{\"type\": \"Line\", \"xAxis\": \"day\", \"yAxis\": \"orders\", \"seriesLabel\": \"Số đơn\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 15, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 30
            };

            // --- Customer Widgets ---
            var wTopCustomers = new Widget
            {
                Name = "top_customers_by_revenue",
                FriendlyLabel = "Top 10 khách hàng chi tiêu nhiều nhất",
                HelpText = "Danh sách khách hàng có tổng chi tiêu cao nhất",
                WidgetType = WidgetType.Table,
                Description = "Top khách hàng theo tổng chi tiêu",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT c.full_name as 'Khách hàng', c.city as 'Thành phố', c.loyalty_points as 'Điểm tích lũy', ROUND(c.total_spent,0) as 'Tổng chi tiêu' FROM customers c WHERE c.total_spent > 0 ORDER BY c.total_spent DESC LIMIT 10\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };
            var wCustomerByCity = new Widget
            {
                Name = "customers_by_city_chart",
                FriendlyLabel = "Khách hàng theo thành phố",
                HelpText = "Biểu đồ tròn phân bổ khách hàng theo thành phố",
                WidgetType = WidgetType.Chart,
                Description = "Pie chart khách hàng theo thành phố",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT city as label, COUNT(*) as value FROM customers GROUP BY city ORDER BY COUNT(*) DESC\", \"xAxis\": \"label\", \"yAxis\": \"value\"}",
                ChartConfig = "{\"type\": \"Donut\", \"xAxis\": \"label\", \"yAxis\": \"value\", \"seriesLabel\": \"Khách hàng\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };
            var wRecentOrders = new Widget
            {
                Name = "recent_orders_table",
                FriendlyLabel = "Đơn hàng gần đây",
                HelpText = "20 đơn hàng mới nhất trong hệ thống",
                WidgetType = WidgetType.Table,
                Description = "Danh sách đơn hàng gần đây",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT o.order_code as 'Mã đơn', COALESCE(c.full_name,'Khách lẻ') as 'Khách hàng', e.full_name as 'Nhân viên', o.status as 'Trạng thái', ROUND(o.total_amount,0) as 'Tổng tiền', strftime('%d/%m/%Y %H:%M', o.created_at) as 'Thời gian' FROM orders o LEFT JOIN customers c ON o.customer_id=c.id JOIN employees e ON o.employee_id=e.id ORDER BY o.created_at DESC LIMIT 20\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 5, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-5), LastRowCount = 20
            };

            // --- Payment Widgets ---
            var wPaymentMethodChart = new Widget
            {
                Name = "payment_method_distribution",
                FriendlyLabel = "Phân bổ phương thức thanh toán",
                HelpText = "Biểu đồ tròn phân bổ theo phương thức thanh toán",
                WidgetType = WidgetType.Chart,
                Description = "Pie chart phương thức thanh toán",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT payment_method as label, COUNT(*) as value FROM payments WHERE status='success' GROUP BY payment_method ORDER BY COUNT(*) DESC\", \"xAxis\": \"label\", \"yAxis\": \"value\"}",
                ChartConfig = "{\"type\": \"Pie\", \"xAxis\": \"label\", \"yAxis\": \"value\", \"seriesLabel\": \"Giao dịch\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 6
            };
            var wDailyPaymentTrend = new Widget
            {
                Name = "daily_payment_trend",
                FriendlyLabel = "Xu hướng thanh toán theo ngày",
                HelpText = "Biểu đồ đường tổng tiền thanh toán 30 ngày gần nhất",
                WidgetType = WidgetType.Chart,
                Description = "Line chart xu hướng thanh toán hằng ngày",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT strftime('%m/%d', paid_at) as day, ROUND(SUM(amount),0) as amount FROM payments WHERE status='success' AND paid_at >= date('now','-30 days') GROUP BY strftime('%Y-%m-%d', paid_at) ORDER BY paid_at ASC\", \"xAxis\": \"day\", \"yAxis\": \"amount\"}",
                ChartConfig = "{\"type\": \"Line\", \"xAxis\": \"day\", \"yAxis\": \"amount\", \"seriesLabel\": \"Doanh thu\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 30
            };
            var wPaymentSummaryTable = new Widget
            {
                Name = "payment_summary_by_method",
                FriendlyLabel = "Tóm tắt thanh toán theo phương thức",
                HelpText = "Bảng thống kê số giao dịch và tổng tiền theo phương thức",
                WidgetType = WidgetType.Table,
                Description = "Bảng tóm tắt thanh toán theo phương thức",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT payment_method as 'Phương thức', COUNT(*) as 'Số GD', ROUND(SUM(amount),0) as 'Tổng tiền', ROUND(AVG(amount),0) as 'TB/GD' FROM payments WHERE status='success' GROUP BY payment_method ORDER BY SUM(amount) DESC\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 6
            };
            var wFailedPayments = new Widget
            {
                Name = "failed_refunded_payments",
                FriendlyLabel = "Giao dịch thất bại / hoàn tiền",
                HelpText = "Danh sách giao dịch bị hủy hoặc hoàn tiền",
                WidgetType = WidgetType.Table,
                Description = "Bảng giao dịch không thành công",
                DataSourceId = dsSales.Id,
                Configuration = "{\"query\": \"SELECT p.transaction_ref as 'Mã GD', o.order_code as 'Mã đơn', p.payment_method as 'PT Thanh toán', p.status as 'Trạng thái', ROUND(p.amount,0) as 'Số tiền', strftime('%d/%m/%Y %H:%M', p.paid_at) as 'Thời gian' FROM payments p JOIN orders o ON p.order_id=o.id WHERE p.status IN ('refunded','failed') ORDER BY p.paid_at DESC LIMIT 20\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 15, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };

            var allWidgets = new[]
            {
                wTotalRevenue, wTotalOrders, wAvgOrder, wTotalCustomers,
                wMonthlyRevenueTrend, wRevenueByCategoryChart, wOrderStatusTable,
                wTopProducts, wProductSalesByCategory, wLowStock, wDailyOrders,
                wTopCustomers, wCustomerByCity, wRecentOrders,
                wPaymentMethodChart, wDailyPaymentTrend, wPaymentSummaryTable, wFailedPayments
            };
            _context.Widgets.AddRange(allWidgets);
            await _context.SaveChangesAsync();

            // Assign widgets to groups
            var overviewWidgetIds = new[] { wTotalRevenue.Id, wTotalOrders.Id, wAvgOrder.Id, wTotalCustomers.Id, wMonthlyRevenueTrend.Id, wRevenueByCategoryChart.Id, wOrderStatusTable.Id };
            var productWidgetIds = new[] { wTopProducts.Id, wProductSalesByCategory.Id, wLowStock.Id, wDailyOrders.Id };
            var customerWidgetIds = new[] { wTopCustomers.Id, wCustomerByCity.Id, wRecentOrders.Id };
            var paymentWidgetIds = new[] { wPaymentMethodChart.Id, wDailyPaymentTrend.Id, wPaymentSummaryTable.Id, wFailedPayments.Id };

            foreach (var wid in overviewWidgetIds)
                _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = grpOverview.Id, WidgetId = wid });
            foreach (var wid in productWidgetIds)
                _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = grpProducts.Id, WidgetId = wid });
            foreach (var wid in customerWidgetIds)
                _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = grpCustomers.Id, WidgetId = wid });
            foreach (var wid in paymentWidgetIds)
                _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = grpPayments.Id, WidgetId = wid });
            await _context.SaveChangesAsync();

            // Seed schedules
            _context.WidgetSchedules.AddRange(
                new WidgetSchedule { WidgetId = wMonthlyRevenueTrend.Id, CronExpression = "0 6 * * *", Timezone = "Asia/Ho_Chi_Minh", IsEnabled = true, RetryOnFailure = true, MaxRetries = 3, LastRunAt = DateTime.UtcNow.AddHours(-18), LastRunStatus = ExecutionStatus.Success, NextRunAt = DateTime.UtcNow.AddHours(6) },
                new WidgetSchedule { WidgetId = wRecentOrders.Id, CronExpression = "*/5 * * * *", Timezone = "Asia/Ho_Chi_Minh", IsEnabled = true, RetryOnFailure = false, MaxRetries = 1, LastRunAt = DateTime.UtcNow.AddMinutes(-5), LastRunStatus = ExecutionStatus.Success, NextRunAt = DateTime.UtcNow.AddMinutes(5) },
                new WidgetSchedule { WidgetId = wDailyPaymentTrend.Id, CronExpression = "0 */4 * * *", Timezone = "Asia/Ho_Chi_Minh", IsEnabled = true, RetryOnFailure = true, MaxRetries = 2, LastRunAt = DateTime.UtcNow.AddHours(-4), LastRunStatus = ExecutionStatus.Success, NextRunAt = DateTime.UtcNow.AddHours(4) }
            );
            await _context.SaveChangesAsync();

            // Seed sample executions
            var executions = new List<WidgetExecution>();
            foreach (var w in allWidgets)
            {
                executions.Add(new WidgetExecution { WidgetId = w.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Scheduler, StartedAt = DateTime.UtcNow.AddHours(-2), CompletedAt = DateTime.UtcNow.AddHours(-2).AddMilliseconds(180), ExecutionTimeMs = 180, RowCount = w.LastRowCount ?? 0 });
                executions.Add(new WidgetExecution { WidgetId = w.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Manual, StartedAt = DateTime.UtcNow.AddMinutes(-30), CompletedAt = DateTime.UtcNow.AddMinutes(-30).AddMilliseconds(210), ExecutionTimeMs = 210, RowCount = w.LastRowCount ?? 0 });
            }
            _context.WidgetExecutions.AddRange(executions);
            await _context.SaveChangesAsync();

            // ── Form Widget ──────────────────────────────────────────────────
            var wContactForm = new Widget
            {
                Name = "contact_form",
                FriendlyLabel = "Form Liên hệ",
                HelpText = "Form thu thập thông tin liên hệ từ khách hàng",
                WidgetType = WidgetType.Form,
                Description = "Form liên hệ tích hợp trên trang contact",
                DataSourceId = dsSales.Id,
                Configuration = "{\"fields\":[{\"name\":\"ho_ten\",\"label\":\"Họ và tên\",\"type\":\"text\",\"required\":true},{\"name\":\"email\",\"label\":\"Email\",\"type\":\"email\",\"required\":true},{\"name\":\"dien_thoai\",\"label\":\"Số điện thoại\",\"type\":\"text\",\"required\":false},{\"name\":\"chu_de\",\"label\":\"Chủ đề\",\"type\":\"select\",\"required\":true,\"options\":[\"Tư vấn sản phẩm\",\"Hỗ trợ kỹ thuật\",\"Khiếu nại đơn hàng\",\"Hợp tác kinh doanh\",\"Khác\"]},{\"name\":\"noi_dung\",\"label\":\"Nội dung\",\"type\":\"textarea\",\"required\":true}],\"submitLabel\":\"Gửi tin nhắn\",\"successMessage\":\"Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi trong vòng 24 giờ.\"}",
                IsActive = true,
                CacheEnabled = false,
                CacheTtlMinutes = 0,
                CreatedBy = "system"
            };
            _context.Widgets.Add(wContactForm);
            await _context.SaveChangesAsync();

            // Assign form widget to customer group
            _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = grpCustomers.Id, WidgetId = wContactForm.Id });
            await _context.SaveChangesAsync();

            // ── Form Submissions ─────────────────────────────────────────────
            _context.FormSubmissions.AddRange(
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Nguyễn Văn An\",\"email\":\"an.nguyen@gmail.com\",\"dien_thoai\":\"0912345678\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Tôi muốn hỏi về gói Pro, cần thêm thông tin về giới hạn widget và nguồn dữ liệu.\"}", IpAddress = "192.168.1.10", SubmittedAt = DateTime.UtcNow.AddDays(-5) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Trần Thị Mai\",\"email\":\"mai.tran@company.vn\",\"dien_thoai\":\"0987654321\",\"chu_de\":\"Hỗ trợ kỹ thuật\",\"noi_dung\":\"Widget biểu đồ không hiển thị đúng trên thiết bị mobile, cần hỗ trợ xử lý gấp.\"}", IpAddress = "10.0.0.5", SubmittedAt = DateTime.UtcNow.AddDays(-3) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Lê Văn Đức\",\"email\":\"duc.le@startup.io\",\"dien_thoai\":\"\",\"chu_de\":\"Hợp tác kinh doanh\",\"noi_dung\":\"Chúng tôi muốn tích hợp WidgetData vào nền tảng SaaS của mình, muốn trao đổi về khả năng cung cấp API white-label.\"}", IpAddress = "203.113.20.5", SubmittedAt = DateTime.UtcNow.AddDays(-1) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Phạm Thị Hoa\",\"email\":\"hoa.pham@gmail.com\",\"dien_thoai\":\"0345678901\",\"chu_de\":\"Khiếu nại đơn hàng\",\"noi_dung\":\"Tôi chưa nhận được email xác nhận đăng ký gói Pro sau khi thanh toán thành công.\"}", IpAddress = "118.70.100.22", SubmittedAt = DateTime.UtcNow.AddHours(-6) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Hoàng Văn Minh\",\"email\":\"minh.hoang@techcorp.vn\",\"dien_thoai\":\"0901234567\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Muốn tư vấn về gói Business cho doanh nghiệp 50+ người dùng, cần SLA rõ ràng và khả năng deploy on-premise.\"}", IpAddress = "14.232.30.100", SubmittedAt = DateTime.UtcNow.AddHours(-2) }
            );
            await _context.SaveChangesAsync();

            // ── Delivery Targets ─────────────────────────────────────────────
            var dtEmail = new DeliveryTarget
            {
                WidgetId = wMonthlyRevenueTrend.Id,
                Name = "Báo cáo doanh thu hàng ngày (Email)",
                Type = DeliveryType.Email,
                Configuration = "{\"recipients\":[\"manager@widgetdata.com\",\"admin@widgetdata.com\"],\"subject\":\"[WidgetData] Báo cáo doanh thu tháng\",\"format\":\"excel\"}",
                IsEnabled = true,
                CreatedBy = "system"
            };
            var dtTelegram = new DeliveryTarget
            {
                WidgetId = wRecentOrders.Id,
                Name = "Thông báo đơn hàng mới (Telegram)",
                Type = DeliveryType.Telegram,
                Configuration = "{\"botToken\":\"demo-bot-token-7654321\",\"chatId\":\"-100123456789\",\"messageTemplate\":\"📦 Có {{row_count}} đơn hàng mới — xem tại WidgetData!\"}",
                IsEnabled = true,
                CreatedBy = "system"
            };
            var dtCsv = new DeliveryTarget
            {
                WidgetId = wTopProducts.Id,
                Name = "Xuất top sản phẩm dạng CSV (FTP)",
                Type = DeliveryType.Csv,
                Configuration = "{\"host\":\"ftp.internal.company.vn\",\"port\":21,\"username\":\"ftpuser\",\"path\":\"/reports/top-products.csv\"}",
                IsEnabled = false,
                CreatedBy = "system"
            };
            _context.DeliveryTargets.AddRange(dtEmail, dtTelegram, dtCsv);
            await _context.SaveChangesAsync();

            _context.DeliveryExecutions.AddRange(
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-1) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-2) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Failed, Message = "SMTP connection timeout after 30s", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-3) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "manual", ExecutedAt = DateTime.UtcNow.AddMinutes(-15) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddHours(-5) }
            );
            await _context.SaveChangesAsync();

            // ── Config Archives ──────────────────────────────────────────────
            _context.WidgetConfigArchives.AddRange(
                new WidgetConfigArchive
                {
                    WidgetId = wMonthlyRevenueTrend.Id,
                    Configuration = "{\"query\":\"SELECT strftime('%Y-%m', created_at) as month, ROUND(SUM(total_amount),0) as revenue FROM orders WHERE status='completed' AND created_at >= date('now','-6 months') GROUP BY month ORDER BY month ASC\"}",
                    ChartConfig = "{\"type\":\"Bar\",\"xAxis\":\"month\",\"yAxis\":\"revenue\",\"seriesLabel\":\"Doanh thu\"}",
                    Note = "Phiên bản gốc — Bar chart, chỉ 6 tháng",
                    TriggerSource = "Manual",
                    ArchivedBy = "admin@widgetdata.com",
                    ArchivedAt = DateTime.UtcNow.AddDays(-30)
                },
                new WidgetConfigArchive
                {
                    WidgetId = wMonthlyRevenueTrend.Id,
                    Configuration = "{\"query\":\"SELECT strftime('%Y-%m', created_at) as month, ROUND(SUM(total_amount),0) as revenue FROM orders WHERE status='completed' AND created_at >= date('now','-9 months') GROUP BY month ORDER BY month ASC\"}",
                    ChartConfig = "{\"type\":\"Line\",\"xAxis\":\"month\",\"yAxis\":\"revenue\",\"seriesLabel\":\"Doanh thu\"}",
                    Note = "Mở rộng 9 tháng, chuyển sang Line chart",
                    TriggerSource = "Manual",
                    ArchivedBy = "manager@widgetdata.com",
                    ArchivedAt = DateTime.UtcNow.AddDays(-7)
                },
                new WidgetConfigArchive
                {
                    WidgetId = wRevenueByCategoryChart.Id,
                    Configuration = "{\"query\":\"SELECT c.name as category, ROUND(SUM(oi.line_total),0) as revenue FROM order_items oi JOIN products p ON oi.product_id=p.id JOIN categories c ON p.category_id=c.id GROUP BY c.name ORDER BY revenue DESC\"}",
                    ChartConfig = "{\"type\":\"Pie\",\"xAxis\":\"category\",\"yAxis\":\"revenue\",\"seriesLabel\":\"Doanh thu\"}",
                    Note = "Phiên bản Pie chart (trước khi đổi sang Bar)",
                    TriggerSource = "OnSave",
                    ArchivedBy = "system",
                    ArchivedAt = DateTime.UtcNow.AddDays(-14)
                }
            );
            await _context.SaveChangesAsync();

            // ── Widget API Activity Logs ─────────────────────────────────────
            var actRand = new Random(77);
            var activityWidgets = new[] { wTotalRevenue, wMonthlyRevenueTrend, wRecentOrders, wTopProducts, wPaymentMethodChart };
            var activities = new List<WidgetApiActivity>();
            foreach (var w in activityWidgets)
            {
                for (int i = 0; i < 20; i++)
                {
                    activities.Add(new WidgetApiActivity
                    {
                        WidgetId = w.Id,
                        ApiEndpoint = $"/api/widgets/{w.Id}/execute",
                        UserId = actRand.Next(0, 3) == 0 ? null : $"user-demo-{actRand.Next(1, 4)}",
                        CalledAt = DateTime.UtcNow.AddHours(-actRand.Next(1, 168)),
                        ResponseTimeMs = actRand.Next(45, 520),
                        StatusCode = actRand.Next(0, 10) == 0 ? 500 : 200
                    });
                }
            }
            _context.WidgetApiActivities.AddRange(activities);
            await _context.SaveChangesAsync();

            // ── Idea Board ───────────────────────────────────────────────────
            var ideaSub = new IdeaSubscription
            {
                WidgetId = wTopProducts.Id,
                Name = "Product Feedback — Internal",
                LabelFilter = "feedback,suggestion",
                WebhookUrl = null,
                IsActive = true,
                CreatedBy = "system"
            };
            _context.IdeaSubscriptions.Add(ideaSub);
            await _context.SaveChangesAsync();

            var ideaPost1 = new IdeaPost { WidgetId = wTopProducts.Id, Title = "Thêm bộ lọc theo khoảng giá", Content = "Khách hàng muốn lọc sản phẩm theo khoảng giá (dưới 500K, 500K-1M, trên 1M) để tìm kiếm nhanh hơn.", Labels = "feedback,suggestion", Status = "Processed", CreatedBy = "customer@example.com", CreatedAt = DateTime.UtcNow.AddDays(-10), ProcessedAt = DateTime.UtcNow.AddDays(-8) };
            var ideaPost2 = new IdeaPost { WidgetId = wTopProducts.Id, Title = "Hiển thị đánh giá sao sản phẩm", Content = "Thêm cột điểm đánh giá trung bình (⭐) vào widget top sản phẩm để khách dễ so sánh.", Labels = "suggestion", Status = "Pending", CreatedBy = "admin@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-5) };
            var ideaPost3 = new IdeaPost { WidgetId = wTopProducts.Id, Title = "Tồn kho hiển thị màu cảnh báo", Content = "Sản phẩm có tồn kho < 10 nên được tô màu đỏ, 10-50 màu vàng để nhân viên dễ theo dõi.", Labels = "feedback", Status = "Processed", CreatedBy = "manager@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-3), ProcessedAt = DateTime.UtcNow.AddDays(-2) };
            _context.IdeaPosts.AddRange(ideaPost1, ideaPost2, ideaPost3);
            await _context.SaveChangesAsync();

            _context.IdeaResults.AddRange(
                new IdeaResult { IdeaPostId = ideaPost1.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"acknowledged\",\"assignedTo\":\"dev@widgetdata.com\",\"note\":\"Sẽ triển khai bộ lọc giá trong sprint Q3-2026\"}", Status = "Processed", CreatedAt = DateTime.UtcNow.AddDays(-8) },
                new IdeaResult { IdeaPostId = ideaPost3.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"acknowledged\",\"note\":\"Đã ghi nhận, sẽ thêm conditional formatting trong UI update tháng 6\"}", Status = "Received", CreatedAt = DateTime.UtcNow.AddDays(-2) }
            );
            await _context.SaveChangesAsync();

            // ── EduViet Course Demo — Widget Group & Widgets ──────────────────
            var grpCourse = new WidgetGroup
            {
                Name = "Phân tích học tập — EduViet",
                Description = "Dashboard theo dõi đăng ký khóa học, tiến độ học viên và doanh thu nền tảng học trực tuyến",
                IsActive = true,
                CreatedBy = "system"
            };
            _context.WidgetGroups.Add(grpCourse);
            await _context.SaveChangesAsync();

            var wCEnrollToday = new Widget
            {
                Name = "course_enrollments_today",
                FriendlyLabel = "Đăng ký mới hôm nay",
                HelpText = "Tổng số học viên đăng ký mới trong ngày hôm nay",
                WidgetType = WidgetType.Metric,
                Description = "KPI: số đăng ký mới trong ngày",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT COUNT(*) as value FROM enrollments WHERE date(enrolled_at) = date('now')\", \"label\": \"Đăng ký mới\", \"format\": \"number\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 5, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-5), LastRowCount = 1
            };
            var wCActiveCourses = new Widget
            {
                Name = "course_active_courses_count",
                FriendlyLabel = "Khóa học đang hoạt động",
                HelpText = "Số lượng khóa học đang được phát hành và có học viên",
                WidgetType = WidgetType.Metric,
                Description = "KPI: tổng khóa học đang active",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT COUNT(*) as value FROM courses WHERE is_published = 1\", \"label\": \"Khóa học\", \"format\": \"number\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 1
            };
            var wCCompletionRate = new Widget
            {
                Name = "course_completion_rate",
                FriendlyLabel = "Tỷ lệ hoàn thành",
                HelpText = "Phần trăm học viên đã hoàn thành khóa học so với tổng đăng ký",
                WidgetType = WidgetType.Metric,
                Description = "KPI: tỷ lệ hoàn thành khóa học (%)",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT ROUND(100.0 * SUM(CASE WHEN status='completed' THEN 1 ELSE 0 END) / NULLIF(COUNT(*), 0), 1) as value FROM enrollments\", \"label\": \"Tỷ lệ hoàn thành (%)\", \"format\": \"percent\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 1
            };
            var wCTodayRevenue = new Widget
            {
                Name = "course_revenue_today",
                FriendlyLabel = "Doanh thu hôm nay",
                HelpText = "Tổng doanh thu từ thanh toán khóa học trong ngày hôm nay",
                WidgetType = WidgetType.Metric,
                Description = "KPI: doanh thu khóa học trong ngày",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT COALESCE(ROUND(SUM(amount), 0), 0) as value FROM course_payments WHERE status='success' AND date(paid_at) = date('now')\", \"label\": \"Doanh thu hôm nay (VNĐ)\", \"format\": \"currency\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 5, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-5), LastRowCount = 1
            };
            var wCEnrollByCategory = new Widget
            {
                Name = "course_enrollments_by_category",
                FriendlyLabel = "Đăng ký theo danh mục",
                HelpText = "Biểu đồ cột số lượng đăng ký theo danh mục khóa học",
                WidgetType = WidgetType.Chart,
                Description = "Bar chart đăng ký theo danh mục",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT c.name as category, COUNT(e.id) as enrollments FROM enrollments e JOIN courses co ON e.course_id=co.id JOIN categories c ON co.category_id=c.id GROUP BY c.name ORDER BY enrollments DESC\", \"xAxis\": \"category\", \"yAxis\": \"enrollments\"}",
                ChartConfig = "{\"type\": \"Bar\", \"xAxis\": \"category\", \"yAxis\": \"enrollments\", \"seriesLabel\": \"Số đăng ký\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 8
            };
            var wCPopularCourses = new Widget
            {
                Name = "course_popular_courses",
                FriendlyLabel = "Khóa học phổ biến nhất",
                HelpText = "Top 10 khóa học có nhiều học viên đăng ký nhất",
                WidgetType = WidgetType.Table,
                Description = "Bảng top khóa học theo số đăng ký",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT co.title as 'Khóa học', i.full_name as 'Giảng viên', ROUND(co.rating, 1) as 'Đánh giá', co.total_enrollments as 'Học viên', ROUND(co.price, 0) as 'Học phí' FROM courses co JOIN instructors i ON co.instructor_id=i.id WHERE co.is_published=1 ORDER BY co.total_enrollments DESC LIMIT 10\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };
            var wCProgressChart = new Widget
            {
                Name = "course_completion_progress_chart",
                FriendlyLabel = "Tiến độ hoàn thành",
                HelpText = "Biểu đồ tròn phân bổ trạng thái tiến độ học viên",
                WidgetType = WidgetType.Chart,
                Description = "Pie chart trạng thái đăng ký",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT CASE status WHEN 'completed' THEN 'Hoàn thành' WHEN 'active' THEN 'Đang học' WHEN 'paused' THEN 'Tạm dừng' ELSE status END as label, COUNT(*) as value FROM enrollments GROUP BY status ORDER BY value DESC\", \"xAxis\": \"label\", \"yAxis\": \"value\"}",
                ChartConfig = "{\"type\": \"Donut\", \"xAxis\": \"label\", \"yAxis\": \"value\", \"seriesLabel\": \"Học viên\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 3
            };
            var wCRecentActivity = new Widget
            {
                Name = "course_recent_student_activity",
                FriendlyLabel = "Hoạt động học viên gần đây",
                HelpText = "20 lượt đăng ký khóa học mới nhất trong hệ thống",
                WidgetType = WidgetType.Table,
                Description = "Bảng hoạt động học viên gần đây",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT s.full_name as 'Học viên', co.title as 'Khóa học', ROUND(e.progress_percent, 0) as 'Tiến độ %', e.status as 'Trạng thái', strftime('%d/%m/%Y', e.enrolled_at) as 'Ngày đăng ký' FROM enrollments e JOIN students s ON e.student_id=s.id JOIN courses co ON e.course_id=co.id ORDER BY e.enrolled_at DESC LIMIT 20\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 5, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-5), LastRowCount = 20
            };
            var wCMonthlyRevenue = new Widget
            {
                Name = "course_monthly_revenue_trend",
                FriendlyLabel = "Doanh thu theo tháng",
                HelpText = "Biểu đồ đường doanh thu khóa học 12 tháng gần nhất",
                WidgetType = WidgetType.Chart,
                Description = "Line chart doanh thu theo tháng",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT strftime('%Y-%m', paid_at) as month, ROUND(SUM(amount), 0) as revenue FROM course_payments WHERE status='success' AND paid_at >= date('now','-12 months') GROUP BY strftime('%Y-%m', paid_at) ORDER BY month ASC\", \"xAxis\": \"month\", \"yAxis\": \"revenue\"}",
                ChartConfig = "{\"type\": \"Line\", \"xAxis\": \"month\", \"yAxis\": \"revenue\", \"seriesLabel\": \"Doanh thu\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 12
            };
            var wCTopInstructors = new Widget
            {
                Name = "course_top_instructors",
                FriendlyLabel = "Top giảng viên",
                HelpText = "Danh sách giảng viên có nhiều học viên và đánh giá cao nhất",
                WidgetType = WidgetType.Table,
                Description = "Bảng top giảng viên theo số học viên",
                DataSourceId = dsCourse.Id,
                Configuration = "{\"query\": \"SELECT i.full_name as 'Giảng viên', i.specialization as 'Chuyên môn', i.total_courses as 'Số khóa', i.total_students as 'Học viên', ROUND(i.rating, 1) as 'Đánh giá' FROM instructors i WHERE i.is_active=1 ORDER BY i.total_students DESC LIMIT 10\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };

            var allCourseWidgets = new[]
            {
                wCEnrollToday, wCActiveCourses, wCCompletionRate, wCTodayRevenue,
                wCEnrollByCategory, wCPopularCourses, wCProgressChart, wCRecentActivity,
                wCMonthlyRevenue, wCTopInstructors
            };
            _context.Widgets.AddRange(allCourseWidgets);
            await _context.SaveChangesAsync();

            foreach (var w in allCourseWidgets)
                _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = grpCourse.Id, WidgetId = w.Id });
            await _context.SaveChangesAsync();

            _context.WidgetSchedules.Add(new WidgetSchedule
            {
                WidgetId = wCMonthlyRevenue.Id, CronExpression = "0 7 * * *",
                Timezone = "Asia/Ho_Chi_Minh", IsEnabled = true, RetryOnFailure = true, MaxRetries = 3,
                LastRunAt = DateTime.UtcNow.AddHours(-19), LastRunStatus = ExecutionStatus.Success,
                NextRunAt = DateTime.UtcNow.AddHours(5)
            });
            await _context.SaveChangesAsync();

            var courseExecs = new List<WidgetExecution>();
            foreach (var w in allCourseWidgets)
            {
                courseExecs.Add(new WidgetExecution { WidgetId = w.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Scheduler, StartedAt = DateTime.UtcNow.AddHours(-3), CompletedAt = DateTime.UtcNow.AddHours(-3).AddMilliseconds(150), ExecutionTimeMs = 150, RowCount = w.LastRowCount ?? 0 });
                courseExecs.Add(new WidgetExecution { WidgetId = w.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Manual, StartedAt = DateTime.UtcNow.AddMinutes(-20), CompletedAt = DateTime.UtcNow.AddMinutes(-20).AddMilliseconds(160), ExecutionTimeMs = 160, RowCount = w.LastRowCount ?? 0 });
            }
            _context.WidgetExecutions.AddRange(courseExecs);
            await _context.SaveChangesAsync();

            // ── VietNews Demo — Widget Group & Widgets ────────────────────────
            var grpNews = new WidgetGroup
            {
                Name = "Phân tích tin tức — VietNews",
                Description = "Dashboard theo dõi lượt xem bài viết, độc giả, bình luận và nguồn truy cập cổng tin tức",
                IsActive = true,
                CreatedBy = "system"
            };
            _context.WidgetGroups.Add(grpNews);
            await _context.SaveChangesAsync();

            var wNViewsToday = new Widget
            {
                Name = "news_total_views_today",
                FriendlyLabel = "Tổng lượt xem hôm nay",
                HelpText = "Tổng số lượt xem bài viết trong ngày hôm nay",
                WidgetType = WidgetType.Metric,
                Description = "KPI: lượt xem bài viết trong ngày",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT COUNT(*) as value FROM article_views WHERE date(viewed_at) = date('now')\", \"label\": \"Lượt xem\", \"format\": \"number\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 5, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-5), LastRowCount = 1
            };
            var wNArticlesToday = new Widget
            {
                Name = "news_articles_published_today",
                FriendlyLabel = "Bài đăng hôm nay",
                HelpText = "Số bài viết được xuất bản trong ngày hôm nay",
                WidgetType = WidgetType.Metric,
                Description = "KPI: số bài viết xuất bản hôm nay",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT COUNT(*) as value FROM articles WHERE date(published_at) = date('now') AND status='published'\", \"label\": \"Bài đăng mới\", \"format\": \"number\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 5, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-5), LastRowCount = 1
            };
            var wNNewReaders = new Widget
            {
                Name = "news_new_readers_today",
                FriendlyLabel = "Độc giả mới",
                HelpText = "Số độc giả đăng ký tài khoản mới trong ngày hôm nay",
                WidgetType = WidgetType.Metric,
                Description = "KPI: độc giả mới đăng ký hôm nay",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT COUNT(*) as value FROM readers WHERE date(registered_at) = date('now')\", \"label\": \"Độc giả mới\", \"format\": \"number\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 5, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-5), LastRowCount = 1
            };
            var wNReadCompletion = new Widget
            {
                Name = "news_read_completion_rate",
                FriendlyLabel = "Tỷ lệ đọc trọn bài",
                HelpText = "Phần trăm bài viết được đọc hơn 80% nội dung trong 7 ngày qua",
                WidgetType = WidgetType.Metric,
                Description = "KPI: tỷ lệ đọc hết bài trung bình (%)",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT ROUND(AVG(read_completion_percent), 1) as value FROM article_views WHERE viewed_at >= datetime('now','-7 days')\", \"label\": \"Đọc trọn bài (%)\", \"format\": \"percent\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 1
            };
            var wNViewsByCategory = new Widget
            {
                Name = "news_views_by_category",
                FriendlyLabel = "Lượt xem theo chuyên mục",
                HelpText = "Biểu đồ cột tổng lượt xem bài viết theo từng chuyên mục",
                WidgetType = WidgetType.Chart,
                Description = "Bar chart lượt xem theo chuyên mục",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT c.name as category, COUNT(av.id) as views FROM article_views av JOIN articles a ON av.article_id=a.id JOIN categories c ON a.category_id=c.id GROUP BY c.name ORDER BY views DESC\", \"xAxis\": \"category\", \"yAxis\": \"views\"}",
                ChartConfig = "{\"type\": \"Bar\", \"xAxis\": \"category\", \"yAxis\": \"views\", \"seriesLabel\": \"Lượt xem\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 30, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 8
            };
            var wNPopularArticles = new Widget
            {
                Name = "news_popular_articles_week",
                FriendlyLabel = "Bài viết phổ biến nhất trong tuần",
                HelpText = "Top 10 bài viết có nhiều lượt xem nhất trong 7 ngày qua",
                WidgetType = WidgetType.Table,
                Description = "Bảng top bài viết theo lượt xem trong tuần",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT a.title as 'Bài viết', c.name as 'Chuyên mục', auth.full_name as 'Tác giả', a.view_count as 'Lượt xem', a.comment_count as 'Bình luận' FROM articles a JOIN categories c ON a.category_id=c.id JOIN authors auth ON a.author_id=auth.id WHERE a.status='published' AND a.published_at >= date('now','-7 days') ORDER BY a.view_count DESC LIMIT 10\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 15, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };
            var wNTrafficSources = new Widget
            {
                Name = "news_traffic_sources",
                FriendlyLabel = "Nguồn truy cập",
                HelpText = "Biểu đồ tròn phân bổ nguồn truy cập bài viết (Google, Direct, Social, Email)",
                WidgetType = WidgetType.Chart,
                Description = "Pie chart nguồn truy cập",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT source as label, COUNT(*) as value FROM article_views GROUP BY source ORDER BY value DESC\", \"xAxis\": \"label\", \"yAxis\": \"value\"}",
                ChartConfig = "{\"type\": \"Pie\", \"xAxis\": \"label\", \"yAxis\": \"value\", \"seriesLabel\": \"Truy cập\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 5
            };
            var wNRecentActivity = new Widget
            {
                Name = "news_recent_reader_activity",
                FriendlyLabel = "Hoạt động độc giả gần đây",
                HelpText = "20 lượt xem bài viết mới nhất trong hệ thống",
                WidgetType = WidgetType.Table,
                Description = "Bảng hoạt động độc giả gần đây",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT COALESCE(r.full_name, 'Khách') as 'Độc giả', a.title as 'Bài viết', av.source as 'Nguồn', av.device as 'Thiết bị', ROUND(av.read_completion_percent, 0) as 'Đọc %', strftime('%d/%m/%Y %H:%M', av.viewed_at) as 'Thời gian' FROM article_views av LEFT JOIN readers r ON av.reader_id=r.id JOIN articles a ON av.article_id=a.id ORDER BY av.viewed_at DESC LIMIT 20\"}",
                IsActive = true, CacheEnabled = false, CacheTtlMinutes = 5, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-5), LastRowCount = 20
            };
            var wNMonthlyViews = new Widget
            {
                Name = "news_monthly_views_trend",
                FriendlyLabel = "Xu hướng lượt xem theo tháng",
                HelpText = "Biểu đồ đường tổng lượt xem bài viết theo tháng trong 12 tháng qua",
                WidgetType = WidgetType.Chart,
                Description = "Line chart lượt xem theo tháng",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT strftime('%Y-%m', viewed_at) as month, COUNT(*) as views FROM article_views WHERE viewed_at >= date('now','-12 months') GROUP BY strftime('%Y-%m', viewed_at) ORDER BY month ASC\", \"xAxis\": \"month\", \"yAxis\": \"views\"}",
                ChartConfig = "{\"type\": \"Line\", \"xAxis\": \"month\", \"yAxis\": \"views\", \"seriesLabel\": \"Lượt xem\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 12
            };
            var wNTopAuthors = new Widget
            {
                Name = "news_top_authors",
                FriendlyLabel = "Tác giả nổi bật",
                HelpText = "Danh sách tác giả có nhiều bài viết và lượt xem cao nhất",
                WidgetType = WidgetType.Table,
                Description = "Bảng top tác giả theo lượt xem",
                DataSourceId = dsNews.Id,
                Configuration = "{\"query\": \"SELECT a.full_name as 'Tác giả', a.total_articles as 'Số bài', a.total_views as 'Lượt xem' FROM authors a WHERE a.is_active=1 ORDER BY a.total_views DESC LIMIT 10\"}",
                IsActive = true, CacheEnabled = true, CacheTtlMinutes = 60, CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-10), LastRowCount = 10
            };

            var allNewsWidgets = new[]
            {
                wNViewsToday, wNArticlesToday, wNNewReaders, wNReadCompletion,
                wNViewsByCategory, wNPopularArticles, wNTrafficSources, wNRecentActivity,
                wNMonthlyViews, wNTopAuthors
            };
            _context.Widgets.AddRange(allNewsWidgets);
            await _context.SaveChangesAsync();

            foreach (var w in allNewsWidgets)
                _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = grpNews.Id, WidgetId = w.Id });
            await _context.SaveChangesAsync();

            _context.WidgetSchedules.Add(new WidgetSchedule
            {
                WidgetId = wNMonthlyViews.Id, CronExpression = "0 6 * * *",
                Timezone = "Asia/Ho_Chi_Minh", IsEnabled = true, RetryOnFailure = true, MaxRetries = 3,
                LastRunAt = DateTime.UtcNow.AddHours(-18), LastRunStatus = ExecutionStatus.Success,
                NextRunAt = DateTime.UtcNow.AddHours(6)
            });
            await _context.SaveChangesAsync();

            var newsExecs = new List<WidgetExecution>();
            foreach (var w in allNewsWidgets)
            {
                newsExecs.Add(new WidgetExecution { WidgetId = w.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Scheduler, StartedAt = DateTime.UtcNow.AddHours(-2), CompletedAt = DateTime.UtcNow.AddHours(-2).AddMilliseconds(120), ExecutionTimeMs = 120, RowCount = w.LastRowCount ?? 0 });
                newsExecs.Add(new WidgetExecution { WidgetId = w.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Manual, StartedAt = DateTime.UtcNow.AddMinutes(-15), CompletedAt = DateTime.UtcNow.AddMinutes(-15).AddMilliseconds(130), ExecutionTimeMs = 130, RowCount = w.LastRowCount ?? 0 });
            }
            _context.WidgetExecutions.AddRange(newsExecs);
            await _context.SaveChangesAsync();

            // ── Demo Pages ────────────────────────────────────────────────────
            await SeedDemoPagesAsync(demoTenant.Id);
        }
    }

    private async Task SeedDemoPagesAsync(int demoTenantId)
    {
        if (await _context.Pages.IgnoreQueryFilters().AnyAsync(p => p.TenantId == demoTenantId))
            return;

        // Sales demo page
        var salesPage = new Page
        {
            TenantId = demoTenantId,
            Title = "Cửa hàng - Sales Dashboard",
            Slug = "sales",
            Description = "Dashboard tổng quan bán hàng: doanh thu, đơn hàng, khách hàng và thanh toán",
            IsActive = true,
            CreatedBy = "system"
        };
        _context.Pages.Add(salesPage);
        await _context.SaveChangesAsync();

        var salesWidgetNames = new[]
        {
            "total_revenue_metric", "total_orders_metric", "avg_order_value_metric",
            "total_customers_metric", "monthly_revenue_trend", "revenue_by_category_chart",
            "order_status_summary", "recent_orders_table"
        };
        var salesWidgets = await _context.Widgets.IgnoreQueryFilters()
            .Where(w => salesWidgetNames.Contains(w.Name))
            .OrderBy(w => Array.IndexOf(salesWidgetNames, w.Name))
            .ToListAsync();
        for (int i = 0; i < salesWidgets.Count; i++)
            _context.PageWidgets.Add(new PageWidget
            {
                PageId = salesPage.Id,
                WidgetId = salesWidgets[i].Id,
                Position = i,
                Width = i < 4 ? 3 : 6
            });
        await _context.SaveChangesAsync();

        // Course demo page
        var coursePage = new Page
        {
            TenantId = demoTenantId,
            Title = "EduViet - Learning Dashboard",
            Slug = "course",
            Description = "Dashboard theo dõi khóa học, học viên và doanh thu nền tảng học trực tuyến",
            IsActive = true,
            CreatedBy = "system"
        };
        _context.Pages.Add(coursePage);
        await _context.SaveChangesAsync();

        var courseWidgetNames = new[]
        {
            "course_enrollments_today", "course_active_courses_count",
            "course_completion_rate", "course_revenue_today",
            "course_enrollments_by_category", "course_popular_courses",
            "course_completion_progress_chart", "course_recent_student_activity"
        };
        var courseWidgets = await _context.Widgets.IgnoreQueryFilters()
            .Where(w => courseWidgetNames.Contains(w.Name))
            .OrderBy(w => Array.IndexOf(courseWidgetNames, w.Name))
            .ToListAsync();
        for (int i = 0; i < courseWidgets.Count; i++)
            _context.PageWidgets.Add(new PageWidget
            {
                PageId = coursePage.Id,
                WidgetId = courseWidgets[i].Id,
                Position = i,
                Width = i < 4 ? 3 : 6
            });
        await _context.SaveChangesAsync();

        // News demo page
        var newsPage = new Page
        {
            TenantId = demoTenantId,
            Title = "VietNews - News Analytics",
            Slug = "news",
            Description = "Dashboard phân tích tin tức: lượt xem, bài viết, độc giả và tác giả",
            IsActive = true,
            CreatedBy = "system"
        };
        _context.Pages.Add(newsPage);
        await _context.SaveChangesAsync();

        var newsWidgetNames = new[]
        {
            "news_total_views_today", "news_articles_published_today",
            "news_new_readers_today", "news_read_completion_rate",
            "news_views_by_category", "news_popular_articles_week",
            "news_traffic_sources", "news_recent_reader_activity"
        };
        var newsWidgets = await _context.Widgets.IgnoreQueryFilters()
            .Where(w => newsWidgetNames.Contains(w.Name))
            .OrderBy(w => Array.IndexOf(newsWidgetNames, w.Name))
            .ToListAsync();
        for (int i = 0; i < newsWidgets.Count; i++)
            _context.PageWidgets.Add(new PageWidget
            {
                PageId = newsPage.Id,
                WidgetId = newsWidgets[i].Id,
                Position = i,
                Width = i < 4 ? 3 : 6
            });
        await _context.SaveChangesAsync();
    }
}
