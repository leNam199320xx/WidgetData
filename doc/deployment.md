# Deployment & Cài đặt

## 📋 Tổng quan

Widget Data hỗ trợ nhiều phương thức triển khai:

1. **Development** - Local development environment
2. **Docker** - Container-based deployment
3. **IIS** - Windows Server hosting
4. **Azure App Service** - Cloud PaaS
5. **Kubernetes** - Container orchestration (future)

---

## 🖥️ 1. Development Setup

### Prerequisites

| Component | Version | Download |
|-----------|---------|----------|
| **.NET SDK** | 8.0+ | [Download](https://dotnet.microsoft.com/download) |
| **SQL Server** | 2019+ | [Download](https://www.microsoft.com/sql-server/sql-server-downloads) |
| **Redis** | 7.0+ | [Download](https://redis.io/download) (Optional) |
| **Node.js** | 18+ | [Download](https://nodejs.org/) (for frontend tools) |
| **VS Code / Visual Studio** | Latest | [VS Code](https://code.visualstudio.com/) / [VS 2022](https://visualstudio.microsoft.com/) |

### Clone Repository

```bash
git clone https://github.com/your-org/widget-data.git
cd widget-data
```

### Project Structure

```
widget-data/
├── src/
│   ├── WidgetData.Domain/          # Entities, interfaces, enums
│   ├── WidgetData.Application/     # DTOs, service interfaces
│   ├── WidgetData.Infrastructure/  # EF Core, repositories, services, CronUtils
│   ├── WidgetData.API/             # ASP.NET Core Web API (JWT, Swagger/Scalar)
│   ├── WidgetData.Web/             # Blazor Web App (MudBlazor admin dashboard)
│   ├── WidgetData.Worker/          # .NET Worker Service (cron job executor)
│   ├── WidgetData.Gateway/         # YARP reverse proxy
│   ├── WidgetData.ServiceDefaults/ # .NET Aspire shared defaults (OpenTelemetry)
│   └── WidgetData.AppHost/         # .NET Aspire orchestration host
├── demo/
│   ├── shop/shop-admin/            # Demo shop admin (Blazor)
│   ├── news/news-front/            # Demo news frontend
│   └── course/course-front/        # Demo course frontend
├── tests/
│   └── WidgetData.Tests/
├── data/                           # SQLite database files
├── doc/                            # Documentation
└── scripts/                        # Helper scripts
```

### Install Dependencies

```bash
# Restore NuGet packages
dotnet restore

# Install EF Core tools (if not installed)
dotnet tool install --global dotnet-ef
```

### Database Setup

```bash
# Connection string mặc định dùng SQLite (widgetdata.db)
# Kiểm tra appsettings.json trong src/WidgetData.API:
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=widgetdata.db"
  }
}

# Chạy migrations
dotnet ef database update --project src/WidgetData.Infrastructure --startup-project src/WidgetData.API
```

### Run Application

**Option 1: Toàn bộ hệ thống qua .NET Aspire (khuyến nghị)**

```bash
dotnet run --project src/WidgetData.AppHost
```

AppHost sẽ khởi động song song: API → Worker → Web → Gateway → Demo projects.

**Option 2: Chạy riêng từng service**

```bash
# Terminal 1: API
dotnet run --project src/WidgetData.API
# API: https://localhost:7001
# Scalar API docs: https://localhost:7001/scalar

# Terminal 2: Worker (cron job executor)
dotnet run --project src/WidgetData.Worker

# Terminal 3: Blazor Web
dotnet run --project src/WidgetData.Web
# Frontend: https://localhost:5001
```

### Access Application

- **Frontend (Blazor Admin)**: https://localhost:5001
- **API**: https://localhost:7001
- **API Docs (Scalar)**: https://localhost:7001/scalar
- **API Gateway (YARP)**: https://localhost:7000
- **.NET Aspire Dashboard**: https://localhost:15888

**Default credentials:**
- Email: `admin@widgetdata.com`
- Password: `Admin@123`

---

## 🐳 2. Docker Deployment

### Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution & projects
COPY ["src/WidgetData.Web/WidgetData.Web.csproj", "WidgetData.Web/"]
COPY ["src/WidgetData.Application/WidgetData.Application.csproj", "WidgetData.Application/"]
COPY ["src/WidgetData.Infrastructure/WidgetData.Infrastructure.csproj", "WidgetData.Infrastructure/"]
COPY ["src/WidgetData.Domain/WidgetData.Domain.csproj", "WidgetData.Domain/"]

# Restore dependencies
RUN dotnet restore "WidgetData.Web/WidgetData.Web.csproj"

# Copy source code
COPY src/ .

# Build & publish
WORKDIR "/src/WidgetData.Web"
RUN dotnet publish "WidgetData.Web.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose ports
EXPOSE 80
EXPOSE 443

# Set environment
ENV ASPNETCORE_URLS=http://+:80

# Run application
ENTRYPOINT ["dotnet", "WidgetData.Web.dll"]
```

### docker-compose.yml

```yaml
version: '3.8'

services:
  # SQL Server
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: widgetdata-sql
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    networks:
      - widgetdata-network

  # Redis
  redis:
    image: redis:7-alpine
    container_name: widgetdata-redis
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data
    networks:
      - widgetdata-network

  # Application
  web:
    build:
      context: .
      dockerfile: docker/Dockerfile
    container_name: widgetdata-web
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=WidgetData;User=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
      - Redis__Configuration=redis:6379
      - Jwt__SecretKey=${JWT_SECRET_KEY}
    ports:
      - "8080:80"
      - "8443:443"
    depends_on:
      - sqlserver
      - redis
    networks:
      - widgetdata-network

volumes:
  sqldata:
  redisdata:

networks:
  widgetdata-network:
    driver: bridge
```

### Build & Run

```bash
# Build images
docker-compose build

# Run containers
docker-compose up -d

# View logs
docker-compose logs -f web

# Stop containers
docker-compose down

# Stop & remove volumes
docker-compose down -v
```

### Database Migration in Docker

```bash
# Run migrations
docker-compose exec web dotnet ef database update --project /app/WidgetData.Infrastructure.dll

# Or use init script
docker-compose exec web dotnet /app/WidgetData.Web.dll migrate
```

---

## 🖥️ 3. IIS Deployment (Windows Server)

### Prerequisites

- Windows Server 2019+
- IIS 10+
- .NET 8 Hosting Bundle
- SQL Server 2019+

### Install .NET Hosting Bundle

```powershell
# Download & install
Invoke-WebRequest -Uri https://download.visualstudio.microsoft.com/download/pr/.../dotnet-hosting-8.0-win.exe -OutFile dotnet-hosting.exe
.\dotnet-hosting.exe /install /quiet /norestart

# Restart IIS
iisreset
```

### Publish Application

```bash
# Publish to folder
dotnet publish src/WidgetData.Web/WidgetData.Web.csproj -c Release -o C:\inetpub\wwwroot\WidgetData

# Or publish profile
dotnet publish src/WidgetData.Web/WidgetData.Web.csproj /p:PublishProfile=IIS
```

### Create IIS Application Pool

```powershell
# Import IIS module
Import-Module WebAdministration

# Create App Pool
New-WebAppPool -Name "WidgetDataAppPool"
Set-ItemProperty IIS:\AppPools\WidgetDataAppPool -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty IIS:\AppPools\WidgetDataAppPool -Name "enable32BitAppOnWin64" -Value $false

# Set identity (recommended: use managed service account)
Set-ItemProperty IIS:\AppPools\WidgetDataAppPool -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
```

### Create IIS Website

```powershell
# Create website
New-Website -Name "WidgetData" `
    -PhysicalPath "C:\inetpub\wwwroot\WidgetData" `
    -ApplicationPool "WidgetDataAppPool" `
    -Port 80

# Add HTTPS binding (if you have certificate)
New-WebBinding -Name "WidgetData" -Protocol "https" -Port 443 -SslFlags 0

# Assign SSL certificate
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*widgetdata.com*"}
New-Item -Path "IIS:\SslBindings\0.0.0.0!443" -Value $cert
```

### Configure appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=WidgetData;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### Set Permissions

```powershell
# Grant IIS AppPool access to folder
$acl = Get-Acl "C:\inetpub\wwwroot\WidgetData"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS AppPool\WidgetDataAppPool", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($rule)
Set-Acl "C:\inetpub\wwwroot\WidgetData" $acl
```

### Verify Deployment

```powershell
# Check application pool status
Get-WebAppPoolState -Name "WidgetDataAppPool"

# Check website status
Get-Website -Name "WidgetData"

# Test URL
Start-Process "http://localhost"
```

---

## ☁️ 4. Azure App Service Deployment

### Option A: Deploy from Visual Studio

1. Right-click project → **Publish**
2. Select **Azure** → **Azure App Service (Windows/Linux)**
3. Create new or select existing App Service
4. Configure settings:
   - Runtime: .NET 8
   - Region: Southeast Asia
   - Pricing tier: B1 or higher
5. Click **Publish**

### Option B: Deploy via Azure CLI

```bash
# Login to Azure
az login

# Create resource group
az group create --name WidgetDataRG --location southeastasia

# Create App Service Plan
az appservice plan create \
    --name WidgetDataPlan \
    --resource-group WidgetDataRG \
    --sku B1 \
    --is-linux

# Create Web App
az webapp create \
    --name widgetdata-app \
    --resource-group WidgetDataRG \
    --plan WidgetDataPlan \
    --runtime "DOTNET|8.0"

# Deploy code
dotnet publish -c Release -o ./publish
cd publish
zip -r ../publish.zip .
az webapp deployment source config-zip \
    --resource-group WidgetDataRG \
    --name widgetdata-app \
    --src ../publish.zip
```

### Configure App Settings

```bash
# Set connection string
az webapp config connection-string set \
    --name widgetdata-app \
    --resource-group WidgetDataRG \
    --connection-string-type SQLAzure \
    --settings DefaultConnection="Server=tcp:widgetdata-sql.database.windows.net,1433;Database=WidgetData;User ID=sqladmin;Password=YourPassword123!;Encrypt=True;"

# Set app settings
az webapp config appsettings set \
    --name widgetdata-app \
    --resource-group WidgetDataRG \
    --settings \
        Jwt__SecretKey="your-secret-key" \
        Redis__Configuration="widgetdata-redis.redis.cache.windows.net:6380,password=your-redis-key,ssl=True"
```

### Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
    --name widgetdata-sql \
    --resource-group WidgetDataRG \
    --location southeastasia \
    --admin-user sqladmin \
    --admin-password YourPassword123!

# Create database
az sql db create \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --name WidgetData \
    --service-objective S0

# Allow Azure services
az sql server firewall-rule create \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --name AllowAzure \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0
```

### Create Azure Cache for Redis

```bash
az redis create \
    --name widgetdata-redis \
    --resource-group WidgetDataRG \
    --location southeastasia \
    --sku Basic \
    --vm-size C0
```

### Run Database Migrations

```bash
# Option 1: From local machine
$env:ConnectionStrings__DefaultConnection="Server=tcp:widgetdata-sql.database.windows.net,1433;..."
dotnet ef database update --project src/WidgetData.Infrastructure --startup-project src/WidgetData.Web

# Option 2: Use Azure CLI with SSH
az webapp create-remote-connection --resource-group WidgetDataRG --name widgetdata-app
dotnet ef database update
```

---

## 🔐 Environment Configuration

### appsettings.Development.json (API)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=widgetdata.db"
  },
  "JwtSettings": {
    "Secret": "dev-secret-key-change-in-production",
    "Issuer": "WidgetData.API",
    "Audience": "WidgetData.Client",
    "ExpirationHours": 24
  },
  "InactivityMonitor": {
    "CheckIntervalMinutes": 60,
    "DefaultThresholdDays": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### appsettings.json (Worker)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=widgetdata.db"
  },
  "SchedulerWorker": {
    "PollingIntervalSeconds": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

### Environment Variables

```bash
# Development (.env)
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Server=localhost;Database=WidgetData;...
Jwt__SecretKey=your-secret-key

# Production (Azure App Settings)
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=tcp:...
Jwt__SecretKey=<from-key-vault>
Redis__Configuration=<redis-connection>
```

---

## 🚀 CI/CD Pipeline

### GitHub Actions

```yaml
# .github/workflows/deploy.yml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
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
      run: dotnet build --configuration Release --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish src/WidgetData.Web/WidgetData.Web.csproj -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: widgetdata-app
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

---

## 🔍 Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "database")
    .AddRedis(redisConnection, name: "redis")
    .AddHangfire(options => { }, name: "hangfire");

app.MapHealthChecks("/health");
```

**Test:**
```bash
curl https://localhost:5001/health
```

---

## 📊 Monitoring & Logging

### Application Insights (Azure)

```bash
# Add package
dotnet add package Microsoft.ApplicationInsights.AspNetCore

# Configure
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]
);
```

### Serilog

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341") // Optional
    .CreateLogger();

builder.Host.UseSerilog();
```

---

← [Quay lại INDEX](INDEX.md)
