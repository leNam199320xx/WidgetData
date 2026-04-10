# Testing Strategy

## 📋 Tổng quan

Widget Data sử dụng 3-tier testing strategy:

1. **Unit Tests** - Test individual components (70% coverage target)
2. **Integration Tests** - Test component interactions
3. **E2E Tests** - Test complete workflows (Playwright/Selenium)

**Test Stack:**
- xUnit - Test framework
- Moq - Mocking library
- FluentAssertions - Assertion library
- Bogus - Fake data generation
- Testcontainers - Docker-based integration tests

---

## 🧪 1. Unit Tests

### Project Setup

```bash
# Create test project
dotnet new xunit -n WidgetData.UnitTests
cd WidgetData.UnitTests

# Add packages
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Bogus
dotnet add package Microsoft.EntityFrameworkCore.InMemory

# Add reference to main project
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
    private readonly WidgetService _sut; // System Under Test
    
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
        // Arrange
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
        
        // Act
        var result = await _sut.GetByIdAsync(widgetId);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(widgetId);
        result.Name.Should().Be("Test Widget");
        
        _repositoryMock.Verify(x => x.GetByIdAsync(widgetId), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenWidgetNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var widgetId = 999;
        _repositoryMock
            .Setup(x => x.GetByIdAsync(widgetId))
            .ReturnsAsync((Widget)null);
        
        // Act
        Func<Task> act = async () => await _sut.GetByIdAsync(widgetId);
        
        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Widget with ID {widgetId} not found");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateAsync_WithInvalidName_ThrowsValidationException(string invalidName)
    {
        // Arrange
        var dto = new WidgetDto { Name = invalidName, WidgetType = "chart" };
        
        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);
        
        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
    
    [Fact]
    public async Task CreateAsync_WithValidData_CreatesWidget()
    {
        // Arrange
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
        
        // Act
        var result = await _sut.CreateAsync(dto);
        
        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        
        _repositoryMock.Verify(x => x.AddAsync(It.Is<Widget>(w => 
            w.Name == dto.Name && w.WidgetType == dto.WidgetType
        )), Times.Once);
        
        // Verify cache invalidation
        _cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Once);
    }
}
```

