# Архитектура «Image2GDRAM Converter» 1.0

> **Целевая архитектура плюс отметки о фактическом состоянии.** Документ составлен на этапе 0 по разделам 4 и 5 `agents.md` и п. 4.6 ТЗ. После этапа 5 **фактически существуют** решение, модуль упаковки, загрузка изображений, конвейер обработки, генераторы вывода, ядро шрифтов и хранение данных (пресеты, настройки, проекты `.iiu`). Описание упаковки помечено «**Фактически (этап 1)**», загрузки и обработки — «**Фактически (этап 2)**», вывода — «**Фактически (этап 3)**», шрифтов — «**Фактически (этап 4)**», пресетов, настроек и проектов — «**Фактически (этап 5)**» в разделе 3.6. Остальные разделы — целевые: сигнатуры в них ориентировочные. Сводка — таблица в разделе 10.

Связанные файлы: [`implementation-plan.md`](implementation-plan.md) (этапы), [`decisions.md`](decisions.md) (решения D/F/N/K), [`requirements_checklist.md`](requirements_checklist.md) (трассировка требований).

---

## 1. Структура решения

```
image2gdram_converter.sln
Directory.Build.props
specification.md, agents.md, README.md
build.ps1, publish.ps1
src/
  image2gdram_converter/        WPF-приложение
  Image2Gdram.Core/             ядро без WPF
  Image2Gdram.Imaging.Wic/      декодер WIC (net8.0-windows)
  Image2Gdram.Fonts.Wpf/        контуры глифов TrueType через WPF (net8.0-windows)
tests/
  Image2Gdram.Reference/        независимая наивная упаковка (эталон)
  Image2Gdram.Core.Tests/       xUnit: ядро
  Image2Gdram.Imaging.Wic.Tests/ xUnit: декодер WIC
  Image2Gdram.Fonts.Wpf.Tests/  xUnit: контуры глифов системных шрифтов
  Image2Gdram.App.Tests/        xUnit: сценарии ViewModel
tools/
  TestAssetsGenerator/          тестовые изображения, листы шрифтов, эталонные массивы
  OutputSamples/                образцы вывода всех форматов (для compile-check.ps1)
  compile-check.ps1             дымовая компиляция сгенерированного C (если есть gcc/clang)
testdata/                       тестовые изображения и testdata/reference/ (эталоны)
docs/                           документация и файлы состояния
```

| Проект | Путь | Платформа | Тип | Ссылки | Пакеты |
|---|---|---|---|---|---|
| `image2gdram_converter` | `src/image2gdram_converter/` | `net8.0-windows` | WinExe (WPF) | `Image2Gdram.Core`, `Image2Gdram.Imaging.Wic`, `Image2Gdram.Fonts.Wpf` | `CommunityToolkit.Mvvm` 8.4.2, AvalonEdit (MIT) — этап 6 |
| `Image2Gdram.Core` | `src/Image2Gdram.Core/` | `net8.0` | библиотека | — | — |
| `Image2Gdram.Imaging.Wic` | `src/Image2Gdram.Imaging.Wic/` | `net8.0-windows` | библиотека | Core | — (WIC из WPF, N-39, N-41) |
| `Image2Gdram.Fonts.Wpf` | `src/Image2Gdram.Fonts.Wpf/` | `net8.0-windows` | библиотека (`UseWPF`) | Core | — (`FormattedText`, `GlyphTypeface` из WPF, D-15, N-48) |
| `Image2Gdram.Reference` | `tests/Image2Gdram.Reference/` | `net8.0` | библиотека | — (Core не используется, N-28) | — |
| `Image2Gdram.Core.Tests` | `tests/Image2Gdram.Core.Tests/` | `net8.0` | xUnit | Core, Reference, `TestAssetsGenerator` | xUnit, `Microsoft.NET.Test.Sdk` |
| `Image2Gdram.Imaging.Wic.Tests` | `tests/Image2Gdram.Imaging.Wic.Tests/` | `net8.0-windows` | xUnit | Core, Imaging.Wic, `TestAssetsGenerator` | xUnit, `Microsoft.NET.Test.Sdk` |
| `Image2Gdram.Fonts.Wpf.Tests` | `tests/Image2Gdram.Fonts.Wpf.Tests/` | `net8.0-windows` | xUnit | Core, Fonts.Wpf | xUnit, `Microsoft.NET.Test.Sdk` |
| `Image2Gdram.App.Tests` | `tests/Image2Gdram.App.Tests/` | `net8.0-windows` | xUnit | приложение, Core | xUnit, `Microsoft.NET.Test.Sdk` |
| `TestAssetsGenerator` | `tools/TestAssetsGenerator/` | `net8.0` | консоль | — (Reference — этап 9, когда появятся эталонные массивы) | — (PNG — собственный кодировщик, N-39) |
| `OutputSamples` | `tools/OutputSamples/` | `net8.0` | консоль | Core, `TestAssetsGenerator` | — |

Проекты `Image2Gdram.Reference` и `Image2Gdram.App.Tests` дополняют структуру раздела 4 `agents.md` (решение N-32). ImageSharp не используется (N-39). `WicImageDecoder` — отдельная сборка `Image2Gdram.Imaging.Wic` (N-41); интерфейс `IImageDecoder` — в Core. Тесты декодера — `Image2Gdram.Imaging.Wic.Tests`, не `App.Tests`. По тому же образцу `WpfGlyphOutlineProvider` вынесен в сборку `Image2Gdram.Fonts.Wpf`, а интерфейс `IGlyphOutlineProvider`, заливка и источник TrueType остаются в Core (N-48); тесты на реальных системных шрифтах — `Image2Gdram.Fonts.Wpf.Tests`. Версии тестовых пакетов — N-38 (MIT и Apache-2.0).

