# Sao lưu & Tính khả dụng cao

## 📋 Tổng quan

Widget Data đảm bảo **99.9% uptime** thông qua:

1. **Database Backup** - Sao lưu & khôi phục tự động
2. **High Availability** - Cân bằng tải, dự phòng
3. **Disaster Recovery** - RPO < 15 phút, RTO < 1 giờ
4. **Data Redundancy** - Nhiều bản sao dữ liệu

---

## 💾 1. Chiến lược Sao lưu Database

### Loại sao lưu

| Loại | Tần suất | Lưu trữ | Mục đích |
|------|-----------|-----------|---------|
| **Sao lưu đầy đủ** | Hàng ngày (2 giờ sáng) | 30 ngày | Toàn bộ database |
| **Sao lưu vi sai** | Mỗi 6 giờ | 7 ngày | Thay đổi từ lần full backup cuối |
| **Nhật ký giao dịch** | Mỗi 15 phút | 24 giờ | Khôi phục theo thời điểm |

### SQL Server Backup

```sql
-- Sao lưu toàn bộ (Full Backup)
BACKUP DATABASE WidgetData
TO DISK = 'C:\Backups\WidgetData_Full_{date}.bak'
WITH COMPRESSION, CHECKSUM, STATS = 10;

-- Sao lưu vi sai (Differential Backup)
BACKUP DATABASE WidgetData
TO DISK = 'C:\Backups\WidgetData_Diff_{date}.bak'
WITH DIFFERENTIAL, COMPRESSION, CHECKSUM;

-- Sao lưu Transaction Log
BACKUP LOG WidgetData
TO DISK = 'C:\Backups\WidgetData_Log_{date}.trn'
WITH COMPRESSION, CHECKSUM;
```

### Script Sao lưu Tự động (PowerShell)

```powershell
# BackupDatabase.ps1
param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "WidgetData",
    [string]$BackupPath = "C:\Backups"
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "$BackupPath\${Database}_Full_$timestamp.bak"

# Tạo thư mục backup nếu chưa tồn tại
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath
}

# Thực thi backup
$query = @"
BACKUP DATABASE [$Database]
TO DISK = N'$backupFile'
WITH COMPRESSION, CHECKSUM, STATS = 10, INIT
"@

Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $query

# Xóa các bản backup cũ hơn 30 ngày
Get-ChildItem $BackupPath -Filter "${Database}_Full_*.bak" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
    Remove-Item -Force

Write-Host "Backup hoàn thành: $backupFile"
```

### Lên lịch Sao lưu (Windows Task Scheduler)

```powershell
# Tạo scheduled task
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" `
    -Argument "-File C:\Scripts\BackupDatabase.ps1"

$trigger = New-ScheduledTaskTrigger -Daily -At 2:00AM

$principal = New-ScheduledTaskPrincipal -UserId "NT AUTHORITY\SYSTEM" `
    -LogonType ServiceAccount -RunLevel Highest

Register-ScheduledTask -TaskName "WidgetData-DailyBackup" `
    -Action $action -Trigger $trigger -Principal $principal
```

### Azure SQL Backup (Automated)

```bash
# Bật automated backup (mặc định trên Azure SQL)
az sql db show --name WidgetData \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --query "earliestRestoreDate"

# Cấu hình retention
az sql db ltr-policy set \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --database WidgetData \
    --weekly-retention P4W \
    --monthly-retention P12M \
    --yearly-retention P5Y \
    --week-of-year 1
```

---

## 🔄 2. Quy trình Khôi phục

### Khôi phục Đầy đủ (SQL Server)

```sql
-- Dừng ứng dụng trước!

-- Khôi phục full backup
RESTORE DATABASE WidgetData
FROM DISK = 'C:\Backups\WidgetData_Full_20260410.bak'
WITH REPLACE, NORECOVERY, STATS = 10;

-- Khôi phục differential (nếu có)
RESTORE DATABASE WidgetData
FROM DISK = 'C:\Backups\WidgetData_Diff_20260410.bak'
WITH NORECOVERY, STATS = 10;

-- Khôi phục transaction log
RESTORE LOG WidgetData
FROM DISK = 'C:\Backups\WidgetData_Log_20260410_1400.trn'
WITH RECOVERY, STATS = 10;

-- Kiểm tra database
DBCC CHECKDB(WidgetData);
```

### Khôi phục Theo Thời điểm

```sql
RESTORE DATABASE WidgetData
FROM DISK = 'C:\Backups\WidgetData_Full_20260410.bak'
WITH REPLACE, NORECOVERY;

