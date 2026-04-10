# Branching & Variables

## Branching & Conditional Logic (Rẽ nhánh)

Pipeline hỗ trợ conditional branching, cho phép thực thi các nhánh khác nhau dựa trên điều kiện.

### 1. If-Else Branching

```json
{
  "widget_name": "CustomerSegmentationPipeline",
  "description": "Phân loại khách hàng và xử lý khác nhau theo segment",
  "steps": [
    {
      "step_id": 1,
      "step_name": "Get Customer Data",
      "step_type": "extract",
      "source": {
        "type": "database",
        "query": "SELECT customer_id, total_spent, last_order_date FROM customers"
      },
      "output": "customers"
    },
    {
      "step_id": 2,
      "step_name": "Check Customer Value",
      "step_type": "branch_condition",
      "condition": {
        "expression": "total_spent > 10000",
        "true_branch": 3,
        "false_branch": 5
      },
      "input": "customers"
    },
    {
      "step_id": 3,
      "step_name": "VIP Customer Processing",
      "step_type": "transform",
      "transformations": [
        { "type": "add_column", "name": "segment", "value": "VIP" },
        { "type": "add_column", "name": "discount_rate", "value": 0.15 }
      ],
      "input": "customers",
      "output": "vip_customers",
      "next_step": 7
    },
    {
      "step_id": 5,
      "step_name": "Regular Customer Processing",
      "step_type": "transform",
      "transformations": [
        { "type": "add_column", "name": "segment", "value": "Regular" },
        { "type": "add_column", "name": "discount_rate", "value": 0.05 }
      ],
      "input": "customers",
      "output": "regular_customers",
      "next_step": 7
    },
    {
      "step_id": 7,
      "step_name": "Merge Results",
      "step_type": "merge",
      "merge_config": {
        "sources": ["vip_customers", "regular_customers"],
        "mode": "union_all"
      },
      "output": "all_customers_segmented"
    }
  ]
}
```

### 2. Switch-Case Branching (Multiple Conditions)

```json
{
  "step_id": 10,
  "step_name": "Route by Order Status",
  "step_type": "branch_switch",
  "switch_config": {
    "variable": "order_status",
    "cases": [
      {
        "condition": "status == 'pending'",
        "next_step": 11,
        "description": "Process pending orders"
      },
      {
        "condition": "status == 'shipped'",
        "next_step": 15,
        "description": "Update tracking info"
      },
      {
        "condition": "status == 'completed'",
        "next_step": 20,
        "description": "Generate invoice"
      },
      {
        "condition": "status == 'cancelled'",
        "next_step": 25,
        "description": "Refund process"
      }
    ],
    "default": 30
  },
  "input": "orders"
}
```

### 3. Parallel Branches (Chạy song song nhiều nhánh)

```json
{
  "step_id": 1,
  "step_name": "Extract Orders",
  "step_type": "extract",
  "output": "orders"
},
{
  "step_id": 2,
  "step_name": "Parallel Processing",
  "step_type": "branch_parallel",
  "parallel_branches": [
    {
      "branch_id": "A",
      "steps": [
        {
          "step_id": "2A1",
          "step_name": "Calculate Revenue Metrics",
          "step_type": "aggregate"
        },
        {
          "step_id": "2A2",
          "step_name": "Revenue Analysis",
          "step_type": "transform"
        }
      ],
      "output": "revenue_analysis"
    },
    {
      "branch_id": "B",
      "steps": [
        {
          "step_id": "2B1",
          "step_name": "Calculate Customer Metrics",
          "step_type": "aggregate"
        },
        {
          "step_id": "2B2",
          "step_name": "Customer Analysis",
          "step_type": "transform"
        }
      ],
      "output": "customer_analysis"
    },
    {
      "branch_id": "C",
      "steps": [
        {
          "step_id": "2C1",
          "step_name": "Calculate Product Metrics",
          "step_type": "aggregate"
        }
      ],
      "output": "product_analysis"
    }
  ],
  "wait_all": true,
  "next_step": 3
},
{
  "step_id": 3,
  "step_name": "Combine All Analysis",
  "step_type": "merge",
  "merge_config": {
    "sources": ["revenue_analysis", "customer_analysis", "product_analysis"]
  }
}
```

