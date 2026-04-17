# sync-storefront.ps1
# Đồng bộ WidgetEngine library từ src/WidgetData.Web/wwwroot/widget-engine
# vào demo/storefront/lib/ và demo/storefront/pages/
# Chạy script này sau khi cập nhật widget-engine.js / widget-engine.css

$src = "$PSScriptRoot\..\..\src\WidgetData.Web\wwwroot\widget-engine"
$libDest   = "$PSScriptRoot\shop-front\lib"
$pagesDest = "$PSScriptRoot\shop-front\pages"

if (-not (Test-Path $src)) {
    Write-Error "Không tìm thấy source: $src"
    exit 1
}

# Sync lib (JS + CSS)
Copy-Item "$src\widget-engine.js"  "$libDest\widget-engine.js"  -Force
Copy-Item "$src\widget-engine.css" "$libDest\widget-engine.css" -Force
Write-Host "[OK] storefront/lib/ synced"

# Sync pages (JSON configs)
Copy-Item "$src\pages\*" "$pagesDest\" -Force
Write-Host "[OK] storefront/pages/ synced"

Write-Host "Done. Storefront is up to date."
