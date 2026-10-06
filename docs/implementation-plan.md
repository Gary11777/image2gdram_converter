# План реализации «Image2GDRAM Converter» 1.0

Файл состояния проекта (раздел 3 `agents.md`). Здесь записаны текущий этап, статусы этапов по таблице раздела 13 `agents.md`, результаты последних сборок и тестов, блокеры и оставшаяся работа. Следующая сессия начинает работу с этого файла, история чата для этого не нужна.

Связанные файлы состояния:

- [`requirements_checklist.md`](requirements_checklist.md) — каждый пункт ТЗ: где реализуется и чем проверяется;
- [`decisions.md`](decisions.md) — решения по неоднозначностям ТЗ и вопросы к заказчику;
- [`architecture.md`](architecture.md) — фактическая структура решения и целевая архитектура.

---

## Текущее состояние

| Параметр | Значение |
|---|---|
| Последний завершённый этап | 4 — «Шрифты (ядро)» (`completed`) |
| Текущий этап | нет: этап 5 не начат |
| Дата обновления | 2026-10-06 |
| Ветка git | `4th_stage_2` (часть 1 этапа 4 — `ee234c8`, влита как `e9d4b23`) |
| Коммит этапа 1 | `этап 1: структура решения, ядро упаковки, эталонная реализация, тесты` |
| Коммит этапа 2 | `этап 2: загрузка изображений и конвейер обработки` |
| Коммит этапа 3 | `этап 3: генераторы вывода C51, STM32, A51, BIN, валидатор имён, кодировки, золотые тесты` |
| Последний `dotnet build` | 2026-10-06 (этап 4): `dotnet build --no-incremental` (Debug) и `dotnet build -c Release --no-incremental` — 10 проектов, **0 предупреждений, 0 ошибок** |
| Последний `dotnet test` | 2026-10-06: Debug и Release — **2343 теста, все прошли** (2312 в `Image2Gdram.Core.Tests`, 17 в `Image2Gdram.Fonts.Wpf.Tests`, 14 в `Image2Gdram.Imaging.Wic.Tests`), 0 пропущено |
| Дымовая компиляция | 2026-10-05: `tools/compile-check.ps1` формирует образцы вывода в `artifacts/compile-check/`; `gcc`, `clang`, `arm-none-eabi-gcc` не найдены — компиляция пропущена (код возврата 0) |
| Запуск приложения | 2026-10-06: `src/image2gdram_converter/bin/Release/net8.0-windows/image2gdram_converter.exe` стартует и остаётся запущенным (окно пока пустое — GUI делается на этапах 6–7) |

---

## Этапы (раздел 13 `agents.md`)

Статусы: `pending` — не начат, `in_progress` — в работе, `blocked` — заблокирован, `completed` — завершён. Этап считается `completed`, только когда его результат достигнут, сборка и тесты зелёные, чек-лист требований обновлён.

| № | Этап | Статус | Результат этапа | Блокеры | Оставшаяся работа |
|---|---|---|---|---|---|
| 0 | Анализ ТЗ | `completed` | `docs/requirements_checklist.md`, `docs/decisions.md`, `docs/implementation-plan.md`, `docs/architecture.md`; `.gitignore` по п. 2.5. Сборки и тестов на этапе нет | нет | нет; 11 вопросов к заказчику (4 — высокого приоритета) открыты в разделе E `decisions.md` и не блокируют этап 1: для каждого действует решение по умолчанию |
| 1 | Структура решения и ядро упаковки | `completed` | `image2gdram_converter.sln` со структурой из раздела 4 `agents.md` (приложение перенесено в `src/`, `Directory.Build.props`); в Core — `MonoBitmap`, `PackingOptions` и перечисления, `IPacker` (`Pack`/`Unpack`/`Locate`/`GetSize`), `Mono1bppPacker`, `PackerRegistry`; независимая эталонная реализация `tests/Image2Gdram.Reference`; 1441 тест упаковки; сверка пресетов с datasheet записана в D-01 | нет | нет. Перенесено на другие этапы по плану: файлы эталонов в `testdata/reference/` — этап 9; пресеты данными — этап 5 |
| 2 | Загрузка и конвейер обработки изображений | `completed` | `IImageDecoder` и `WicImageDecoder` (проект `Image2Gdram.Imaging.Wic`), `RgbaImage`, шаги 1–7 п. 4.1.2 (фон, поворот и отражение, размер, серое, бинаризация, правки, упаковка), `EditHistory`, `tools/TestAssetsGenerator` и три PNG в `testdata/`; 1534 теста | нет | нет. Интерфейс выбора кадра и мышь в сетке — этапы 6–7; листы шрифтов и эталонные массивы — этап 9 |
| 3 | Генераторы вывода | `completed` | `Image2Gdram.Core.Output`: генераторы C для C51, C для STM32, A51 (модуль и фрагмент `$INCLUDE`), BIN за `IOutputGenerator` и `OutputGeneratorRegistry`; заголовок-комментарий, валидатор имён и имя по умолчанию, CP1251 и UTF-8 без BOM, CRLF; карта «байт → позиция в тексте»; запись файлов с подтверждением перезаписи; `Core.Text.Cp1251`, `RussianPlural`, `Core.Diagnostics`; `tools/OutputSamples`, `tools/compile-check.ps1`; золотые тесты по приложению В; 1772 тест | нет | нет. Дымовая компиляция пропущена (компиляторов нет) — компиляция в Keil, IAR, STM32CubeIDE за заказчиком; подключение к интерфейсу — этапы 6–7 |
| 4 | Шрифты (ядро) | `completed` | CP1251, таблица 256 символов, растеризация контуров TTF, лист символов, импорт массивов из C и A51, диапазоны символов; тесты. Debug и Release (`--no-incremental`) — 0 предупреждений, 0 ошибок; `dotnet test` — 2343 из 2343 | нет | нет. Подключение к интерфейсу — этапы 6–7; файлы листов-эталонов — этап 9 |
| 5 | Пресеты, настройки, проекты `.iiu` | `pending` | встроенный каталог пресетов, пользовательские пресеты, `settings.json` с запасным путём `%APPDATA%\ImageIU`, сериализация проекта `.iiu`; тесты «сохранить → загрузить → идентично» | нет | весь этап |
| 6 | GUI: оболочка и вкладка «Конвертер картинок» | `pending` | главное окно, словари строк, сетка предпросмотра, окно кода, ручная правка, undo/redo, подсветка байта | нет | весь этап |
| 7 | GUI: вкладка «Генератор шрифтов» | `pending` | таблица 16×16, редактор символа, источники и диапазоны, предпросмотр строки | нет | весь этап |
| 8 | Производительность, надёжность, валидация, DPI | `pending` | замеры по п. 5.1 ТЗ, асинхронный пересчёт с отменой, глобальные обработчики исключений, валидация полей, PerMonitorV2 | нет | весь этап |
| 9 | Тестовые материалы, эталоны, документация, публикация | `pending` | `testdata/` (изображения, листы шрифтов, эталоны), `docs/` по разделу 9 ТЗ, `README.md`, `build.ps1`, `publish.ps1`, single-file exe | нет | весь этап |
| 10 | Финальная проверка | `pending` | чек-лист требований пройден построчно, пропуски закрыты, итоговый отчёт | нет | весь этап |