### Branching Visualization

```
                  Start
                    |
              [Extract Data]
                    |
            [Branch Condition]
               /          \
              /            \
    (IF total > 10k)  (ELSE)
         /                  \
   [VIP Process]      [Regular Process]
         \                  /
          \                /
           \              /
            [Merge Results]
                  |
                [End]
```

```
                [Extract Orders]
                      |
              [Parallel Branches]
             /        |         \
            /         |          \
    [Branch A]   [Branch B]   [Branch C]
    Revenue      Customer     Product
    Analysis     Analysis     Analysis
            \         |         /
             \        |        /
              \       |       /
            [Combine Results]
                    |
                  [End]
```

## Variables & Parameters (Biến)

Pipeline hỗ trợ biến để lưu trữ và truyền dữ liệu giữa các steps.

### 1. Variable Types

#### Global Variables (Widget-level)
```json
{
  "widget_name": "SalesReportWithVariables",
  "variables": {
    "report_date": "2026-04-10",
    "min_amount": 1000,
    "currency": "USD",
    "exchange_rate": 1.0,
    "company_name": "ACME Corp"
  },
  "steps": [...]
}
```

#### Step Variables (Local to step)
```json
{
  "step_id": 5,
  "step_name": "Calculate Totals",
  "step_type": "transform",
  "variables": {
    "tax_rate": 0.1,
    "shipping_cost": 50
  },
  "transformations": [
    {
      "type": "add_column",
      "name": "total_with_tax",
      "expression": "subtotal * (1 + ${tax_rate})"
    },
    {
      "type": "add_column",
      "name": "grand_total",
      "expression": "total_with_tax + ${shipping_cost}"
    }
  ]
}
```

#### Runtime Variables (Computed during execution)
```json
{
  "step_id": 3,
  "step_name": "Set Runtime Variables",
  "step_type": "set_variable",
  "variables": [
    {
      "name": "total_orders",
      "source": "query",
      "query": "SELECT COUNT(*) FROM orders"
    },
    {
      "name": "avg_order_value",
      "source": "query",
      "query": "SELECT AVG(total) FROM orders"
    },
    {
      "name": "current_timestamp",
      "source": "function",
      "function": "NOW()"
    }
  ]
}
```

### 2. Variable Usage Examples

#### Sử dụng biến trong SQL Query
```json
{
  "step_id": 1,
  "step_name": "Extract Orders by Date",
  "step_type": "extract",
  "source": {
    "type": "database",
    "query": "SELECT * FROM orders WHERE order_date >= '${report_date}' AND total >= ${min_amount}"
  },
  "output": "filtered_orders"
}
```

#### Sử dụng biến trong Transform
```json
{
  "step_id": 4,
  "step_name": "Add Company Info",
  "step_type": "transform",
  "transformations": [
    {
      "type": "add_column",
      "name": "company",
      "value": "${company_name}"
    },
    {
      "type": "add_column",
      "name": "report_generated_at",
      "expression": "${current_timestamp}"
    },
    {
      "type": "add_column",
      "name": "amount_usd",
      "expression": "amount * ${exchange_rate}"
    }
  ]
}
```

#### Sử dụng biến trong Conditions
```json
{
  "step_id": 6,
  "step_name": "Branch by Amount",
  "step_type": "branch_condition",
  "condition": {
    "expression": "total > ${min_amount} AND currency == '${currency}'",
    "true_branch": 7,
    "false_branch": 10
  }
}
```

### 3. Parameter Input (Widget Parameters)

```json
{
  "widget_name": "ParameterizedSalesReport",
  "description": "Sales report với parameters do user nhập",
  "parameters": [
    {
      "name": "start_date",
      "type": "date",
      "required": true,
      "default": "2026-01-01",
      "description": "Report start date"
    },
    {
      "name": "end_date",
      "type": "date",
      "required": true,
      "default": "2026-12-31",
      "description": "Report end date"
    },
    {
      "name": "category",
      "type": "string",
      "required": false,
      "default": "All",
      "options": ["All", "Electronics", "Clothing", "Food"],
      "description": "Product category filter"
    },
    {
      "name": "min_revenue",
      "type": "number",
      "required": false,
      "default": 0,
      "min": 0,
      "max": 1000000,
      "description": "Minimum revenue threshold"
    }
  ],
  "steps": [
    {
      "step_id": 1,
      "step_type": "extract",
      "source": {
        "query": "SELECT * FROM sales WHERE sale_date BETWEEN '${start_date}' AND '${end_date}' AND (category = '${category}' OR '${category}' = 'All') AND revenue >= ${min_revenue}"
      }
    }
  ]
}
```