```mermaid
flowchart LR
    App["image2gdram_converter (net8.0-windows)"] --> Core["Image2Gdram.Core (net8.0)"]
    App --> Wic["Image2Gdram.Imaging.Wic"]
    Wic --> Core
    App --> FontsWpf["Image2Gdram.Fonts.Wpf"]
    FontsWpf --> Core
    FontsWpfTests["Image2Gdram.Fonts.Wpf.Tests"] --> FontsWpf
    CoreTests["Image2Gdram.Core.Tests"] --> Core
    CoreTests --> Ref["Image2Gdram.Reference"]
    CoreTests --> Tool["TestAssetsGenerator"]
    WicTests["Image2Gdram.Imaging.Wic.Tests"] --> Wic
    WicTests --> Tool
    AppTests["Image2Gdram.App.Tests"] --> App
    AppTests --> Core
```

### Правила зависимостей

- Core не ссылается на WPF и не содержит строк интерфейса: предупреждения и ошибки Core возвращает кодами с параметрами (`Diagnostic`), текст для пользователя берётся из словаря приложения. Тексты внутри генерируемых файлов (заголовок-комментарий на русском) — часть формата вывода и живут в Core.
- ImageSharp нет (N-39, K-05). WIC (`System.Windows.Media.Imaging`) только в `WicImageDecoder` в проекте `net8.0-windows`. Типы шрифтов WPF (`FormattedText`, `GlyphTypeface`) только в `Image2Gdram.Fonts.Wpf`; растеризация контуров — собственный код Core (D-15). Core остаётся `net8.0` без WPF. `TestAssetsGenerator` пишет PNG собственным кодировщиком, без NuGet. Поворот, масштабирование, серое и дизеринг — собственный код над `RgbaImage`.
- `Image2Gdram.Reference` не ссылается на Core и не делит с ним исходники.
- Приложение: MVVM на CommunityToolkit.Mvvm; в code-behind только визуальная логика (мышь в сетке, прокрутка).
- Общие свойства сборки — `Directory.Build.props`: `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `Deterministic=true`; версия 1.0.0, Product «Image2GDRAM Converter».

---

## 2. Конвейер модулей (п. 4.6 ТЗ)

```mermaid
flowchart LR
    File["Файл BMP/PNG/JPEG/GIF"] --> Decoder["IImageDecoder"]
    Decoder --> Frames["DecodedImage: кадры RgbaImage"]
    Frames --> Pipeline["ImagePipeline: шаги 1-5"]
    Pipeline --> Mono["MonoBitmap"]
    Mono --> Overrides["PixelOverrides: шаг 6"]
    Overrides --> Packer["IPacker: шаг 7"]
    Packer --> Bytes["byte array"]
    Bytes --> Generator["IOutputGenerator"]
    Generator --> Doc["OutputDocument: файлы и ByteSpanMap"]
    Sources["IGlyphSource: TTF, лист, импорт"] --> Glyphs["GlyphSet"]
    Glyphs --> FontTable["FontTable: 256 MonoBitmap + ручные правки"]
    FontTable --> Packer
    FontTable --> FontData["FontOutputData"]
    FontData --> Generator