### Уточнения привязки работ к этапам

- **Сверка пресетов с datasheet** (приложение А ТЗ, примечание 1: «на этапе 1») выполнена на этапе 1 по общедоступным datasheet контроллеров; результат — в D-01 [`decisions.md`](decisions.md). Окончательное подтверждение пресетов UC1608, NT7108 и SSD1305 остаётся за заказчиком (Q-01).
- **Генератор тестовых изображений** приложения Б (`tools/TestAssetsGenerator`) создаётся на этапе 2, а не на этапе 9: изображения нужны тестам конвейера (повороты и отражения проверяются по маркерам), а приложение Б ТЗ указывает «на этапе 2». На этапе 9 к нему добавляются листы шрифтов и эталонные массивы (решение N-01).
- **Независимая эталонная реализация упаковки** (`tests/Image2Gdram.Reference`) создана на этапе 1 вместе с ядром упаковки; на этапе 9 по ней формируются файлы эталонов.

---

## Итоги этапа 1

Выполнено по разбивке этапа:

1. Заготовка WPF перенесена из корня в `src/image2gdram_converter/` через `git mv`; имя проекта и пространство имён `image2gdram_converter` сохранены, `CommunityToolkit.Mvvm` 8.4.2 сохранён. Ссылка на `SixLabors.ImageSharp` 4.1.2 **удалена** (K-05): пакет ещё не используется, а без лицензионного ключа сборка Debug давала предупреждения, а Release — ошибку.
2. `image2gdram_converter.sln` и `Directory.Build.props`: `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `Deterministic`, версия 1.0.0, Product «Image2GDRAM Converter».
3. Проекты: `src/Image2Gdram.Core` (`net8.0`, без пакетов), `tests/Image2Gdram.Reference` (`net8.0`, без ссылки на Core), `tests/Image2Gdram.Core.Tests` (xUnit, N-38).
4. Core, `Image2Gdram.Core.Packing`: `MonoBitmap`, `PackingOptions` (значения по умолчанию — N-35), `PixelFormat`, `ByteOrder`, `PackDirection`, `BitOrder`, `PageTraversal`, `BitLocation`, `IPacker`, `Mono1bppPacker` (контракт — N-36), `PackerRegistry`, `PackerNotRegisteredException`; `Image2Gdram.Core.Text.ProductInfo` (D-02).
5. Reference: `ReferencePacker` и `RefOptions.AllCombinations` (16 комбинаций), построение «по определению» через строки бит (N-37).
6. Тесты: контрольные байты F-05 (вместе с инверсией и горизонтальным LSB first), таблица п. 4.2.1, 1536 и 6144 байта, 240 пикселей = 40 байт (6 бит) и 30 байт (8 бит), 3840 и 1024 байта, спрайт 13×11, 1024×1024 = 131072 байта. Все 16 комбинаций на случайных растрах 14 размеров (seed зависит только от размера) плюс 1024×1024 — побайтно против эталона. Проверены: `Locate` против `Pack` (взаимно однозначное соответствие битам); `Unpack(Pack(b)) == b`; игнорирование битов дополнения при `Unpack`; инверсия, в том числе битов дополнения; раскладка таблицы шрифта `c·N`; независимость от `ByteOrder`; реестр и расширение новым форматом без правки остальных модулей; валидация входов; самопроверка эталона.
7. Сверка пресетов UC1608, NT7108 и SSD1305 с datasheet — D-01.

Открытые вопросы после этапа 1: Q-01 (дополнен: порядок бит NT7108, смещение столбцов RET012864DGPP3N). Q-09 и Q-12 закрыты решением N-39.

---

## Окружение и расхождения

Окружение (проверено 2026-10-03):

- Windows, PowerShell; .NET SDK 8.0.425 — единственный установленный SDK.
- `gcc`, `clang`, `arm-none-eabi-gcc` не найдены (повторно проверено 2026-10-05). Скрипт `tools/compile-check.ps1` (этап 3) сообщает об этом и пропускает дымовую компиляцию. Компиляция в Keil C51/A51, MDK-ARM, IAR EWARM и STM32CubeIDE — действие заказчика.
- Кодировка исходников: все `.cs` и `.md` — UTF-8 без BOM; `tools/compile-check.ps1` — UTF-8 с BOM (иначе Windows PowerShell 5.1 читает кириллицу как ANSI). Инструмент записи файлов агента на этой машине может сохранить новый файл в CP1251; после правок проверять, что файлы читаются как строгий UTF-8, а символы вне CP1251 в тестах писать через `\uXXXX`.
- Python 3.13 установлен, но в решении не используется: эталоны строит независимая C#-реализация (решение N-28).
- Git: remote `origin` → `https://github.com/Gary11777/image2gdram_converter.git`; этап 0 влит в основную ветку через PR #1, этап 1 — в ветке `1st_stage`; пуш выполняет пользователь.

Расхождения, найденные на этапе 0:

| Что | Расхождение | Как устранено |
|---|---|---|
| п. 2.5 `agents.md` | в репозитории не было `.gitignore`, в первый коммит попала папка `obj/` (5 файлов восстановления NuGet) | добавлен `.gitignore` (`bin/`, `obj/`, `.vs/`, `*.user`, `publish/`, `TestResults/`), `obj/` снята с учёта (`git rm -r --cached obj`) в коммите этапа 0 |
| `docs/` | файлов состояния не было | созданы на этапе 0; сверять с кодом нечего — код продукта не писался |
| Приложения А и Б ТЗ | ссылаются на «этап 1» и «этап 2» договора, а таблица этапов раздела 10 ТЗ пуста | сопоставлено с этапами раздела 13 `agents.md` (см. «Уточнения привязки» выше и решение K-01) |

Расхождения, найденные на этапе 1:

| Что | Расхождение | Как устранено |
|---|---|---|
| `decisions.md`, статусы решений | в рабочей копии (до начала этапа 1, без коммита) пользователь сменил статус «ждёт заказчика» на «принято (Q-xx)» у D-01 (пресеты UC1608, NT7108, SSD1305), D-02, D-03 и пяти решений N-…, а вопросы Q-01…Q-10 в разделе E остались в таблице | правка пользователя сохранена и вошла в коммит этапа 1; решения действуют как принятые, вопросы раздела E оставлены для ответа заказчика (подтверждение, а не блокер) |
| Разделы 1 и 4 `agents.md` против п. 2.4 и раздела 14 | заготовка подключала `SixLabors.ImageSharp` 4.1.2, который без лицензионного ключа даёт предупреждения в Debug и ошибку в Release | ссылка удалена из проекта приложения (K-05); выбор декодера — Q-12, закрыт решением N-39 (WIC) |
| План этапа 1, шаг 1 | план требовал сохранить ImageSharp в проекте приложения до этапа 2 | не выполнено сознательно из-за K-05; `architecture.md` обновлён |