### 4. Variable Scopes

```csharp
// Variable resolution order
1. Step Variables (highest priority)
2. Runtime Variables
3. Global Widget Variables
4. User Input Parameters
5. System Variables (lowest priority)

// Example:
${step_var}         // From current step
${global_var}       // From widget variables
${param:start_date} // From user parameters
${sys:current_user} // System variable
${prev:output}      // Output from previous step
```

### 5. Built-in System Variables

```json
{
  "system_variables": {
    "${sys:current_user}": "user@example.com",
    "${sys:current_time}": "2026-04-10T14:30:00Z",
    "${sys:widget_id}": 123,
    "${sys:execution_id}": "exec-456",
    "${sys:environment}": "production"
  }
}
```

### 6. Variable Operations

#### Set Variable
```json
{
  "step_id": 2,
  "step_name": "Calculate and Store Variable",
  "step_type": "set_variable",
  "variables": [
    {
      "name": "discount_rate",
      "expression": "IF(${total_orders} > 100, 0.15, 0.10)"
    },
    {
      "name": "processing_fee",
      "expression": "${total_amount} * 0.03"
    }
  ]
}
```

#### Update Variable (Modify existing)
```json
{
  "step_id": 5,
  "step_type": "update_variable",
  "variables": [
    {
      "name": "counter",
      "operation": "increment",
      "value": 1
    },
    {
      "name": "total_revenue",
      "operation": "add",
      "value": "${current_order_total}"
    }
  ]
}
```

## Complex Example: Branching + Variables

```json
{
  "widget_name": "OrderProcessingPipelineAdvanced",
  "description": "Xử lý đơn hàng với branching và variables",
  "parameters": [
    {
      "name": "order_date",
      "type": "date",
      "required": true
    },
    {
      "name": "priority_threshold",
      "type": "number",
      "default": 5000
    }
  ],
  "variables": {
    "tax_rate": 0.1,
    "express_shipping_cost": 50,
    "standard_shipping_cost": 10
  },
  "steps": [
    {
      "step_id": 1,
      "step_name": "Extract Orders",
      "step_type": "extract",
      "source": {
        "query": "SELECT * FROM orders WHERE order_date = '${param:order_date}'"
      },
      "output": "orders"
    },
    {
      "step_id": 2,
      "step_name": "Calculate Order Metrics",
      "step_type": "set_variable",
      "variables": [
        {
          "name": "total_order_count",
          "source": "aggregate",
          "aggregate": {
            "input": "orders",
            "function": "count"
          }
        },
        {
          "name": "avg_order_value",
          "source": "aggregate",
          "aggregate": {
            "input": "orders",
            "column": "total_amount",
            "function": "avg"
          }
        }
      ]
    },
    {
      "step_id": 3,
      "step_name": "Branch by Order Value",
      "step_type": "branch_condition",
      "condition": {
        "expression": "total_amount > ${param:priority_threshold}",
        "true_branch": 4,
        "false_branch": 10
      },
      "input": "orders"
    },
    {
      "step_id": 4,
      "step_name": "High Value Order Processing",
      "step_type": "transform",
      "transformations": [
        {
          "type": "add_column",
          "name": "priority",
          "value": "HIGH"
        },
        {
          "type": "add_column",
          "name": "shipping_cost",
          "value": "${express_shipping_cost}"
        },
        {
          "type": "add_column",
          "name": "tax",
          "expression": "total_amount * ${tax_rate}"
        },
        {
          "type": "add_column",
          "name": "grand_total",
          "expression": "total_amount + tax + ${express_shipping_cost}"
        }
      ],
      "input": "orders",
      "output": "high_value_orders",
      "next_step": 20
    },
    {
      "step_id": 10,
      "step_name": "Standard Order Processing",
      "step_type": "transform",
      "transformations": [
        {
          "type": "add_column",
          "name": "priority",
          "value": "STANDARD"
        },
        {
          "type": "add_column",
          "name": "shipping_cost",
          "value": "${standard_shipping_cost}"
        },
        {
          "type": "add_column",
          "name": "tax",
          "expression": "total_amount * ${tax_rate}"
        },
        {
          "type": "add_column",
          "name": "grand_total",
          "expression": "total_amount + tax + ${standard_shipping_cost}"
        }
      ],
      "input": "orders",
      "output": "standard_orders",
      "next_step": 20
    },
    {
      "step_id": 20,
      "step_name": "Merge All Orders",
      "step_type": "merge",
      "merge_config": {
        "sources": ["high_value_orders", "standard_orders"],
        "mode": "union_all"
      },
      "output": "processed_orders"
    },
    {
      "step_id": 21,
      "step_name": "Add Summary Info",
      "step_type": "transform",
      "transformations": [
        {
          "type": "add_column",
          "name": "total_orders_today",
          "value": "${total_order_count}"
        },
        {
          "type": "add_column",
          "name": "avg_order_value_today",
          "value": "${avg_order_value}"
        },
        {
          "type": "add_column",
          "name": "processed_by",
          "value": "${sys:current_user}"
        },
        {
          "type": "add_column",
          "name": "processed_at",
          "value": "${sys:current_time}"
        }
      ],
      "input": "processed_orders",
      "output": "final_result"
    }
  ]
}
```

