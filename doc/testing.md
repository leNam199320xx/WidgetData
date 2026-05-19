# Chiến lược Kiểm thử

## 📋 Tổng quan

Widget Data sử dụng 3-tier testing strategy:

1. **Unit Tests** - Kiểm thử từng component riêng lẻ (mục tiêu 70% coverage)
2. **Integration Tests** - Kiểm thử tương tác giữa các component
3. **E2E Tests** - Kiểm thử toàn bộ quy trình (Playwright/Selenium)

**Test Stack:**
- xUnit - Test framework
- Moq - Mocking library
- FluentAssertions - Assertion library
- Bogus - Fake data generation
- Testcontainers - Docker-based integration tests

---

## 🧪 1. Unit Tests

### Thiết lập Dự án

```bash
# Tạo dự án test
dotnet new xunit -n WidgetData.UnitTests
cd WidgetData.UnitTests

# Thêm các package
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Bogus
dotnet add package Microsoft.EntityFrameworkCore.InMemory

# Thêm tham chiếu đến dự án chính
dotnet add reference ../src/WidgetData.Application/WidgetData.Application.csproj
```

### Service Unit Test Example

```csharp
using Xunit;
using Moq;
using FluentAssertions;

public class WidgetServiceTests
{
    private readonly Mock<IWidgetRepository> _repositoryMock;
    private readonly Mock<ILogger<WidgetService>> _loggerMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly WidgetService _sut; // Đối tượng Đang Kiểm thử
    
    public WidgetServiceTests()
    {
        _repositoryMock = new Mock<IWidgetRepository>();
        _loggerMock = new Mock<ILogger<WidgetService>>();
        _cacheMock = new Mock<ICacheService>();
        
        _sut = new WidgetService(
            _repositoryMock.Object,
            _loggerMock.Object,
            _cacheMock.Object
        );
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenWidgetExists_ReturnsWidget()
    {
        // Sắp xếp
        var widgetId = 123;
        var expectedWidget = new Widget
        {
            Id = widgetId,
            Name = "Test Widget",
            WidgetType = "chart"
        };
        
        _repositoryMock
            .Setup(x => x.GetByIdAsync(widgetId))
            .ReturnsAsync(expectedWidget);
        
        // Thực thi
        var result = await _sut.GetByIdAsync(widgetId);
        
        // Kiểm tra
        result.Should().NotBeNull();
        result.Id.Should().Be(widgetId);
        result.Name.Should().Be("Test Widget");
        
        _repositoryMock.Verify(x => x.GetByIdAsync(widgetId), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenWidgetNotFound_ThrowsNotFoundException()
    {
        // Sắp xếp
        var widgetId = 999;
        _repositoryMock
            .Setup(x => x.GetByIdAsync(widgetId))
            .ReturnsAsync((Widget)null);
        
        // Thực thi
        Func<Task> act = async () => await _sut.GetByIdAsync(widgetId);
        
        // Kiểm tra
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Widget with ID {widgetId} not found");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateAsync_WithInvalidName_ThrowsValidationException(string invalidName)
    {
        // Sắp xếp
        var dto = new WidgetDto { Name = invalidName, WidgetType = "chart" };
        
        // Thực thi
        Func<Task> act = async () => await _sut.CreateAsync(dto);
        
        // Kiểm tra
        await act.Should().ThrowAsync<ValidationException>();
    }
    
    [Fact]
    public async Task CreateAsync_WithValidData_CreatesWidget()
    {
        // Sắp xếp
        var dto = new WidgetDto
        {
            Name = "New Widget",
            WidgetType = "chart",
            DataSourceId = 1
        };
        
        var createdWidget = new Widget
        {
            Id = 1,
            Name = dto.Name,
            WidgetType = dto.WidgetType
        };
        
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Widget>()))
            .ReturnsAsync(createdWidget);
        
        // Thực thi
        var result = await _sut.CreateAsync(dto);
        
        // Kiểm tra
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        
        _repositoryMock.Verify(x => x.AddAsync(It.Is<Widget>(w => 
            w.Name == dto.Name && w.WidgetType == dto.WidgetType
        )), Times.Once);
        
        // Xác minh cache bị xóa
        _cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Once);
    }
}
```

