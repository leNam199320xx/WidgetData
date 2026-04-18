using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using WidgetData.Application.DTOs;

namespace WidgetData.API.Controllers;

/// <summary>
/// Public store API – exposes the seeded sales.db for the e-commerce frontend demo.
/// All endpoints are AllowAnonymous; no JWT required to browse the shop.
/// </summary>
[ApiController]
[Route("api/store")]
[AllowAnonymous]
public class StoreController : ControllerBase
{
    private static readonly HashSet<string> ValidPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "cash", "bank_transfer", "credit_card" };

    private readonly string _cs;

    public StoreController()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "sales.db");
        _cs = $"Data Source={dbPath}";
    }

    // ── GET /api/store/categories ─────────────────────────────

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        if (!System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory, "sales.db")))
            return Ok(new List<StoreCategoryDto>());

        var list = new List<StoreCategoryDto>();
        using var conn = new SqliteConnection(_cs);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, description FROM categories ORDER BY name";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new StoreCategoryDto
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
        return Ok(list);
    }

    // ── GET /api/store/products ───────────────────────────────

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        if (!System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory, "sales.db")))
            return Ok(new StoreProductListResponse());

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (page - 1) * pageSize;

        var whereParts = new List<string> { "p.is_active = 1" };
        if (categoryId.HasValue) whereParts.Add("p.category_id = @catId");
        if (!string.IsNullOrWhiteSpace(search))
            whereParts.Add("(LOWER(p.name) LIKE LOWER(@search) OR LOWER(p.sku) LIKE LOWER(@search) OR LOWER(c.name) LIKE LOWER(@search))");
        var where = "WHERE " + string.Join(" AND ", whereParts);

        using var conn = new SqliteConnection(_cs);
        await conn.OpenAsync();

        // Total count
        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = $"SELECT COUNT(*) FROM products p JOIN categories c ON p.category_id = c.id {where}";
        if (categoryId.HasValue) countCmd.Parameters.AddWithValue("@catId", categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@search", $"%{search}%");
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);

        // Rows
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT p.id, p.category_id, c.name, p.name, p.sku, p.description,
                   p.unit_price, p.stock_quantity, p.unit
            FROM products p
            JOIN categories c ON p.category_id = c.id
            {where}
            ORDER BY p.name
            LIMIT @limit OFFSET @offset";
        if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catId", categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@search", $"%{search}%");
        cmd.Parameters.AddWithValue("@limit", pageSize);
        cmd.Parameters.AddWithValue("@offset", offset);

        var items = new List<StoreProductDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new StoreProductDto
            {
                Id = reader.GetInt32(0),
                CategoryId = reader.GetInt32(1),
                CategoryName = reader.GetString(2),
                Name = reader.GetString(3),
                Sku = reader.GetString(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                Price = (decimal)reader.GetDouble(6),
                Stock = reader.GetInt32(7),
                Unit = reader.GetString(8),
                IsActive = true
            });
        }

        return Ok(new StoreProductListResponse
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    // ── GET /api/store/products/{id} ──────────────────────────

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        if (!System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory, "sales.db")))
            return NotFound();

        using var conn = new SqliteConnection(_cs);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT p.id, p.category_id, c.name, p.name, p.sku, p.description,
                   p.unit_price, p.stock_quantity, p.unit, p.is_active
            FROM products p
            JOIN categories c ON p.category_id = c.id
            WHERE p.id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return NotFound();

        return Ok(new StoreProductDto
        {
            Id = reader.GetInt32(0),
            CategoryId = reader.GetInt32(1),
            CategoryName = reader.GetString(2),
            Name = reader.GetString(3),
            Sku = reader.GetString(4),
            Description = reader.IsDBNull(5) ? null : reader.GetString(5),
            Price = (decimal)reader.GetDouble(6),
            Stock = reader.GetInt32(7),
            Unit = reader.GetString(8),
            IsActive = reader.GetInt32(9) == 1
        });
    }

    // ── POST /api/store/orders ────────────────────────────────

    [HttpPost("orders")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        if (!dto.Items.Any())
            return BadRequest(new { error = "Giỏ hàng trống" });
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            return BadRequest(new { error = "Tên khách hàng không được để trống" });
        if (string.IsNullOrWhiteSpace(dto.CustomerPhone))
            return BadRequest(new { error = "Số điện thoại không được để trống" });
        if (!ValidPaymentMethods.Contains(dto.PaymentMethod))
            return BadRequest(new { error = $"Phương thức thanh toán không hợp lệ. Chấp nhận: {string.Join(", ", ValidPaymentMethods)}" });

        if (!System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory, "sales.db")))
            return StatusCode(503, new { error = "Hệ thống đang khởi động, vui lòng thử lại" });

        using var conn = new SqliteConnection(_cs);
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            // Validate each product and calculate subtotal
            decimal subtotal = 0;
            var lineItems = new List<(int productId, int qty, decimal unitPrice)>();

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    return BadRequest(new { error = $"Số lượng không hợp lệ cho sản phẩm ID {item.ProductId}" });

                using var prodCmd = conn.CreateCommand();
                prodCmd.Transaction = tx;
                prodCmd.CommandText = "SELECT unit_price, stock_quantity, name FROM products WHERE id = @id AND is_active = 1";
                prodCmd.Parameters.AddWithValue("@id", item.ProductId);
                using var pr = await prodCmd.ExecuteReaderAsync();
                if (!await pr.ReadAsync())
                    return BadRequest(new { error = $"Sản phẩm ID {item.ProductId} không tồn tại" });

                decimal unitPrice = (decimal)pr.GetDouble(0);
                int stock = pr.GetInt32(1);
                string prodName = pr.GetString(2);
                if (item.Quantity > stock)
                    return BadRequest(new { error = $"Sản phẩm '{prodName}' chỉ còn {stock} cái" });

                lineItems.Add((item.ProductId, item.Quantity, unitPrice));
                subtotal += unitPrice * item.Quantity;
            }

            // Insert guest customer
            using var custCmd = conn.CreateCommand();
            custCmd.Transaction = tx;
            custCmd.CommandText = @"
                INSERT INTO customers (full_name, phone, address, city, loyalty_points, total_spent)
                VALUES (@name, @phone, @addr, '', 0, 0);
                SELECT last_insert_rowid();";
            custCmd.Parameters.AddWithValue("@name", dto.CustomerName.Trim());
            custCmd.Parameters.AddWithValue("@phone", dto.CustomerPhone.Trim());
            custCmd.Parameters.AddWithValue("@addr", dto.CustomerAddress?.Trim() ?? "");
            var customerId = Convert.ToInt64(await custCmd.ExecuteScalarAsync());

            // Insert order
            var orderCode = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
            using var orderCmd = conn.CreateCommand();
            orderCmd.Transaction = tx;
            orderCmd.CommandText = @"
                INSERT INTO orders (order_code, customer_id, employee_id, status,
                                    subtotal, discount_amount, tax_amount, total_amount, note)
                VALUES (@code, @cust, 1, 'pending', @sub, 0, 0, @total, @note);
                SELECT last_insert_rowid();";
            orderCmd.Parameters.AddWithValue("@code", orderCode);
            orderCmd.Parameters.AddWithValue("@cust", customerId);
            orderCmd.Parameters.AddWithValue("@sub", (double)subtotal);
            orderCmd.Parameters.AddWithValue("@total", (double)subtotal);
            orderCmd.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(dto.Note) ? "" : dto.Note.Trim());
            var orderId = Convert.ToInt64(await orderCmd.ExecuteScalarAsync());

            // Insert order items + decrement stock
            foreach (var (productId, qty, unitPrice) in lineItems)
            {
                using var itemCmd = conn.CreateCommand();
                itemCmd.Transaction = tx;
                itemCmd.CommandText = @"
                    INSERT INTO order_items (order_id, product_id, quantity, unit_price, discount_percent, line_total)
                    VALUES (@ord, @prod, @qty, @price, 0, @total)";
                itemCmd.Parameters.AddWithValue("@ord", orderId);
                itemCmd.Parameters.AddWithValue("@prod", productId);
                itemCmd.Parameters.AddWithValue("@qty", qty);
                itemCmd.Parameters.AddWithValue("@price", (double)unitPrice);
                itemCmd.Parameters.AddWithValue("@total", (double)(unitPrice * qty));
                await itemCmd.ExecuteNonQueryAsync();

                using var stockCmd = conn.CreateCommand();
                stockCmd.Transaction = tx;
                stockCmd.CommandText = "UPDATE products SET stock_quantity = stock_quantity - @qty WHERE id = @id";
                stockCmd.Parameters.AddWithValue("@qty", qty);
                stockCmd.Parameters.AddWithValue("@id", productId);
                await stockCmd.ExecuteNonQueryAsync();
            }

            // Insert payment record
            using var payCmd = conn.CreateCommand();
            payCmd.Transaction = tx;
            payCmd.CommandText = @"
                INSERT INTO payments (order_id, payment_method, amount, status, transaction_ref)
                VALUES (@ord, @method, @amount, 'pending', @ref)";
            payCmd.Parameters.AddWithValue("@ord", orderId);
            payCmd.Parameters.AddWithValue("@method", dto.PaymentMethod);
            payCmd.Parameters.AddWithValue("@amount", (double)subtotal);
            payCmd.Parameters.AddWithValue("@ref", $"REF-{orderCode}");
            await payCmd.ExecuteNonQueryAsync();

            tx.Commit();

            return Ok(new PlaceOrderResultDto
            {
                OrderCode = orderCode,
                TotalAmount = subtotal,
                ItemCount = lineItems.Sum(i => i.qty),
                Status = "pending"
            });
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