## Execution Engine Implementation

```csharp
public class PipelineExecutionEngine {
    private Dictionary<string, object> _variables = new();
    private Dictionary<string, object> _stepOutputs = new();
    
    public async Task<WidgetResult> ExecuteAsync(Widget widget, Dictionary<string, object> parameters) {
        // Initialize variables
        InitializeVariables(widget.Variables, parameters);
        
        var currentStepId = widget.Steps.First().StepId;
        
        while (currentStepId != null) {
            var step = widget.Steps.FirstOrDefault(s => s.StepId == currentStepId);
            if (step == null) break;
            
            // Resolve variables in step config
            ResolveVariables(step);
            
            // Execute based on step type
            switch (step.StepType) {
                case "branch_condition":
                    currentStepId = await ExecuteBranchConditionAsync(step);
                    break;
                    
                case "branch_switch":
                    currentStepId = await ExecuteBranchSwitchAsync(step);
                    break;
                    
                case "branch_parallel":
                    await ExecuteParallelBranchesAsync(step);
                    currentStepId = step.NextStep;
                    break;
                    
                case "set_variable":
                    await ExecuteSetVariableAsync(step);
                    currentStepId = step.NextStep;
                    break;
                    
                default:
                    var result = await ExecuteStepAsync(step);
                    _stepOutputs[step.Output] = result;
                    currentStepId = step.NextStep;
                    break;
            }
        }
        
        return new WidgetResult {
            Success = true,
            Data = _stepOutputs.Values.Last(),
            Variables = _variables
        };
    }
    
    private void ResolveVariables(Step step) {
        // Replace ${variable_name} with actual values
        var json = JsonSerializer.Serialize(step);
        foreach (var variable in _variables) {
            json = json.Replace($"${{{variable.Key}}}", variable.Value.ToString());
        }
        step = JsonSerializer.Deserialize<Step>(json);
    }
    
    private async Task<int?> ExecuteBranchConditionAsync(Step step) {
        var condition = step.Condition.Expression;
        var result = EvaluateExpression(condition, _variables, _stepOutputs);
        
        return result ? step.Condition.TrueBranch : step.Condition.FalseBranch;
    }
}
```

## Benefits of Branching & Variables

✅ **Dynamic Pipelines**: Pipeline behavior thay đổi dựa trên dữ liệu  
✅ **Code Reusability**: Dùng biến thay vì hardcode values  
✅ **Flexibility**: User có thể input parameters khi chạy widget  
✅ **Conditional Logic**: Xử lý khác nhau cho các cases khác nhau  
✅ **Parallel Processing**: Tăng performance với parallel branches  
✅ **Maintainability**: Thay đổi biến ở một chỗ, áp dụng toàn pipeline  

---

[⬅️ Quay lại README](../README.md)
