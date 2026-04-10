# Multi-Step Data Processing

## Tổng quan
Widget hỗ trợ xử lý dữ liệu qua nhiều bước (steps), cho phép xây dựng data pipeline phức tạp mà không cần code. Mỗi step nhận đầu vào từ step trước, xử lý và chuyển kết quả cho step tiếp theo.

## Luồng thực thi Multi-Step

```
Step 1: Extract          Step 2: Transform       Step 3: Aggregate       Step 4: Final Output
┌──────────────┐        ┌──────────────┐        ┌──────────────┐        ┌──────────────┐
│ Read from DB │──────>│ Filter data  │──────>│ Group by     │──────>│ Format &     │
│ OR File      │        │ Add columns  │        │ Calculate    │        │ Cache result │
│ OR API       │        │ Join data    │        │ Sort         │        │              │
└──────────────┘        └──────────────┘        └──────────────┘        └──────────────┘
     Output                  Output                 Output                Final Result
  (Raw Data)          (Transformed Data)        (Aggregated Data)        (Ready to Display)
```

## Các loại Steps hỗ trợ

### 1. **Extract Step** (Đọc dữ liệu)
- Đọc từ database (SQL query)
- Đọc từ file (CSV, JSON, Excel)
- Gọi API endpoint
- Sử dụng kết quả từ widget khác

### 2. **Transform Step** (Biến đổi dữ liệu)
- Filter rows (WHERE conditions)
- Add calculated columns
- Rename columns
- Data type conversion
- String manipulation
- Date/time formatting

### 3. **Join Step** (Kết hợp dữ liệu)
- Inner join
- Left/Right join
- Union/Union All
- Merge data từ nhiều sources

### 4. **Aggregate Step** (Tổng hợp)
- GROUP BY
- SUM, AVG, COUNT, MIN, MAX
- DISTINCT
- PIVOT/UNPIVOT

### 5. **Filter Step** (Lọc)
- WHERE conditions
- HAVING clauses
- TOP N / LIMIT
- OFFSET pagination

### 6. **Output Step** (Xuất kết quả)
- Format output (JSON, CSV, XML)
- Cache result
- Send to webhook
- Save to database

## Use Cases

### Use Case 1: Sales Report với nhiều nguồn
```
Step 1: Đọc orders từ SQL Server
Step 2: Đọc product info từ CSV file
Step 3: Join orders + products
Step 4: Calculate total revenue per product
Step 5: Filter top 10 products
Step 6: Format và cache kết quả
```

### Use Case 2: Data Enrichment
```
Step 1: Đọc customer list từ database
Step 2: Gọi API để lấy thêm demographic data
Step 3: Merge customer + demographic
Step 4: Calculate customer lifetime value
Step 5: Segment customers
```

### Use Case 3: ETL Pipeline
```
Step 1: Extract từ legacy database
Step 2: Clean & validate data
Step 3: Transform format
Step 4: Aggregate by date
Step 5: Load vào data warehouse
```

## Ví dụ cấu hình Multi-Step Widget

### Complex Example: 6-Step Revenue Analysis

```json
{
  "widget_name": "MonthlyRevenueByCategory",
  "description": "Tính tổng doanh thu theo category với multi-step processing",
  "execution_mode": "sequential",
  "steps": [
    {
      "step_id": 1,
      "step_name": "Extract Orders",
      "step_type": "extract",
      "source": {
        "type": "database",
        "connection": "main_db",
        "query": "SELECT order_id, product_id, quantity, price, order_date FROM orders WHERE order_date >= DATEADD(month, -1, GETDATE())"
      },
      "output": "orders_raw"
    },
    {
      "step_id": 2,
      "step_name": "Get Product Categories",
      "step_type": "extract",
      "source": {
        "type": "file",
        "file_path": "data/products.csv",
        "columns": ["product_id", "product_name", "category"]
      },
      "output": "products_data"
    },
    {
      "step_id": 3,
      "step_name": "Join Orders with Products",
      "step_type": "join",
      "join_config": {
        "left": "orders_raw",
        "right": "products_data",
        "join_type": "inner",
        "on": "product_id"
      },
      "output": "orders_with_category"
    },
    {
      "step_id": 4,
      "step_name": "Calculate Total per Order",
      "step_type": "transform",
      "transformations": [
        {
          "type": "add_column",
          "name": "total_amount",
          "expression": "quantity * price"
        }
      ],
      "input": "orders_with_category",
      "output": "orders_with_total"
    },
    {
      "step_id": 5,
      "step_name": "Aggregate by Category",
      "step_type": "aggregate",
      "aggregate_config": {
        "group_by": ["category"],
        "aggregations": [
          {
            "column": "total_amount",
            "function": "sum",
            "alias": "total_revenue"
          },
          {
            "column": "order_id",
            "function": "count",
            "alias": "order_count"
          }
        ],
        "order_by": "total_revenue DESC"
      },
      "input": "orders_with_total",
      "output": "revenue_by_category"
    },
    {
      "step_id": 6,
      "step_name": "Final Output",
      "step_type": "output",
      "output_config": {
        "format": "json",
        "cache": {
          "enabled": true,
          "ttl": 3600
        },
        "columns": ["category", "total_revenue", "order_count"]
      },
      "input": "revenue_by_category"
    }
  ],
  "schedule": {
    "enabled": true,
    "cron": "0 0 * * *"
  }
}
```

### Simple Example: 3-Step Widget