---

## Итоги этапа 2

Выполнено по результату этапа:

1. `Image2Gdram.Core.Imaging`: `RgbaImage`, `IImageDecoder`, `ImageInfo`, `DecodedImage`, `ImageLoadException` (`TooLarge`, `MemoryLimit`, `Unsupported`, `Corrupted`, `IoError`), `ImageLimits`.
2. `Image2Gdram.Imaging.Wic` (`net8.0-windows`, без NuGet): `WicImageDecoder`. Размер читается из заголовка до копирования пикселей. EXIF поворачивается своим кодом (N-41). Цветовой профиль не применяется. Кадры GIF собираются в полный холст с disposal 2 и 3. Файл открывается только на чтение. Приложение ссылается на эту сборку.
3. `Image2Gdram.Core.Processing`: шаги 1–5 (`BackgroundCompositor`, `RotateFlipStep`, `ResizeStep`, `GrayscaleStep`, пороговый режим, Флойд — Стейнберг, Аткинсон, Байер 4×4 и 8×8), `ImagePipeline` с кэшем промежуточных кадров, шаг 6 (`PixelOverrides`, `FramePixelOverrides`), шаг 7 (`RunAndPack`). Сторона больше 1024 в режиме «по исходному» — `PipelineException` (D-07). Значения по умолчанию — N-41.
4. `Image2Gdram.Core.Editing`: `EditHistory` (глубина 200) и `PixelStrokeAction` (один штрих — одно действие).
5. `tools/TestAssetsGenerator`: изображения приложения Б по раскладке N-40, PNG своим кодировщиком. Файлы: `testdata/test_pattern_240x128.png`, `testdata/test_pattern_128x64.png`, `testdata/test_sprite_13x11.png`.
6. Тесты: конвейер, маркеры тестового изображения, история правок — в `Image2Gdram.Core.Tests`; декодер (форматы, альфа, EXIF, GIF, пределы, повреждённые файлы) — в `Image2Gdram.Imaging.Wic.Tests`. Прежние 1441 тест упаковки остаются зелёными. Итого 1534.

Новые решения: N-40 (координаты тестовых изображений), N-41 (значения конвейера по умолчанию, таблица EXIF, цвет disposal 2, отдельная сборка декодера).

## Итоги этапа 3

Выполнено по результату этапа:

1. `Image2Gdram.Core.Output` — данные и контракт: `OutputData` (`ImageOutputData` — один кадр или все кадры GIF; `FontOutputData` — 256 символов), `ImageSourceInfo`, `FontSourceInfo`, `PresetInfo`, `OutputOptions` (формат, имя, кодировка, байт в строке 1…16, формат чисел A51, тип элемента STM32, расширение `.a51`/`.asm`, дата), `IOutputGenerator`, `OutputGeneratorRegistry` (новый формат — новый класс и `Register`), `OutputDocument` (файлы, карта байтов `ByteSpanMap`, предупреждения).
2. Генераторы: `C51CGenerator` (`unsigned char code`), `Stm32CGenerator` (`const uint8_t` с `<stdint.h>` или `const unsigned char`, без `static`), `A51ModuleGenerator` (`PUBLIC`, `?CO?ИМЯ SEGMENT CODE`, `RSEG`, `END`), `A51IncludeGenerator` (заголовок, метка и `DB`), `BinGenerator` (только байты; предпросмотр — hex-дамп). Общие части: `HeaderCommentBuilder` (N-02, N-04, N-42), `NumberFormatter` (D-09), `GlyphCommentFormatter` (D-10), `DataLayout` (D-08), `TextBuilder` (CRLF), `OutputEncoder` (CP1251 своей таблицей, UTF-8 без BOM). Шрифты — двумерный массив `[256][<ИМЯ>_BYTES_PER_CHAR]`; все кадры GIF — `[<ИМЯ>_FRAMES][<ИМЯ>_FRAME_SIZE]` (D-11, N-20). Предупреждение о массиве больше 65535 байт для C51 и A51 (D-12).
3. Имена: `ReservedWords` и `NameValidator` (N-05, N-06; 27 символов для модуля A51), `DefaultNameBuilder` (D-13).
4. `OutputWriter`: имена файлов из имени массива (N-43), одно подтверждение перезаписи на все файлы, запрет записи в исходные файлы (N-18), запись через временный файл, ошибки `OutputWriteException` с кодом.
5. `Image2Gdram.Core.Text`: `Cp1251` (D-16: 0x98 не занят; печатаемость D-10), `RussianPlural` (D-04). `Image2Gdram.Core.Diagnostics`: `Diagnostic` с кодом и аргументами (N-45).
6. `tools/OutputSamples` — образцы вывода всех форматов для тестовых изображений и шрифтов 6×8, 8×8, 12×16; `tools/compile-check.ps1` — дымовая компиляция образцов (`-std=c99 -Wall -Wextra -pedantic -Werror`, для C51 `-Dcode=`), при отсутствии компиляторов сообщает и пропускает.
7. Тесты (+238): золотые тесты В.1–В.4 с поправками D-02, D-03, N-02, K-06; карта байтов для всех форматов; CRLF и отсутствие BOM; одинаковый текст в CP1251 и UTF-8 и побайтная проверка кодировок; детерминизм без даты; формат даты; `0FFh`, `11111111b`, `0C0h`; перенос строк шрифта; склонение; валидатор и имя по умолчанию; предупреждения 64 КБ и замены символов; запись файлов; расширение реестра.

Новые решения: N-42 (очистка строк пользователя в заголовке), N-43 (имена файлов), N-44 (выравнивание макросов, дамп BIN, длина строк A51, ошибки генератора), N-45 (`Core.Text` и `Core.Diagnostics`), конфликт K-06 и вопрос Q-13 (заголовок в `.h`).

## Этап 4: критерий завершения (для всего этапа)

Этап 4 разбит на две части: часть 1 — архитектура, CP1251, растеризация TTF, таблица, диапазоны (сделана); часть 2 — лист символов, импорт массивов, закрытие этапа (сделана, см. «Итоги этапа 4, часть 2»). Статус `completed` ставит часть 2, и только когда выполнено всё перечисленное:

1. **Результат таблицы раздела 13 `agents.md`:** в Core есть и покрыты тестами CP1251, растеризация TTF, лист символов, импорт массивов из C и A51, диапазоны символов; все источники отдают результат через `IGlyphSource` в `FontTable`, а таблица — генераторам через `FontOutputData`.
2. **Чек-лист `requirements_checklist.md`:**
   - 4.2.1.1, 4.2.1.2, 4.2.1.4, 4.2.1.5, 4.2.2.3, 4.2.2.5, 4.2.2.6, 4.2.2.8, 4.2.3.2–4.2.3.4, 4.3.7.2 — «реализовано, проверено» с именами тестов;
   - 4.2.2.1, 4.2.2.2, 4.2.2.4, 4.2.2.7, 4.2.3.1, 4.2.3.5, 4.2.3.6, 5.4.2 — «частично», и в строке осталась только работа интерфейса этапа 7 (ядро проверено тестами);
   - в 4.6.1, 6.1, 6.3, 6.5, 8.3 закрыты части, относящиеся к шрифтам (лист и импорт: ошибки файлов без аварии, исходник не меняется, детерминизм, форматы входа).
3. **П. 2.4 `agents.md`:** `dotnet build` (Debug и Release, `--no-incremental`) — 0 ошибок, 0 предупреждений; `dotnet test` — все тесты зелёные, включая 2102 теста после части 1.
4. `decisions.md`, `architecture.md` (раздел 3.5 «Целевое» переведён в «Фактически», раздел 10), этот файл (статус, итоги, журнал) обновлены; коммит части 2.

## Итоги этапа 4, часть 1

Выполнено (ветка `4th_stage`):

1. **Модель таблицы** (`src/Image2Gdram.Core/Fonts/`): `FontCellSize`, `GlyphSet`, `FontTable` и `GlyphOrigin` (слой источника и слой ручных правок по D-14 и N-46; `Pack` по F-04; `ToOutputData` → `FontOutputData` этапа 3), `GlyphOps` и `ShiftDirection`, `Core/Editing/GlyphEditAction.cs`.
2. **Источники:** контракт `IGlyphSource` и `GlyphSourceResult` (`IGlyphSource.cs`); TrueType — `GlyphOutline`, `OutlinePoint`, `FontFaceSpec`, `FontMetrics`, `IGlyphOutlineProvider` (`GlyphOutline.cs`), `GlyphOutlineCache`, `PolygonRasterizer` с `FillRule` и `GlyphRenderMode`, `TrueTypeOptions` и `TrueTypeGlyphSource`.
3. **Диапазоны:** `CharRangeSet`, `CharRangePreset`, `CharRangeParseError`/`CharRangeParseErrorKind` (N-13, N-50); 0x98 не входит в предустановленные диапазоны.
4. **CP1251** (`src/Image2Gdram.Core/Text/Cp1251.cs`): добавлены `GetString` и `TryFromDecoded` (N-49); `App.OnStartup` регистрирует провайдер кодовых страниц (D-16). Новые коды `DiagnosticCode.FontNotFound`, `GlyphsMissingInFont`.
5. **WPF-часть:** проект `src/Image2Gdram.Fonts.Wpf` (`WpfGlyphOutlineProvider`, N-48), подключён к решению и к приложению.
6. **Тесты (+330):** `tests/Image2Gdram.Core.Tests/Fonts/` — `PolygonRasterizerTests` (nonzero против evenodd: «О», «8», пентаграмма, «бабочка»; отсчёт на ребре; горизонтальные рёбра; вырожденные и пустые контуры; контур вне ячейки и частичная обрезка; отрицательные смещения; суперсэмплинг, таблица яркости и граница порога; 200 случайных многоугольников против независимого числа обхода; детерминизм), `TrueTypeGlyphSourceTests` (на поддельном провайдере `FakeOutlineProvider`: базовая линия, смещения и обрезка, диапазоны, кириллица, отсутствующие глифы, 0x98 и управляющие коды, кэш и LRU, отмена), `FontTableTests` (таблица п. 4.2.1, 1536 и 6144 байта, пустые символы с инверсией и без для 16 комбинаций × 3 ячейки, сравнение с Reference, `Unpack`, 6×8 в 6-битном режиме, правки), `GlyphEditingTests`, `CharRangeSetTests`; `Text/Cp1251Tests` дополнен (все 256 кодов, 0x98, 0xA0, 0xAD, 0x7F, 0x00–0x1F). Новый проект `tests/Image2Gdram.Fonts.Wpf.Tests` — 17 тестов свойств на Arial, Consolas, Courier New.

Новые решения: N-46 (модель таблицы и правки), N-47 (размещение глифа и детали растеризации), N-48 (отдельная сборка WPF и кэш), N-49 (CP1251: провайдер, `GetString`, 0x98), N-50 (синтаксис произвольного диапазона).

---

## Итоги этапа 4, часть 2

Выполнено (ветка `4th_stage_2`, контракты части 1 не менялись):

1. **Лист** (`SheetOptions`, `SheetGlyphSource`): ячейки слева направо и сверху вниз, свои отступы и интервалы, порог на белом фоне, перенос в ячейку шрифта с обрезкой и дополнением фоном, только выбранные диапазоны, предупреждение `SheetTooSmall`.
2. **Импорт** (`Image2Gdram.Core.Fonts.Import`): `TextFileReader` (UTF-8, иначе CP1251, 16 МБ, файл только на чтение), `ArrayImportParser` (числа, комментарии, массивы C и серии `DB`), `ImportGlyphSource` (раскладка через `IPacker.Unpack`, предупреждение `ImportValueCountMismatch`). Ошибка значения остаётся на массиве, ошибка структуры файла — исключение.
3. **Интеграция:** лист и импорт заполняют `FontTable` части 1. `ReplaceSource` по-прежнему сохраняет ручные правки (D-14); замена таблицы импортом — `ResetAllManual` и `ReplaceSource`. `ToOutputData` уходит в генераторы этапа 3; текст C51/A51 шрифта 6×8 совпадает с оформлением приложения В с поправками D-02 и D-03; разбор этого текста возвращает байты `FontTable.Pack`.
4. **Тесты (+241):** лист, числа, комментарии, структура C и A51, кодировки, 16 комбинаций упаковки × 3 ячейки, сквозной путь через генераторы, PNG-лист через `WicImageDecoder`. Решения N-51 и N-52.

Отступлений от замороженных контрактов части 1 нет. У `PngWriter` (утилита тестовых изображений) добавлена перегрузка записи монохромного PNG — тем же кодировщиком, чтобы тест мог прочитать лист через WIC. Отдельного класса `FontImporter` нет: его роль выполняет `ImportGlyphSource`, как в контракте передачи.

## Этап 4: передача второй части

**Статус: выполнено** (2026-10-06). Контракты ниже не менялись. К этапу 5 не переходить из этой сессии — этап 5 в таблице остаётся `pending`.

Вторая часть этапа 4 реализует лист символов и импорт массивов строго по контрактам ниже и закрывает этап по критерию из раздела «Этап 4: критерий завершения».

### 1. Что уже сделано

