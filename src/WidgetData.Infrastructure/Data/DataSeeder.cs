using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
        await ReconcileMigrationHistoryAsync();
        await _context.Database.MigrateAsync();

        var adminSeed = await LoadAdminSeedAsync();

        await EnsureRolesAsync(adminSeed.Roles);

        var tenantBySlug = await EnsureTenantsAsync(adminSeed.Tenants);
        await EnsureUsersAsync(adminSeed.Users, tenantBySlug);

        if (!tenantBySlug.TryGetValue("demo", out var demoTenant))
            throw new InvalidOperationException("Missing required 'demo' tenant in admin seed data.");

        var salesJsonPath = Path.Combine(AppContext.BaseDirectory, "demo-sales.json");
        var courseJsonPath = Path.Combine(AppContext.BaseDirectory, "demo-course.json");
        var newsJsonPath = Path.Combine(AppContext.BaseDirectory, "demo-news.json");

        EnsureDemoJsonFile(salesJsonPath, GetSalesDemoJson());
        EnsureDemoJsonFile(courseJsonPath, GetCourseDemoJson());
        EnsureDemoJsonFile(newsJsonPath, GetNewsDemoJson());
        await EnsureDemoSourcesUseJsonFilesAsync(salesJsonPath, courseJsonPath, newsJsonPath);

        if (!await _context.DataSources.AnyAsync())
        {
            var dsSales = new DataSource
            {
                Name = "Cửa hàng - Sales Data",
                SourceType = DataSourceType.Json,
                Description = "Dữ liệu JSON demo cho bán hàng, khách hàng, sản phẩm và thanh toán",
                FileStoragePath = salesJsonPath,
                OriginalFileName = "demo-sales.json",
                StoredFileName = "demo-sales.json",
                FileContentType = "application/json",
                FileSizeBytes = new FileInfo(salesJsonPath).Length,
                FileUploadedAt = DateTime.UtcNow,
                FileUploadedBy = "system",
                IsActive = true,
                CreatedBy = "system",
                LastTestedAt = DateTime.UtcNow,
                LastTestResult = "Connection successful"
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
                LastTestResult = "Connection successful"
            };
            var dsCourse = new DataSource
            {
                Name = "EduViet - Course Data",
                SourceType = DataSourceType.Json,
                Description = "Dữ liệu JSON demo cho nền tảng học trực tuyến EduViet",
                FileStoragePath = courseJsonPath,
                OriginalFileName = "demo-course.json",
                StoredFileName = "demo-course.json",
                FileContentType = "application/json",
                FileSizeBytes = new FileInfo(courseJsonPath).Length,
                FileUploadedAt = DateTime.UtcNow,
                FileUploadedBy = "system",
                IsActive = true,
                CreatedBy = "system",
                LastTestedAt = DateTime.UtcNow,
                LastTestResult = "Connection successful"
            };
            var dsNews = new DataSource
            {
                Name = "VietNews - News Data",
                SourceType = DataSourceType.Json,
                Description = "Dữ liệu JSON demo cho cổng tin tức VietNews",
                FileStoragePath = newsJsonPath,
                OriginalFileName = "demo-news.json",
                StoredFileName = "demo-news.json",
                FileContentType = "application/json",
                FileSizeBytes = new FileInfo(newsJsonPath).Length,
                FileUploadedAt = DateTime.UtcNow,
                FileUploadedBy = "system",
                IsActive = true,
                CreatedBy = "system",
                LastTestedAt = DateTime.UtcNow,
                LastTestResult = "Connection successful"
            };
            _context.DataSources.AddRange(dsSales, dsApi, dsCourse, dsNews);
            await _context.SaveChangesAsync();

            // --- Widget Groups (Report Pages) ---
            var grpOverview = new WidgetGroup
            {
                Name = "Tổng quan doanh thu",
                Description = "Dashboard tổng quan về doanh thu, đơn hàng và hiệu suất kinh doanh",
                IsActive = true,
                CreatedBy = "system"
            };
            var grpProducts = new WidgetGroup
            {
                Name = "Báo cáo sản phẩm",
                Description = "Phân tích doanh số theo sản phẩm và danh mục",
                IsActive = true,
                CreatedBy = "system"
            };
            var grpCustomers = new WidgetGroup
            {
                Name = "Báo cáo khách hàng",
                Description = "Thống kê khách hàng, doanh thu theo khách hàng",
                IsActive = true,
                CreatedBy = "system"
            };
            var grpPayments = new WidgetGroup
            {
                Name = "Báo cáo thanh toán",
                Description = "Phân tích phương thức thanh toán và trạng thái giao dịch",
                IsActive = true,
                CreatedBy = "system"
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
                // Week 5 ago
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Nguyễn Văn An\",\"email\":\"an.nguyen@gmail.com\",\"dien_thoai\":\"0912345678\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Tôi muốn hỏi về gói Pro, cần thêm thông tin về giới hạn widget và nguồn dữ liệu.\"}", IpAddress = "192.168.1.10", SubmittedAt = DateTime.UtcNow.AddDays(-35) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Vũ Thị Lan\",\"email\":\"lan.vu@outlook.com\",\"dien_thoai\":\"0911223344\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Công ty tôi đang dùng Google Data Studio nhưng muốn chuyển sang WidgetData vì cần kết nối nhiều CSDL hơn.\"}", IpAddress = "113.161.50.12", SubmittedAt = DateTime.UtcNow.AddDays(-33) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Đặng Quốc Hùng\",\"email\":\"hung.dang@enterprise.vn\",\"dien_thoai\":\"0938877665\",\"chu_de\":\"Hợp tác kinh doanh\",\"noi_dung\":\"Chúng tôi là đại lý phần mềm tại miền Trung, muốn thảo luận về chương trình đối tác reseller.\"}", IpAddress = "203.113.10.5", SubmittedAt = DateTime.UtcNow.AddDays(-30) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Bùi Thị Thanh\",\"email\":\"thanh.bui@school.edu.vn\",\"dien_thoai\":\"0356789012\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Trường chúng tôi muốn dùng WidgetData để theo dõi điểm số và tiến độ học sinh, xin tư vấn gói phù hợp.\"}", IpAddress = "118.70.200.30", SubmittedAt = DateTime.UtcNow.AddDays(-28) },
                // Week 4 ago
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Trần Thị Mai\",\"email\":\"mai.tran@company.vn\",\"dien_thoai\":\"0987654321\",\"chu_de\":\"Hỗ trợ kỹ thuật\",\"noi_dung\":\"Widget biểu đồ không hiển thị đúng trên thiết bị mobile, cần hỗ trợ xử lý gấp.\"}", IpAddress = "10.0.0.5", SubmittedAt = DateTime.UtcNow.AddDays(-25) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Ngô Tuấn Anh\",\"email\":\"tuananh.ngo@fintech.io\",\"dien_thoai\":\"0909123456\",\"chu_de\":\"Hỗ trợ kỹ thuật\",\"noi_dung\":\"API execute widget trả về lỗi 500 khi query có JOIN nhiều bảng, vui lòng kiểm tra giúp tôi.\"}", IpAddress = "14.232.80.9", SubmittedAt = DateTime.UtcNow.AddDays(-24) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Phạm Thị Ngọc\",\"email\":\"ngoc.pham@retail.vn\",\"dien_thoai\":\"0345111222\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Doanh nghiệp bán lẻ 20 cửa hàng, cần dashboard tổng hợp doanh thu toàn hệ thống, có giải pháp không?\"}", IpAddress = "42.112.55.10", SubmittedAt = DateTime.UtcNow.AddDays(-22) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Hoàng Anh Khoa\",\"email\":\"khoa.hoang@logistic.com\",\"dien_thoai\":\"\",\"chu_de\":\"Khác\",\"noi_dung\":\"Tôi cần tích hợp WidgetData với hệ thống ERP SAP của công ty. API có hỗ trợ OAuth2 không?\"}", IpAddress = "27.72.30.15", SubmittedAt = DateTime.UtcNow.AddDays(-20) },
                // Week 3 ago
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Lê Văn Đức\",\"email\":\"duc.le@startup.io\",\"dien_thoai\":\"\",\"chu_de\":\"Hợp tác kinh doanh\",\"noi_dung\":\"Chúng tôi muốn tích hợp WidgetData vào nền tảng SaaS của mình, muốn trao đổi về khả năng cung cấp API white-label.\"}", IpAddress = "203.113.20.5", SubmittedAt = DateTime.UtcNow.AddDays(-18) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Trương Minh Khánh\",\"email\":\"khanh.truong@hospital.vn\",\"dien_thoai\":\"0912000111\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Bệnh viện chúng tôi cần BI dashboard cho phòng kế toán và phòng khám, dữ liệu MSSQL Server.\"}", IpAddress = "14.161.20.5", SubmittedAt = DateTime.UtcNow.AddDays(-17) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Đinh Thị Hằng\",\"email\":\"hang.dinh@ecommerce.vn\",\"dien_thoai\":\"0976543210\",\"chu_de\":\"Hỗ trợ kỹ thuật\",\"noi_dung\":\"Embedding widget lên website của tôi bị lỗi CORS, đã thêm domain vào whitelist nhưng vẫn không được.\"}", IpAddress = "113.161.88.7", SubmittedAt = DateTime.UtcNow.AddDays(-15) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Nguyễn Đức Thịnh\",\"email\":\"thinh.nguyen@bank.com\",\"dien_thoai\":\"0901555666\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Ngân hàng cần giải pháp BI đáp ứng yêu cầu bảo mật cao (ISO 27001). WidgetData có chứng chỉ nào không?\"}", IpAddress = "203.162.5.2", SubmittedAt = DateTime.UtcNow.AddDays(-14) },
                // Week 2 ago
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Cao Thị Hương\",\"email\":\"huong.cao@media.vn\",\"dien_thoai\":\"0988001122\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Công ty truyền thông muốn theo dõi KPI biên tập: lượt đọc, chia sẻ, thời gian đọc theo tác giả.\"}", IpAddress = "27.68.100.20", SubmittedAt = DateTime.UtcNow.AddDays(-12) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Phạm Thị Hoa\",\"email\":\"hoa.pham@gmail.com\",\"dien_thoai\":\"0345678901\",\"chu_de\":\"Khiếu nại đơn hàng\",\"noi_dung\":\"Tôi chưa nhận được email xác nhận đăng ký gói Pro sau khi thanh toán thành công.\"}", IpAddress = "118.70.100.22", SubmittedAt = DateTime.UtcNow.AddDays(-10) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Lý Thanh Sơn\",\"email\":\"son.ly@manufacturing.vn\",\"dien_thoai\":\"0912334455\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Nhà máy sản xuất cần monitor OEE, sản lượng và tỷ lệ lỗi theo ca trực tiếp từ CSDL MySQL.\"}", IpAddress = "116.109.200.5", SubmittedAt = DateTime.UtcNow.AddDays(-9) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Võ Thị Kim Anh\",\"email\":\"kimanh.vo@healthcare.vn\",\"dien_thoai\":\"0966778899\",\"chu_de\":\"Hỗ trợ kỹ thuật\",\"noi_dung\":\"Widget dạng table hiển thị ký tự tiếng Việt bị lỗi encoding khi xuất CSV. Vui lòng hỗ trợ.\"}", IpAddress = "14.232.150.33", SubmittedAt = DateTime.UtcNow.AddDays(-8) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Hà Văn Long\",\"email\":\"long.ha@agriculture.vn\",\"dien_thoai\":\"0978112233\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Cần dashboard theo dõi mùa vụ, sản lượng thu hoạch và giá cả thị trường nông sản theo vùng.\"}", IpAddress = "42.116.10.8", SubmittedAt = DateTime.UtcNow.AddDays(-7) },
                // Last week
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Hoàng Văn Minh\",\"email\":\"minh.hoang@techcorp.vn\",\"dien_thoai\":\"0901234567\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Muốn tư vấn về gói Business cho doanh nghiệp 50+ người dùng, cần SLA rõ ràng và khả năng deploy on-premise.\"}", IpAddress = "14.232.30.100", SubmittedAt = DateTime.UtcNow.AddDays(-5) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Nguyễn Thị Thanh Hà\",\"email\":\"ha.nguyen@insurance.vn\",\"dien_thoai\":\"0912667788\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Công ty bảo hiểm muốn visualize dữ liệu hợp đồng, bồi thường và KPI đại lý theo vùng.\"}", IpAddress = "103.28.100.7", SubmittedAt = DateTime.UtcNow.AddDays(-4) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Trần Quốc Bảo\",\"email\":\"bao.tran@edu.vn\",\"dien_thoai\":\"0938556677\",\"chu_de\":\"Hỗ trợ kỹ thuật\",\"noi_dung\":\"Tôi không thể tạo thêm widget sau khi đạt giới hạn gói Free. Làm cách nào để nâng cấp tài khoản?\"}", IpAddress = "171.240.50.9", SubmittedAt = DateTime.UtcNow.AddDays(-3) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Phan Thị Minh Châu\",\"email\":\"chau.phan@beauty.vn\",\"dien_thoai\":\"0945321987\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Chuỗi mỹ phẩm 5 chi nhánh muốn xem doanh thu, tồn kho và top sản phẩm bán chạy real-time.\"}", IpAddress = "14.161.70.4", SubmittedAt = DateTime.UtcNow.AddDays(-2) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Đỗ Văn Dương\",\"email\":\"duong.do@construction.vn\",\"dien_thoai\":\"0977889900\",\"chu_de\":\"Hợp tác kinh doanh\",\"noi_dung\":\"Công ty xây dựng muốn tích hợp WidgetData vào hệ thống quản lý dự án để theo dõi tiến độ và chi phí.\"}", IpAddress = "27.72.90.11", SubmittedAt = DateTime.UtcNow.AddDays(-1) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Lương Thị Bích Ngọc\",\"email\":\"ngoc.luong@travel.vn\",\"dien_thoai\":\"0901778899\",\"chu_de\":\"Khác\",\"noi_dung\":\"Công ty du lịch muốn theo dõi đặt tour, doanh thu theo điểm đến và đánh giá khách hàng.\"}", IpAddress = "118.70.60.15", SubmittedAt = DateTime.UtcNow.AddHours(-6) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Lê Xuân Trường\",\"email\":\"truong.le@fintech.vn\",\"dien_thoai\":\"0912445566\",\"chu_de\":\"Hỗ trợ kỹ thuật\",\"noi_dung\":\"Scheduled widget chạy lúc 00:00 UTC nhưng timezone Việt Nam là +7, lịch chạy bị lệch 7 tiếng.\"}", IpAddress = "42.112.20.8", SubmittedAt = DateTime.UtcNow.AddHours(-3) },
                new FormSubmission { WidgetId = wContactForm.Id, Data = "{\"ho_ten\":\"Trịnh Thị Lan Anh\",\"email\":\"lananh.trinh@startup.vn\",\"dien_thoai\":\"0966334455\",\"chu_de\":\"Tư vấn sản phẩm\",\"noi_dung\":\"Startup của tôi muốn embed widget dashboard vào app mobile React Native. API có CORS hỗ trợ không?\"}", IpAddress = "171.244.80.5", SubmittedAt = DateTime.UtcNow.AddHours(-1) }
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
                // Email delivery history (30 days)
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-30) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-29) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Failed,  Message = "SMTP connection timeout after 30s", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-28) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-27) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-26) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-25) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Failed,  Message = "Authentication failed: Invalid credentials", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-20) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Failed,  Message = "Authentication failed: Invalid credentials", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-19) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-18) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "manual", ExecutedAt = DateTime.UtcNow.AddDays(-15) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-10) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-5) },
                new DeliveryExecution { DeliveryTargetId = dtEmail.Id, Status = ExecutionStatus.Success, Message = "Đã gửi đến 2 người nhận", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-1) },
                // Telegram delivery history
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-30) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-25) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Failed,  Message = "Telegram API error 403: bot was blocked by the user", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-22) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-20) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-15) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "manual",    ExecutedAt = DateTime.UtcNow.AddDays(-12) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-10) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-7) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Failed,  Message = "Network timeout after 10s — retrying next run", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-4) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-3) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-2) },
                new DeliveryExecution { DeliveryTargetId = dtTelegram.Id, Status = ExecutionStatus.Success, Message = "Message sent successfully", TriggeredBy = "manual",    ExecutedAt = DateTime.UtcNow.AddHours(-5) },
                // CSV/FTP delivery history (disabled target — shows some historical runs before disabled)
                new DeliveryExecution { DeliveryTargetId = dtCsv.Id, Status = ExecutionStatus.Success, Message = "File exported: /reports/top-products.csv (42KB)", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-14) },
                new DeliveryExecution { DeliveryTargetId = dtCsv.Id, Status = ExecutionStatus.Failed,  Message = "FTP connection refused: host unreachable", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-13) },
                new DeliveryExecution { DeliveryTargetId = dtCsv.Id, Status = ExecutionStatus.Failed,  Message = "FTP connection refused: host unreachable", TriggeredBy = "scheduler", ExecutedAt = DateTime.UtcNow.AddDays(-12) },
                new DeliveryExecution { DeliveryTargetId = dtCsv.Id, Status = ExecutionStatus.Success, Message = "File exported: /reports/top-products.csv (45KB)", TriggeredBy = "manual",    ExecutedAt = DateTime.UtcNow.AddDays(-11) }
            );
            await _context.SaveChangesAsync();

            // ── Config Archives ──────────────────────────────────────────────
            _context.WidgetConfigArchives.AddRange(
                // monthly_revenue_trend history (3 versions)
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
                // revenue_by_category_chart history (2 versions)
                new WidgetConfigArchive
                {
                    WidgetId = wRevenueByCategoryChart.Id,
                    Configuration = "{\"query\":\"SELECT c.name as category, ROUND(SUM(oi.line_total),0) as revenue FROM order_items oi JOIN products p ON oi.product_id=p.id JOIN categories c ON p.category_id=c.id GROUP BY c.name ORDER BY revenue DESC\"}",
                    ChartConfig = "{\"type\":\"Pie\",\"xAxis\":\"category\",\"yAxis\":\"revenue\",\"seriesLabel\":\"Doanh thu\"}",
                    Note = "Phiên bản Pie chart (trước khi đổi sang Bar)",
                    TriggerSource = "OnSave",
                    ArchivedBy = "system",
                    ArchivedAt = DateTime.UtcNow.AddDays(-14)
                },
                new WidgetConfigArchive
                {
                    WidgetId = wRevenueByCategoryChart.Id,
                    Configuration = "{\"query\":\"SELECT c.name as category, ROUND(SUM(oi.line_total),0) as revenue FROM order_items oi JOIN products p ON oi.product_id=p.id JOIN categories c ON p.category_id=c.id JOIN orders o ON oi.order_id=o.id WHERE o.status='completed' GROUP BY c.name ORDER BY revenue DESC\"}",
                    ChartConfig = "{\"type\":\"Bar\",\"xAxis\":\"category\",\"yAxis\":\"revenue\",\"seriesLabel\":\"Doanh thu\"}",
                    Note = "Lọc chỉ đơn hàng completed, đổi sang Bar chart",
                    TriggerSource = "Manual",
                    ArchivedBy = "admin@widgetdata.com",
                    ArchivedAt = DateTime.UtcNow.AddDays(-5)
                },
                // top_products_by_revenue history (2 versions)
                new WidgetConfigArchive
                {
                    WidgetId = wTopProducts.Id,
                    Configuration = "{\"query\":\"SELECT p.name as 'Sản phẩm', SUM(oi.quantity) as 'SL bán', ROUND(SUM(oi.line_total),0) as 'Doanh thu' FROM order_items oi JOIN products p ON oi.product_id=p.id JOIN orders o ON oi.order_id=o.id WHERE o.status='completed' GROUP BY p.id ORDER BY SUM(oi.line_total) DESC LIMIT 5\"}",
                    Note = "Phiên bản v1 — Top 5, chưa có danh mục",
                    TriggerSource = "OnSave",
                    ArchivedBy = "system",
                    ArchivedAt = DateTime.UtcNow.AddDays(-21)
                },
                new WidgetConfigArchive
                {
                    WidgetId = wTopProducts.Id,
                    Configuration = "{\"query\":\"SELECT p.name as 'Sản phẩm', c.name as 'Danh mục', SUM(oi.quantity) as 'SL bán', ROUND(SUM(oi.line_total),0) as 'Doanh thu' FROM order_items oi JOIN products p ON oi.product_id=p.id JOIN categories c ON p.category_id=c.id JOIN orders o ON oi.order_id=o.id WHERE o.status='completed' GROUP BY p.id ORDER BY SUM(oi.line_total) DESC LIMIT 10\"}",
                    Note = "Thêm cột Danh mục, tăng giới hạn lên Top 10",
                    TriggerSource = "Manual",
                    ArchivedBy = "manager@widgetdata.com",
                    ArchivedAt = DateTime.UtcNow.AddDays(-10)
                },
                // order_status_summary history
                new WidgetConfigArchive
                {
                    WidgetId = wOrderStatusTable.Id,
                    Configuration = "{\"query\":\"SELECT status as 'Trạng thái', COUNT(*) as 'Số đơn' FROM orders GROUP BY status ORDER BY COUNT(*) DESC\"}",
                    Note = "Phiên bản đơn giản — chưa tính tổng tiền",
                    TriggerSource = "OnSave",
                    ArchivedBy = "system",
                    ArchivedAt = DateTime.UtcNow.AddDays(-18)
                },
                // payment_method_distribution history
                new WidgetConfigArchive
                {
                    WidgetId = wPaymentMethodChart.Id,
                    Configuration = "{\"query\":\"SELECT payment_method as label, COUNT(*) as value FROM payments GROUP BY payment_method ORDER BY COUNT(*) DESC\"}",
                    ChartConfig = "{\"type\":\"Donut\",\"xAxis\":\"label\",\"yAxis\":\"value\",\"seriesLabel\":\"Giao dịch\"}",
                    Note = "Phiên bản Donut, chưa lọc status=success",
                    TriggerSource = "OnSave",
                    ArchivedBy = "system",
                    ArchivedAt = DateTime.UtcNow.AddDays(-16)
                },
                // customer_by_city history
                new WidgetConfigArchive
                {
                    WidgetId = wCustomerByCity.Id,
                    Configuration = "{\"query\":\"SELECT city as label, COUNT(*) as value FROM customers GROUP BY city ORDER BY COUNT(*) DESC LIMIT 5\"}",
                    ChartConfig = "{\"type\":\"Pie\",\"xAxis\":\"label\",\"yAxis\":\"value\",\"seriesLabel\":\"Khách hàng\"}",
                    Note = "Giới hạn Top 5 thành phố",
                    TriggerSource = "Manual",
                    ArchivedBy = "admin@widgetdata.com",
                    ArchivedAt = DateTime.UtcNow.AddDays(-9)
                },
                // low_stock_products history
                new WidgetConfigArchive
                {
                    WidgetId = wLowStock.Id,
                    Configuration = "{\"query\":\"SELECT p.sku as 'SKU', p.name as 'Sản phẩm', p.stock_quantity as 'Tồn kho' FROM products p WHERE p.stock_quantity < 100 ORDER BY p.stock_quantity ASC LIMIT 20\"}",
                    Note = "Phiên bản v1 — ngưỡng < 100 (đã giảm xuống < 50)",
                    TriggerSource = "Manual",
                    ArchivedBy = "manager@widgetdata.com",
                    ArchivedAt = DateTime.UtcNow.AddDays(-25)
                }
            );
            await _context.SaveChangesAsync();

            // ── Widget API Activity Logs ─────────────────────────────────────
            var actRand = new Random(77);
            var activityWidgets = new[] {
                wTotalRevenue, wTotalOrders, wAvgOrder, wTotalCustomers,
                wMonthlyRevenueTrend, wRevenueByCategoryChart, wOrderStatusTable,
                wTopProducts, wProductSalesByCategory, wLowStock, wDailyOrders,
                wTopCustomers, wCustomerByCity, wRecentOrders,
                wPaymentMethodChart, wDailyPaymentTrend, wPaymentSummaryTable, wFailedPayments
            };
            var activities = new List<WidgetApiActivity>();
            foreach (var w in activityWidgets)
            {
                for (int i = 0; i < 40; i++)
                {
                    activities.Add(new WidgetApiActivity
                    {
                        WidgetId = w.Id,
                        ApiEndpoint = $"/api/widgets/{w.Id}/execute",
                        UserId = actRand.Next(0, 4) == 0 ? null : $"user-demo-{actRand.Next(1, 5)}",
                        CalledAt = DateTime.UtcNow.AddHours(-actRand.Next(1, 720)),
                        ResponseTimeMs = actRand.Next(40, 650),
                        StatusCode = actRand.Next(0, 12) == 0 ? 500 : (actRand.Next(0, 20) == 0 ? 429 : 200)
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

            var ideaPost1 = new IdeaPost { WidgetId = wTopProducts.Id, Title = "Thêm bộ lọc theo khoảng giá", Content = "Khách hàng muốn lọc sản phẩm theo khoảng giá (dưới 500K, 500K-1M, trên 1M) để tìm kiếm nhanh hơn.", Labels = "feedback,suggestion", Status = "Processed", CreatedBy = "customer@example.com", CreatedAt = DateTime.UtcNow.AddDays(-30), ProcessedAt = DateTime.UtcNow.AddDays(-28) };
            var ideaPost2 = new IdeaPost { WidgetId = wTopProducts.Id, Title = "Hiển thị đánh giá sao sản phẩm", Content = "Thêm cột điểm đánh giá trung bình (⭐) vào widget top sản phẩm để khách dễ so sánh.", Labels = "suggestion", Status = "Pending", CreatedBy = "admin@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-20) };
            var ideaPost3 = new IdeaPost { WidgetId = wTopProducts.Id, Title = "Tồn kho hiển thị màu cảnh báo", Content = "Sản phẩm có tồn kho < 10 nên được tô màu đỏ, 10-50 màu vàng để nhân viên dễ theo dõi.", Labels = "feedback", Status = "Processed", CreatedBy = "manager@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-18), ProcessedAt = DateTime.UtcNow.AddDays(-16) };
            var ideaPost4 = new IdeaPost { WidgetId = wMonthlyRevenueTrend.Id, Title = "So sánh doanh thu cùng kỳ năm trước", Content = "Biểu đồ doanh thu nên có thêm đường so sánh năm trước để dễ thấy tăng trưởng hay sụt giảm.", Labels = "suggestion,enhancement", Status = "Pending", CreatedBy = "manager@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-15) };
            var ideaPost5 = new IdeaPost { WidgetId = wMonthlyRevenueTrend.Id, Title = "Xuất biểu đồ dạng PNG/PDF", Content = "Cần nút tải biểu đồ xuống dạng hình ảnh để đưa vào báo cáo PowerPoint.", Labels = "suggestion", Status = "Processed", CreatedBy = "admin@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-14), ProcessedAt = DateTime.UtcNow.AddDays(-12) };
            var ideaPost6 = new IdeaPost { WidgetId = wRecentOrders.Id, Title = "Thêm nút xem chi tiết đơn hàng", Content = "Từ bảng đơn hàng gần đây, cần click để xem toàn bộ sản phẩm trong đơn.", Labels = "feedback,enhancement", Status = "Pending", CreatedBy = "user@shop.demo", CreatedAt = DateTime.UtcNow.AddDays(-12) };
            var ideaPost7 = new IdeaPost { WidgetId = wRecentOrders.Id, Title = "Lọc đơn hàng theo trạng thái real-time", Content = "Muốn filter đơn hàng theo trạng thái (pending/completed/cancelled) ngay trên bảng mà không cần reload.", Labels = "suggestion", Status = "Pending", CreatedBy = "admin@shop.demo", CreatedAt = DateTime.UtcNow.AddDays(-10) };
            var ideaPost8 = new IdeaPost { WidgetId = wCustomerByCity.Id, Title = "Thêm bản đồ Việt Nam theo tỉnh thành", Content = "Biểu đồ phân bổ khách hàng dạng choropleth map sẽ trực quan hơn Donut chart hiện tại.", Labels = "suggestion,enhancement", Status = "Processed", CreatedBy = "manager@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-9), ProcessedAt = DateTime.UtcNow.AddDays(-7) };
            var ideaPost9 = new IdeaPost { WidgetId = wPaymentMethodChart.Id, Title = "Hiển thị tỷ lệ phần trăm trong biểu đồ tròn", Content = "Label trên Pie chart nên hiển thị cả số tuyệt đối và phần trăm để dễ đọc.", Labels = "feedback", Status = "Pending", CreatedBy = "dev@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-7) };
            var ideaPost10 = new IdeaPost { WidgetId = wTopProducts.Id, Title = "Tích hợp ảnh sản phẩm trong bảng", Content = "Nếu URL ảnh có trong CSDL, widget nên hiển thị thumbnail nhỏ cạnh tên sản phẩm.", Labels = "suggestion", Status = "Pending", CreatedBy = "admin@shop.demo", CreatedAt = DateTime.UtcNow.AddDays(-5) };
            var ideaPost11 = new IdeaPost { WidgetId = wDailyOrders.Id, Title = "Cảnh báo khi đơn hàng giảm đột ngột", Content = "Nếu số đơn trong ngày thấp hơn 30% so với trung bình 7 ngày, nên tô đỏ và gửi Telegram.", Labels = "feedback,suggestion", Status = "Pending", CreatedBy = "manager@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-3) };
            var ideaPost12 = new IdeaPost { WidgetId = wLowStock.Id, Title = "Tự động tạo đơn đặt hàng khi tồn kho thấp", Content = "Khi sản phẩm dưới ngưỡng, hệ thống nên tự tạo draft PO gửi email cho nhà cung cấp.", Labels = "enhancement", Status = "Pending", CreatedBy = "admin@widgetdata.com", CreatedAt = DateTime.UtcNow.AddDays(-1) };
            _context.IdeaPosts.AddRange(ideaPost1, ideaPost2, ideaPost3, ideaPost4, ideaPost5, ideaPost6, ideaPost7, ideaPost8, ideaPost9, ideaPost10, ideaPost11, ideaPost12);
            await _context.SaveChangesAsync();

            _context.IdeaResults.AddRange(
                new IdeaResult { IdeaPostId = ideaPost1.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"acknowledged\",\"assignedTo\":\"dev@widgetdata.com\",\"note\":\"Sẽ triển khai bộ lọc giá trong sprint Q3-2026\"}", Status = "Processed", CreatedAt = DateTime.UtcNow.AddDays(-28) },
                new IdeaResult { IdeaPostId = ideaPost3.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"acknowledged\",\"note\":\"Đã ghi nhận, sẽ thêm conditional formatting trong UI update tháng 6\"}", Status = "Received", CreatedAt = DateTime.UtcNow.AddDays(-16) },
                new IdeaResult { IdeaPostId = ideaPost5.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"implemented\",\"note\":\"Đã thêm nút Export PNG/PDF trong toolbar của widget chart\",\"releasedIn\":\"v1.4.0\"}", Status = "Processed", CreatedAt = DateTime.UtcNow.AddDays(-12) },
                new IdeaResult { IdeaPostId = ideaPost8.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"in-progress\",\"assignedTo\":\"dev@widgetdata.com\",\"note\":\"Đang nghiên cứu thư viện Leaflet.js cho map Việt Nam, dự kiến Q4-2026\"}", Status = "Received", CreatedAt = DateTime.UtcNow.AddDays(-7) },
                new IdeaResult { IdeaPostId = ideaPost2.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"backlog\",\"note\":\"Ghi nhận vào backlog Q4-2026, cần schema rating trong CSDL\"}", Status = "Received", CreatedAt = DateTime.UtcNow.AddDays(-18) },
                new IdeaResult { IdeaPostId = ideaPost4.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"backlog\",\"note\":\"Thêm tham số so sánh năm trước vào chart config — dự kiến v1.5\"}", Status = "Received", CreatedAt = DateTime.UtcNow.AddDays(-13) },
                new IdeaResult { IdeaPostId = ideaPost6.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"acknowledged\",\"note\":\"Cần hỗ trợ drill-down từ widget — đang thiết kế API\"}", Status = "Received", CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new IdeaResult { IdeaPostId = ideaPost9.Id, IdeaSubscriptionId = ideaSub.Id, ResultContent = "{\"status\":\"acknowledged\",\"note\":\"Sẽ cấu hình qua chartConfig.showPercent trong bản cập nhật tới\"}", Status = "Received", CreatedAt = DateTime.UtcNow.AddDays(-5) }
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
            await SeedDemoPagesAsync(demoTenant.Id, tenantBySlug);
        }

        await SeedAuditLogsAsync(adminSeed.AuditLogs);
    }

    private async Task SeedDemoPagesAsync(int demoTenantId, Dictionary<string, Tenant> tenantBySlug)
    {
        if (await _context.Pages.IgnoreQueryFilters().AnyAsync(p => p.TenantId == demoTenantId))
            return;

        // Sales demo page (demo tenant)
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

        // Course demo page (demo tenant)
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

        // News demo page (demo tenant)
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

        // ── Pages for shop / news / course / retail tenants ──────────────────
        // Each tenant gets its own set of pages that reuse the shared demo widgets.

        if (tenantBySlug.TryGetValue("shop", out var shopTenant))
            await SeedTenantPageAsync(shopTenant.Id, "shop-sales-dashboard", "Shop - Sales Dashboard",
                "Dashboard bán hàng cho Shop Tenant: doanh thu, đơn hàng và khách hàng",
                salesWidgetNames);

        if (tenantBySlug.TryGetValue("news", out var newsTenant))
            await SeedTenantPageAsync(newsTenant.Id, "vietnews-analytics", "VietNews - News Analytics",
                "Dashboard phân tích tin tức cho News Tenant: lượt xem, bài viết và độc giả",
                newsWidgetNames);

        if (tenantBySlug.TryGetValue("course", out var courseTenant))
            await SeedTenantPageAsync(courseTenant.Id, "eduviet-learning", "EduViet - Learning Dashboard",
                "Dashboard học trực tuyến cho Course Tenant: khóa học, học viên và tiến độ",
                courseWidgetNames);

        if (tenantBySlug.TryGetValue("retail", out var retailTenant))
            await SeedTenantPageAsync(retailTenant.Id, "retail-overview", "Retail - Overview Dashboard",
                "Dashboard tổng quan cho Retail Tenant: doanh thu, sản phẩm và thanh toán",
                salesWidgetNames);
    }

    private async Task SeedTenantPageAsync(int tenantId, string slug, string title, string description, string[] widgetNames)
    {
        if (await _context.Pages.IgnoreQueryFilters().AnyAsync(p => p.TenantId == tenantId))
            return;

        var page = new Page
        {
            TenantId = tenantId,
            Title = title,
            Slug = slug,
            Description = description,
            IsActive = true,
            CreatedBy = "system"
        };
        _context.Pages.Add(page);
        await _context.SaveChangesAsync();

        var widgets = await _context.Widgets.IgnoreQueryFilters()
            .Where(w => widgetNames.Contains(w.Name))
            .ToListAsync();
        var nameIndex = widgetNames
            .Select((name, idx) => (name, idx))
            .ToDictionary(x => x.name, x => x.idx);
        widgets = widgets
            .OrderBy(w => nameIndex.GetValueOrDefault(w.Name, int.MaxValue))
            .ToList();
        for (int i = 0; i < widgets.Count; i++)
            _context.PageWidgets.Add(new PageWidget
            {
                PageId = page.Id,
                WidgetId = widgets[i].Id,
                Position = i,
                Width = i < 4 ? 3 : 6
            });
        await _context.SaveChangesAsync();
    }

    private async Task EnsureDemoSourcesUseJsonFilesAsync(string salesJsonPath, string courseJsonPath, string newsJsonPath)
    {
        var demoSources = await _context.DataSources
            .Where(ds =>
                ds.Name == "Cửa hàng - Sales DB" ||
                ds.Name == "Cửa hàng - Sales Data" ||
                ds.Name == "EduViet - Course DB" ||
                ds.Name == "EduViet - Course Data" ||
                ds.Name == "VietNews - News DB" ||
                ds.Name == "VietNews - News Data")
            .ToListAsync();

        if (demoSources.Count == 0) return;

        foreach (var ds in demoSources)
        {
            if (ds.Name.StartsWith("Cửa hàng", StringComparison.OrdinalIgnoreCase))
            {
                ds.Name = "Cửa hàng - Sales Data";
                ds.SourceType = DataSourceType.Json;
                ds.Description = "Dữ liệu JSON demo cho bán hàng, khách hàng, sản phẩm và thanh toán";
                ds.FileStoragePath = salesJsonPath;
                ds.OriginalFileName = "demo-sales.json";
                ds.StoredFileName = "demo-sales.json";
                ds.FileContentType = "application/json";
                ds.ConnectionString = null;
            }
            else if (ds.Name.StartsWith("EduViet", StringComparison.OrdinalIgnoreCase))
            {
                ds.Name = "EduViet - Course Data";
                ds.SourceType = DataSourceType.Json;
                ds.Description = "Dữ liệu JSON demo cho nền tảng học trực tuyến EduViet";
                ds.FileStoragePath = courseJsonPath;
                ds.OriginalFileName = "demo-course.json";
                ds.StoredFileName = "demo-course.json";
                ds.FileContentType = "application/json";
                ds.ConnectionString = null;
            }
            else if (ds.Name.StartsWith("VietNews", StringComparison.OrdinalIgnoreCase))
            {
                ds.Name = "VietNews - News Data";
                ds.SourceType = DataSourceType.Json;
                ds.Description = "Dữ liệu JSON demo cho cổng tin tức VietNews";
                ds.FileStoragePath = newsJsonPath;
                ds.OriginalFileName = "demo-news.json";
                ds.StoredFileName = "demo-news.json";
                ds.FileContentType = "application/json";
                ds.ConnectionString = null;
            }

            ds.FileSizeBytes = new FileInfo(ds.FileStoragePath!).Length;
            ds.FileUploadedAt = DateTime.UtcNow;
            ds.FileUploadedBy = "system";
            ds.LastTestedAt = DateTime.UtcNow;
            ds.LastTestResult = "Connection successful";
        }

        await _context.SaveChangesAsync();
    }

    private static void EnsureDemoJsonFile(string filePath, string content)
    {
        if (File.Exists(filePath)) return;
        File.WriteAllText(filePath, content);
    }

    private static string GetSalesDemoJson() => """
[
  { "value": 125000000, "month": "2026-01", "revenue": 125000000, "category": "Điện thoại", "quantity": 420, "status": "completed", "payment_method": "bank_transfer", "amount": 125000000, "label": "Điện thoại", "city": "Hà Nội", "day": "01/15" },
  { "value": 119500000, "month": "2026-02", "revenue": 119500000, "category": "Laptop", "quantity": 250, "status": "completed", "payment_method": "card", "amount": 119500000, "label": "Laptop", "city": "TP. Hồ Chí Minh", "day": "02/15" },
  { "value": 132800000, "month": "2026-03", "revenue": 132800000, "category": "Phụ kiện", "quantity": 960, "status": "completed", "payment_method": "cash", "amount": 132800000, "label": "Phụ kiện", "city": "Đà Nẵng", "day": "03/15" },
  { "value": 141200000, "month": "2026-04", "revenue": 141200000, "category": "Máy tính bảng", "quantity": 340, "status": "completed", "payment_method": "e_wallet", "amount": 141200000, "label": "Máy tính bảng", "city": "Cần Thơ", "day": "04/15" },
  { "value": 138400000, "month": "2026-05", "revenue": 138400000, "category": "Thiết bị mạng", "quantity": 220, "status": "completed", "payment_method": "bank_transfer", "amount": 138400000, "label": "Thiết bị mạng", "city": "Hải Phòng", "day": "05/15" }
]
""";

    private static string GetCourseDemoJson() => """
[
  { "value": 3850, "month": "2026-01", "revenue": 385000000, "category": "Lập trình", "quantity": 520, "status": "active", "payment_method": "card", "amount": 385000000, "label": "Lập trình", "city": "Hà Nội", "day": "01/20" },
  { "value": 4020, "month": "2026-02", "revenue": 402000000, "category": "Thiết kế", "quantity": 470, "status": "active", "payment_method": "bank_transfer", "amount": 402000000, "label": "Thiết kế", "city": "TP. Hồ Chí Minh", "day": "02/20" },
  { "value": 4175, "month": "2026-03", "revenue": 417500000, "category": "Marketing", "quantity": 560, "status": "active", "payment_method": "e_wallet", "amount": 417500000, "label": "Marketing", "city": "Đà Nẵng", "day": "03/20" },
  { "value": 4380, "month": "2026-04", "revenue": 438000000, "category": "Data", "quantity": 610, "status": "active", "payment_method": "card", "amount": 438000000, "label": "Data", "city": "Huế", "day": "04/20" },
  { "value": 4510, "month": "2026-05", "revenue": 451000000, "category": "AI", "quantity": 645, "status": "active", "payment_method": "bank_transfer", "amount": 451000000, "label": "AI", "city": "Nha Trang", "day": "05/20" }
]
""";

    private static string GetNewsDemoJson() => """
[
  { "value": 188000, "month": "2026-01", "revenue": 188000, "category": "Thời sự", "quantity": 95, "status": "published", "payment_method": "organic", "amount": 188000, "label": "Thời sự", "city": "Hà Nội", "day": "01/10" },
  { "value": 201500, "month": "2026-02", "revenue": 201500, "category": "Kinh doanh", "quantity": 88, "status": "published", "payment_method": "social", "amount": 201500, "label": "Kinh doanh", "city": "TP. Hồ Chí Minh", "day": "02/10" },
  { "value": 213200, "month": "2026-03", "revenue": 213200, "category": "Công nghệ", "quantity": 102, "status": "published", "payment_method": "search", "amount": 213200, "label": "Công nghệ", "city": "Đà Nẵng", "day": "03/10" },
  { "value": 209700, "month": "2026-04", "revenue": 209700, "category": "Giáo dục", "quantity": 91, "status": "published", "payment_method": "direct", "amount": 209700, "label": "Giáo dục", "city": "Cần Thơ", "day": "04/10" },
  { "value": 227900, "month": "2026-05", "revenue": 227900, "category": "Thể thao", "quantity": 110, "status": "published", "payment_method": "organic", "amount": 227900, "label": "Thể thao", "city": "Hải Phòng", "day": "05/10" }
]
""";

    private static readonly JsonSerializerOptions SeedJsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string AdminSeedDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "admin");

    private async Task<AdminSeedData> LoadAdminSeedAsync()
    {
        var roles = await LoadSeedFileAsync<List<string>>("roles.json");
        var tenants = await LoadSeedFileAsync<List<AdminTenantSeed>>("tenants.json");
        var users = await LoadSeedFileAsync<List<AdminUserSeed>>("users.json");
        var auditLogs = await LoadSeedFileAsync<List<AdminAuditLogSeed>>("audit-logs.json");

        var normalizedRoles = NormalizeRoles(roles);
        var normalizedUsers = users
            .Select(user => user with { Roles = NormalizeRoles(user.Roles) })
            .ToList();

        return new AdminSeedData(
            normalizedRoles,
            tenants,
            normalizedUsers,
            auditLogs);
    }

    private static async Task<T> LoadSeedFileAsync<T>(string fileName) where T : class
    {
        var filePath = Path.Combine(AdminSeedDirectory, fileName);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Seed file not found: {filePath}", filePath);

        await using var stream = File.OpenRead(filePath);
        var data = await JsonSerializer.DeserializeAsync<T>(stream, SeedJsonOptions);
        return data ?? throw new InvalidOperationException($"Seed file '{fileName}' is empty or invalid JSON.");
    }

    private static List<string> NormalizeRoles(IEnumerable<string> roles)
        => roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task EnsureRolesAsync(IReadOnlyCollection<string> roles)
    {
        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task<Dictionary<string, Tenant>> EnsureTenantsAsync(IReadOnlyCollection<AdminTenantSeed> tenantSeeds)
    {
        var slugs = tenantSeeds
            .Select(t => t.Slug)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingTenants = await _context.Tenants
            .Where(t => slugs.Contains(t.Slug))
            .ToListAsync();

        var tenantBySlug = existingTenants.ToDictionary(t => t.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in tenantSeeds)
        {
            if (string.IsNullOrWhiteSpace(seed.Slug))
                continue;

            if (tenantBySlug.TryGetValue(seed.Slug, out var existing))
            {
                // Keep existing tenant metadata untouched; seed only creates missing tenants.
                continue;
            }

            var tenant = new Tenant
            {
                Name = seed.Name,
                Slug = seed.Slug,
                IsActive = seed.IsActive,
                Plan = seed.Plan,
                ContactEmail = seed.ContactEmail,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            tenantBySlug[tenant.Slug] = tenant;
        }

        await _context.SaveChangesAsync();

        return await _context.Tenants
            .Where(t => slugs.Contains(t.Slug))
            .ToDictionaryAsync(t => t.Slug, StringComparer.OrdinalIgnoreCase);
    }

    private async Task EnsureUsersAsync(IReadOnlyCollection<AdminUserSeed> userSeeds, IReadOnlyDictionary<string, Tenant> tenantBySlug)
    {
        foreach (var seed in userSeeds)
        {
            if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
                continue;

            var existingUser = await _userManager.FindByEmailAsync(seed.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    DisplayName = seed.DisplayName,
                    EmailConfirmed = true,
                    IsActive = seed.IsActive,
                    TenantId = ResolveTenantId(seed.TenantSlug, tenantBySlug),
                    LastLoginAt = seed.LastLoginDaysAgo.HasValue ? DateTime.UtcNow.AddDays(-seed.LastLoginDaysAgo.Value) : null
                };

                var createResult = await _userManager.CreateAsync(user, seed.Password);
                if (!createResult.Succeeded)
                    continue;

                existingUser = user;
            }

            foreach (var role in seed.Roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!await _userManager.IsInRoleAsync(existingUser, role))
                    await _userManager.AddToRoleAsync(existingUser, role);
            }
        }
    }

    private async Task SeedAuditLogsAsync(IReadOnlyCollection<AdminAuditLogSeed> auditLogSeeds)
    {
        if (await _context.AuditLogs.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var rand = new Random(42);

        var logs = auditLogSeeds.Select(seed => new AuditLog
        {
            Action = seed.Action,
            EntityType = seed.EntityType,
            EntityId = seed.EntityId,
            UserId = seed.UserId,
            UserEmail = seed.UserEmail,
            OldValues = seed.OldValues,
            NewValues = seed.NewValues,
            IpAddress = seed.RandomIp ? CreateRandomIp(rand) : seed.IpAddress,
            UserAgent = seed.UserAgent,
            Timestamp = now.AddDays(-seed.DaysAgo),
            Notes = seed.Notes
        }).ToList();

        _context.AuditLogs.AddRange(logs);
        await _context.SaveChangesAsync();
    }

    private static string CreateRandomIp(Random random)
        => $"{random.Next(1, 255)}.{random.Next(0, 255)}.{random.Next(0, 255)}.{random.Next(1, 255)}";

    private static int? ResolveTenantId(string? tenantSlug, IReadOnlyDictionary<string, Tenant> tenantBySlug)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            return null;

        return tenantBySlug.TryGetValue(tenantSlug, out var tenant) ? tenant.Id : null;
    }

    private sealed record AdminSeedData(
        List<string> Roles,
        List<AdminTenantSeed> Tenants,
        List<AdminUserSeed> Users,
        List<AdminAuditLogSeed> AuditLogs);

    private sealed record AdminTenantSeed
    {
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
        public string Plan { get; init; } = "free";
        public string? ContactEmail { get; init; }
    }

    private sealed record AdminUserSeed
    {
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
        public string? TenantSlug { get; init; }
        public int? LastLoginDaysAgo { get; init; }
        public List<string> Roles { get; init; } = [];
    }

    private sealed record AdminAuditLogSeed
    {
        public string Action { get; init; } = string.Empty;
        public string? EntityType { get; init; }
        public string? EntityId { get; init; }
        public string? UserId { get; init; }
        public string? UserEmail { get; init; }
        public string? OldValues { get; init; }
        public string? NewValues { get; init; }
        public string? IpAddress { get; init; }
        public bool RandomIp { get; init; }
        public string? UserAgent { get; init; }
        public int DaysAgo { get; init; }
        public string? Notes { get; init; }
    }

    /// <summary>
    /// Detects databases that were created outside of EF Core migrations (e.g., via EnsureCreated
    /// or from a previous migration set that was later squashed) and marks the corresponding
    /// migrations as applied in __EFMigrationsHistory so that MigrateAsync does not attempt
    /// to re-create tables that already exist.
    /// </summary>
    private async Task ReconcileMigrationHistoryAsync()
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var cmd = connection.CreateCommand();

        // Ensure the EF migrations history table exists
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            )
            """;
        await cmd.ExecuteNonQueryAsync();

        await EnsureWidgetApiActivitiesTableAsync(connection);

        // For each migration, if its characteristic schema object exists but the migration
        // is not yet recorded, mark it as applied so MigrateAsync will skip it.
        await TryMarkMigrationAppliedAsync(connection,
            "20260421161413_AddWidgetApiActivityAndInactivityFields",
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='WidgetApiActivities'");

        await TryMarkMigrationAppliedAsync(connection,
            "20260425170354_AddFormSubmissions",
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='FormSubmissions'");

        await TryMarkMigrationAppliedAsync(connection,
            "20260505164318_AddTenantAndPageSupport",
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Tenants'");

        await TryMarkMigrationAppliedAsync(connection,
            "20260507152721_AddOperationalIndexes",
            "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_WidgetExecutions_WidgetId_StartedAt'");
    }

    private static async Task EnsureWidgetApiActivitiesTableAsync(System.Data.Common.DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "WidgetApiActivities" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_WidgetApiActivities" PRIMARY KEY AUTOINCREMENT,
                "WidgetId" INTEGER NOT NULL,
                "ApiEndpoint" TEXT NOT NULL,
                "UserId" TEXT NULL,
                "CalledAt" TEXT NOT NULL,
                "ResponseTimeMs" INTEGER NULL,
                "StatusCode" INTEGER NOT NULL,
                CONSTRAINT "FK_WidgetApiActivities_Widgets_WidgetId" FOREIGN KEY ("WidgetId") REFERENCES "Widgets" ("Id") ON DELETE CASCADE
            );
            """;
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = """
            CREATE INDEX IF NOT EXISTS "IX_WidgetApiActivities_WidgetId_CalledAt"
            ON "WidgetApiActivities" ("WidgetId", "CalledAt");
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task TryMarkMigrationAppliedAsync(
        System.Data.Common.DbConnection connection,
        string migrationId,
        string schemaExistsQuery)
    {
        // Derive the EF Core product version from the assembly to keep history consistent
        var efAttr = Attribute.GetCustomAttribute(typeof(DbContext).Assembly,
            typeof(System.Reflection.AssemblyInformationalVersionAttribute))
            as System.Reflection.AssemblyInformationalVersionAttribute;
        var efVersion = efAttr?.InformationalVersion ?? "10.0.0";
        var plusIndex = efVersion.IndexOf('+');
        if (plusIndex >= 0) efVersion = efVersion[..plusIndex];

        using var cmd = connection.CreateCommand();

        // Skip if already recorded in history
        cmd.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = @migrationId";
        var p = cmd.CreateParameter();
        p.ParameterName = "@migrationId";
        p.Value = migrationId;
        cmd.Parameters.Add(p);
        var alreadyRecorded = (long)(await cmd.ExecuteScalarAsync())! > 0;
        if (alreadyRecorded) return;

        // Only mark as applied if the schema already exists in the database
        cmd.CommandText = schemaExistsQuery;
        cmd.Parameters.Clear();
        var schemaExists = (long)(await cmd.ExecuteScalarAsync())! > 0;
        if (!schemaExists) return;

        cmd.CommandText = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@migrationId, @productVersion)";
        var pId = cmd.CreateParameter();
        pId.ParameterName = "@migrationId";
        pId.Value = migrationId;
        cmd.Parameters.Add(pId);
        var pVer = cmd.CreateParameter();
        pVer.ParameterName = "@productVersion";
        pVer.Value = efVersion;
        cmd.Parameters.Add(pVer);
        await cmd.ExecuteNonQueryAsync();
    }
}