RESTORE LOG WidgetData
FROM DISK = 'C:\Backups\WidgetData_Log_20260410_1400.trn'
WITH RECOVERY, STOPAT = '2026-04-10 14:30:00';
```

### Azure SQL Restore

```bash
# Khôi phục theo thời điểm (point-in-time restore)
az sql db restore \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --name WidgetData \
    --dest-name WidgetData-Restored \
    --time "2026-04-10T14:30:00Z"

# Khôi phục từ bản backup lưu trữ dài hạn
az sql db ltr-backup restore \
    --location southeastasia \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --database WidgetData \
    --backup-id "/subscriptions/.../backups/..." \
    --dest-database WidgetData-Restored
```

---

## 🔁 3. Kiến trúc Tính khả dụng cao

### Thiết lập Cân bằng tải

```
                    ┌─────────────┐
                    │   Azure LB  │
                    │  (Port 443) │
                    └──────┬──────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
   ┌────▼────┐      ┌──────▼─────┐     ┌─────▼────┐
   │ Web-01  │      │  Web-02    │     │  Web-03  │
   │ (Active)│      │  (Active)  │     │ (Standby)│
   └────┬────┘      └──────┬─────┘     └─────┬────┘
        │                  │                  │
        └──────────────────┼──────────────────┘
                           │
                    ┌──────▼──────┐
                    │  SQL Server │
                    │ Always On AG│
                    └─────────────┘
```

### SQL Server Always On Availability Group

```sql
-- Tạo Availability Group
CREATE AVAILABILITY GROUP [WidgetData_AG]
FOR DATABASE [WidgetData]
REPLICA ON 
    'SQL-Primary' WITH (
        ENDPOINT_URL = 'TCP://sql-primary.local:5022',
        AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
        FAILOVER_MODE = AUTOMATIC,
        SECONDARY_ROLE (ALLOW_CONNECTIONS = READ_ONLY)
    ),
    'SQL-Secondary' WITH (
        ENDPOINT_URL = 'TCP://sql-secondary.local:5022',
        AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
        FAILOVER_MODE = AUTOMATIC,
        SECONDARY_ROLE (ALLOW_CONNECTIONS = READ_ONLY)
    );

-- Tạo Listener
ALTER AVAILABILITY GROUP [WidgetData_AG]
ADD LISTENER 'WidgetData-Listener' (
    WITH IP ((N'10.0.0.100', N'255.255.255.0')),
    PORT = 1433
);
```

### Chuỗi kết nối cho HA

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=WidgetData-Listener;Database=WidgetData;Integrated Security=True;MultiSubnetFailover=True;ApplicationIntent=ReadWrite;"
  }
}
```

### Azure SQL with Geo-Replication

```bash
# Tạo database phụ (geo-replica)
az sql db replica create \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --name WidgetData \
    --partner-resource-group WidgetDataRG-DR \
    --partner-server widgetdata-sql-dr \
    --partner-database WidgetData-DR

# Chuyển đổi dự phòng sang secondary
az sql db replica set-primary \
    --resource-group WidgetDataRG-DR \
    --server widgetdata-sql-dr \
    --name WidgetData-DR
```

---

## 🚨 4. Kế hoạch Phục hồi sau Sự cố

### Mục tiêu Khôi phục

| Chỉ số | Mục tiêu | Định nghĩa |
|--------|--------|------------|
| **RPO** (Recovery Point Objective) | < 15 phút | Mức mất dữ liệu tối đa có thể chấp nhận |
| **RTO** (Recovery Time Objective) | < 1 giờ | Thời gian ngừng hoạt động tối đa có thể chấp nhận |
| **MTTR** (Mean Time To Repair) | < 30 phút | Thời gian sửa chữa trung bình |

### Danh sách kiểm tra DR

#### Giai đoạn 1: Phát hiện (0–5 phút)
- [ ] Cảnh báo giám sát được kích hoạt
- [ ] Sự cố được ghi vào hệ thống quản lý ticket
- [ ] Kỹ sư trực được thông báo
- [ ] Đánh giá mức độ nghiêm trọng & phạm vi ảnh hưởng

#### Giai đoạn 2: Cô lập (5–15 phút)
- [ ] Cô lập các thành phần bị ảnh hưởng
- [ ] Chuyển sang chế độ chỉ đọc nếu cần
- [ ] Thông báo cho các bên liên quan
- [ ] Bắt đầu phân tích nguyên nhân gốc rễ