Файлы и типы перечислены в «Итогах этапа 4, часть 1»; сигнатуры — `architecture.md`, раздел 3.5 «Фактически». Тестовые помощники для повторного использования: `tests/Image2Gdram.Core.Tests/Packing/TestBitmaps.cs` (`Random`, `SinglePixel`, `Filled`, `ToCore`, `ToArray`, `CombinationIndexes`), `Fonts/Polygons.cs`, `Fonts/FakeOutlineProvider.cs`; эталон упаковки — `Image2Gdram.Reference.ReferencePacker` и `RefOptions.AllCombinations`.

### 2. Замороженные контракты

Публичные сигнатуры ниже не меняются (только добавление новых членов, если без них нельзя, с записью в `decisions.md`):

- `IGlyphSource` — `FontSourceInfo Info { get; }`, `GlyphSourceResult Render(FontCellSize cell, CharRangeSet ranges, CancellationToken cancellationToken = default)`; `Render` не меняет состояния (кроме внутренних кэшей) и детерминирован. Ошибки входа, обнаруженные при `Render`, — диагностикой; неверные аргументы конструктора — `ArgumentException`/`ArgumentOutOfRangeException`.
- `GlyphSourceResult(GlyphSet glyphs, IReadOnlyList<int>? missingCodes = null, IReadOnlyList<Diagnostic>? diagnostics = null)` — `Glyphs`, `MissingCodes` (только TTF, N-23), `Diagnostics`.
- `GlyphSet(FontCellSize)` — `Set(code, bitmap)` (копия; размер растра равен ячейке), `Get`, `Contains`, `Codes`, `Count`, `Cell`.
- `FontTable` — `ReplaceSource(GlyphSet)`, `SetManual`, `RevertToSource`, `ResetAllManual`, `HasManualEdits`, `ManualCodes`, `GetGlyph`, `GetSourceGlyph`, `GetManualGlyph`, `GetOrigin`, `IsManual`, `IsEmpty`, `Clone`, `static GetBytesPerChar`, `Pack`, `ToOutputData`; `CharCount = 256`.
- `FontCellSize` (`Cell6x8`, `Cell8x8`, `Cell12x16`, `All`, `Get`, `TryGet`), `CharRangeSet` (`Default`, `Codes`, `Contains`, `TryParseCustom`), `CharRangePreset`.
- `IGlyphOutlineProvider`, `GlyphOutline`, `OutlinePoint`, `FontFaceSpec`, `FontMetrics`, `GlyphOutlineCache`, `TrueTypeOptions`, `TrueTypeGlyphSource`, `PolygonRasterizer`, `FillRule`, `GlyphRenderMode`, `GlyphOps`, `ShiftDirection`, `GlyphEditAction`, `WpfGlyphOutlineProvider`.
- `Cp1251.GetString(ReadOnlySpan<byte>)` (байт 0x98 → U+0098), `Cp1251.TryFromDecoded(char, out byte)` (U+0098 → 0x98), `ToUnicode`, `TryFromUnicode`, `GetBytes`.
- Этапы 1–3 (не менять): `IPacker.GetSize`/`Unpack` (длина данных ровно `GetSize`, инверсия учитывается, биты дополнения игнорируются), `PackerRegistry`, `FontSourceInfo.Sheet(fileName)`/`Import(fileName, arrayName)` (путь отбрасывается), `FontOutputData`, `RgbaImage`, `IImageDecoder`, `BackgroundCompositor`, `GrayscaleStep`.
- `DiagnosticCode` — новые коды добавляются **в конец** перечисления; аргументы — строки `InvariantCulture`; текст для пользователя — в словаре интерфейса (этапы 6–7), не в Core.

### 3. Спецификация оставшегося

#### 3.1. Растровый лист символов (п. 4.2.2, источник 2; N-12, N-27, N-33)

Контракт (пространство имён `Image2Gdram.Core.Fonts`):

```csharp
public sealed record SheetOptions
{
    public int CellWidth { get; init; } = 6;   // 1…64 — ячейка листа; по умолчанию интерфейс ставит ячейку шрифта
    public int CellHeight { get; init; } = 8;  // 1…64
    public int MarginX { get; init; }          // 0…1024 — отступ от левого края листа
    public int MarginY { get; init; }          // 0…1024 — отступ от верхнего края
    public int SpacingX { get; init; }         // 0…256 — интервал между ячейками по X
    public int SpacingY { get; init; }         // 0…256 — интервал по Y
    public int CharsPerRow { get; init; } = 16; // 1…256
    public int FirstCode { get; init; }        // 0x00…0xFF, по умолчанию 0x00
    public int Threshold { get; init; } = 128; // 0…255, свой порог (N-33)
}

public sealed class SheetGlyphSource : IGlyphSource
{
    public SheetGlyphSource(RgbaImage sheet, string fileName, SheetOptions options); // проверка диапазонов — ArgumentOutOfRangeException
    public FontSourceInfo Info { get; }  // FontSourceInfo.Sheet(fileName)
    public GlyphSourceResult Render(FontCellSize cell, CharRangeSet ranges, CancellationToken cancellationToken = default);
}
```

Семантика:

- Ячейка листа с номером k (k = 0, 1, …) стоит в столбце `k mod CharsPerRow` и строке `k div CharsPerRow`; её левый верхний угол — `(MarginX + col·(CellWidth + SpacingX), MarginY + row·(CellHeight + SpacingY))`. Ячейке k соответствует код `FirstCode + k`; коды больше 0xFF не существуют (лист дальше не читается), коды меньше `FirstCode` источник не заполняет. Символы читаются слева направо и сверху вниз.
- Заполняются только коды из `ranges` (N-27), включая явно выбранные 0x98 и управляющие коды (N-50).
- Пиксель листа: наложение на **белый** фон по F-06 (`c' = (c·a + 255·(255 − a) + 127) / 255`), яркость по F-09 (`Y = (299·R + 587·G + 114·B + 500) / 1000`), активен при `Y < Threshold`. Можно использовать `BackgroundCompositor` и `GrayscaleStep`, если результат совпадает с формулами.
- Растр ячейки листа переносится в левый верхний угол ячейки шрифта: лишнее обрезается, недостающее — фон (N-12). Пиксели ячейки листа за пределами изображения — фон.
- Ячейка, левый верхний угол которой лежит за пределами листа, отсутствует: код не заполняется. Если таких кодов среди выбранных (≥ `FirstCode` и ≤ 0xFF) хотя бы один — одно предупреждение `DiagnosticCode.SheetTooSmall` (добавить), аргументы: число ячеек, которые поместились на листе, и первый незаполненный код в виде `0xC0`.
- Загрузка файла BMP/PNG — вызывающий код через `IImageDecoder` (берётся первый кадр); Core-класс получает `RgbaImage` и не открывает файлы. Источник не меняет переданный `RgbaImage`.

#### 3.2. Импорт массивов из C и A51 (п. 4.2.2, источник 4; D-17, N-21, N-27)

Контракт (пространство имён `Image2Gdram.Core.Fonts.Import`):