### Unit Test Repository (DB trong Bộ nhớ)

```csharp
public class WidgetRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly WidgetRepository _repository;
    
    public WidgetRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _repository = new WidgetRepository(_context);
        
        // Nạp dữ liệu ban đầu
        SeedData();
    }
    
    private void SeedData()
    {
        _context.Widgets.AddRange(
            new Widget { Id = 1, Name = "Widget 1", WidgetType = "chart" },
            new Widget { Id = 2, Name = "Widget 2", WidgetType = "table" },
            new Widget { Id = 3, Name = "Widget 3", WidgetType = "metric" }
        );
        _context.SaveChanges();
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsAllWidgets()
    {
        // Thực thi
        var widgets = await _repository.GetAllAsync();
        
        // Kiểm tra
        widgets.Should().HaveCount(3);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsWidget()
    {
        // Thực thi
        var widget = await _repository.GetByIdAsync(1);
        
        // Kiểm tra
        widget.Should().NotBeNull();
        widget.Name.Should().Be("Widget 1");
    }
    
    [Fact]
    public async Task AddAsync_AddsWidgetToDatabase()
    {
        // Sắp xếp
        var newWidget = new Widget
        {
            Name = "Widget 4",
            WidgetType = "chart"
        };
        
        // Thực thi
        var result = await _repository.AddAsync(newWidget);
        
        // Kiểm tra
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        
        var allWidgets = await _repository.GetAllAsync();
        allWidgets.Should().HaveCount(4);
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### Dữ liệu Test với Bogus

```csharp
using Bogus;

public class WidgetFaker
{
    private readonly Faker<Widget> _faker;
    
    public WidgetFaker()
    {
        _faker = new Faker<Widget>()
            .RuleFor(w => w.Id, f => f.Random.Int(1, 1000))
            .RuleFor(w => w.Name, f => f.Commerce.ProductName())
            .RuleFor(w => w.WidgetType, f => f.PickRandom("chart", "table", "metric", "map"))
            .RuleFor(w => w.IsActive, f => f.Random.Bool())
            .RuleFor(w => w.CreatedAt, f => f.Date.Past())
            .RuleFor(w => w.UpdatedAt, f => f.Date.Recent());
    }
    
    public Widget Generate() => _faker.Generate();
    
    public List<Widget> Generate(int count) => _faker.Generate(count);
}

// Cách dùng
[Fact]
public async Task ProcessMultipleWidgets_Test()
{
    // Sắp xếp
    var faker = new WidgetFaker();
    var widgets = faker.Generate(100);
    
    // Act & Assert
    // ...
}
```

---

## 🔗 2. Integration Tests

### Thiết lập với Testcontainers

```bash
dotnet new xunit -n WidgetData.IntegrationTests
dotnet add package Testcontainers
dotnet add package Testcontainers.MsSql
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

```csharp
using Testcontainers.MsSql;

public class IntegrationTestBase : IAsyncLifetime
{
    private MsSqlContainer _sqlContainer;
    protected string ConnectionString { get; private set; }
    
    public async Task InitializeAsync()
    {
        // Khởi động container SQL Server
        _sqlContainer = new MsSqlBuilder()
            .WithPassword("YourStrong@Passw0rd")
            .Build();
        
        await _sqlContainer.StartAsync();
        
        ConnectionString = _sqlContainer.GetConnectionString();
        
        // Chạy migrations
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        
        using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }
}
```

### Integration Test API