#### Giai đoạn 3: Khôi phục (15–45 phút)
- [ ] Thực hiện chuyển đổi dự phòng sang site DR
- [ ] Khôi phục từ backup (nếu cần)
- [ ] Xác minh tính toàn vẹn dữ liệu
- [ ] Kiểm thử các quy trình quan trọng

#### Giai đoạn 4: Xác minh (45–60 phút)
- [ ] Chạy smoke test
- [ ] Xác minh tất cả dịch vụ hoạt động bình thường
- [ ] Theo dõi các bất thường
- [ ] Cập nhật trang trạng thái

#### Giai đoạn 5: Sau sự cố (Sau khi khôi phục)
- [ ] Ghi lại timeline sự cố
- [ ] Tiến hành post-mortem
- [ ] Triển khai các biện pháp phòng ngừa
- [ ] Cập nhật runbook

### Sổ tay Phục hồi DR

```powershell
# DR-Runbook.ps1
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("DatabaseFailure", "ApplicationFailure", "NetworkOutage")]
    [string]$IncidentType
)

Write-Host "=== KHỞI ĐỘNG KHÔI PHỤC THẢM HỌA ===" -ForegroundColor Red
Write-Host "Loại sự cố: $IncidentType"
Write-Host "Thời gian: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

switch ($IncidentType) {
    "DatabaseFailure" {
        Write-Host "`n1. Kiểm tra trạng thái database..."
        # Kiểm tra database chính
        $dbStatus = Invoke-Sqlcmd -ServerInstance "sql-primary" -Query "SELECT @@SERVERNAME, DATABASEPROPERTYEX('WidgetData', 'Status')"
        
        if ($dbStatus.Column1 -ne "ONLINE") {
            Write-Host "2. DB chính offline. Đang khởi động failover..."
            
            # Chuyển đổi dự phòng sang secondary
            Invoke-Sqlcmd -ServerInstance "sql-secondary" -Query "ALTER AVAILABILITY GROUP [WidgetData_AG] FAILOVER;"
            
            Write-Host "3. Cập nhật cấu hình ứng dụng..."
            # Cập nhật connection string trỏ sang secondary
            
            Write-Host "4. Xác minh failover..."
            Start-Sleep -Seconds 10
            
            # Kiểm tra kết nối
            $newStatus = Invoke-Sqlcmd -ServerInstance "sql-secondary" -Query "SELECT DB_NAME(), @@VERSION"
            Write-Host "Failover hoàn thành. Primary mới: $($newStatus.Column1)"
        }
    }
    
    "ApplicationFailure" {
        Write-Host "`n1. Khởi động lại application pools..."
        Restart-WebAppPool -Name "WidgetDataAppPool"
        
        Write-Host "2. Kiểm tra health endpoint..."
        $health = Invoke-RestMethod -Uri "https://localhost:5001/health"
        
        if ($health.status -eq "Healthy") {
            Write-Host "Ứng dụng đã khôi phục thành công"
        } else {
            Write-Host "Ứng dụng vẫn không khỏe mạnh. Đang leo thang..."
        }
    }
    
    "NetworkOutage" {
        Write-Host "`n1. Kiểm tra kết nối mạng..."
        Test-NetConnection -ComputerName "sql-primary" -Port 1433
        
        Write-Host "2. Kích hoạt site DR..."
        # Logic kích hoạt datacenter DR
    }
}

Write-Host "`n=== QUY TRÌNH DR HOÀN TẤT ===" -ForegroundColor Green
```

---

## 📂 5. Sao lưu File & Cấu hình

### Sao lưu File ứng dụng

```powershell
# BackupAppFiles.ps1
$backupPath = "\\backup-server\WidgetData\AppFiles"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$destination = "$backupPath\Backup_$timestamp"

# Sao lưu file ứng dụng
Copy-Item "C:\inetpub\wwwroot\WidgetData" -Destination $destination -Recurse

# Sao lưu cấu hình
Copy-Item "C:\WidgetData\appsettings.Production.json" -Destination "$destination\Config"

# Tạo archive
Compress-Archive -Path $destination -DestinationPath "$destination.zip"

Write-Host "Application files backed up to: $destination.zip"
```

### Sao lưu Dữ liệu Redis

```bash
# Snapshot Redis (tự động)
redis-cli BGSAVE

# Sao chép file RDB
cp /var/lib/redis/dump.rdb /backups/redis/dump_$(date +%Y%m%d_%H%M%S).rdb