```csharp
public enum ImportSyntax { C, Asm }                        // по расширению: .c/.h — C; .asm/.a51/.inc — Asm
public enum TextFileEncoding { Utf8, Cp1251 }
public sealed record DecodedText(string Text, TextFileEncoding Encoding);

public static class TextFileReader
{
    public const long MaxFileSize = 16L * 1024 * 1024;     // N-21
    public static ImportSyntax GetSyntax(string path);      // другое расширение — ArgumentException
    public static DecodedText Read(string path);            // ошибки — ArrayImportException
    public static DecodedText Decode(ReadOnlySpan<byte> bytes);
}

public enum ImportedArrayKind { CInitializer, AsmDb }
public sealed record ArrayImportIssue(ArrayImportErrorKind Kind, int Line, string Token);
public sealed record ImportedArray(string? Name, ImportedArrayKind Kind, int Line, IReadOnlyList<byte> Values, ArrayImportIssue? Error);

public enum ArrayImportErrorKind
{
    ValueOutOfRange, InvalidNumber, CharacterNotInCp1251,                  // ошибки массива
    UnterminatedComment, UnterminatedString, UnbalancedBraces,              // ошибки файла
    FileTooLarge, IoError,
}

public sealed class ArrayImportException : Exception                       // Kind, Line (с 1; 0 — нет строки), Token
{
    public ArrayImportErrorKind Kind { get; }
    public int Line { get; }
    public string? Token { get; }
}

public static class ArrayImportParser
{
    public static IReadOnlyList<ImportedArray> Parse(string text, ImportSyntax syntax); // в порядке файла; пусто — массивов нет
}

public sealed class ImportGlyphSource : IGlyphSource
{
    public ImportGlyphSource(ImportedArray array, string fileName, IPacker packer, PackingOptions packing); // array.Error != null — ArgumentException
    public FontSourceInfo Info { get; }  // FontSourceInfo.Import(fileName, array.Name)
    public GlyphSourceResult Render(FontCellSize cell, CharRangeSet ranges, CancellationToken cancellationToken = default);
}
```

Ошибка значения (вне 0…255, неверное число, символ вне CP1251) относится к массиву: массив попадает в список с `Error` и пустыми `Values`, остальные массивы файла доступны, а при выборе ошибочного массива интерфейс показывает причину и номер строки. Ошибка структуры файла (незакрытый комментарий, строка или скобки) и ошибки файла — `ArrayImportException`. Номера строк — с 1; перевод строки — CRLF, LF или CR; номер считается сквозь многострочные комментарии.

Все случаи, которые должны быть реализованы и покрыты тестами:

1. **Шестнадцатеричные с префиксом:** `0x1F`, `0X1F`, `0x1f`, `0x0`, `0x00FF` (ведущие нули допустимы) → значение; `0x` без цифр, `0xG1` → `InvalidNumber`; `0x100` → `ValueOutOfRange`.
2. **Шестнадцатеричные с суффиксом:** `1Fh`, `1fH`, `0C0h`, `0FFh`, `00h`; запись обязана начинаться с цифры: `C0h`, `FFh` — не число (идентификатор) → `InvalidNumber`; `100h` → `ValueOutOfRange`. Суффикс `h` проверяется первым: `0Bh` = 0x0B, `1Bh` = 0x1B.
3. **Двоичные:** `01010101b`, `0101B` (суффикс; только 0 и 1), `0b01010101`, `0B1` (префикс); `0b` = 0 (суффиксная запись числа `0`); `0102b`, `0b102`, `0b` с другими цифрами → `InvalidNumber`; больше 8 значащих разрядов со значением > 255 (`111111111b`) → `ValueOutOfRange`; ведущие нули (`000000001b`) допустимы.
4. **Десятичные:** `0`, `255`, `7`; `256`, `-1` → `ValueOutOfRange`; знак `+5`, выражения `1+2`, `LOW(X)`, `$`, символьные литералы C `'A'`, приведения `(unsigned char)0x1F` → `InvalidNumber`. В синтаксисе C число с ведущим нулём — восьмеричное по правилам C (`010` = 8, `0377` = 255, `08` → `InvalidNumber`); в синтаксисе Asm — десятичное (`010` = 10).
5. **Суффиксы целых в C:** `0x1Fu`, `0x1FU`, `255UL`, `1l` — суффиксы `u`/`U`/`l`/`L` (в любом сочетании, не больше одного `u` и двух `l`) отбрасываются; в Asm суффиксы не допускаются.
6. **Комментарии:** `/* … */` (в том числе многострочные, с `{`, `}`, `=`, `DB`, `;` и числами внутри), `//` до конца строки — в обоих синтаксисах; `;` до конца строки — только в синтаксисе Asm (в C `;` — конец объявления, иначе `… = {1}; … b[] = {2};` в одной строке потерял бы второй массив). Комментарии не вкладываются: первый `*/` закрывает. `//` и `;` внутри `/* */` и внутри строковых литералов — не комментарии; `/*` внутри `//`-комментария игнорируется. Незакрытый `/*` → `ArrayImportException(UnterminatedComment)` с номером строки открытия.
7. **Массив C:** инициализатор `{ … }` сразу после `=` (между ними допустимы пробелы и комментарии). Имя — последний идентификатор перед первой `[` объявления, а если `[` нет — перед `=`; ключевые слова и атрибуты (`const`, `static`, `code`, `xdata`, `PROGMEM`, `__attribute__((…))`, `uint8_t`, `unsigned char`) на имя не влияют. Многомерные `[256][6]` допустимы. `{` без предшествующего `=` (тела функций, `struct`, `enum`) массивом не считается, но массивы внутри них (`= {`) находятся. `= 5;`, `= "строка"`, `= &x` — не массив, пропускаются. Объявления без инициализатора (`extern unsigned char font[];`) и строки препроцессора (`#define`, `#include`, `#if`) пропускаются; условная компиляция не вычисляется.
8. **Вложенные скобки:** любая глубина, значения разворачиваются в порядке текста (`{{1,2},{3,{4}}}` → 1, 2, 3, 4); пустой инициализатор `{}` — массив из 0 значений; завершающая запятая (`{1,2,}`, `{{1},}`) допустима; пустой элемент в середине (`{1,,2}`) → `InvalidNumber`; лишняя `}` или незакрытая `{` → `ArrayImportException(UnbalancedBraces)` с номером строки.
9. **Строки `DB` (Asm):** директива без учёта регистра (`DB`, `db`, `Db`); метка с двоеточием (`FONT_6X8:`) на своей строке или перед `DB` в той же строке; метка без двоеточия перед `DB` (`FONT DB 1`). Непрерывная последовательность строк `DB` — один массив; пустые строки и строки только с комментарием последовательность **не** прерывают (вывод самой программы содержит такие строки между кадрами); прерывают любая метка (начинает новый массив с этим именем) и любая другая директива или команда (`DW`, `DD`, `RSEG`, `END`, `MOV`, `$INCLUDE`, `PUBLIC`…). `DB` без метки — массив без имени (`Name = null`). Завершающая запятая в строке `DB` допустима, пустой элемент в середине → `InvalidNumber`.
10. **Строковые литералы в `DB`:** `'AB'` и `"AB"` → 0x41, 0x42; удвоенная кавычка внутри (`'It''s'`) — одна кавычка; `''` — ноль значений; кириллица → коды CP1251 через `Cp1251.TryFromDecoded` (`'А'` → 0xC0; U+0098 из CP1251-файла → 0x98); символ вне CP1251 (возможен в UTF-8-файле) → `CharacterNotInCp1251` с номером строки; `;`, `//`, `,` внутри строки — символы строки; незакрытая строка → `ArrayImportException(UnterminatedString)`. Строки смешиваются с числами: `DB 'A', 0, "B"`. В синтаксисе C строковые литералы внутри `{ … }` → `InvalidNumber`.
11. **Несколько массивов:** все массивы файла возвращаются в порядке появления, с именем, видом, номером строки начала (C — строка имени; Asm — строка метки или первой `DB`) и значениями; одинаковые имена допустимы. Выбор массива — интерфейс (N-21: список «имя или „без имени“, строка, число значений»).
12. **Значение вне 0…255** → ошибка массива с номером строки этого значения и текстом числа (D-17).
13. **Кодировка файла** (`TextFileReader`): размер больше 16 МБ → `FileTooLarge` до чтения содержимого; файл открывается только на чтение (`FileShare.ReadWrite`) и не меняется (N-18); сначала строгий UTF-8 (`throwOnInvalidBytes`; BOM `EF BB BF` отбрасывается), при ошибке — CP1251 через `Cp1251.GetString` (0x98 → U+0098); чистый ASCII — `Utf8`; пустой файл — пустой текст; ошибки ввода-вывода → `IoError`.
14. **Раскладка по символам** (`ImportGlyphSource`): `N = packer.GetSize(cell.Width, cell.Height, packing)`, ожидается `256·N` значений. Символ c получает значения `c·N … c·N + N − 1` через `packer.Unpack` с текущими параметрами упаковки — инверсия учитывается, биты дополнения игнорируются. Диапазоны не применяются (N-27): заполняются все коды, для которых есть хотя бы одно значение.
15. **Меньше значений:** символы без значений не заполняются (пустые); неполный последний символ дополняется байтами фона (0x00, с инверсией 0xFF), то есть недостающие пиксели — фон. **Больше значений:** лишние отбрасываются. В обоих случаях — одно предупреждение `DiagnosticCode.ImportValueCountMismatch` (добавить), аргументы: ожидаемое и фактическое число значений (десятичные).
16. **Замена таблицы:** импорт заменяет всю таблицу (D-14): интерфейс после подтверждения (если `HasManualEdits`) вызывает `ResetAllManual()` и `ReplaceSource(result.Glyphs)`; `FontTable` для этого не меняется.