```csharp
public class WidgetApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    
    public WidgetApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Thay DB thực bằng in-memory
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });
        
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task GetWidgets_ReturnsSuccessAndCorrectContentType()
    {
        // Thực thi
        var response = await _client.GetAsync("/api/widgets");
        
        // Kiểm tra
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.ToString()
            .Should().Be("application/json; charset=utf-8");
    }
    
    [Fact]
    public async Task CreateWidget_WithValidData_ReturnsCreated()
    {
        // Sắp xếp
        var dto = new WidgetDto
        {
            Name = "Test Widget",
            WidgetType = "chart",
            DataSourceId = 1
        };
        
        // Thực thi
        var response = await _client.PostAsJsonAsync("/api/widgets", dto);
        
        // Kiểm tra
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var widget = await response.Content.ReadFromJsonAsync<Widget>();
        widget.Should().NotBeNull();
        widget.Name.Should().Be("Test Widget");
    }
    
    [Fact]
    public async Task GetWidget_WithInvalidId_ReturnsNotFound()
    {
        // Thực thi
        var response = await _client.GetAsync("/api/widgets/9999");
        
        // Kiểm tra
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### Integration Test Database

```csharp
public class WidgetRepositoryIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task AddWidget_PersistsToDatabase()
    {
        // Sắp xếp
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        
        var widget = new Widget
        {
            Name = "Integration Test Widget",
            WidgetType = "chart"
        };
        
        // Thực thi
        using (var context = new ApplicationDbContext(options))
        {
            var repository = new WidgetRepository(context);
            await repository.AddAsync(widget);
        }
        
        // Kiểm tra
        using (var context = new ApplicationDbContext(options))
        {
            var savedWidget = await context.Widgets
                .FirstOrDefaultAsync(w => w.Name == "Integration Test Widget");
            
            savedWidget.Should().NotBeNull();
            savedWidget.WidgetType.Should().Be("chart");
        }
    }
}
```

---

## 🎭 3. End-to-End Tests (Playwright)

### Thiết lập

```bash
dotnet new xunit -n WidgetData.E2ETests
dotnet add package Microsoft.Playwright
pwsh bin/Debug/net8.0/playwright.ps1 install
```

### Blazor E2E Test

```csharp
using Microsoft.Playwright;
using Xunit;

public class DashboardE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;
    
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });
        _page = await _browser.NewPageAsync();
    }
    
    [Fact]
    public async Task Dashboard_LoadsSuccessfully()
    {
        // Sắp xếp & Thực thi
        await _page.GotoAsync("https://localhost:5001/dashboard");
        
        // Kiểm tra
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard" }))
            .ToBeVisibleAsync();
    }
    
    [Fact]
    public async Task CreateWidget_WorkflowCompletes()
    {
        // Điều hướng đến dashboard
        await _page.GotoAsync("https://localhost:5001/dashboard");
        
        // Nhấn nút "Create Widget"
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create Widget" }).ClickAsync();
        
        // Điền form
        await _page.GetByLabel("Name").FillAsync("E2E Test Widget");
        await _page.GetByLabel("Type").SelectOptionAsync("chart");
        await _page.GetByLabel("Data Source").SelectOptionAsync("1");
        
        // Gửi
        await _page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        
        // Xác minh thông báo thành công
        await Expect(_page.GetByText("Widget created successfully"))
            .ToBeVisibleAsync();
        
        // Xác minh widget xuất hiện trong danh sách
        await Expect(_page.GetByText("E2E Test Widget"))
            .ToBeVisibleAsync();
    }
    
    [Fact]
    public async Task WidgetExecution_DisplaysResults()
    {
        // Điều hướng đến trang widget
        await _page.GotoAsync("https://localhost:5001/widgets/1");
        
        // Nhấn thực thi
        await _page.GetByRole(AriaRole.Button, new() { Name = "Execute" }).ClickAsync();
        
        // Chờ kết quả
        await _page.WaitForSelectorAsync(".widget-results", new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000
        });
        
        // Xác minh kết quả xuất hiện
        var resultsText = await _page.Locator(".widget-results").TextContentAsync();
        resultsText.Should().NotBeEmpty();
    }
    
    public async Task DisposeAsync()
    {
        await _page.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}