# Lên lịch với cron
0 2 * * * redis-cli BGSAVE && cp /var/lib/redis/dump.rdb /backups/redis/dump_$(date +\%Y\%m\%d).rdb
```

---

## 🔐 6. Bảo mật Backup

### Mã hóa Dữ liệu lưu trữ

```sql
-- Tạo chứng chỉ
CREATE CERTIFICATE BackupCertificate
WITH SUBJECT = 'Widget Data Backup Certificate';

-- Sao lưu có mã hóa
BACKUP DATABASE WidgetData
TO DISK = 'C:\Backups\WidgetData_Encrypted.bak'
WITH COMPRESSION,
     ENCRYPTION (
         ALGORITHM = AES_256,
         SERVER CERTIFICATE = BackupCertificate
     );
```

### Tải lên Cloud Storage (Azure)

```powershell
# Cài đặt Azure PowerShell
Install-Module -Name Az -AllowClobber

# Tải backup lên Azure Blob Storage
$storageAccount = "widgetdatabackups"
$containerName = "database-backups"
$backupFile = "C:\Backups\WidgetData_Full_20260410.bak"

$context = New-AzStorageContext -StorageAccountName $storageAccount -StorageAccountKey $key

Set-AzStorageBlobContent -File $backupFile `
    -Container $containerName `
    -Blob "WidgetData_$(Get-Date -Format 'yyyyMMdd').bak" `
    -Context $context
```

---

## 📊 7. Giám sát Backup

### Xác minh Backup thành công

```sql
-- Kiểm tra backup gần nhất
SELECT 
    database_name,
    backup_start_date,
    backup_finish_date,
    DATEDIFF(SECOND, backup_start_date, backup_finish_date) AS duration_seconds,
    compressed_backup_size / 1024 / 1024 AS size_mb,
    type,
    CASE type
        WHEN 'D' THEN 'Full'
        WHEN 'I' THEN 'Differential'
        WHEN 'L' THEN 'Log'
    END AS backup_type
FROM msdb.dbo.backupset
WHERE database_name = 'WidgetData'
ORDER BY backup_start_date DESC;
```

### Job Xác thực Backup

```csharp
public class BackupValidationJob
{
    private readonly ILogger<BackupValidationJob> _logger;
    private readonly IEmailService _emailService;
    
    public async Task ValidateBackupsAsync()
    {
        var backupPath = @"C:\Backups";
        var today = DateTime.Today;
        
        // Kiểm tra xem backup hôm nay có tồn tại không
        var todayBackup = Directory.GetFiles(backupPath, $"*Full_{today:yyyyMMdd}*.bak");
        
        if (todayBackup.Length == 0)
        {
            _logger.LogError("Thiếu backup hàng ngày cho {Date}", today);
            
            await _emailService.SendAlertAsync(
                "admin@widgetdata.com",
                "CẢNH BÁO: Thiếu Backup Hàng ngày",
                $"Không tìm thấy backup cho {today:yyyy-MM-dd}"
            );
        }
        else
        {
            _logger.LogInformation("Backup đã xác minh: {BackupFile}", todayBackup[0]);
        }
        
        // Kiểm tra độ tuổi backup
        var latestBackup = new DirectoryInfo(backupPath)
            .GetFiles("*Full*.bak")
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();
        
        if (latestBackup != null && (DateTime.Now - latestBackup.LastWriteTime).TotalHours > 25)
        {
            _logger.LogWarning("Latest backup is {Hours} hours old", 
                (DateTime.Now - latestBackup.LastWriteTime).TotalHours);
        }
    }
}
```

---

## ✅ Danh sách kiểm tra Backup & DR

### Hàng ngày
- [ ] Xác minh backup tự động đã hoàn thành
- [ ] Kiểm tra kích thước file backup (trong phạm vi mong đợi)
- [ ] Theo dõi dung lượng đĩa trên storage backup
- [ ] Xem xét log backup để phát hiện lỗi

### Hàng tuần
- [ ] Kiểm thử quy trình khôi phục (môi trường không phải sản xuất)
- [ ] Xác minh chính sách giữ backup
- [ ] Kiểm tra đồng bộ backup offsite

### Hàng tháng
- [ ] DR drill toàn diện (mô phỏng sự cố ngừng hoạt động)
- [ ] Cập nhật tài liệu DR
- [ ] Xem xét chi phí giữ backup
- [ ] Kiểm toán log truy cập backup

### Hàng quý
- [ ] Xem xét & cập nhật kế hoạch DR
- [ ] Tối ưu hóa chiến lược backup
- [ ] Đào tạo DR cho đội nhóm
- [ ] Kiểm toán tuân thủ

---

← [Quay lại INDEX](INDEX.md)