```json
{
  "widget_name": "TopCustomersBySpending",
  "description": "Top 10 khách hàng chi tiêu nhiều nhất",
  "steps": [
    {
      "step_id": 1,
      "step_name": "Get Customer Orders",
      "step_type": "extract",
      "source": {
        "type": "database",
        "query": "SELECT customer_id, order_date, total_amount FROM orders"
      },
      "output": "customer_orders"
    },
    {
      "step_id": 2,
      "step_name": "Calculate Total Spending",
      "step_type": "aggregate",
      "aggregate_config": {
        "group_by": ["customer_id"],
        "aggregations": [
          { "column": "total_amount", "function": "sum", "alias": "total_spent" },
          { "column": "order_date", "function": "count", "alias": "order_count" }
        ]
      },
      "input": "customer_orders",
      "output": "customer_totals"
    },
    {
      "step_id": 3,
      "step_name": "Get Top 10",
      "step_type": "filter",
      "filter_config": {
        "order_by": "total_spent DESC",
        "limit": 10
      },
      "input": "customer_totals",
      "output": "final_result"
    }
  ]
}
```

## Step Execution Flow

```csharp
// Pseudo code cho step execution
public async Task<WidgetResult> ExecuteWidgetAsync(Widget widget) {
    var stepResults = new Dictionary<string, object>();
    
    foreach (var step in widget.Steps.OrderBy(s => s.StepId)) {
        try {
            // Get input từ step trước
            var input = step.Input != null ? stepResults[step.Input] : null;
            
            // Execute step dựa trên type
            var result = step.StepType switch {
                "extract" => await ExecuteExtractStepAsync(step),
                "transform" => await ExecuteTransformStepAsync(step, input),
                "join" => await ExecuteJoinStepAsync(step, stepResults),
                "aggregate" => await ExecuteAggregateStepAsync(step, input),
                "filter" => await ExecuteFilterStepAsync(step, input),
                "output" => await ExecuteOutputStepAsync(step, input),
                _ => throw new NotSupportedException($"Step type {step.StepType} not supported")
            };
            
            // Lưu kết quả step
            stepResults[step.Output] = result;
            
            // Log step completion
            await LogStepExecutionAsync(widget.Id, step.StepId, result.RowCount, true);
        }
        catch (Exception ex) {
            // Log error và dừng execution
            await LogStepExecutionAsync(widget.Id, step.StepId, 0, false, ex.Message);
            throw new StepExecutionException($"Step {step.StepId} failed: {ex.Message}", ex);
        }
    }
    
    // Return kết quả cuối cùng
    var finalStep = widget.Steps.OrderByDescending(s => s.StepId).First();
    return new WidgetResult {
        Success = true,
        Data = stepResults[finalStep.Output],
        ExecutedSteps = widget.Steps.Count
    };
}
```

## Lợi ích của Multi-Step Processing

✅ **Tái sử dụng**: Mỗi step có thể được test và reuse riêng  
✅ **Dễ debug**: Xem kết quả từng step để tìm lỗi  
✅ **Linh hoạt**: Thêm/xóa/sửa step mà không ảnh hưởng các step khác  
✅ **Performance**: Cache kết quả trung gian, parallel execution một số steps  
✅ **Maintainability**: Logic rõ ràng, dễ hiểu hơn một query phức tạp  
✅ **No-code**: Business users có thể tạo pipeline mà không cần viết code  

## Step Dependencies & Parallel Execution

```json
{
  "execution_mode": "parallel_where_possible",
  "steps": [
    { "step_id": 1, "dependencies": [] },        // Chạy đầu tiên
    { "step_id": 2, "dependencies": [] },        // Chạy song song với step 1
    { "step_id": 3, "dependencies": [1, 2] },    // Chạy sau khi 1 và 2 xong
    { "step_id": 4, "dependencies": [3] }        // Chạy cuối cùng
  ]
}
```

```
Parallel Execution:

   Step 1          Step 2
      \              /
       \            /
        \          /
         Step 3
            |
            |
         Step 4
```

## Frontend: Step Builder UI

```razor
@* Blazor component cho visual step builder *@
<MudPaper Class="pa-4">
    <MudText Typo="Typo.h6">Widget Steps</MudText>
    
    @foreach (var step in Widget.Steps.OrderBy(s => s.StepId)) {
        <MudCard Class="my-2">
            <MudCardHeader>
                <CardHeaderContent>
                    <MudText>Step @step.StepId: @step.StepName</MudText>
                    <MudChip Size="Size.Small" Color="Color.Primary">@step.StepType</MudChip>
                </CardHeaderContent>
                <CardHeaderActions>
                    <MudIconButton Icon="@Icons.Material.Filled.Edit" OnClick="@(() => EditStep(step))" />
                    <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@(() => DeleteStep(step))" />
                    <MudIconButton Icon="@Icons.Material.Filled.PlayArrow" OnClick="@(() => TestStep(step))" />
                </CardHeaderActions>
            </MudCardHeader>
            <MudCardContent>
                <MudText Typo="Typo.body2">Input: @(step.Input ?? "None")</MudText>
                <MudText Typo="Typo.body2">Output: @step.Output</MudText>
            </MudCardContent>
        </MudCard>
        
        @if (step.StepId < Widget.Steps.Max(s => s.StepId)) {
            <MudIcon Icon="@Icons.Material.Filled.ArrowDownward" Class="mx-auto d-block" />
        }
    }
    
    <MudButton StartIcon="@Icons.Material.Filled.Add" 
               Color="Color.Primary" 
               OnClick="AddNewStep"
               Class="mt-2">
        Add Step
    </MudButton>
</MudPaper>
```

---

[⬅️ Quay lại README](../README.md)
