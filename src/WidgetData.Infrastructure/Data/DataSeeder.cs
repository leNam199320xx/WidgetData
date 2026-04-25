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

        string[] roles = { "Admin", "Manager", "Developer", "Viewer" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!await _userManager.Users.AnyAsync())
        {
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

        // Ensure sales.db exists
        var salesDbPath = Path.Combine(AppContext.BaseDirectory, "sales.db");
        SalesDataSeeder.EnsureSalesDatabase(salesDbPath);

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
            _context.DataSources.AddRange(dsSales, dsApi);
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
        }
    }
}