```

---

## 🚀 4. Kiểm thử Hiệu năng

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
public class WidgetServiceBenchmarks
{
    private WidgetService _service;
    private WidgetDto _dto;
    
    [GlobalSetup]
    public void Setup()
    {
        // Thiết lập các dependency
        _service = new WidgetService(/* ... */);
        _dto = new WidgetDto
        {
            Name = "Benchmark Widget",
            WidgetType = "chart"
        };
    }
    
    [Benchmark]
    public async Task CreateWidget()
    {
        await _service.CreateAsync(_dto);
    }
    
    [Benchmark]
    public async Task GetWidget()
    {
        await _service.GetByIdAsync(1);
    }
}

// Chạy: dotnet run -c Release
```

---

## 📊 5. Độ phủ Code

```bash
# Cài đặt công cụ coverage
dotnet tool install --global dotnet-coverage

# Chạy tests với coverage
dotnet test --collect:"XPlat Code Coverage"

# Tạo báo cáo HTML
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# Mở báo cáo
start coverage-report/index.html
```

### Mục tiêu Coverage

| Thành phần | Mục tiêu | Hiện tại |
|-----------|--------|---------|
| **Services** | 80% | 75% |
| **Repositories** | 70% | 68% |
| **Controllers** | 60% | 55% |
| **Tổng thể** | 70% | 66% |

---

## 🎯 Các thực hành Kiểm thử Tốt nhất

### Mẫu AAA (Sắp xếp, Thực thi, Kiểm tra)

```csharp
[Fact]
public async Task ExampleTest()
{
    // Sắp xếp - Thiết lập dữ liệu test & mock
    var widgetId = 123;
    _repositoryMock.Setup(x => x.GetByIdAsync(widgetId))
        .ReturnsAsync(new Widget { Id = widgetId });
    
    // Thực thi - Gọi phương thức cần kiểm thử
    var result = await _sut.GetByIdAsync(widgetId);
    
    // Kiểm tra - Xác nhận kết quả
    result.Should().NotBeNull();
    result.Id.Should().Be(widgetId);
}
```

### Quy ước Đặt tên Test

```csharp
// Mẫu: TênPhươngThức_KịchBản_KếtQuảMongĐợi

[Fact]
public async Task GetByIdAsync_WhenWidgetExists_ReturnsWidget() { }

[Fact]
public async Task GetByIdAsync_WhenWidgetNotFound_ThrowsNotFoundException() { }

[Fact]
public async Task CreateAsync_WithInvalidData_ThrowsValidationException() { }
```

### Một Assert Mỗi Test (Linh hoạt)

```csharp
// ✅ ĐÚNG: Một assertion logic duy nhất
[Fact]
public async Task CreateWidget_SetsAllProperties()
{
    var result = await _sut.CreateAsync(dto);
    
    result.Should().BeEquivalentTo(new Widget
    {
        Name = dto.Name,
        WidgetType = dto.WidgetType,
        DataSourceId = dto.DataSourceId
    });
}

// ⚠️ Được: Nhiều assertion liên quan
[Fact]
public async Task CreateWidget_CallsDependencies()
{
    await _sut.CreateAsync(dto);
    
    _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Widget>()), Times.Once);
    _cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Once);
}
```

---

## 🔄 Tích hợp CI/CD

```yaml
# .github/workflows/test.yml
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Unit Tests
      run: dotnet test tests/WidgetData.UnitTests --no-build --verbosity normal
    
    - name: Run Integration Tests
      run: dotnet test tests/WidgetData.IntegrationTests --no-build --verbosity normal
    
    - name: Generate Coverage Report
      run: |
        dotnet test --collect:"XPlat Code Coverage"
        reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'
```

---

← [Quay lại INDEX](INDEX.md)
