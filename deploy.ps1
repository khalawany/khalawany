# Run this script as Administrator
param()

$ErrorActionPreference = "Stop"
$ServiceName  = "FamilyTube"
$ServicePath  = "C:\FamilyTube"
$PublishOut   = "$PSScriptRoot\publish_out"
$ProjectFile  = "$PSScriptRoot\KhalawanyTube.csproj"

Write-Host "==> Building..." -ForegroundColor Cyan
dotnet publish $ProjectFile -c Release -r win-x64 --self-contained false -o $PublishOut
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed."; exit 1 }

Write-Host "==> Stopping service..." -ForegroundColor Cyan
Stop-Service $ServiceName -Force
Start-Sleep -Seconds 2

Write-Host "==> Copying files to $ServicePath ..." -ForegroundColor Cyan
# Preserve the database and uploads; only replace app files
$excludes = @("*.db", "*.db-shm", "*.db-wal", "uploads")
Get-ChildItem $PublishOut | Where-Object {
    $name = $_.Name
    -not ($excludes | Where-Object { $name -like $_ })
} | ForEach-Object {
    Copy-Item $_.FullName -Destination $ServicePath -Recurse -Force
}

# Also copy wwwroot (static assets - css, js, etc.)
if (Test-Path "$PublishOut\wwwroot") {
    Copy-Item "$PublishOut\wwwroot" -Destination $ServicePath -Recurse -Force
}

Write-Host "==> Starting service..." -ForegroundColor Cyan
Start-Service $ServiceName
Start-Sleep -Seconds 2

$status = (Get-Service $ServiceName).Status
Write-Host "==> Service status: $status" -ForegroundColor $(if ($status -eq 'Running') { 'Green' } else { 'Red' })