```

Каждый модуль общается с соседями только через типы на стрелках. Новый режим упаковки или формат вывода добавляется новым классом и регистрацией в реестре без правки остальных модулей (раздел 7).

---

## 3. Ядро: пространства имён и ключевые типы

Сигнатуры ориентировочные; окончательные уточняются при реализации и вносятся сюда.

### 3.1. `Image2Gdram.Core.Imaging` — загрузка (этап 2)

**Фактически (этап 2).** Сигнатуры ниже совпадают с кодом. Отличия от черновика этапа 0: код `ImageLoadError.MemoryLimit` (суммарный объём кадров больше 512 МБ, N-41); размер читается своим разбором заголовка, затем WIC копирует пиксели.

- `RgbaImage` — `Width`, `Height`, `Pixels` (прямой RGBA, 8 бит на канал, строки сверху вниз). Пиксели можно задать через `SetPixel`; после передачи в `ImagePipeline` буфер не меняют: конвейер кэширует кадр по ссылке.
- `ImageInfo` — формат, ширина и высота в файле (до EXIF), число кадров.
- `DecodedImage` — `IReadOnlyList<RgbaImage> Frames` одного размера, формат, признак анимации. Размер кадра — уже после EXIF.
- `IImageDecoder` — `ImageInfo Identify(string path)`, `DecodedImage Decode(string path, CancellationToken cancellationToken = default)`.
- `ImageLoadException` — `ImageLoadError` (`TooLarge`, `MemoryLimit`, `Unsupported`, `Corrupted`, `IoError`), путь и необязательные ширина и высота. Текст исключения — причина для пользователя.
- `ImageLimits` — сторона 8192 включительно, суммарно не больше 512 МБ RGBA.
- `WicImageDecoder : IImageDecoder` — проект `Image2Gdram.Imaging.Wic` (N-39, N-41). Цветовой профиль не применяется (`IgnoreColorProfile`). Ориентация EXIF — `RotateFlipStep` (N-19, N-41). Кадры GIF собирает собственный код: холст изначально прозрачный, disposal 2 и 3 — N-41; лимит 512 МБ проверяется до выделения холста (N-20). Файл открывается только на чтение и после возврата не занят.

### 3.2. `Image2Gdram.Core.Processing` — обработка, шаги 1–6 п. 4.1.2 (этап 2)

**Фактически (этап 2).** Классы шагов совпадают с черновиком. Дополнительно: `Binarizer.Create`, `FramePixelOverrides`, `ImagePipeline.Pack` и `RunAndPack` (шаг 7 через `PackerRegistry`), `PipelineException` с кодом `SourceLargerThan1024` (D-07).

- `ProcessingOptions` (sealed record, `Default`) — фон, поворот, отражения, ручной размер или «по исходному», режим вписывания, выравнивание, смещение, алгоритм масштабирования, режим бинаризации, порог. Значения по умолчанию — N-41.
- Шаги, каждый — отдельный класс с явными параметрами:
  - `BackgroundCompositor` — F-06;
  - `RotateFlipStep` — F-07, N-10; им же пользуется декодер для EXIF;
  - `ResizeStep` (`FitMode`: `None`/`Fit`/`Stretch`/`Fill`; `Alignment`: 9 позиций; `ResampleMode`: `AreaAverage`/`NearestNeighbor`) — F-08, D-06;
  - `GrayscaleStep` → `GrayImage` — F-09;
  - `IBinarizer` → `MonoBitmap`: `ThresholdBinarizer`, `FloydSteinbergDitherer`, `AtkinsonDitherer`, `BayerDitherer(4|8)` — F-09, F-10. Выбор — `Binarizer.Create`.
- `ImagePipeline` — `MonoBitmap Run(RgbaImage source, ProcessingOptions options, PixelOverrides? overrides, CancellationToken cancellationToken)`. Кэш промежуточных кадров принадлежит экземпляру и не потокобезопасен: смена порога или режима бинаризации не повторяет масштабирование. `RunAndPack` добавляет шаг 7.
- `PixelOverrides` — разреженный словарь `(x, y) → active`; `Apply` пропускает координаты вне растра; `Entries` идёт по возрастанию y, затем x. `FramePixelOverrides` хранит отдельный набор на кадр GIF (F-11).

### 3.2.1. `Image2Gdram.Core.Editing` — история правок (этап 2, частично)

**Фактически (этап 2).** `IEditAction`, `EditHistory` (глубина 200, N-14), `PixelStrokeAction` (один штрих — одно действие; пустой штрих не записывается). Действие кладётся в историю уже выполненным. **Этап 4:** `GlyphEditAction` — правка символа шрифта через тот же `IEditAction` (раздел 3.5).

### 3.3. `Image2Gdram.Core.Packing` — упаковка, шаг 7 (этап 1)

**Фактически (этап 1).** Сигнатуры ниже совпадают с кодом `src/Image2Gdram.Core/Packing/`.

- `MonoBitmap` (sealed class) — конструктор `(int width, int height)` (обе стороны ≥ 1), `Width`, `Height`, `bool this[int x, int y]` (хранение — `bool[]`, выход за границы — `ArgumentOutOfRangeException`), `ReadOnlySpan<bool> GetRow(int y)`, `Clone()`, `bool ContentEquals(MonoBitmap? other)`.
- Перечисления: `PixelFormat` (`Mono1bpp`; зарезервировано `Rgb565`), `ByteOrder` (`BigEndian`, `LittleEndian`), `PackDirection` (`Horizontal`, `Vertical`), `BitOrder` (`LsbFirst`, `MsbFirst`), `PageTraversal` (`ByPages`, `ByColumns`).
- `PackingOptions` (sealed record, `init`-свойства, `static Default`) — `PixelFormat`, `Direction`, `BitOrder`, `int BitsPerByte` (8 или 6), `PageTraversal`, `bool Invert`, `ByteOrder` (для будущего RGB565). Значения по умолчанию — N-35.
- `BitLocation` — `readonly record struct (int ByteIndex, int Bit)`.
- `IPacker`:
  - `PixelFormat Format { get; }`;
  - `int GetSize(int width, int height, PackingOptions options)`;
  - `byte[] Pack(MonoBitmap bitmap, PackingOptions options)`;
  - `void Pack(MonoBitmap bitmap, PackingOptions options, Span<byte> destination)` — буфер длиной ровно `GetSize` (символ в таблице шрифта);
  - `MonoBitmap Unpack(ReadOnlySpan<byte> data, int width, int height, PackingOptions options)` — длина ровно `GetSize`, с учётом инверсии, биты дополнения игнорируются;
  - `BitLocation Locate(int x, int y, int width, int height, PackingOptions options)`.
- `Mono1bppPacker : IPacker` — F-01…F-04; контракт по неприменимым параметрам и ошибкам — N-36.
- `PackerRegistry` — `static CreateDefault()` (регистрирует `Mono1bppPacker`), `Register(IPacker packer)` (формат берётся из `packer.Format`; повторная регистрация — `InvalidOperationException`), `TryGet(PixelFormat, out IPacker?)`, `Get(PixelFormat)`, `Get(PackingOptions)`, `Formats` (по возрастанию значения перечисления). Незарегистрированный формат — `PackerNotRegisteredException : NotSupportedException` со свойством `Format`.

### 3.4. `Image2Gdram.Core.Output` — генерация вывода (этап 3)

**Фактически (этап 3).** Сигнатуры ниже совпадают с кодом. Отличия от черновика этапа 0: вместо `OutputRequest` — абстрактный `OutputData`; у `OutputFile` нет поля кодировки (текст и уже закодированные байты); `ByteSpanMap` хранит один файл данных и одну длину литерала.

- Перечисления (`OutputEnums.cs`): `OutputFormat` (`CKeilC51`, `CStm32`, `A51Module`, `A51Include`, `Bin`), `OutputEncoding` (`Cp1251`, `Utf8NoBom`), `AsmNumberFormat` (`Hex`, `Binary`), `Stm32ElementType` (`Uint8T`, `UnsignedChar`), `AsmFileExtension` (`A51`, `Asm`), `OutputFileKind` (`CSource`, `CHeader`, `Assembly`, `Binary`), `HeaderField` (поле заголовка в предупреждении о замене символов).
- `OutputOptions` (sealed record) — `Format` (по умолчанию `CKeilC51`), `ArrayName`, `Encoding` (`Cp1251`), `BytesPerLine` (16; допустимо 1…16), `AsmNumberFormat`, `AsmFileExtension`, `Stm32ElementType` (`Uint8T`), `IncludeDate` (`true`), `DateTime? GeneratedAt` (явное время для тестов; иначе текущее).
- `OutputData` (абстрактный: `Packing`, `Preset`, `TotalBytes`, `GetAllBytes()`):
  - `ImageOutputData` — `Width`, `Height`, один кадр или все кадры (`AllFrames`, `FrameCount`, `FrameSize`, `GetFrame(i)`), `ImageSourceInfo` (только имя файла, число кадров исходника, выбранный кадр);
  - `FontOutputData` — `CharCount = 256`, `CellWidth`, `CellHeight`, `BytesPerChar`, `GetGlyph(code)`, `FontSourceInfo` (`TrueType`, `Sheet`, `Import`, `Manual`).
- `PresetInfo` — `Named(name, controller)`, `CustomBasedOn(name)`, `Custom` (строка «пресет» заголовка, N-02). Каталог, из которого берутся имя и контроллер, — этап 5 (`PresetCatalog`, `PresetBinding.ToInfo`).
- `IOutputGenerator` — `OutputFormat Format`, `OutputDocument Generate(OutputData data, OutputOptions options)`.
- `OutputGeneratorRegistry` — `CreateDefault()` (пять генераторов), `Register` (повтор — `InvalidOperationException`), `TryGet`, `Get` (нет генератора — `NotSupportedException`), `Formats`, `Generate(data, options)`.
- `OutputDocument` — `Files` (файл данных первым: `.c`, затем `.h`; для A51 и BIN — один файл), `ByteMap`, `Diagnostics`. `OutputFile` — `FileName`, `Kind`, `Text` (для окна кода и буфера обмена; у BIN — hex-дамп), `Content` (байты для записи). `ByteSpanMap` — `FileIndex`, `Length`, `Count`, индексатор `ByteSpan(FileIndex, Start, Length)` — позиция литерала байта в `Text`.
- Генераторы: `GeneratorBase` (проверка имени и `BytesPerLine`, предупреждение D-12 для `CKeilC51`, `A51Module`, `A51Include`) → `CGeneratorBase` → `C51CGenerator`, `Stm32CGenerator`; `A51GeneratorBase` → `A51ModuleGenerator`, `A51IncludeGenerator`; `BinGenerator`.
- Вспомогательные: `HeaderCommentBuilder` (N-02, N-04, N-42), `NumberFormatter` (D-09), `GlyphCommentFormatter` (D-10), `DataLayout` (D-08; строки изображения), `NameValidator` + `ReservedWords` (п. 4.4.2, N-05, N-06), `DefaultNameBuilder` (D-13), `OutputEncoder` (CP1251 своей таблицей, UTF-8 без BOM), внутренний `TextBuilder` (CRLF, финальный CRLF, позиции для карты).
- `OutputWriter` — `GetTargetPaths(document, directory)`, `Save(document, directory, confirmOverwrite, protectedPaths)` → `OutputSaveResult` (`Saved`/`Cancelled`); имена файлов — N-43; запрет записи в исходные файлы — N-18; ошибки — `OutputWriteException` с `OutputWriteError` (`WouldOverwriteSource`, `AccessDenied`, `IoError`).
- Генераторы не подключены к интерфейсу: это этапы 6–7.

### 3.5. `Image2Gdram.Core.Fonts` — шрифты (этап 4)

**Фактически (этап 4, часть 1).** Сигнатуры ниже совпадают с кодом `src/Image2Gdram.Core/Fonts/`, `Core/Editing/GlyphEditAction.cs` и `src/Image2Gdram.Fonts.Wpf/`. Они заморожены для второй части этапа (раздел «Этап 4: передача второй части» в [`implementation-plan.md`](implementation-plan.md)). Отличия от черновика этапа 0: вместо класса `Glyph` таблица хранит два слоя `MonoBitmap?` (источник и ручные правки), а `GlyphOrigin` — `None`/`Source`/`Manual` (N-46); `CharRangeSet.Codes` — `IReadOnlyList<int>`; провайдер контуров живёт в отдельной сборке, а не в приложении (N-48).

- `Cp1251` (в `Image2Gdram.Core.Text`, N-45): `char? ToUnicode(byte)` (0x98 → `null`, D-16), `bool TryFromUnicode(char, out byte)`, `bool IsPrintable(byte)` (D-10), `byte[] GetBytes(string)`, `RegisterEncodingProvider()`; **добавлено на этапе 4:** `string GetString(ReadOnlySpan<byte>)` (байт 0x98 → U+0098) и `bool TryFromDecoded(char, out byte)` (обратно, включая U+0098 → 0x98) — для текстового файла импорта и строковых литералов `DB` (N-49).
- `FontCellSize` (sealed record, закрытый конструктор) — только `Cell6x8`, `Cell8x8`, `Cell12x16`; `All`, `TryGet(w, h, out)`, `Get(w, h)` (иначе `ArgumentOutOfRangeException`), `ToString()` → `6x8`.
- `GlyphSet` — результат источника: `Cell`, `Count`, `Codes` (по возрастанию), `Contains`, `MonoBitmap? Get(code)` (копия), `Set(code, bitmap)` (хранит копию; размер равен ячейке, иначе `ArgumentException`).
- `FontTable` — `CharCount = 256`, `Cell`; слой источника и слой ручных правок (D-14, N-27, N-46): `GetGlyph` (действующий растр — правка, иначе источник, иначе пустой), `GetSourceGlyph`, `GetManualGlyph`, `GetOrigin`, `IsManual`, `IsEmpty` (нет активных пикселей), `HasManualEdits`, `ManualCodes`; `ReplaceSource(GlyphSet)` (правки сохраняются), `SetManual`, `RevertToSource`, `ResetAllManual`, `Clone`. Упаковка: `static GetBytesPerChar(cell, packer, options)`, `byte[] Pack(IPacker, PackingOptions)` (символ c — байты `c·N … c·N + N − 1`, F-04), `FontOutputData ToOutputData(packer, options, FontSourceInfo, PresetInfo)` — вход генераторов этапа 3. Все методы возвращают и принимают копии растров.
- `CharRangeSet` — `[Flags] CharRangePreset` (`Latin`, `Cyrillic`, `OtherCp1251`), `CharRangeSet(presets, customCodes)`, `Default` (латиница + кириллица, 161 код), `Codes`, `CustomCodes`, `Contains`, `GetPresetCodes`, `TryParseCustom(text, out codes, out CharRangeParseError?)` (N-13, N-50). 0x98 в предустановленные диапазоны не входит никогда.
- `IGlyphSource` — `FontSourceInfo Info`, `GlyphSourceResult Render(FontCellSize, CharRangeSet, CancellationToken)`; `GlyphSourceResult` — `Glyphs`, `MissingCodes`, `Diagnostics`. Общий контракт трёх источников: TTF, лист, импорт (N-27).
- `GlyphOutline` (контуры из `OutlinePoint`, координаты в пикселях от начала глифа, y вниз, базовая линия y = 0), `FontFaceSpec` (гарнитура, размер 1…64 px, жирный, курсив), `FontMetrics` (подъём и спуск, px), `IGlyphOutlineProvider` (`GetInstalledFamilies`, `TryGetMetrics`, `GetOutline` — `null`, если глифа нет).
- `GlyphOutlineCache` — потокобезопасный кэш контуров и метрик поверх провайдера, LRU на 8 начертаний (N-48): смена смещения, режима, порога, диапазонов и упаковки не обращается к WPF повторно.
- `PolygonRasterizer` — собственная заливка строками развёртки, `FillRule` (`NonZero` для глифов, `EvenOdd` для тестов), `GlyphRenderMode` (`Aliased` — центр пикселя, `Antialiased` — 4×4), `ComputeCoverage`, `Rasterize`, `CoverageToLuma` (D-15, N-47).
- `TrueTypeOptions` (гарнитура, смещение X/Y −32…32, режим, порог 128) и `TrueTypeGlyphSource : IGlyphSource` — код → Unicode по CP1251, базовая линия `ComputeBaseline`, растеризация в ячейку, отсутствующие глифы в `MissingCodes` и `GlyphsMissingInFont`, нет гарнитуры — `FontNotFound` (N-23, N-47).
- `GlyphOps` (`Clear`, `Invert`, `Toggle`, `Shift` с `ShiftDirection`, `Fit` — вставка другого размера в левый верхний угол, N-22) и `Core.Editing.GlyphEditAction : IEditAction` (замена ручного слоя одного символа или возврат к источнику; пустое изменение не записывается) — для редактора этапа 7.
- `Image2Gdram.Fonts.Wpf.WpfGlyphOutlineProvider : IGlyphOutlineProvider` — `FormattedText` (`pixelsPerDip = 1.0`, `TextFormattingMode.Ideal`) → `BuildGeometry` → `GetFlattenedPathGeometry(0.01)` → полигоны; глиф проверяется по `GlyphTypeface.CharacterToGlyphMap`; вызовы под блокировкой, работает из любого потока (N-47, N-48).

**Фактически (этап 4, часть 2).** Сигнатуры совпадают с контрактом передачи. Пространство имён листа — `Image2Gdram.Core.Fonts`, импорта — `Image2Gdram.Core.Fonts.Import`. Отдельного класса `FontImporter` нет: раскладку делает `ImportGlyphSource`.

- `SheetOptions` — ячейка листа 1…64, отступы 0…1024, интервалы 0…256, символов в строке 1…256, первый код 0x00…0xFF, порог 0…255.
- `SheetGlyphSource : IGlyphSource` — `RgbaImage` и `SheetOptions`; `FontSourceInfo.Sheet`. Пиксель: белый фон (F-06) и яркость (F-09), активен при `Y < Threshold`. Растр ячейки переносится в левый верхний угол ячейки шрифта (`GlyphOps.Fit`). Коды вне `CharRangeSet` и с началом ячейки за листом не заполняются. Предупреждение `SheetTooSmall` (N-51).
- `TextFileReader` — `GetSyntax`, `Read`, `Decode`; лимит 16 МБ до чтения содержимого; строгий UTF-8 (BOM отбрасывается), иначе `Cp1251.GetString`; файл только на чтение, `FileShare.ReadWrite`.
- `ArrayImportParser.Parse` — массивы C (`{ … }` после `=`) и серии `DB` в порядке файла. Ошибка значения — `ImportedArray.Error` и пустые `Values`; незакрытый комментарий, строка или скобки — `ArrayImportException` (N-52).
- `ImportGlyphSource : IGlyphSource` — `IPacker.Unpack`, символ `c` занимает `c·N … c·N+N−1`. Меньше значений — неполный символ дополняется фоном, коды без байтов не заполняются; больше — лишние отбрасываются. Предупреждение `ImportValueCountMismatch`. Диапазоны не применяются. Массив с ошибкой в конструктор не принимается.

### 3.6. Прочие модули Core (этапы 1–5)

- `Image2Gdram.Core.Editing` — **сделано для штриха пикселей (этап 2) и символа шрифта (этап 4),** см. п. 3.2.1 и 3.5.
- `Image2Gdram.Core.Presets` — **сделано (этап 5, N-53).** `ColorScheme` (`Lcd` / `Oled`), `Preset` (имя, контроллер, размер 1…1024, упаковка, схема, `IsBuiltIn`), `PresetCatalog.Shared` (ресурс `presets.json`, шесть пресетов D-01; «Пользовательский» в каталог не входит), `PresetBinding` и `PresetApplication` (картинка: размер, упаковка и схема; шрифт: упаковка и схема; пустой выбор значения не меняет), `UserPresetStore` (добавить, переименовать, удалить; встроенные неизменяемы), `PresetException`.
- `Image2Gdram.Core.Settings` — **сделано (этап 5, N-25, N-53).** `ImageTabParameters`, `FontTabParameters`, `AppSettings` (язык, обе вкладки, пресеты, недавние файлы, папки), `SettingsPathResolver` (`AppContext.BaseDirectory`, иначе `%APPDATA%\ImageIU`), `SettingsService` (атомарная запись; повреждённый файл — умолчания и `SettingsFileReset`), `SettingsException`.
- `Image2Gdram.Core.Projects` — **сделано (этап 5, N-16, N-53).** `ProjectDocument` (`formatVersion` 1, `ImageProjectTab`, `FontProjectTab`), `ProjectSerializer`. JSON через DTO, не через ViewModel: перечисления строками, UTF-8 без BOM. Путь исходника — абсолютный и относительный; при открытии сначала относительный. Нет файла — `ProjectSourceNotFound`, параметры и 256 символов всё равно читаются.
- `Image2Gdram.Core.Text` — **сделано (этапы 1, 3):** `ProductInfo` (константа «Image2GDRAM Converter 1.0», D-02), `RussianPlural` (`Select`, `Format`, `Bytes`, `Symbols`, `Frames`; D-04), `Cp1251` (N-45).
- `Image2Gdram.Core.Diagnostics` — **сделано (этап 3):** `record Diagnostic(DiagnosticCode Code, DiagnosticSeverity Severity, IReadOnlyList<string> Arguments)`; коды `ArrayExceeds64KForC51`, `NonCp1251CharactersReplaced` (этап 3), `FontNotFound`, `GlyphsMissingInFont` (этап 4, часть 1), `SheetTooSmall`, `ImportValueCountMismatch` (этап 4, часть 2), `SettingsFileReset`, `ProjectSourceNotFound` (этап 5). Следующие этапы добавляют свои коды в конец того же перечисления.

---

## 4. Приложение WPF (этапы 6–8)

```
src/image2gdram_converter/
  App.xaml(.cs)            регистрация CP1251, глобальные обработчики исключений, загрузка словаря
  app.manifest             PerMonitorV2
  Views/                   MainWindow, ImageConverterView, FontGeneratorView, диалоги
  ViewModels/              MVVM (CommunityToolkit.Mvvm)
  Controls/                PixelGridControl, GlyphTableControl, CodeView
  Services/                диалоги, файлы, буфер обмена, локализация, пересчёт
  Resources/Strings.ru-RU.xaml   встроенный словарь строк