### Repository Unit Test (In-Memory DB)

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
        
        // Seed data
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
        // Act
        var widgets = await _repository.GetAllAsync();
        
        // Assert
        widgets.Should().HaveCount(3);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsWidget()
    {
        // Act
        var widget = await _repository.GetByIdAsync(1);
        
        // Assert
        widget.Should().NotBeNull();
        widget.Name.Should().Be("Widget 1");
    }
    
    [Fact]
    public async Task AddAsync_AddsWidgetToDatabase()
    {
        // Arrange
        var newWidget = new Widget
        {
            Name = "Widget 4",
            WidgetType = "chart"
        };
        
        // Act
        var result = await _repository.AddAsync(newWidget);
        
        // Assert
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

### Test Data with Bogus

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

// Usage
[Fact]
public async Task ProcessMultipleWidgets_Test()
{
    // Arrange
    var faker = new WidgetFaker();
    var widgets = faker.Generate(100);
    
    // Act & Assert
    // ...
}
```

---

## 🔗 2. Integration Tests

### Setup with Testcontainers

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
        // Start SQL Server container
        _sqlContainer = new MsSqlBuilder()
            .WithPassword("YourStrong@Passw0rd")
            .Build();
        
        await _sqlContainer.StartAsync();
        
        ConnectionString = _sqlContainer.GetConnectionString();
        
        // Run migrations
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

### API Integration Test

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
                // Replace real DB with in-memory
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
        // Act
        var response = await _client.GetAsync("/api/widgets");
        
        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.ToString()
            .Should().Be("application/json; charset=utf-8");
    }
    
    [Fact]
    public async Task CreateWidget_WithValidData_ReturnsCreated()
    {
        // Arrange
        var dto = new WidgetDto
        {
            Name = "Test Widget",
            WidgetType = "chart",
            DataSourceId = 1
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/widgets", dto);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var widget = await response.Content.ReadFromJsonAsync<Widget>();
        widget.Should().NotBeNull();
        widget.Name.Should().Be("Test Widget");
    }
    
    [Fact]
    public async Task GetWidget_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/widgets/9999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### Database Integration Test

```csharp
public class WidgetRepositoryIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task AddWidget_PersistsToDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        
        var widget = new Widget
        {
            Name = "Integration Test Widget",
            WidgetType = "chart"
        };
        
        // Act
        using (var context = new ApplicationDbContext(options))
        {
            var repository = new WidgetRepository(context);
            await repository.AddAsync(widget);
        }
        
        // Assert
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

### Setup

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
        // Arrange & Act
        await _page.GotoAsync("https://localhost:5001/dashboard");
        
        // Assert
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard" }))
            .ToBeVisibleAsync();
    }
    
    [Fact]
    public async Task CreateWidget_WorkflowCompletes()
    {
        // Navigate to dashboard
        await _page.GotoAsync("https://localhost:5001/dashboard");
        
        // Click "Create Widget" button
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create Widget" }).ClickAsync();
        
        // Fill form
        await _page.GetByLabel("Name").FillAsync("E2E Test Widget");
        await _page.GetByLabel("Type").SelectOptionAsync("chart");
        await _page.GetByLabel("Data Source").SelectOptionAsync("1");
        
        // Submit
        await _page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        
        // Verify success message
        await Expect(_page.GetByText("Widget created successfully"))
            .ToBeVisibleAsync();
        
        // Verify widget appears in list
        await Expect(_page.GetByText("E2E Test Widget"))
            .ToBeVisibleAsync();
    }
    
    [Fact]
    public async Task WidgetExecution_DisplaysResults()
    {
        // Navigate to widget page
        await _page.GotoAsync("https://localhost:5001/widgets/1");
        
        // Click execute
        await _page.GetByRole(AriaRole.Button, new() { Name = "Execute" }).ClickAsync();
        
        // Wait for results
        await _page.WaitForSelectorAsync(".widget-results", new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000
        });
        
        // Verify results appear
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

## 🚀 4. Performance Tests

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
        // Setup dependencies
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

// Run: dotnet run -c Release
```

---

## 📊 5. Code Coverage

```bash
# Install coverage tool
dotnet tool install --global dotnet-coverage

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# Open report
start coverage-report/index.html
```

### Coverage Targets

| Component | Target | Current |
|-----------|--------|---------|
| **Services** | 80% | 75% |
| **Repositories** | 70% | 68% |
| **Controllers** | 60% | 55% |
| **Overall** | 70% | 66% |

---

## 🎯 Testing Best Practices

### AAA Pattern (Arrange, Act, Assert)

```csharp
[Fact]
public async Task ExampleTest()
{
    // Arrange - Setup test data & mocks
    var widgetId = 123;
    _repositoryMock.Setup(x => x.GetByIdAsync(widgetId))
        .ReturnsAsync(new Widget { Id = widgetId });
    
    // Act - Execute the method under test
    var result = await _sut.GetByIdAsync(widgetId);
    
    // Assert - Verify the outcome
    result.Should().NotBeNull();
    result.Id.Should().Be(widgetId);
}
```

### Test Naming Convention

```csharp
// Pattern: MethodName_Scenario_ExpectedResult

[Fact]
public async Task GetByIdAsync_WhenWidgetExists_ReturnsWidget() { }

[Fact]
public async Task GetByIdAsync_WhenWidgetNotFound_ThrowsNotFoundException() { }

[Fact]
public async Task CreateAsync_WithInvalidData_ThrowsValidationException() { }
```

### One Assert Per Test (Flexible)

```csharp
// ✅ GOOD: Single logical assertion
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

// ⚠️ OK: Multiple related assertions
[Fact]
public async Task CreateWidget_CallsDependencies()
{
    await _sut.CreateAsync(dto);
    
    _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Widget>()), Times.Once);
    _cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Once);
}
```

---

## 🔄 CI/CD Integration

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
