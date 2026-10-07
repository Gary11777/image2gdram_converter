# Публикация одного exe для Windows x64. Результат: папка publish\
# Запуск из корня репозитория: .\publish.ps1
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $MyInvocation.MyCommand.Path)

dotnet publish src/image2gdram_converter/image2gdram_converter.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish

exit $LASTEXITCODE