Languages/ (рядом с exe)   дополнительные словари, выбираются в settings.json
```

- ViewModels: `MainViewModel` (вкладки, проект, закрытие с запросом N-15), `ImageConverterViewModel`, `FontGeneratorViewModel`, `GlyphEditorViewModel`, `PackingOptionsViewModel`, `OutputOptionsViewModel`, `PresetSelectorViewModel` («Пользовательский (на основе …)», N-26). Валидация — `ObservableValidator` (`INotifyDataErrorInfo`).
- Сервисы (интерфейсы — для тестов `App.Tests`): `IDialogService` (подтверждения), `IFileDialogService`, `IClipboardService`, `ILocalizationService` (строки по ключу из словаря), `RecalcScheduler` (debounce 30–50 мс, отмена предыдущего пересчёта через `CancellationToken`, выполнение в фоне, результат — в UI-поток).
- `PixelGridControl` — рисование в `WriteableBitmap` (не элемент на пиксель), `BitmapScalingMode=NearestNeighbor`, масштаб 1–32 и «по размеру окна» (N-24), линии сетки и толстые линии через 8 или 6 пикселей по направлению упаковки, подсказка через `IPacker.Locate`, рисование ЛКМ/ПКМ (N-08), события штриха для `EditHistory`.
- `GlyphTableControl` — 16×16 с подписями кодов; пустые и изменённые вручную символы различимы.
- `CodeView` — AvalonEdit только для чтения, моноширинный шрифт, подсветка диапазона по `ByteSpanMap`; подвкладки `.c`/`.h`, hex-дамп для BIN (N-17).
- `WpfGlyphOutlineProvider` — **не в приложении, а в сборке `Image2Gdram.Fonts.Wpf`** (раздел 3.5, N-48); приложение создаёт его и `GlyphOutlineCache` и передаёт в `TrueTypeGlyphSource`. `App.OnStartup` регистрирует провайдер кодовых страниц (D-16, N-49).

---

## 5. Поток пересчёта

```mermaid
sequenceDiagram
    participant UI as ViewModel
    participant Sch as RecalcScheduler
    participant Core as Core
    UI->>Sch: параметр изменён
    Sch->>Sch: debounce, отмена предыдущего токена
    Sch->>Core: ImagePipeline.Run в фоне
    Core-->>Sch: MonoBitmap
    Sch->>Core: IPacker.Pack и IOutputGenerator.Generate
    Core-->>Sch: OutputDocument
    Sch-->>UI: обновить сетку, код, предупреждения