Неоднозначности, решённые здесь (синтаксис по расширению и `;` в C, восьмеричные числа в C, ошибка на уровне массива, завершающая запятая, дополнение неполного символа, пустые и комментарные строки внутри `DB`, параметры листа за краем изображения), часть 2 записывает в `decisions.md` как N-51 (лист) и N-52 (импорт) в формате п. 2.1.

#### 3.3. Диапазоны символов

Реализованы в части 1 (`CharRangeSet`, N-13, N-50). Части 2 остаётся только применить их в `SheetGlyphSource` (п. 3.1) и не применять в импорте.

### 4. Обязательные тесты части 2

Тесты ядра — `tests/Image2Gdram.Core.Tests/Fonts/` (без WPF), с файлами — во временной папке.

1. **Лист:** лист, построенный в тесте как `RgbaImage`, для ячеек 6×8, 8×8 и 12×16 с отступами и интервалами, разными по X и Y, и 16 символами в строке — каждый символ совпадает с ожидаемым растром; `CharsPerRow` 1, 10 и 256; `FirstCode` 0x20 и 0xF0 (коды больше 0xFF не появляются); ячейка листа больше и меньше ячейки шрифта (обрезка и фон); порог на границе (`Y = threshold − 1` — активен, `Y = threshold` — нет); прозрачный и полупрозрачный пиксель (формула F-06 на белом фоне); заполняются только выбранные диапазоны, 0x98 — только явно; лист меньше нужного — частичное заполнение и `SheetTooSmall` с аргументами; ячейка частично за краем — фон; неверные параметры — исключение; `Info` без пути; детерминизм; исходный `RgbaImage` не изменён.
2. **Лист через декодер:** PNG-лист, записанный тестовым кодировщиком `TestAssetsGenerator`, читается `WicImageDecoder` и даёт те же символы, что `RgbaImage` (`tests/Image2Gdram.Imaging.Wic.Tests` или новый тест `net8.0-windows`).
3. **Числа:** теория на каждый формат и каждую ошибку из случаев 1–5 (значение или вид ошибки, номер строки, текст).
4. **Комментарии:** случай 6 полностью, включая `;` в C и в Asm, комментарии внутри строк, незакрытый `/*` с номером строки, номера строк после многострочного комментария при CRLF, LF и CR.
5. **Структура C:** случаи 7 и 8 — имена при `const`, `code`, `PROGMEM`, `__attribute__`, многомерных размерах; пропуск `extern`, `#define`, `= 5;`, тел функций; вложенность 3 уровня; `{}`; завершающая запятая; несбалансированные скобки.
6. **Структура A51:** случаи 9 и 10 — метки с двоеточием и без, метка на отдельной строке, пустые и комментарные строки не прерывают, другая директива прерывает, `db` в нижнем регистре, строки в одинарных и двойных кавычках, удвоенная кавычка, кириллица и 0x98, символ вне CP1251, незакрытая строка.
7. **Несколько массивов:** C и A51 — порядок, имена, строки начала, число значений; ошибочный массив не мешает остальным.
8. **Кодировки** (`TextFileReader`): UTF-8 с BOM и без, CP1251 с кириллицей, неверный UTF-8 → CP1251, байт 0x98, пустой файл, файл больше 16 МБ (`FileTooLarge` без чтения содержимого — например, разреженный файл через `SetLength`), отсутствующий файл → `IoError`, хеш файла до и после совпадает, файл не заблокирован после чтения, `GetSyntax` для пяти расширений и ошибка для других.
9. **Круговой тест с генераторами этапа 3:** таблица `FontTable` (случайная, фиксированный seed) для 3 ячеек → `ToOutputData` → `C51CGenerator`, `Stm32CGenerator`, `A51ModuleGenerator`, `A51IncludeGenerator` (числа `0FFh` и `11111111b`; CP1251 и UTF-8; дата включена и выключена) → `TextFileReader.Decode(Content)` → `Parse` → ровно один массив данных с ожидаемым именем и значениями, побайтно равными `FontTable.Pack`; примеры приложения В (В.1–В.3) разбираются.
10. **Раскладка:** для 16 комбинаций упаковки × 3 ячеек `Pack` → импорт → таблица равна исходной; ровно `256·N` значений — без предупреждения; меньше (в том числе некратно N, с инверсией и без) — дополнение фоном и `ImportValueCountMismatch` с числами; больше — лишние отброшены и предупреждение; диапазоны не влияют; `Info` содержит имя массива; массив с ошибкой — `ArgumentException`.
11. **Сценарий замены таблицы:** таблица с ручными правками → `ResetAllManual` + `ReplaceSource` из импорта → правок нет, все символы из импорта.

