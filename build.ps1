# Сборка Release и тесты. Запуск из корня репозитория: .\build.ps1
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $MyInvocation.MyCommand.Path)

dotnet build -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet test -c Release --no-build --nologo
exit $LASTEXITCODE
