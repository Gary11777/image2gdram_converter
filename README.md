# Image2GDRAM Converter 1.0

Программа для Windows превращает изображение или набор символов в массив байтов видеопамяти монохромного дисплея (GDRAM). На выходе — исходный текст C для Keil C51 и для STM32, модуль или фрагмент A51, либо двоичный файл BIN.

Целевые модули: WG240128A (T6963C), W0240128 (UC1608), RG12864F (NT7108), RET012864DGPP3N (SSD1305), OLED128X64-0.96 (SSD1306), HT1.3-OLED-BW / HR0161 (SH1106).

Интерфейс на русском. Программе не нужны права администратора и доступ в интернет. Исходные файлы изображений и шрифтов она не изменяет.

## Что нужно для сборки

- Windows 10 или 11, 64-разрядная
- .NET SDK 8.0 (проверено на SDK 8.0.425)

Сторонний декодер изображений не используется: BMP, PNG, JPEG и GIF читает WIC, который входит в Windows.

## Сборка и тесты

Из корня репозитория:

```powershell
.\build.ps1
```

Скрипт выполняет `dotnet build -c Release` и `dotnet test -c Release`. Ожидается 0 ошибок и 0 предупреждений, все тесты зелёные.

По отдельности:

```powershell
dotnet build -c Release
dotnet test -c Release
```

Запуск из исходников (окно откроется на несколько секунд, его можно закрыть):

```powershell
dotnet run --project src/image2gdram_converter -c Release
```

## Публикация

```powershell
.\publish.ps1
```

Команда публикует один самодостаточный файл `publish\image2gdram_converter.exe` для Windows x64. Установленный .NET на машине пользователя не нужен. При первом закрытии программа создаёт `settings.json` рядом с exe. Если папка exe недоступна для записи, настройки пишутся в `%APPDATA%\ImageIU\settings.json`.

Версия сборки — 1.0.0, имя продукта — Image2GDRAM Converter.

## Тестовые материалы

Изображения приложения Б и листы шрифтов лежат в `testdata/`. Эталонные массивы — в `testdata/reference/` (описание в `index.md`). Их строит независимая от ядра утилита:

```powershell
dotnet run --project tools/TestAssetsGenerator -c Release
```

Эталоны нужно согласовать с заказчиком. Подробности — в `docs/pmi.md` и `docs/decisions.md` (вопросы Q-01, Q-08, Q-11).

## Документация

| Файл | Содержание |
|---|---|
| `docs/user_guide.md` | Руководство пользователя |
| `docs/output_formats.md` | Форматы вывода и примеры для пресетов |
| `docs/pmi.md` | Программа и методика испытаний |
| `docs/decisions.md` | Решения по неоднозначностям ТЗ |
| `specification.md` | Техническое задание |

## Лицензии зависимостей

Во время работы программы:

| Пакет | Версия | Лицензия |
|---|---|---|
| CommunityToolkit.Mvvm | 8.4.2 | MIT |
| AvalonEdit | 6.3.1.120 | MIT |

Платформа — .NET 8 (библиотека Microsoft, поставляется внутри self-contained exe).

Только для сборки тестов, в опубликованный exe не входят:

| Пакет | Версия | Лицензия |
|---|---|---|
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 2.8.2 | Apache-2.0 |
| Microsoft.NET.Test.Sdk | 17.12.0 | MIT |

Six Labors ImageSharp не используется.