### 5. Что обновить после части 2

- `requirements_checklist.md`: 4.2.2.3, 4.2.2.5, 4.2.2.6, 4.2.2.7 (ядро; диалог — этап 7), 4.2.2.8, 4.2.3.1 (лист), 6.1, 6.3, 6.5, 8.3, 4.6.1 — с именами тестов; шапка «Состояние на …».
- `architecture.md`: раздел 3.5 — «Целевое (этап 4, часть 2)» перенести в «Фактически» с итоговыми сигнатурами; раздел 3.6 — новые коды `DiagnosticCode`; раздел 8 — тесты листа и импорта; раздел 10 — Core: Fonts «сделано», число тестов.
- `decisions.md`: N-51, N-52.
- Этот файл: строка этапа 4 → `completed`, «Текущее состояние», «Итоги этапа 4, часть 2», журнал.
- Перед коммитом проверить, что все новые `.cs` и `.md` читаются как строгий UTF-8 без символов U+0098 и U+FFFD (см. «Окружение и расхождения»: инструмент записи может сохранить файл в CP1251).

---

## Журнал

| Дата | Этап | Запись |
|---|---|---|
| 2026-10-03 | 0 | Начат анализ ТЗ. Прочитаны `agents.md` и `specification.md` целиком (включая приложения А–В). `docs/` не было. Добавлен `.gitignore`, `obj/` снята с учёта git. |
| 2026-10-03 | 0 | Созданы четыре файла состояния. `decisions.md`: 17 решений раздела 7 `agents.md` (D-01…D-17), 11 записей формул раздела 6 (F-01…F-11), 34 новых решения (N-01…N-34), 4 конфликта (K-01…K-04), 11 вопросов к заказчику (Q-01…Q-11). Чек-лист покрывает разделы 1–11 и приложения А–В ТЗ; все пункты «не реализовано». Перекрёстные ссылки проверены: все упомянутые ID определены. Сборка и тесты не выполнялись — код не создавался. Этап 0 → `completed`. |
| 2026-10-03 | 1 | Этап 1 → `in_progress`. Файлы состояния сверены с кодом и коммитами (ветка `1st_stage` от `219de94`, слияние PR #1 файлов не меняло); найдена незакоммиченная правка статусов в `decisions.md` (см. выше). |
| 2026-10-03 | 1 | Структура решения: приложение перенесено в `src/`, созданы `.sln`, `Directory.Build.props`, Core, Reference, Core.Tests. Первая сборка показала проверку лицензии ImageSharp 4.1.2 (предупреждения Debug, ошибка Release) — ссылка удалена, K-05 и Q-12. |
| 2026-10-03 | 1 | Ядро упаковки, эталон и тесты. `dotnet build` Debug и Release — 0 предупреждений, 0 ошибок; `dotnet test` — 1441 из 1441. Приложение запускается. Сверка с datasheet записана в D-01; решения N-35…N-38. Чек-лист и `architecture.md` обновлены. Этап 1 → `completed`. |
| 2026-10-04 | — | Q-09 и Q-12 закрыты решением N-39: декодер WIC, ImageSharp не используется. План этапа 2, `architecture.md`, чек-лист и `agents.md` приведены к этому решению. |
| 2026-10-04 | 1 | Сверка файлов состояния с кодом перед коммитом этапа 1. `architecture.md`: таблица раздела 10 приведена к факту этапа 1; в разделах 3.3 и 7 исправлены сигнатуры `IPacker` (`Format`, `Pack` в `Span`) и `PackerRegistry.Register(IPacker)`. Чек-лист и `decisions.md` совпадают с кодом — без изменений. Повторный прогон: Debug и Release — 0 предупреждений, 0 ошибок; `dotnet test` — 1441 из 1441. Этап 1 остаётся `completed`. |
| 2026-10-05 | 2 | Этап 2 → `in_progress`. Этап 1 в плане был `completed`, код и коммит этапа 1 на месте. |
| 2026-10-05 | 2 | Загрузка WIC, конвейер п. 4.1.2, история правок, генератор изображений приложения Б. Debug и Release — 0 предупреждений, 0 ошибок; `dotnet test` — 1534 из 1534. Приложение запускается. Решения N-40 и N-41. Этап 2 → `completed`. |
| 2026-10-05 | 3 | Этап 3 → `in_progress`. Этап 2 в плане `completed`, код и коммит этапа 2 на месте (ветка `3rd_stage` от `b6bd705`, рабочая копия чистая). |
| 2026-10-05 | 3 | Генераторы C51, STM32, A51 (модуль и фрагмент), BIN, заголовок, валидатор имён, кодировки, карта байтов, запись файлов, образцы и `compile-check.ps1`. Debug и Release (`--no-incremental`) — 0 предупреждений, 0 ошибок; `dotnet test` — 1772 из 1772. Дымовая компиляция пропущена: компиляторов нет. Приложение запускается. Решения N-42…N-45, K-06, Q-13. Этап 3 → `completed`. |
| 2026-10-06 | 4 | Этап 4 → `in_progress` (часть 1). Этап 3 в плане `completed`, код и коммит этапа 3 на месте (ветка `4th_stage` от `56972c0`). Записан критерий завершения всего этапа 4. |
| 2026-10-06 | 4 | Часть 1: модель таблицы, диапазоны, CP1251 (`GetString`, `TryFromDecoded`), заливка и суперсэмплинг, источник TTF, кэш контуров, проект `Image2Gdram.Fonts.Wpf` и его тесты. Решения N-46…N-50. Debug и Release (`--no-incremental`) — 0 предупреждений, 0 ошибок; `dotnet test` — 2102 из 2102. Приложение запускается. Инструмент записи сохранял новые файлы в CP1251 и портил символы вне CP1251 — файлы перекодированы в UTF-8 и проверены. Лист символов и импорт массивов переданы во вторую часть (раздел «Этап 4: передача второй части»). Этап 4 остаётся `in_progress`. |
| 2026-10-06 | 4 | Часть 2: лист символов, импорт массивов C и A51, раскладка в таблицу, сквозной путь в генераторы этапа 3. Решения N-51 и N-52. Контракты части 1 не менялись. Debug и Release (`--no-incremental`) — 0 предупреждений, 0 ошибок; `dotnet test` — 2343 из 2343. Новые файлы снова пришлось перекодировать в UTF-8. Этап 4 → `completed`. Этап 5 не начат. |
