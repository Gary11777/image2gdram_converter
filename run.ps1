# Пересборка, обновление publish\image2gdram_converter.exe и запуск.
# Запуск из корня репозитория: .\run.ps1 [-Test] [-Clean] [-Force]
#   -Test   перед публикацией выполнить build.ps1 (сборка и тесты)
#   -Clean  перед публикацией выполнить dotnet clean
#   -Force  завершить программу, если она не закрылась сама за 15 с
param(
    [switch]$Test,
    [switch]$Clean,
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $MyInvocation.MyCommand.Path)

$publish = Join-Path $PWD 'publish'
$exe = Join-Path $publish 'image2gdram_converter.exe'

$running = @(Get-Process -Name 'image2gdram_converter' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $exe })
foreach ($p in $running) {
    Write-Host "Закрываю запущенную копию (PID $($p.Id))..." -ForegroundColor Yellow
    [void]$p.CloseMainWindow()
    if (-not $p.WaitForExit(15000)) {
        if (-not $Force) {
            throw 'Программа не закрылась за 15 с (возможно, ждёт ответа на запрос сохранения). Закройте её или запустите .\run.ps1 -Force.'
        }
        $p.Kill()
        $p.WaitForExit()
    }
}

if ($Clean) {
    dotnet clean src/image2gdram_converter/image2gdram_converter.csproj -c Release --nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($Test) {
    & .\build.ps1
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& .\publish.ps1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Test-Path $exe)) { throw "Не найден $exe" }
Get-Item $exe | Format-Table Name, Length, LastWriteTime -AutoSize
Start-Process -FilePath $exe -WorkingDirectory $publish
