# Backup & High Availability

## 📋 Tổng quan

Widget Data đảm bảo **99.9% uptime** thông qua:

1. **Database Backup** - Automated backup & restore
2. **High Availability** - Load balancing, failover
3. **Disaster Recovery** - RPO < 15 min, RTO < 1 hour
4. **Data Redundancy** - Multiple replicas

---

## 💾 1. Database Backup Strategy

### Backup Types

| Type | Frequency | Retention | Purpose |
|------|-----------|-----------|---------|
| **Full Backup** | Daily (2 AM) | 30 days | Complete database |
| **Differential** | Every 6 hours | 7 days | Changes since last full |
| **Transaction Log** | Every 15 min | 24 hours | Point-in-time recovery |

### SQL Server Backup

```sql
-- Full Backup
BACKUP DATABASE WidgetData
TO DISK = 'C:\Backups\WidgetData_Full_{date}.bak'
WITH COMPRESSION, CHECKSUM, STATS = 10;

-- Differential Backup
BACKUP DATABASE WidgetData
TO DISK = 'C:\Backups\WidgetData_Diff_{date}.bak'
WITH DIFFERENTIAL, COMPRESSION, CHECKSUM;

-- Transaction Log Backup
BACKUP LOG WidgetData
TO DISK = 'C:\Backups\WidgetData_Log_{date}.trn'
WITH COMPRESSION, CHECKSUM;
```

### Automated Backup Script (PowerShell)

```powershell
# BackupDatabase.ps1
param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "WidgetData",
    [string]$BackupPath = "C:\Backups"
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "$BackupPath\${Database}_Full_$timestamp.bak"

# Create backup directory if not exists
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath
}

# Execute backup
$query = @"
BACKUP DATABASE [$Database]
TO DISK = N'$backupFile'
WITH COMPRESSION, CHECKSUM, STATS = 10, INIT
"@

Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $query

# Delete backups older than 30 days
Get-ChildItem $BackupPath -Filter "${Database}_Full_*.bak" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
    Remove-Item -Force

Write-Host "Backup completed: $backupFile"
```

### Schedule Backup (Windows Task Scheduler)

```powershell
# Create scheduled task
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
# Enable automated backups (default on Azure SQL)
az sql db show --name WidgetData \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --query "earliestRestoreDate"

# Configure retention
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

## 🔄 2. Restore Procedures

### Full Restore (SQL Server)

```sql
-- Stop application first!

-- Restore full backup
RESTORE DATABASE WidgetData
FROM DISK = 'C:\Backups\WidgetData_Full_20260410.bak'
WITH REPLACE, NORECOVERY, STATS = 10;

-- Restore differential (if any)
RESTORE DATABASE WidgetData
FROM DISK = 'C:\Backups\WidgetData_Diff_20260410.bak'
WITH NORECOVERY, STATS = 10;

-- Restore transaction log
RESTORE LOG WidgetData
FROM DISK = 'C:\Backups\WidgetData_Log_20260410_1400.trn'
WITH RECOVERY, STATS = 10;

-- Verify database
DBCC CHECKDB(WidgetData);
```

### Point-in-Time Restore

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
# Point-in-time restore
az sql db restore \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --name WidgetData \
    --dest-name WidgetData-Restored \
    --time "2026-04-10T14:30:00Z"

# From long-term retention backup
az sql db ltr-backup restore \
    --location southeastasia \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --database WidgetData \
    --backup-id "/subscriptions/.../backups/..." \
    --dest-database WidgetData-Restored
```

---

## 🔁 3. High Availability Architecture

### Load Balanced Setup

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
-- Create Availability Group
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

-- Create Listener
ALTER AVAILABILITY GROUP [WidgetData_AG]
ADD LISTENER 'WidgetData-Listener' (
    WITH IP ((N'10.0.0.100', N'255.255.255.0')),
    PORT = 1433
);
```

### Connection String for HA

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=WidgetData-Listener;Database=WidgetData;Integrated Security=True;MultiSubnetFailover=True;ApplicationIntent=ReadWrite;"
  }
}
```