```

Смена только параметров упаковки или вывода начинает пересчёт с шага упаковки (кэш `MonoBitmap` и кэш контуров глифов). Временные требования п. 5.1 ТЗ измеряются на этапе 8.

---

## 6. Хранение данных

Сделано на этапе 5 (N-16, N-25, N-53). ViewModel напрямую не сериализуются.

- Встроенные пресеты — ресурс `Presets/presets.json` в Core (D-01). Пользовательские — массив в `settings.json`.
- `settings.json` — рядом с программой (`AppContext.BaseDirectory`) или, если туда нельзя записать файл, в `%APPDATA%\ImageIU\settings.json`.
- Проект `.iiu` — JSON UTF-8 без BOM, `formatVersion` 1. В нём обе вкладки, параметры, правки пикселей и все 256 символов.
- Порядок полей в JSON — порядок свойств DTO. Списки, у которых есть естественный порядок (коды, кадры, точки), пишутся в этом порядке. `Dictionary` для файла не используется.

---

## 7. Расширяемость

- **Новый режим упаковки:** класс `IPacker` со своим `Format` + `PackerRegistry.Register(packer)` (проверено тестом `ValidationAndRegistryTests.New_packer_is_added_by_registration_only`). Для RGB565 в `PackingOptions` уже есть `PixelFormat` и `ByteOrder`; монохромный упаковщик их игнорирует (п. 4.6). В интерфейсе 1.0 цветные режимы не показываются.
- **Новый формат вывода:** класс `IOutputGenerator` + регистрация в `OutputGeneratorRegistry`; конвейер и упаковщик не меняются (проверено тестом `OutputBehaviourTests.New_format_is_added_by_registration_only`).
- **Новый язык интерфейса:** файл словаря в папке `Languages\` рядом с exe и параметр языка в `settings.json`, без пересборки.
- **Декодер изображений:** выбранная реализация — `WicImageDecoder` на WIC (N-39), не запасной вариант. Другой декодер добавляется новой реализацией `IImageDecoder` без правки конвейера.

---

## 8. Тесты и инструменты

- `Image2Gdram.Core.Tests`: упаковщик (16 комбинаций, контрольные примеры F-05, `Locate`, `Unpack(Pack(b))`, сравнение с Reference), конвейер и тестовые изображения приложения Б, генераторы вывода (папки `Output/` и `Text/`: золотые тесты В.1–В.4, карта байтов, кодировки, CRLF, детерминизм, имена, запись файлов), ядро шрифтов (папка `Fonts/`: заливка против независимого числа обхода, суперсэмплинг, источник TTF на поддельном провайдере `FakeOutlineProvider`, таблица против Reference, диапазоны, правка символа, лист `SheetGlyphSourceTests`, импорт `ArrayImportParserTests`, `TextFileReaderTests`, `ImportGlyphSourceTests`, сквозной путь `FontSourceIntegrationTests`), пресеты, настройки и проекты (этап 5: `PresetCatalogTests`, `UserPresetStoreTests`, `SettingsServiceTests`, `ProjectSerializerTests`).
- `Image2Gdram.Fonts.Wpf.Tests` (**этап 4**): свойства растеризации реальными системными шрифтами (Arial, Consolas, Courier New) — метрики, базовая линия, отсутствующие глифы, повторяемость между потоками, начертания, порог; без побайтных сравнений.
- `tools/OutputSamples` (**сделано, этап 3**; `net8.0`, ссылки на Core и `TestAssetsGenerator`): `OutputSamples <папка>` пишет вывод всех форматов (`stm32`, `stm32_uchar`, `stm32_utf8`, `c51`, `a51_module`, `a51_include`, `bin`) для тестовых изображений, спрайта, анимации и шрифтов 6×8, 8×8, 12×16, дата отключена. Нужен `compile-check.ps1` и для ручного просмотра. Тесты `WicImageDecoder` — в `Image2Gdram.Imaging.Wic.Tests` (N-39).
- `Image2Gdram.App.Tests`: смена пресета → «Пользовательский (на основе …)», сброс правок с подтверждением и откатом, undo/redo, валидация, запрос при закрытии; сервисы подменяются заглушками.
- `Image2Gdram.Reference`: наивная упаковка «по определению» (F-01…F-04) — источник эталонов.
- `TestAssetsGenerator`: **сделано (этап 2)** — `testdata/test_pattern_240x128.png`, `testdata/test_pattern_128x64.png`, `testdata/test_sprite_13x11.png` (раскладка N-40), встроенный шрифт 5×7, PNG своим кодировщиком. На этапе 4 у `PngWriter` добавлена перегрузка `Write(stream, width, height, isActive)` — тем же кодировщиком тест листа пишет PNG, который читает `WicImageDecoder`. Листы шрифтов 6×8, 8×8, 12×16 как файлы эталонов и `testdata/reference/` с `index.md` — этап 9; тогда же появится ссылка на `Image2Gdram.Reference`.
- `tools/compile-check.ps1` (**сделано, этап 3**): запускает `OutputSamples` в `artifacts/compile-check/` (папка в `.gitignore`); при наличии `gcc`/`clang`/`arm-none-eabi-gcc` компилирует вывод STM32 с `-std=c99 -Wall -Wextra -pedantic -Werror -c` и вывод C51 с `-Dcode=`; иначе сообщает и пропускает с кодом 0 (на этой машине компиляторов нет).

---

## 9. Сборка и поставка

- `build.ps1` — `dotnet build -c Release` и `dotnet test`.
- `publish.ps1` — `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` в `publish/`.

---

## 10. Состояние реализации

Состояние после этапа 5 (проверено 2026-10-06: `dotnet build --no-incremental` Debug и Release — 0 ошибок, 0 предупреждений; `dotnet test` Debug и Release — 2374 из 2374, из них 2343 в `Image2Gdram.Core.Tests`, 17 в `Image2Gdram.Fonts.Wpf.Tests` и 14 в `Image2Gdram.Imaging.Wic.Tests`).

| Модуль | Состояние |
|---|---|
| Структура решения | **сделано (этапы 1–3):** `image2gdram_converter.sln` (папки `src`, `tests`, `tools`; 10 проектов), `Directory.Build.props`; приложение в `src/image2gdram_converter/`; `tools/compile-check.ps1`. Ещё нет (целевое): `README.md`, `build.ps1`, `publish.ps1` — этап 9 |
| Core: Packing | **сделано (этап 1):** раздел 3.3 |
| Core: Text | **сделано (этапы 1, 3):** `ProductInfo` (D-02), `RussianPlural` (D-04), `Cp1251` (D-16, N-45) |
| Core: Imaging, Processing, Editing | **сделано (этап 2):** разделы 3.1, 3.2 и 3.2.1. Декодер WIC — отдельный проект, не часть Core |
| Core: Output | **сделано (этап 3):** раздел 3.4; к интерфейсу не подключено (этапы 6–7) |
| Core: Diagnostics | **сделано (этап 3):** `Diagnostic` (раздел 3.6). Ошибка конвейера по-прежнему свой код у `PipelineException`. Коды этапа 5: `SettingsFileReset`, `ProjectSourceNotFound` |
| Core: Fonts | **сделано (этап 4):** раздел 3.5 — таблица 256 символов, диапазоны, источник TTF, заливка, кэш контуров, операции и правка символа, лист символов, импорт массивов C и A51 |
| `src/Image2Gdram.Fonts.Wpf` | **сделано (этап 4):** `WpfGlyphOutlineProvider` (D-15, N-48) |
| Core: Presets, Settings, Projects | **сделано (этап 5):** раздел 3.6. К интерфейсу не подключено (этапы 6–7): выбор пресета в окне, автосохранение через 1 с и запрос при закрытии |
| Приложение WPF | пустая заготовка (`MainWindow.xaml` без содержимого; `App.OnStartup` регистрирует CP1251), ссылается на Core, `Image2Gdram.Imaging.Wic` и `Image2Gdram.Fonts.Wpf`; пакеты — только `CommunityToolkit.Mvvm` 8.4.2. Запускается. GUI — этапы 6–8 |
| `src/Image2Gdram.Imaging.Wic` | **сделано (этап 2):** `WicImageDecoder`, разбор заголовка до копирования пикселей, сборка кадров GIF |
| `tests/Image2Gdram.Reference` | **сделано (этап 1):** `ReferencePacker`, `RefOptions.AllCombinations` (16 комбинаций), без ссылки на Core (N-28, N-37) |
| `tests/Image2Gdram.Core.Tests` | **упаковка (этап 1), конвейер (этап 2), генераторы вывода (этап 3), ядро шрифтов (этап 4), пресеты, настройки и проекты (этап 5):** 2343 теста |
| `tests/Image2Gdram.Imaging.Wic.Tests` | **сделано (этапы 2 и 4):** 14 тестов — декодер и PNG-лист через `WicImageDecoder` |
| `tests/Image2Gdram.Fonts.Wpf.Tests` | **сделано (этап 4):** 17 тестов на системных шрифтах |
| `tests/Image2Gdram.App.Tests` | не создан (этап 6) |
| `tools/TestAssetsGenerator` | **сделано для приложения Б (этап 2):** три PNG в `testdata/` |
| `tools/OutputSamples`, `tools/compile-check.ps1` | **сделано (этап 3):** образцы вывода и дымовая компиляция; на этой машине компиляция пропускается — компиляторов нет |
