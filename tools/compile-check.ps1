<#
.SYNOPSIS
    Дымовая проверка компиляции сгенерированного кода C (раздел 11 agents.md, п. 11.2 ТЗ).

.DESCRIPTION
    Генерирует образцы вывода утилитой tools/OutputSamples (изображения, кадры GIF, шрифты трёх размеров;
    все форматы), затем компилирует их каждым найденным компилятором из списка gcc, clang, arm-none-eabi-gcc:
      - вывод для STM32 — с -std=c99 -Wall -Wextra -pedantic -Werror;
      - вывод для Keil C51 — с теми же ключами и -Dcode=.
    Если компиляторов нет, сообщает об этом и пропускает компиляцию (код возврата 0).
    Компиляция в Keil C51/A51, MDK-ARM, IAR EWARM и STM32CubeIDE — действие заказчика (ПМИ).

.PARAMETER OutDir
    Папка для образцов и объектных файлов. По умолчанию artifacts/compile-check в корне репозитория.
#>
param(
    [string]$OutDir = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\compile-check')
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent

if (Test-Path $OutDir) {
    Remove-Item -Recurse -Force $OutDir
}

Write-Host "Генерация образцов вывода в $OutDir"
& dotnet run --project (Join-Path $repo 'tools\OutputSamples\OutputSamples.csproj') -c Release -- $OutDir | Out-Host
if ($LASTEXITCODE -ne 0) {
    Write-Error "OutputSamples завершилась с кодом $LASTEXITCODE."
    exit 1
}

$compilers = @('gcc', 'clang', 'arm-none-eabi-gcc') | Where-Object { Get-Command $_ -ErrorAction SilentlyContinue }
if (-not $compilers) {
    Write-Host 'Компиляторы gcc, clang и arm-none-eabi-gcc не найдены: дымовая компиляция пропущена.'
    exit 0
}

$flags = @('-std=c99', '-Wall', '-Wextra', '-pedantic', '-Werror', '-c')
$sets = @(
    @{ Folder = 'stm32'; Extra = @() },
    @{ Folder = 'stm32_uchar'; Extra = @() },
    @{ Folder = 'stm32_utf8'; Extra = @() },
    @{ Folder = 'c51'; Extra = @('-Dcode=') }
)

$failed = 0
foreach ($cc in $compilers) {
    foreach ($set in $sets) {
        $source = Join-Path $OutDir $set.Folder
        $objects = Join-Path $OutDir "obj\$cc\$($set.Folder)"
        New-Item -ItemType Directory -Force -Path $objects | Out-Null
        foreach ($file in Get-ChildItem -Path $source -Filter '*.c' | Sort-Object Name) {
            $object = Join-Path $objects ($file.BaseName + '.o')
            $arguments = $flags + $set.Extra + @('-I', $source, $file.FullName, '-o', $object)
            & $cc @arguments
            if ($LASTEXITCODE -ne 0) {
                Write-Host "ОШИБКА: $cc $($set.Folder)\$($file.Name)"
                $failed++
            }
            else {
                Write-Host "OK: $cc $($set.Folder)\$($file.Name)"
            }
        }
    }
}

if ($failed -gt 0) {
    Write-Error "Не скомпилировано файлов: $failed."
    exit 1
}

Write-Host 'Дымовая компиляция пройдена.'
exit 0