### Azure SQL with Geo-Replication

```bash
# Create secondary database (geo-replica)
az sql db replica create \
    --resource-group WidgetDataRG \
    --server widgetdata-sql \
    --name WidgetData \
    --partner-resource-group WidgetDataRG-DR \
    --partner-server widgetdata-sql-dr \
    --partner-database WidgetData-DR

# Failover to secondary
az sql db replica set-primary \
    --resource-group WidgetDataRG-DR \
    --server widgetdata-sql-dr \
    --name WidgetData-DR
```

---

## 🚨 4. Disaster Recovery Plan

### Recovery Objectives

| Metric | Target | Definition |
|--------|--------|------------|
| **RPO** (Recovery Point Objective) | < 15 minutes | Maximum acceptable data loss |
| **RTO** (Recovery Time Objective) | < 1 hour | Maximum acceptable downtime |
| **MTTR** (Mean Time To Repair) | < 30 minutes | Average repair time |

### DR Checklist

#### Phase 1: Detection (0-5 min)
- [ ] Monitoring alerts triggered
- [ ] Incident logged in ticketing system
- [ ] On-call engineer notified
- [ ] Assess severity & scope

#### Phase 2: Containment (5-15 min)
- [ ] Isolate affected components
- [ ] Switch to read-only mode if needed
- [ ] Notify stakeholders
- [ ] Begin root cause analysis

#### Phase 3: Recovery (15-45 min)
- [ ] Execute failover to DR site
- [ ] Restore from backup (if needed)
- [ ] Verify data integrity
- [ ] Test critical workflows

#### Phase 4: Verification (45-60 min)
- [ ] Run smoke tests
- [ ] Verify all services operational
- [ ] Monitor for anomalies
- [ ] Update status page

#### Phase 5: Post-Incident (After recovery)
- [ ] Document incident timeline
- [ ] Conduct post-mortem
- [ ] Implement preventive measures
- [ ] Update runbooks

### DR Runbook

```powershell
# DR-Runbook.ps1
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("DatabaseFailure", "ApplicationFailure", "NetworkOutage")]
    [string]$IncidentType
)

Write-Host "=== DISASTER RECOVERY INITIATED ===" -ForegroundColor Red
Write-Host "Incident Type: $IncidentType"
Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

switch ($IncidentType) {
    "DatabaseFailure" {
        Write-Host "`n1. Checking database status..."
        # Check primary database
        $dbStatus = Invoke-Sqlcmd -ServerInstance "sql-primary" -Query "SELECT @@SERVERNAME, DATABASEPROPERTYEX('WidgetData', 'Status')"
        
        if ($dbStatus.Column1 -ne "ONLINE") {
            Write-Host "2. Primary DB offline. Initiating failover..."
            
            # Failover to secondary
            Invoke-Sqlcmd -ServerInstance "sql-secondary" -Query "ALTER AVAILABILITY GROUP [WidgetData_AG] FAILOVER;"
            
            Write-Host "3. Updating app config..."
            # Update connection string to point to secondary
            
            Write-Host "4. Verifying failover..."
            Start-Sleep -Seconds 10
            
            # Test connection
            $newStatus = Invoke-Sqlcmd -ServerInstance "sql-secondary" -Query "SELECT DB_NAME(), @@VERSION"
            Write-Host "Failover complete. New primary: $($newStatus.Column1)"
        }
    }
    
    "ApplicationFailure" {
        Write-Host "`n1. Restarting application pools..."
        Restart-WebAppPool -Name "WidgetDataAppPool"
        
        Write-Host "2. Checking health endpoint..."
        $health = Invoke-RestMethod -Uri "https://localhost:5001/health"
        
        if ($health.status -eq "Healthy") {
            Write-Host "Application recovered successfully"
        } else {
            Write-Host "Application still unhealthy. Escalating..."
        }
    }
    
    "NetworkOutage" {
        Write-Host "`n1. Checking network connectivity..."
        Test-NetConnection -ComputerName "sql-primary" -Port 1433
        
        Write-Host "2. Activating DR site..."
        # Logic to activate DR datacenter
    }
}

