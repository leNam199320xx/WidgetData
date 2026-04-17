namespace WidgetData.Application.DTOs;

public class StoreCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class StoreProductDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class StoreProductListResponse
{
    public IList<StoreProductDto> Items { get; set; } = new List<StoreProductDto>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class PlaceOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class PlaceOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? Note { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public IList<PlaceOrderItemDto> Items { get; set; } = new List<PlaceOrderItemDto>();
}

public class PlaceOrderResultDto
{
    public string OrderCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public string Status { get; set; } = string.Empty;
}