Write-Host "`n=== DR PROCEDURE COMPLETED ===" -ForegroundColor Green
```

---

## 📂 5. File & Configuration Backup

### Application Files Backup

```powershell
# BackupAppFiles.ps1
$backupPath = "\\backup-server\WidgetData\AppFiles"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$destination = "$backupPath\Backup_$timestamp"

# Backup application files
Copy-Item "C:\inetpub\wwwroot\WidgetData" -Destination $destination -Recurse

# Backup configuration
Copy-Item "C:\WidgetData\appsettings.Production.json" -Destination "$destination\Config"

# Create archive
Compress-Archive -Path $destination -DestinationPath "$destination.zip"

Write-Host "Application files backed up to: $destination.zip"
```

### Redis Data Backup

```bash
# Redis snapshot (automatic)
redis-cli BGSAVE

# Copy RDB file
cp /var/lib/redis/dump.rdb /backups/redis/dump_$(date +%Y%m%d_%H%M%S).rdb

# Schedule with cron
0 2 * * * redis-cli BGSAVE && cp /var/lib/redis/dump.rdb /backups/redis/dump_$(date +\%Y\%m\%d).rdb
```

---

## 🔐 6. Backup Security

### Encryption at Rest

```sql
-- Create certificate
CREATE CERTIFICATE BackupCertificate
WITH SUBJECT = 'Widget Data Backup Certificate';

-- Backup with encryption
BACKUP DATABASE WidgetData
TO DISK = 'C:\Backups\WidgetData_Encrypted.bak'
WITH COMPRESSION,
     ENCRYPTION (
         ALGORITHM = AES_256,
         SERVER CERTIFICATE = BackupCertificate
     );
```

### Upload to Cloud Storage (Azure)

```powershell
# Install Azure PowerShell
Install-Module -Name Az -AllowClobber

# Upload backup to Azure Blob Storage
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

## 📊 7. Backup Monitoring

### Verify Backup Success

```sql
-- Check last backup
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

### Backup Validation Job

```csharp
public class BackupValidationJob
{
    private readonly ILogger<BackupValidationJob> _logger;
    private readonly IEmailService _emailService;
    
    public async Task ValidateBackupsAsync()
    {
        var backupPath = @"C:\Backups";
        var today = DateTime.Today;
        
        // Check if today's full backup exists
        var todayBackup = Directory.GetFiles(backupPath, $"*Full_{today:yyyyMMdd}*.bak");
        
        if (todayBackup.Length == 0)
        {
            _logger.LogError("Daily backup missing for {Date}", today);
            
            await _emailService.SendAlertAsync(
                "admin@widgetdata.com",
                "ALERT: Daily Backup Missing",
                $"No backup found for {today:yyyy-MM-dd}"
            );
        }
        else
        {
            _logger.LogInformation("Backup validated: {BackupFile}", todayBackup[0]);
        }
        
        // Check backup age
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

## ✅ Backup & DR Checklist

### Daily
- [ ] Verify automated backups completed
- [ ] Check backup file sizes (expected range)
- [ ] Monitor disk space on backup storage
- [ ] Review backup logs for errors

### Weekly
- [ ] Test restore procedure (non-production)
- [ ] Verify backup retention policy
- [ ] Check offsite backup synchronization

### Monthly
- [ ] Full DR drill (simulate outage)
- [ ] Update DR documentation
- [ ] Review backup retention costs
- [ ] Audit backup access logs

### Quarterly
- [ ] DR plan review & update
- [ ] Backup strategy optimization
- [ ] Team DR training
- [ ] Compliance audit

---

← [Quay lại INDEX](INDEX.md)
