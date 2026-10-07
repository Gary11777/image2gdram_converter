# Аудит этапа 10: финальная проверка требований

Файл этапа 10 (раздел 13 `agents.md`). Каждый пункт `specification.md` (разделы 1–11, приложения А–В) и каждое проверяемое требование разделов 1, 2, 4–12 и 14 `agents.md` сопоставлены с реализацией (файл:строка), тестами и способом подтверждения. Статусы чек-листа, записи журнала плана и само наличие кода доказательством не считались: каждый тест, на который опирается строка, прочитан или запущен, поведение подтверждено прогоном или запуском программы.

- **Фаза А (аудит):** 2026-10-07, ветка `10th_stage` от `251886c`. Код, тесты и `testdata/` не менялись, `tools/TestAssetsGenerator` не запускался.
- **Фаза Б (исправления):** результаты повторной проверки — в разделе 8 и в строках таблиц со ссылкой на раздел 8.

## 1. Исходное состояние и проверки запуском (фаза А)

| Проверка | Команда | Результат |
|---|---|---|
| Сборка Debug | `dotnet build --no-incremental` | 0 ошибок, 0 предупреждений |
| Сборка Release | `dotnet build -c Release --no-incremental` | 0 ошибок, 0 предупреждений (`TreatWarningsAsErrors` в `Directory.Build.props:6`) |
| Тесты Debug и Release | `dotnet test`, `dotnet test -c Release` | 2506 из 2506: `Image2Gdram.Core.Tests` 2347, `Image2Gdram.App.Tests` 127, `Image2Gdram.Fonts.Wpf.Tests` 17, `Image2Gdram.Imaging.Wic.Tests` 15; пропущенных нет |
| Ссылки чек-листа на тесты | сверка 174 ссылок `Класс.Метод` из `requirements_checklist.md` с 439 методами тестов | все 174 существуют |
| Ключевые тесты отдельно | `dotnet test -c Release --filter` по `ControlExampleTests`, `SizeTests`, `Side_8192…`, `Animated_gif_composites_disposal`, `Disposal_restore_previous…`, `ReferenceFileTests`, двум тестам реестров, `Generation_without_date_is_byte_identical`, `Reference_assembly_does_not_reference_core`, `Interface_literals_are_not_written…` | 43 из 43 |
| Детерминизм вывода | `tools/OutputSamples` трижды: два запуска в культуре ru-RU (культура машины) и один с `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`; SHA-256 всех файлов | 77 файлов (C51, STM32 ×3, A51 модуль и фрагмент, BIN) побайтно совпадают во всех трёх запусках |
| Оформление файлов вывода | побайтный разбор тех же 77 файлов | ни одного BOM, ни одного одиночного LF или CR, каждый текстовый файл заканчивается CRLF |
| Дымовая компиляция | `tools/compile-check.ps1` | образцы созданы в `artifacts/compile-check/`; `gcc`, `clang`, `arm-none-eabi-gcc` не найдены — компиляция пропущена, код возврата 0 |
| Время обновления п. 5.1 | `dotnet run -c Release --project tools/UiProbe -- all artifacts/audit/ui-probe` | 45 сценариев в пределах: 240×128 — максимум 85 мс (предел 200), 1024×1024 — 146 мс, исходник 2048×2048 — 195 мс (предел 1000), шрифты — 88 мс (предел 200) |
| Масштаб 100–200 % | тот же запуск, режим `dpi` | окно 1024×680, обе вкладки при 100, 125, 150, 175, 200 %: обрезанных элементов 0; снимки просмотрены |
| Опубликованный exe | копия `publish/image2gdram_converter.exe` в пустой папке, запуск, `CloseMainWindow` | окно «Image2GDRAM Converter» отвечает, процесс завершился с кодом 0, рядом с exe создан `settings.json`. Product «Image2GDRAM Converter», FileVersion 1.0.0.0. ProductVersion `1.0.0+1f6e4ac…` — exe собран до последнего коммита этапа 9 (AU-15) |
| Заглушки | поиск `TODO`, `FIXME`, `HACK`, `NotImplementedException` в `*.cs`, `*.xaml`, `*.ps1`, `*.csproj` | нет. Единственный `NotSupportedException` в `src` — `ConvertBack` одностороннего конвертера (`App/Controls/RgbaImageConverter.cs:37`) |
| Неработающие команды | все `[RelayCommand]` вкладок и окна прочитаны (`MainViewModel.cs:146–238`, `ImageConverterViewModel.cs:531–666`, `FontGeneratorViewModel.cs:441–655`, `GlyphEditorViewModel.cs:101–136`) | у каждой команды есть реализация; дефект «Выход» — AU-03 |
| Сеть и ImageSharp | поиск `HttpClient`, `WebClient`, `System.Net`, `Socket` в `src`; `PackageReference` во всех проектах | обращений к сети нет; пакеты — `CommunityToolkit.Mvvm` 8.4.2, `AvalonEdit` 6.3.1.120, тестовые `xunit` 2.9.3, `xunit.runner.visualstudio` 2.8.2, `Microsoft.NET.Test.Sdk` 17.12.0; ImageSharp нет |
| Кодировка `docs/` | строгий декодер UTF-8 по 7 файлам `docs/`, `README.md`, `testdata/reference/index.md` | все — UTF-8 без BOM, без U+FFFD и U+0098 |
| Кодировка исходников | тот же декодер по 261 отслеживаемому файлу `*.cs`, `*.xaml`, `*.ps1`, `*.json`, `*.csproj`, `*.props`, `*.md` | все — строгий UTF-8, испорченных символов нет |

## 2. Обозначения

Пути сокращены: `Core/` — `src/Image2Gdram.Core/`, `App/` — `src/image2gdram_converter/`, `Wic/` — `src/Image2Gdram.Imaging.Wic/`, `FontsWpf/` — `src/Image2Gdram.Fonts.Wpf/`. Тесты названы `Класс.Метод`; файлы классов перечислены в разделе 9.

Статусы: **реализовано, проверено**; **частично**; **не реализовано**; **за заказчиком** (проверка возможна только у заказчика, ссылка на пункт `docs/pmi.md`).

Серьёзность открытого пропуска:

- **critical** — неверные байты, вывод или поведение, противоречащее ТЗ;
- **major** — функция ТЗ отсутствует, не работает или не проверена;
- **minor** — документация, оформление, тексты.

`—` — пропуска нет. Пропуски пронумерованы AU-01…AU-17 (сводка — раздел 5).

## 3. Пункты ТЗ

### 3.1. Разделы 1–3

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| 1.1 | Наименование «Image2GDRAM Converter», версия 1.0 | `Core/Text/ProductInfo.cs:4`; `Directory.Build.props:10–13` | `ProductInfoTests`; первая строка заголовка — `CGeneratorGoldenTests.Stm32_image_128x64_matches_appendix_v4`; свойства exe прочитаны (раздел 1) | реализовано, проверено | minor | пересобрать exe из итогового коммита (AU-15) |
| 1.2 | Основания для разработки | — | — | не применяется | — | — |
| 1.3 | Windows 10/11 x64 | `publish.ps1:6–12` (`-r win-x64`) | запуск exe на Windows сборки 26200 (раздел 1) | за заказчиком (`docs/pmi.md`, п. 3.2) | — | вторая ОС — у заказчика |
| 1.4.1 | Код для Keil C51 и A51 | `Core/Output/C51CGenerator.cs:4`, `A51GeneratorBase.cs:11,129,150` | `CGeneratorGoldenTests.C51_font_12x16_matches_appendix_v1`, `Small_c51_image_is_byte_exact`, `A51GeneratorGoldenTests.Module_font_6x8_matches_appendix_v2`, `Include_fragment_matches_appendix_v3`; `compile-check.ps1` — компиляторов нет | за заказчиком (`docs/pmi.md`, п. 3.3, 4.2) | — | компиляция в Keil C51/A51 |
| 1.4.2 | Код для STM32L496Z: MDK-ARM, IAR, GCC | `Core/Output/Stm32CGenerator.cs:7` | `CGeneratorGoldenTests.Stm32_image_128x64_matches_appendix_v4`, `Stm32_unsigned_char_variant_has_no_stdint` | за заказчиком (`docs/pmi.md`, п. 3.3, 4.2) | — | компиляция в трёх средах |
| 2.0 | Массивы, совместимые с GDRAM дисплеев раздела 2 | весь конвейер Core, обе вкладки | `ReferenceFileTests` сравнивает только упаковщик: PNG бинаризует сам тест (`ReferenceFileTests.cs:102–118`), а не конвейер программы | частично; дисплеи — за заказчиком (`docs/pmi.md`, п. 3.4) | major | сквозной тест «PNG → `ImagePipeline` с пресетом → генератор BIN» против `testdata/reference` (AU-01) |
| 2.1 | WG240128A (T6963C, 240×128) | `Core/Presets/presets.json:5`, `PresetCatalog.cs:10` | `PresetCatalogTests.Built_in_presets_match_d01`; `ReferenceFileTests` (`h_msb_8.bin`) | реализовано, проверено; дисплей — за заказчиком (`docs/pmi.md`, п. 3.4) | — | — |
| 2.2 | W0240128 (UC1608) | `presets.json`, D-01 | `PresetCatalogTests.Built_in_presets_match_d01`; RT `v_lsb_pages.bin` | реализовано, проверено; подтверждение пресета — за заказчиком (Q-01, `docs/pmi.md`, п. 4.3) | — | — |
| 2.3 | RG12864F (NT7108) | `presets.json`, D-01 | то же | реализовано, проверено; Q-01 — за заказчиком (`docs/pmi.md`, п. 4.3) | — | — |
| 2.4 | RET012864DGPP3N (SSD1305) | `presets.json`, D-01 | то же | реализовано, проверено; Q-01 — за заказчиком (`docs/pmi.md`, п. 4.3) | — | — |
| 2.5 | OLED128X64-0.96 (SSD1306) | `presets.json` | то же | реализовано, проверено; дисплей — за заказчиком (`docs/pmi.md`, п. 3.4) | — | — |
| 2.6 | HT1.3-OLED-BW / HR0161 (SH1106) | `presets.json`; смещение — `docs/user_guide.md`, раздел 7 | то же | реализовано, проверено | — | — |
| 3.1 | Размеры как Ш×В | `Core/Output/HeaderCommentBuilder.cs:40,47,175` («ШxВ», D-03); подписи интерфейса «Ширина», «Высота» в `App/Resources/Strings.ru-RU.xaml` | `HeaderCommentTests.Image_header_matches_n02`, `Font_header_matches_n02`; снимки окна | реализовано, проверено | minor | в чек-листе статус «частично, интерфейс — этап 6» устарел (AU-14) |
| 3.2 | Активный пиксель — бит 1, фон — 0 | `Core/Packing/Mono1bppPacker.cs:139–147,166–171` | `ControlExampleTests.Single_pixel_at_origin_gives_expected_first_byte` (12 случаев), `LayoutTests.Empty_bitmap_packs_to_background_bytes` | реализовано, проверено | — | — |
| 3.3 | Страница N — строки 8N…8N+7 | `Mono1bppPacker.cs:95–100,160–171` | `ControlExampleTests.Last_pixel_of_vertical_page_is_opposite_bit`, `LayoutTests.Traversal_by_pages_for_12x16…` | реализовано, проверено | — | — |
| 3.4 | LSB — бит 0, MSB — старший используемый | `Mono1bppPacker.cs:92,100,143,165` | `ControlExampleTests` (8 и 6 бит), `ReferenceComparisonTests` | реализовано, проверено | — | — |
| 3.5 | Биты дополнения — фон (0, с инверсией 1) | `Mono1bppPacker.cs:137–147,159,114–120` | `LayoutTests.Horizontal_padding_bits_of_13_pixel_row_are_background`, `Vertical_padding_bits_of_last_page_are_background`, `Bits_7_and_6_are_padding_in_6_bit_mode`; `LocateAndUnpackTests.Locate_is_consistent_with_pack` (каждый незанятый бит равен инверсии) | реализовано, проверено | — | — |
| 3.6 | Пресет — именованный набор | `Core/Presets/Preset.cs:9` | `PresetCatalogTests.Built_in_presets_match_d01`, `UserPresetStoreTests` | реализовано, проверено | — | — |
| 3.7 | Эталонный массив | `tests/Image2Gdram.Reference/ReferencePacker.cs:10`; `tools/TestAssetsGenerator/ReferenceAssets.cs:8`; `testdata/reference/` (6 входов × 16 `.bin` и 16 `.c`) | `ReferenceImplementationTests`, `ReferenceFileTests` | реализовано, проверено; согласование — за заказчиком (Q-08, `docs/pmi.md`, п. 4.1) | — | — |

### 3.2. Раздел 4.1 — конвертер изображений

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| 4.1.1.1 | BMP, PNG, JPEG, GIF | `Wic/WicImageDecoder.cs:30`, `Wic/ImageHeaderReader.cs:15` | `WicImageDecoderTests.Decodes_png_bmp_jpeg_and_static_gif` (пиксель каждого формата) | реализовано, проверено | — | — |
| 4.1.1.2 | До 8192×8192, больше — сообщение | `Core/Imaging/ImageLimits.cs:4`; размер из заголовка до `BitmapDecoder` — `WicImageDecoder.cs:34–35,41,399–410` | `WicImageDecoderTests.Side_8192_is_accepted_and_8193_is_too_large_before_pixel_copy` (файл 8193×1 без данных пикселей даёт `TooLarge`, а не `Corrupted`, — проверка раньше копирования); текст — `App/UserText.cs:22–26` | реализовано, проверено | — | — |
| 4.1.1.3 | Прозрачность: фон белый/чёрный, по альфе | `Core/Processing/BackgroundCompositor.cs:11–31` (F-06) | `BackgroundCompositorTests.Channel_mixes_by_alpha`, `Opaque_pixel_keeps_color_and_transparent_pixel_becomes_background`; `WicImageDecoderTests.Png_keeps_straight_alpha`, `Bmp32_keeps_alpha` | реализовано, проверено | — | — |
| 4.1.1.4 | Анимированный GIF: выбор кадра, по умолчанию первый | кадры — `WicImageDecoder.cs:93–143`; вкладка — `App/ViewModels/ImageConverterViewModel.cs:385–398`, `ImageConverterViewModel.Properties.cs:340` | декодер — `WicImageDecoderTests.Animated_gif_composites_disposal`, `Disposal_restore_previous_drops_the_frame_after_display`. На уровне вкладки тестов нет: ни один тест `App.Tests` не открывает многокадровое изображение | частично | major | тест вкладки: кадр 1 по умолчанию, смена кадра меняет вывод, правки у каждого кадра свои (AU-06) |
| 4.1.1.5 | Экспорт всех кадров `[кадры][размер]` | генераторы — `Core/Output/CGeneratorBase.cs:37–47`, `A51GeneratorBase.cs:55–70`, `BinGenerator.cs:9`; флажок — `ImageConverterViewModel.Properties.cs:350`, `ImageConverterViewModel.cs:796–826` | ядро — `CGeneratorGoldenTests.All_gif_frames_form_a_two_dimensional_array`, `A51GeneratorGoldenTests.Image_lines_have_no_comments_and_frames_are_marked`, `OutputBehaviourTests.Bin_contains_only_the_array_bytes_and_frames_follow_each_other`. Флажок вкладки тестом не проверен | частично | major | тест вкладки: «все кадры» даёт `_FRAMES` и `_FRAME_SIZE` и байты всех кадров (AU-06) |
| 4.1.2.1 | Шаг 1 | `BackgroundCompositor.cs:14` | `BackgroundCompositorTests` | реализовано, проверено | — | — |
| 4.1.2.2 | Шаг 2: поворот, отражения | `Core/Processing/RotateFlipStep.cs:11` (N-10) | `RotateFlipTests.Rotate90_moves_pixels_clockwise`, `Rotation_moves_corner_markers_of_test_pattern` (маркеры приложения Б) | реализовано, проверено | — | — |
| 4.1.2.3 | Шаг 3: размер | `Core/Processing/ResizeStep.cs:13–39` | `ResizeStepTests` | реализовано, проверено | — | — |
| 4.1.2.4 | Шаг 4: Y = 0,299R + 0,587G + 0,114B | `Core/Processing/GrayscaleStep.cs:11–12` (F-09) | `GrayscaleAndBinarizerTests.Luminance_uses_integer_rec601` | реализовано, проверено | — | — |
| 4.1.2.5 | Шаг 5: бинаризация | `Core/Processing/Binarizer.cs:4`, `ImagePipeline.cs:49` | `GrayscaleAndBinarizerTests` | реализовано, проверено | — | — |
| 4.1.2.6 | Шаг 6: ручная правка | `ImagePipeline.cs:50`, `Core/Processing/PixelOverrides.cs:10`, `FramePixelOverrides.cs:7`; `App/ViewModels/ImageConverterViewModel.cs:448` | `PixelOverrideTests`, `ImagePipelineTests.Manual_edits_are_applied_after_binarization`, `ImageConverterViewModelTests.A_stroke_is_one_undo_step` | реализовано, проверено | — | — |
| 4.1.2.7 | Шаг 7: инверсия и упаковка | `ImagePipeline.cs:55–73`, `Mono1bppPacker.cs:103–121` | `ImagePipelineTests.Pack_is_the_seventh_step_and_inversion_flips_the_byte`, `LayoutTests.Inversion_flips_every_byte` | реализовано, проверено | — | — |
| 4.1.2.8 | Порядок 1→7, каждый шаг настраивается | `ImagePipeline.cs:32–52`; поля шагов — `App/Views/ImageConverterView.xaml` | `ImagePipelineTests.Steps_run_in_order_and_match_the_individual_classes`; снимок вкладки (раздел 1) | реализовано, проверено | — | — |
| 4.1.3.1 | Размер: пресет, вручную, по исходнику | `ImagePipeline.cs:143–156`; `ImageConverterViewModel.cs:742–757` | `ImagePipelineTests.Match_source_rejects_a_side_above_1024_and_accepts_1024`, `PresetCatalogTests.Image_preset_sets_size_packing_and_scheme_and_custom_does_not`, `ImageConverterViewModelTests.Editing_a_preset_parameter_shows_custom_based_on_that_preset`, `ValidationTests.Size_fields_are_not_checked_when_the_size_follows_the_source` | реализовано, проверено | — | — |
| 4.1.3.2 | Без масштабирования: выравнивание и обрезка | `ResizeStep.cs:30–38,51,270–299` | `ResizeStepTests.None_crops_and_fill_crops_the_long_side`, `Nine_alignments_place_unscaled_image` | реализовано, проверено | — | — |
| 4.1.3.3 | Вписать с пропорциями | `ResizeStep.cs:53,107–120` | `ResizeStepTests.Scaled_size_follows_fit_mode`, `Fit_centers_with_odd_remainder_toward_top` | реализовано, проверено | — | — |
| 4.1.3.4 | Растянуть | `ResizeStep.cs:52` | `ResizeStepTests.Stretch_changes_both_axes` | реализовано, проверено | — | — |
| 4.1.3.5 | Заполнить с обрезкой | `ResizeStep.cs:54,107–120` | `ResizeStepTests.Scaled_size_follows_fit_mode`, `None_crops_and_fill_crops_the_long_side` | реализовано, проверено | — | — |
| 4.1.3.6 | 9 позиций и смещение X/Y | `ResizeStep.cs:63–94,302` | `ResizeStepTests.Nine_alignments_place_unscaled_image`, `Odd_center_remainder_goes_right_and_down`, `Offset_shifts_and_clips` | реализовано, проверено | — | — |
| 4.1.3.7 | Непокрытое — фон | `ResizeStep.cs:34–35` (D-06) | `ResizeStepTests.Uncovered_pixels_use_step1_background` | реализовано, проверено | — | — |
| 4.1.3.8 | С усреднением (по умолчанию) или ближайший сосед | `ResizeStep.cs:96–104,130–157,159–240`; умолчание — `Core/Processing/ProcessingOptions.cs:39` | `ResizeStepTests.Area_average_keeps_a_thin_line_that_nearest_neighbor_skips`, `Nearest_neighbor_doubles_pixel_art_without_mixing` | реализовано, проверено | — | — |
| 4.1.4.1 | Порог: Y < порога, 0–255, по умолчанию 128 | `Core/Processing/ThresholdBinarizer.cs:8`; `ProcessingOptions.cs:13,43` | `GrayscaleAndBinarizerTests.Threshold_is_strictly_less_than` (Y = T − 1 и Y = T) | реализовано, проверено | — | — |
| 4.1.4.2 | Флойд — Стейнберг | `Core/Processing/FloydSteinbergDitherer.cs:11` (F-10) | `GrayscaleAndBinarizerTests.Floyd_steinberg_matches_hand_calculated_block`, `Dithering_is_repeatable` | реализовано, проверено | — | — |
| 4.1.4.3 | Аткинсон | `Core/Processing/AtkinsonDitherer.cs:11–36` | `GrayscaleAndBinarizerTests.Atkinson_distributes_only_six_eighths`, `Dithering_is_repeatable` | реализовано, проверено | — | — |
| 4.1.4.4 | Байер 4×4 и 8×8 | `Core/Processing/BayerDitherer.cs:25–88` | `GrayscaleAndBinarizerTests.Bayer4_matrix_matches_f10_and_uniform_gray_is_half_active`, `Bayer8_is_the_recursive_expansion_and_half_of_a_uniform_field` | реализовано, проверено | — | — |
| 4.1.4.5 | В дизеринге порог — регулятор яркости | `FloydSteinbergDitherer.cs`, `AtkinsonDitherer.cs:24`, `BayerDitherer.cs:39` | `GrayscaleAndBinarizerTests.Higher_threshold_activates_more_pixels` | реализовано, проверено | — | — |
| 4.1.5.1 | Щелчок переключает; ЛКМ — точки, ПКМ — фон | `App/Controls/PixelGridControl.cs:41`, `App/PixelPaint.cs`, `ImageConverterViewModel.cs:448` | `GridAndPaintTests.Paint_targets_the_visible_dot`, `ImageConverterViewModelTests.A_stroke_is_one_undo_step` | реализовано, проверено | — | — |
| 4.1.5.2 | Ctrl+Z / Ctrl+Y | `Core/Editing/EditHistory.cs:16`, `PixelStrokeAction.cs:9`; `App/MainWindow.xaml:11–14`, `MainViewModel.cs:146–150` | `EditHistoryTests`, `ImageConverterViewModelTests.A_stroke_is_one_undo_step` | реализовано, проверено | — | — |
| 4.1.5.3 | Сброс правок при шагах 1–5 с предупреждением | `ImageConverterViewModel.cs:911` (`ConfirmStep`), `742–757` | `ImageConverterViewModelTests.Refusing_to_reset_edits_restores_the_parameter`, `Accepting_the_reset_clears_edits_and_history`, `Choosing_another_preset_is_rolled_back_when_edits_stay` | реализовано, проверено | — | — |

### 3.3. Раздел 4.2 — генератор шрифтов

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| 4.2.1.1 | 256 ячеек CP1251, прямая индексация | `Core/Fonts/FontTable.cs:25,136`; `Core/Text/Cp1251.cs:10` | `FontTableTests.Glyph_with_code_c_occupies_bytes_c_times_n`, `Table_matches_reference_packer_glyph_by_glyph`, `Cp1251Tests.All_256_codes_have_distinct_characters_except_unassigned_0x98` | реализовано, проверено | — | — |
| 4.2.1.2 | Ячейки 6×8, 8×8, 12×16 | `Core/Fonts/FontCellSize.cs:7` | `FontTableTests.Only_three_cell_sizes_exist` | реализовано, проверено | — | — |
| 4.2.1.3 | Байт на символ по таблице | `Mono1bppPacker.cs:80–83` | `SizeTests.Bytes_per_char_match_table_4_2_1` (9 значений) | реализовано, проверено | — | — |
| 4.2.1.4 | 1536 и 6144 байта | `FontTable.cs:136` | `FontTableTests.Array_sizes_are_1536_and_6144_bytes_vertically`, `SizeTests.Font_array_size_is_256_times_bytes_per_char` | реализовано, проверено | — | — |
| 4.2.1.5 | Неиспользуемые — фон, индексы не смещаются | `FontTable.cs` | `FontTableTests.Empty_table_is_all_background` (16 комбинаций × 3 ячейки), `Unused_codes_stay_in_place_and_are_background` | реализовано, проверено | — | — |
| 4.2.2.1 | TTF: гарнитура, размер, начертание, смещение, режим | `Core/Fonts/TrueTypeGlyphSource.cs:40,86`, `PolygonRasterizer.cs:29`, `GlyphOutlineCache.cs:9`; `FontsWpf/WpfGlyphOutlineProvider.cs:62,76–79` (`pixelsPerDip: 1.0`, `CharacterToGlyphMap`) | `PolygonRasterizerTests`, `TrueTypeGlyphSourceTests`, `WpfGlyphOutlineProviderTests.Synthesized_and_real_styles_change_the_glyph`, `FontGeneratorViewModelTests.Regenerating_true_type_keeps_a_manual_glyph_and_lists_a_missing_one` | реализовано, проверено | — | — |
| 4.2.2.2 | Код → Unicode по CP1251; отсутствующие пустые и перечислены | `TrueTypeGlyphSource.cs:130`; список — `FontGeneratorView.xaml:466` | `TrueTypeGlyphSourceTests.Missing_glyphs_stay_empty_and_are_listed_in_ascending_order`, `Cyrillic_codes_are_mapped_through_cp1251`; `FontGeneratorViewModelTests.Regenerating_true_type…` (`MissingText`) | реализовано, проверено | — | — |
| 4.2.2.3 | Растровый лист | `Core/Fonts/SheetGlyphSource.cs:14,49,66,110` | `SheetGlyphSourceTests` (16 методов), `SheetGlyphSourceDecoderTests.Png_sheet_matches_the_rgba_source` | реализовано, проверено | — | — |
| 4.2.2.4 | Ручное рисование и правка | `App/ViewModels/GlyphEditorViewModel.cs:13,69,101–136`; `Core/Editing/GlyphEditAction.cs:11` | `GlyphEditingTests`, `FontGeneratorViewModelTests.A_stroke_is_one_undo_step_and_the_origin_pixel_is_packed`, `Revert_and_reset…`, `Paste_fits_a_smaller_glyph_into_the_top_left` | реализовано, проверено | — | — |
| 4.2.2.5 | Импорт `.c`, `.h`, `.asm`, `.a51`, `.inc`; числа `0x..`, `..h`, десятичные, `..b` | `Core/Fonts/Import/ArrayImportParser.cs:9`, `TextFileReader.cs:28,56,98` | `ArrayImportParserTests` (14 методов, в том числе `Number_literals_have_the_defined_value`, `Appendix_examples_parse`), `TextFileReaderTests` | реализовано, проверено | — | — |
| 4.2.2.6 | Раскладка по ячейке и упаковке | `Core/Fonts/Import/ImportGlyphSource.cs:13,40` | `ImportGlyphSourceTests.Pack_then_import_restores_the_table` (16 × 3), `FontSourceIntegrationTests.Generated_font_text_imports_back_to_the_same_table` | реализовано, проверено | — | — |
| 4.2.2.7 | Несколько массивов — выбор | `ArrayImportParser.cs:11`; `App/Views/ImportDialog.cs`, `FontGeneratorViewModel.cs:474` | `ArrayImportParserTests.Several_arrays_keep_order_and_an_error_does_not_hide_the_rest`, `FontGeneratorViewModelTests.One_array_is_imported_without_a_dialog_and_several_arrays_use_the_choice` | реализовано, проверено | — | — |
| 4.2.2.8 | Меньше/больше значений — предупреждение | `ImportGlyphSource.cs:78` | `ImportGlyphSourceTests.Fewer_values_pad_the_last_glyph_and_warn`, `Extra_values_are_dropped_with_one_warning` | реализовано, проверено | — | — |
| 4.2.3.1 | Выбор кодов для источников 1 и 2 | `Core/Fonts/CharRangeSet.cs:44` | `TrueTypeGlyphSourceTests.Only_codes_of_the_selected_ranges_are_filled`, `SheetGlyphSourceTests.Only_selected_ranges_are_filled_and_0x98_only_when_explicit`, `ImportGlyphSourceTests.Ranges_do_not_limit_an_import` | реализовано, проверено | — | — |
| 4.2.3.2 | 0x20–0x7E | `CharRangeSet.cs:8–18` | `CharRangeSetTests.Latin_preset_is_0x20_to_0x7E` | реализовано, проверено | — | — |
| 4.2.3.3 | 0xC0–0xFF, 0xA8, 0xB8 | `CharRangeSet.cs` | `CharRangeSetTests.Cyrillic_preset_is_0xC0_to_0xFF_plus_yo` | реализовано, проверено | — | — |
| 4.2.3.4 | 0x80–0xBF без 0x98 | `CharRangeSet.cs:18` | `CharRangeSetTests.Other_preset_is_0x80_to_0xBF_without_0x98` | реализовано, проверено | — | — |
| 4.2.3.5 | Произвольный диапазон | `CharRangeSet.TryParseCustom` | `CharRangeSetTests.Custom_range_is_parsed`, `Invalid_custom_range_reports_kind_and_position`, `FontGeneratorViewModelTests.An_invalid_custom_range_blocks_generation` | реализовано, проверено | — | — |
| 4.2.3.6 | 0x00–0x1F, 0x7F, 0x98 пустые, можно нарисовать | `CharRangeSet.cs`, `FontTable.cs` | `CharRangeSetTests.Presets_combine_without_duplicates_and_never_include_control_codes_or_0x98`, `TrueTypeGlyphSourceTests.Control_codes_and_0x98_are_never_rendered_nor_listed_as_missing`, `FontGeneratorViewModelTests.A_stroke_is_one_undo_step…` (код 0) | реализовано, проверено | — | — |
| 4.2.4.1 | Строка предпросмотра «Привет! Hello 123» | `Core/Settings/TabParameters.cs:34`; `FontGeneratorViewModel.Properties.cs:428` | `FontGeneratorViewModelTests.Preview_text_starts_as_the_agreed_sample` | реализовано, проверено | — | — |
| 4.2.4.2 | Строка → CP1251, символы таблицы, масштаб | `App/StringPreviewMap.cs:9`, `App/Controls/StringPreviewControl.cs:15` | `FontGeneratorViewModelTests.Characters_outside_cp1251_are_marked_in_the_preview_map`; снимок вкладки (раздел 1) | реализовано, проверено | — | — |
| 4.2.4.3 | Вне CP1251 — пустая выделенная ячейка | `StringPreviewControl.cs` | тот же тест; рамка видна на снимке | реализовано, проверено | — | — |

### 3.4. Раздел 4.3 — параметры и упаковка

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| 4.3.0.1 | Параметры упаковки на обеих вкладках | `App/Views/ImageConverterView.xaml`, `FontGeneratorView.xaml` (D-05) | `FontGeneratorViewModelTests.Accepting_a_new_cell_replaces_the_table`; снимки обеих вкладок | реализовано, проверено | — | — |
| 4.3.0.2 | Пресет задаёт параметры | `Core/Presets/PresetApplication.cs:12,34`; `ImageConverterViewModel.cs:742–757` | `PresetCatalogTests.Image_preset_sets_size_packing_and_scheme_and_custom_does_not`, `Font_preset_does_not_change_the_cell`, `FontGeneratorViewModelTests.Preset_sets_packing_and_scheme_but_not_the_cell` | реализовано, проверено | major | после выбора пресета с другим размером имя массива по умолчанию сохраняет старый суффикс (AU-02; см. 4.4.2.1) |
| 4.3.0.3 | «Пользовательский (на основе …)» | `Core/Presets/PresetBinding.cs:9`, `App/ViewModels/PresetSelectorViewModel.cs:99` | `ImageConverterViewModelTests.Editing_a_preset_parameter_shows_custom_based_on_that_preset`, `FontGeneratorViewModelTests.Preset_sets_packing_and_scheme_but_not_the_cell`, `UiStringTests.Custom_preset_caption_matches_the_agreed_phrase` | реализовано, проверено | — | — |
| 4.3.1.1 | LSB first | `Mono1bppPacker.cs:92,100` | `ControlExampleTests` (0x01/0xFE), `Glyph_A_6x8_vertical_lsb_first_matches_appendix_V2` | реализовано, проверено | — | — |
| 4.3.1.2 | MSB first (бит 7, при 6 битах — бит 5) | `Mono1bppPacker.cs:92,143` | `ControlExampleTests` (0x80/0x7F, 0x20/0xDF), `Diagonal_in_horizontal_msb_first_gives_walking_bit` | реализовано, проверено | — | — |
| 4.3.2.1 | Горизонтальный режим | `Mono1bppPacker.cs:123–150` | `ReferenceComparisonTests` (14 размеров × 8 комбинаций `h_*`, 1024×1024), `ReferenceFileTests` | реализовано, проверено | — | — |
| 4.3.2.2 | Вертикальный режим | `Mono1bppPacker.cs:152–174` | то же для `v_*` | реализовано, проверено | — | — |
| 4.3.3.1 | 8 бит по умолчанию | `Core/Packing/PackingOptions.cs:20` | `LayoutTests.Default_options_are_8_bits_by_pages_without_inversion` | реализовано, проверено | — | — |
| 4.3.3.2 | 6 бит: биты 5…0, 240 пикселей = 40 байт | `Mono1bppPacker.cs:80–83,197–201` | `SizeTests.Row_of_240_pixels_takes_40_bytes_with_6_bits_and_30_with_8`, `LayoutTests.Bits_7_and_6_are_padding_in_6_bit_mode` | реализовано, проверено | — | — |
| 4.3.3.3 | Бит в байте только в горизонтальном | `ImageConverterView.xaml`, `FontGeneratorView.xaml` (неактивное поле) | `ImageConverterViewModelTests.Horizontal_mode_hides_page_traversal_and_a_short_height_does_too`, `FontGeneratorViewModelTests.Accepting_a_new_cell_replaces_the_table` | реализовано, проверено | minor | в чек-листе статус «частично» устарел (AU-14) |
| 4.3.4.1 | По страницам / по столбцам | `Mono1bppPacker.cs:97–99,170` | `LayoutTests.Traversal_by_pages_for_12x16…`, `Traversal_by_columns_for_12x16…` (пример п. 4.3.4) | реализовано, проверено | — | — |
| 4.3.4.2 | Обход только в вертикальном при высоте > 8 | то же; поле — вкладки | `LayoutTests.Traversal_orders_coincide_when_height_is_at_most_8`, `ImageConverterViewModelTests.Horizontal_mode_hides_page_traversal…` | реализовано, проверено | — | — |
| 4.3.5.1 | Размеры пресетов | `presets.json` | `PresetCatalogTests.Built_in_presets_match_d01`, `SizeTests.Preset_screen_buffers_have_expected_size` | реализовано, проверено | — | — |
| 4.3.5.2 | Вручную 1…1024 | `ProcessingOptions.cs:11–12`; валидация вкладки | `ValidationTests.Image_field_error_is_shown_at_once_blocks_output_and_clears_when_fixed` (0, 1025, 1, 1024) | реализовано, проверено | — | — |
| 4.3.5.3 | По исходнику — только картинки | `ImagePipeline.cs:145–153` (D-07) | `ImagePipelineTests.Match_source_rejects_a_side_above_1024_and_accepts_1024` | реализовано, проверено | — | — |
| 4.3.6.1 | Некратные 8; прижатие к углу; дополнение | `Mono1bppPacker.cs` | `SizeTests.Sprite_13x11_sizes_round_up_on_both_axes`, `ReferenceComparisonTests` (1×1, 13×11, 7×17 …), `ReferenceFileTests` (`test_sprite_13x11`) | реализовано, проверено | — | — |
| 4.3.6.2 | Фактические размеры в файле | `HeaderCommentBuilder.cs:40`, `CGeneratorBase.cs:42–58` | `CGeneratorGoldenTests.Sprite_13x11_keeps_its_actual_size_in_header_and_macros` | реализовано, проверено | — | — |
| 4.3.7.1 | «Инвертировать» | `Mono1bppPacker.cs:114–120`; флажки обеих вкладок | `ControlExampleTests`, `LayoutTests.Inversion_flips_every_byte`, `GridAndPaintTests.Displayed_bit_flips_with_inversion` | реализовано, проверено | — | — |
| 4.3.7.2 | Инверсия на биты дополнения и пустые символы | `Mono1bppPacker.cs:114–120` | `FontTableTests.Empty_table_is_all_background`, `Font_6x8_in_6_bit_mode_has_padding_bits_set_by_inversion` | реализовано, проверено | minor | в чек-листе ссылка «переключатель — этапы 6–7» устарела (AU-14) |

### 3.5. Раздел 4.4 — вывод и совместимость

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| 4.4.1.1 | Окно кода обновляется при каждом изменении | `App/Services/RecalcScheduler.cs:9,19`, `App/Controls/CodeView.xaml.cs:13` | `RecalcSchedulerTests` (5), `ImageConverterViewModelTests.Hover_reports_the_packed_byte_of_the_origin`; `UiProbe perf` (раздел 1) | реализовано, проверено | — | — |
| 4.4.1.2 | Копирование в буфер обмена | `ImageConverterViewModel.cs:563–572`, `FontGeneratorViewModel.cs:517–524`, `App/Services/ClipboardService.cs:8` | CRLF текста — `OutputBehaviourTests.Text_uses_crlf_only_and_ends_with_crlf`. Что команда кладёт в буфер именно видимый текст файла, тестом не проверено | частично | major | тест обеих вкладок: «Копировать» кладёт `OutputFile.Text` выбранного файла (AU-07) |
| 4.4.1.3 | C — пара `.c` и `.h` | `Core/Output/OutputWriter.cs:42,59`; `ImageConverterViewModel.cs:574–605`, `FontGeneratorViewModel.cs:526–568` | ядро — `OutputWriterTests.C_output_is_saved_as_c_and_h_pair`, `CGeneratorGoldenTests.C_source_includes_its_own_header`. Команда вкладки не проверена | частично | major | тест команды «Сохранить код» (AU-08) |
| 4.4.1.4 | Ассемблер `.a51` или `.asm` | `A51GeneratorBase.cs:102–108` | `A51GeneratorGoldenTests.Extension_is_chosen_by_the_user` | реализовано, проверено | — | — |
| 4.4.1.5 | `.bin` — только байты | `Core/Output/BinGenerator.cs:9–36` | `OutputBehaviourTests.Bin_contains_only_the_array_bytes_and_frames_follow_each_other`, `Packed_test_pattern_reaches_bin_unchanged` | реализовано, проверено | — | — |
| 4.4.1.6 | Подтверждение перезаписи | `OutputWriter.cs:59`; колбэк — `ImageConverterViewModel.cs:590–594`, `FontGeneratorViewModel.cs:553–557` | ядро — `OutputWriterTests.Existing_files_require_confirmation_and_refusal_keeps_them`. Передача ответа диалога из команды не проверена | частично | major | тест: отказ в диалоге оставляет файл без изменений, согласие перезаписывает (AU-08) |
| 4.4.2.1 | Имя задаётся пользователем; по умолчанию из файла и размера | `Core/Output/DefaultNameBuilder.cs:20,49`; `ImageConverterViewModel.cs:968–1014` | `NameTests.Default_name_follows_d13` и др. Дефект: при выборе пресета `LoadState` пересчитывает признак «имя задано пользователем» уже по новому размеру (`ImageConverterViewModel.cs:372`), поэтому для массива 240×128 остаётся имя `test_pattern_240x128_128x64` (снимок `UiProbe`) и больше не обновляется | частично | major | при применении пресета сохранять признак автоматического имени и пересчитывать имя по новому размеру; тест (AU-02) |
| 4.4.2.2 | Латиница, цифры, `_`, не с цифры | `Core/Output/NameValidator.cs:38,48` | `NameTests.Invalid_names_are_rejected_with_reason`, `ValidationTests` (имя на обеих вкладках) | реализовано, проверено | minor | в чек-листе «поле шрифта — этап 7» устарело (AU-14) |
| 4.4.2.3 | Не более 31 символа | `NameValidator.cs` (N-05: 27 для модуля A51) | `NameTests.Length_limit_is_31_and_27_for_a51_module`, `ValidationTests.Name_valid_in_c_becomes_invalid_when_the_format_switches_to_an_a51_module` | реализовано, проверено | — | — |
| 4.4.2.4 | Не ключевые слова C, Keil C51, A51 | `Core/Output/ReservedWords.cs:4` (N-06) | `NameTests.Every_reserved_word_is_rejected`, `Reserved_lists_contain_n06_entries` | реализовано, проверено | — | — |
| 4.4.2.5 | Макросы — верхний регистр | `CGeneratorBase.cs:42–70,85–97` | `CGeneratorGoldenTests.Stm32_image_128x64_matches_appendix_v4`, `C51_font_12x16_matches_appendix_v1` | реализовано, проверено | — | — |
| 4.4.3.1 | Название и версия | `HeaderCommentBuilder.cs:28` | `HeaderCommentTests.Image_header_matches_n02` | реализовано, проверено | — | — |
| 4.4.3.2 | Дата и время, отключаемо | `HeaderCommentBuilder.cs:15,29–33` | `HeaderCommentTests.Date_line_is_second_and_uses_fixed_format`, `OutputBehaviourTests.Generation_without_date_is_byte_identical`; тройной запуск `OutputSamples` (раздел 1) | реализовано, проверено | — | — |
| 4.4.3.3 | Источник или шрифт | `HeaderCommentBuilder.cs:122–145` | `HeaderCommentTests.Font_source_is_described_by_kind`, `Gif_frame_and_all_frames_are_described` | реализовано, проверено | — | — |
| 4.4.3.4 | Пресет | `HeaderCommentBuilder.cs:148–159` | `HeaderCommentTests.Preset_variants`, `PresetCatalogTests.Named_preset_info_includes_the_controller` | реализовано, проверено | — | — |
| 4.4.3.5 | Ширина и высота | `HeaderCommentBuilder.cs:40,47` | `HeaderCommentTests.Image_header_matches_n02`, `Font_header_matches_n02` | реализовано, проверено | — | — |
| 4.4.3.6 | Направление, порядок бит, бит в байте, обход, инверсия | `HeaderCommentBuilder.cs:63–98` | `HeaderCommentTests.Packing_description_shows_only_applicable_parameters` | реализовано, проверено | — | — |
| 4.4.3.7 | Размер массива | `HeaderCommentBuilder.cs:41–43,49`, `Core/Text/RussianPlural.cs:12` | `RussianPluralTests.Bytes_follow_russian_plural_rule` | реализовано, проверено | — | — |
| 4.4.4.1 | Байт в строке 1…16 (по умолчанию 16) | `Core/Output/DataLayout.cs:4`, `GeneratorBase.cs` | `FormattingTests.Image_lines_have_bytes_per_line_and_remainder`, `Bytes_per_line_outside_1_to_16_is_rejected`, `ValidationTests` | реализовано, проверено | — | — |
| 4.4.4.2 | Шрифт: символ с новой строки, перенос поровну | `DataLayout.cs` (D-08), `CGeneratorBase.cs:186` | `FormattingTests.Glyph_lines_are_split_evenly`, `CGeneratorGoldenTests.Font_with_32_bytes_per_char_takes_two_lines_of_16` | реализовано, проверено | — | — |
| 4.4.4.3 | C — только `0x0F` | `Core/Output/NumberFormatter.cs:6` | `FormattingTests.C_literals_are_upper_case_hex`, `CGeneratorGoldenTests.C_output_uses_only_block_comments_hex_literals_and_no_static` | реализовано, проверено | — | — |
| 4.4.4.4 | Ассемблер `0FFh` или `11111111b` | `NumberFormatter.cs:7–8,29` | `FormattingTests.Asm_hex_data_always_has_leading_zero_and_two_digits`, `Asm_binary_data_has_eight_digits`, `Asm_code_has_leading_zero_only_before_letter` | реализовано, проверено | — | — |
| 4.4.4.5 | Комментарий символа; непечатаемые — только код | `Core/Output/GlyphCommentFormatter.cs:10–13` (D-10) | `FormattingTests.Glyph_comments_show_printable_characters_only`, `Cp1251Tests.Printable_codes_follow_d10` | реализовано, проверено | — | — |
| 4.4.4.6 | В C только `/* */` | `CGeneratorBase.cs` | `CGeneratorGoldenTests.C_output_uses_only_block_comments_hex_literals_and_no_static` | реализовано, проверено | — | — |
| 4.4.5.1 | C51: `code`, `unsigned char` | `C51CGenerator.cs:3–4` | `CGeneratorGoldenTests.C51_font_12x16_matches_appendix_v1` | реализовано, проверено | — | — |
| 4.4.5.2 | A51: `DB` | `A51GeneratorBase.cs:112–125` | `A51GeneratorGoldenTests.Lines_never_end_with_comma_and_stay_below_256_characters` | реализовано, проверено | — | — |
| 4.4.5.3 | Модуль: `SEGMENT CODE`, `RSEG`, `PUBLIC`, `END` | `A51GeneratorBase.cs:129–144` | `A51GeneratorGoldenTests.Module_font_6x8_matches_appendix_v2`; ассемблирование — заказчик | реализовано, проверено; Keil A51 — за заказчиком (`docs/pmi.md`, п. 3.3) | — | — |
| 4.4.5.4 | Фрагмент `$INCLUDE` | `A51GeneratorBase.cs:150–161` | `A51GeneratorGoldenTests.Include_fragment_matches_appendix_v3` | реализовано, проверено; Keil A51 — за заказчиком (`docs/pmi.md`, п. 3.3) | — | — |
| 4.4.6.1 | STM32: `const` | `Stm32CGenerator.cs:7` | `CGeneratorGoldenTests.Stm32_image_128x64_matches_appendix_v4` | реализовано, проверено | — | — |
| 4.4.6.2 | `uint8_t` или `unsigned char` | `Stm32CGenerator.cs:20` | `CGeneratorGoldenTests.Stm32_unsigned_char_variant_has_no_stdint` | реализовано, проверено | — | — |
| 4.4.6.3 | Без `static`, `extern` в `.h` | `CGeneratorBase.cs` | `CGeneratorGoldenTests.C_output_uses_only_block_comments_hex_literals_and_no_static` | реализовано, проверено | — | — |
| 4.4.6.4 | Без предупреждений в MDK-ARM, IAR, GCC | `Stm32CGenerator.cs`; `tools/compile-check.ps1` | GT; `compile-check.ps1` пропущен (компиляторов нет) | за заказчиком (`docs/pmi.md`, п. 3.3, 4.2) | — | — |
| 4.4.7.1 | `.h`: защита от повторного включения | `CGeneratorBase.cs:85–86` | GT В.1, В.4 | реализовано, проверено | — | — |
| 4.4.7.2 | `.h`: `#define` | `CGeneratorBase.cs:42–70,97` | GT В.1, В.4, `All_gif_frames_form_a_two_dimensional_array` | реализовано, проверено | — | — |
| 4.4.7.3 | `.h`: `extern` | `CGeneratorBase.cs` | GT В.1, В.4 | реализовано, проверено | — | — |
| 4.4.8.1 | CP1251 по умолчанию или UTF-8 без BOM | `Core/Output/OutputEncoder.cs:7–11` | `OutputBehaviourTests.Cp1251_and_utf8_without_bom_encode_the_same_text`; разбор 77 файлов (раздел 1) | реализовано, проверено | minor | в чек-листе «список шрифта — этап 7» устарело (AU-14) |
| 4.4.8.2 | CRLF | `Core/Output/TextBuilder.cs:6–8` | `OutputBehaviourTests.Text_uses_crlf_only_and_ends_with_crlf`; разбор 77 файлов | реализовано, проверено | — | — |

### 3.6. Разделы 4.5 и 4.6

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| 4.5.1 | Последние параметры сохраняются и восстанавливаются | `Core/Settings/SettingsService.cs:42,66`; `App/ViewModels/MainViewModel.cs:63–103,387–404` | ядро — `SettingsServiceTests.Save_then_load_matches…`; сохранение из окна — `MainViewModelTests.Parameter_change_is_saved_on_the_autosave_callback`. Восстановление параметров вкладок из загруженного файла и сообщение о повреждённом файле (`MainViewModel.cs:99–102`) тестом не проверены | частично | major | тест: параметры обеих вкладок из `settings.json` попадают во ViewModel, повреждённый файл даёт сообщение в строке состояния (AU-10) |
| 4.5.2 | Свои пресеты: сохранить, переименовать, удалить | `Core/Presets/UserPresetStore.cs:8`; команды — `ImageConverterViewModel.cs:607–690`, `FontGeneratorViewModel.cs:591–680` | ядро — `UserPresetStoreTests`. Команды вкладок тестами не проверены | частично | major | тест команд «Сохранить», «Переименовать», «Удалить» и запрета для встроенных (AU-09) |
| 4.5.3 | Проект `.iiu` | `Core/Projects/ProjectSerializer.cs:10,12,28`; `MainViewModel.cs:240–318` | ядро — `ProjectSerializerTests` (круговое сохранение, нет исходника, относительный путь). Сборка документа из ViewModel и обратная загрузка не проверены | частично | major | тест «сохранить проект → открыть в новом окне → параметры, правки и 256 символов совпадают» (AU-10) |
| 4.5.4 | `settings.json` рядом с exe, иначе `%APPDATA%\ImageIU` | `Core/Settings/SettingsPathResolver.cs:13–60` (реальная попытка записи) | `SettingsServiceTests.Writable_directory_keeps_settings_beside_the_program`, `Unwritable_base_directory_uses_appdata_imageiu`; запуск exe в пустой папке создал `settings.json` (раздел 1) | реализовано, проверено | — | — |
| 4.6.1 | Независимые модули | `Core` (`net8.0`, без ссылок), `IImageDecoder` (`Core/Imaging/IImageDecoder.cs:6`), `IPacker`, `IOutputGenerator`, `IGlyphSource` | `ValidationAndRegistryTests.New_packer_is_added_by_registration_only`, `OutputBehaviourTests.New_format_is_added_by_registration_only`, `FontSourceIntegrationTests`; ссылки проектов прочитаны | реализовано, проверено | minor | в чек-листе «частично, подключение — этапы 6–7» устарело (AU-14) |
| 4.6.2 | Новый режим или формат — без правки остальных модулей | `Core/Packing/PackerRegistry.cs:24`, `Core/Output/OutputGeneratorRegistry.cs:28` | те же два теста регистрации | реализовано, проверено | — | — |
| 4.6.3 | Задел RGB565 и порядок байтов | `Core/Packing/PackingEnums.cs:4,14`, `PackingOptions.cs:13,27` | `ValidationAndRegistryTests.Unregistered_format_gives_clear_error`, `Color_format_is_rejected_by_monochrome_packer` | реализовано, проверено | — | — |
| 4.6.4 | Для монохрома биты цвета игнорируются | `Mono1bppPacker.cs`, `GrayscaleStep.cs:11` | `LayoutTests.Byte_order_does_not_affect_monochrome_output`, `GrayscaleAndBinarizerTests.Luminance_ignores_alpha` | реализовано, проверено | — | — |

### 3.7. Раздел 5 — интерфейс

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| 5.1.1 | Две вкладки | `App/MainWindow.xaml:50–57` | снимки обеих вкладок (раздел 1) | реализовано, проверено | — | — |
| 5.1.2 | Русский язык | `App/Resources/Strings.ru-RU.xaml` | `UiStringTests.Dictionary_keys_used_by_the_shell_exist`; снимки | реализовано, проверено | — | — |
| 5.1.3 | Строки отдельно от кода; новый язык без изменения программы | `App/Services/LocalizationService.cs:59–83`, `App/App.xaml.cs:26` | `UiStringTests.Interface_literals_are_not_written_in_code_or_markup` (только проект приложения). Загрузка `Languages\<язык>.xaml` тестом не проверена; испорченный файл словаря бросает исключение в `OnStartup` до создания окна (`LocalizationService.cs:74–75`). Сообщения `WicImageDecoder.cs:426–446` и `ImageHeaderReader.cs:342–345` написаны по-русски в коде; в интерфейс попадает только текст `IoError`, и там префикс удваивается (`UserText.cs:30`) | частично | major | перехват ошибок словаря с сообщением, проверка имени языка, тесты загрузки (AU-05); убрать удвоение и русские тексты исключений (AU-11) |
| 5.1.4 | Масштаб 100–200 % | `App/app.manifest:12–15` (PerMonitorV2), `App/GridScale.cs:44`, `App/WindowFit.cs:10` | `DpiTests` (30); `UiProbe dpi` — 0 обрезанных элементов при 100–200 % | реализовано, проверено; смена масштаба на физическом мониторе — за заказчиком (`docs/pmi.md`, п. 3.1) | — | — |
| 5.1.5 | Минимум 1024×680 | `App/MainWindow.xaml:6`, `MainWindow.xaml.cs:26–40` | `DpiTests.Window_keeps_the_minimum_of_the_specification_on_a_large_screen`, `Window_fits_the_work_area_at_any_windows_scale`; снимки окна 1024×680 | реализовано, проверено | — | — |
| 5.1.6 | ≤ 200 мс и ≤ 1 с | `RecalcScheduler.cs:19` (пауза 40 мс), кэш `ImagePipeline.cs:75–141` | `PerformanceTests` (3); `UiProbe perf`: максимум 85, 195 и 88 мс | реализовано, проверено | — | — |
| 5.2.1 | Клетки; 1:1…32:1 и «по размеру окна» | `App/Controls/PixelGridControl.cs:41,98,203` (`WriteableBitmap`, `NearestNeighbor`) | `GridAndPaintTests.Fit_picks_the_largest_integer_scale_that_fits`, `DpiTests` | реализовано, проверено | — | — |
| 5.2.2 | Линии сетки; толстые через 8 или 6 | `PixelGridControl.cs:396` | `GridAndPaintTests.Thick_step_follows_the_packing_direction` | реализовано, проверено | — | — |
| 5.2.3 | Подсказка: X, Y, байт, бит, значение | `App/UserText.cs:136–169`, `ImageConverterViewModel.cs:487`, `Mono1bppPacker.cs:63` | `LocateAndUnpackTests.Locate_is_consistent_with_pack`, `Locate_gives_tooltip_values_for_t6963c_6_bit_mode`, `ImageConverterViewModelTests.Hover_reports_the_packed_byte_of_the_origin` | реализовано, проверено | — | — |
| 5.2.4 | Подсветка байта в коде | `Core/Output/OutputDocument.cs` (`ByteSpanMap`), `CodeView.xaml.cs` | `OutputBehaviourTests.Byte_map_points_at_each_byte_literal`, `ImageConverterViewModelTests.Hover_reports…`, `FontGeneratorViewModelTests.A_stroke_is_one_undo_step…` | реализовано, проверено | — | — |
| 5.2.5 | Схема «ЖКИ»/«OLED» | `Core/Presets/ColorScheme.cs`, `App/Controls/SchemeColors.cs` | `PresetCatalogTests.Built_in_presets_match_d01`, `Image_preset_sets_size_packing_and_scheme_and_custom_does_not`; снимки (OLED) | реализовано, проверено | — | — |
| 5.2.6 | Упаковка, инверсия, порог сразу видны | `PixelGridControl.cs`, `ImageConverterViewModel.cs:853–890` | `GridAndPaintTests.Displayed_bit_flips_with_inversion`, `ImageConverterViewModelTests.Accepting_the_reset_clears_edits_and_history` | реализовано, проверено | — | — |
| 5.3.1 | Исходник и результат рядом | `App/Views/ImageConverterView.xaml:350` | снимок вкладки | реализовано, проверено | — | — |
| 5.3.2 | Порог — ползунок и поле | `ImageConverterView.xaml:183` | `ValidationTests.Image_field_error_is_shown_at_once…` (поле порога) | реализовано, проверено | — | — |
| 5.3.3 | Все параметры п. 4.1 и 4.3 | `ImageConverterView.xaml` | снимки при 100–200 % (раздел 1) | реализовано, проверено | — | — |
| 5.4.1 | Таблица 16×16 с подписями | `App/Controls/GlyphTableControl.cs:16,167`, `FontGeneratorView.xaml:429` | `FontGeneratorViewModelTests.A_stroke_is_one_undo_step…`; снимок вкладки | реализовано, проверено | — | — |
| 5.4.2 | Редактор: переключение, очистка, инверсия, сдвиг, копирование и вставка | `GlyphEditorViewModel.cs:101–136`, `Core/Fonts/GlyphOps.cs:17`, `App/GlyphClipboard.cs:9` | `GlyphEditingTests`, `FontGeneratorViewModelTests.Paste_fits_a_smaller_glyph_into_the_top_left`, `Glyph_text_round_trips_with_and_without_inversion` | реализовано, проверено | — | — |
| 5.4.3 | Параметры источника и диапазонов | `FontGeneratorView.xaml` | `FontGeneratorViewModelTests` (источники, диапазоны) | реализовано, проверено | — | — |
| 5.4.4 | Предпросмотр строки | `StringPreviewControl.cs:15` | `FontGeneratorViewModelTests.Preview_text_starts_as_the_agreed_sample` | реализовано, проверено | — | — |

### 3.8. Разделы 6–8

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| 6.1 | Повреждённые и большие файлы — без аварии, с причиной | декодер `WicImageDecoder.cs:63–90`; импорт, лист, настройки, проект; `App/Services/UnhandledExceptionReporter.cs:12`, `App/App.xaml.cs:19–22` | `WicImageDecoderTests.Broken_and_foreign_files_do_not_crash`, `Huge_gif_is_rejected_by_the_memory_limit`, `TextFileReaderTests`, `SettingsServiceTests.Corrupt_unknown_version_and_numeric_enum_reset_without_throwing`, `ProjectSerializerTests.Corrupt_file_and_unknown_version_are_errors_and_the_file_stays`, `UnhandledExceptionReporterTests` (6). Испорченный словарь `Languages\*.xaml` оставляет процесс без окна | частично | major | AU-05 |
| 6.2 | Недопустимые значения подсвечиваются, генерация невозможна | `ObservableValidator` во ViewModel; `App/Resources/Shared.xaml` | `ValidationTests` (41) | реализовано, проверено | — | — |
| 6.3 | Исходные файлы не изменяются | открытие только на чтение (`WicImageDecoder.cs:379`, `TextFileReader.cs`); запрет записи — `OutputWriter.cs:14,59` | `WicImageDecoderTests.Decode_does_not_modify_or_lock_the_file`, `OutputWriterTests.Source_file_is_never_overwritten`, `TextFileReaderTests.Read_round_trips…`. Каждая вкладка защищает только свои исходники (`ImageConverterViewModel.cs:594`, `FontGeneratorViewModel.cs:540–557`): вывод картинки с тем же именем может перезаписать импортированный массив шрифта после подтверждения перезаписи | частично | major | передавать в `OutputWriter` исходники обеих вкладок; тест (AU-04) |
| 6.4 | Запрос при закрытии с несохранёнными изменениями | `MainViewModel.cs:158–168,320–337`, `MainWindow.xaml.cs:69–75` | `MainViewModelTests.Closing_with_pixel_edits_asks_and_cancel_keeps_the_window`, `Discard_closes_and_still_writes_settings`, `Closing_with_a_drawn_glyph_asks…`. Команда меню «Выход» вызывает `Application.Shutdown()` (`MainViewModel.cs:237–238`); при `Shutdown` WPF игнорирует `Cancel` в `Closing`, поэтому «Отмена» в запросе закрывает программу и теряет правки | частично | major | «Выход» закрывает главное окно через `Close()`; тест (AU-03) |
| 6.5 | Детерминизм | весь Core; `InvariantCulture` во всех форматах вывода (`HeaderCommentBuilder.cs`, `NumberFormatter.cs`, `CGeneratorBase.cs`, `BinGenerator.cs`, `RussianPlural.cs:31`) | `LayoutTests.Repeated_packing_is_byte_identical`, `ImagePipelineTests.Repeated_run_is_identical_including_dithering`, `OutputBehaviourTests.Generation_without_date_is_byte_identical` и др.; тройной запуск `OutputSamples` (раздел 1). Тесты идут в культуре машины (ru-RU), явной фиксации культуры нет | реализовано, проверено | minor | тест с явными культурами ru-RU и th-TH и включённой датой (AU-12); статус чек-листа «частично» устарел (AU-14) |
| 7.1 | Windows 10/11 x64 | `publish.ps1` | запуск exe (раздел 1) | за заказчиком (`docs/pmi.md`, п. 3.2) | — | — |
| 7.2 | 2 ядра, 4 ГБ, 200 МБ, экран от 1366×768 | лимит 512 МБ (`ImageLimits.cs`), окно в рабочей области (`WindowFit.cs`) | `DpiTests.Window_fits_the_work_area_at_any_windows_scale`; размер `publish/` около 156 МБ. В `docs/pmi.md` нет пункта и строки протокола для минимальной конфигурации | частично | minor | добавить в ПМИ испытание на минимальной конфигурации и строку протокола (AU-17); само испытание — за заказчиком |
| 7.3 | Без прав администратора | `App/app.manifest:7` (`asInvoker`); запасной путь настроек | `SettingsServiceTests.Unwritable_base_directory_uses_appdata_imageiu`; запуск exe | реализовано, проверено | — | — |
| 7.4 | Без интернета | нет сетевых API в `src` (раздел 1) | поиск по коду; запуск exe | реализовано, проверено | — | — |
| 7.5 | Достаточно руководства | `docs/user_guide.md` | ДОК: все параметры и шесть сценариев; нет описания добавления языка | реализовано, проверено | minor | раздел о словарях `Languages\` (AU-13) |
| 8.1 | C# + .NET 8 + WPF | все `*.csproj` (`net8.0`, `net8.0-windows`) | сборка (раздел 1) | реализовано, проверено | — | — |
| 8.2 | Portable: один exe со всеми зависимостями | `publish.ps1:6–12` | запуск копии exe в пустой папке | реализовано, проверено; машина без .NET — за заказчиком (`docs/pmi.md`, п. 2, 4.1) | — | — |
| 8.3 | Входные данные | декодер, `WpfGlyphOutlineProvider`, `TextFileReader.GetSyntax` | `WicImageDecoderTests.Decodes_png_bmp_jpeg_and_static_gif`, `WpfGlyphOutlineProviderTests`, `TextFileReaderTests.Extension_selects_the_syntax` | реализовано, проверено | minor | статус чек-листа «частично» устарел (AU-14) |
| 8.4.1 | Keil C51 9.59 и A51 8.2.7 | генераторы C51 и A51 | GT | за заказчиком (`docs/pmi.md`, п. 3.3, 4.2) | — | — |
| 8.4.2 | MDK-ARM (AC5, AC6) | `Stm32CGenerator.cs` | GT | за заказчиком (`docs/pmi.md`, п. 3.3, 4.2) | — | — |
| 8.4.3 | IAR EWARM | `Stm32CGenerator.cs` | GT | за заказчиком (`docs/pmi.md`, п. 3.3, 4.2) | — | — |
| 8.4.4 | STM32CubeIDE (GCC) | `Stm32CGenerator.cs`; `tools/compile-check.ps1` | GT; скрипт пропускает компиляцию | за заказчиком (`docs/pmi.md`, п. 3.1, 3.3, 4.2) | — | — |
| 8.5 | Проект — JSON UTF-8 | `ProjectSerializer.cs`, `Core/Persistence/JsonFormat.cs:17,59` (перечисления строками) | `ProjectSerializerTests.Save_then_load_restores_both_tabs_and_the_second_save_matches_byte_for_byte` | реализовано, проверено | — | — |

### 3.9. Разделы 9–11

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| 9.1 | Руководство пользователя | `docs/user_guide.md` | ДОК: запуск, обе вкладки, параметры, формулы, шесть дисплеев, `font[(unsigned char)c]`, SH1106, NT7108, CP1251 | реализовано, проверено | minor | AU-13 |
| 9.2 | Форматы вывода с примерами для пресетов | `docs/output_formats.md` | ДОК: шесть пресетов | реализовано, проверено | — | — |
| 9.3 | ПМИ | `docs/pmi.md` | ДОК: состав, порядок, протокол; нет минимальной конфигурации и строк протокола для масштаба монитора и согласования тестовых изображений | реализовано, проверено | minor | AU-17 |
| 9.4 | Исходники и инструкция по сборке | `README.md`, `build.ps1`, `publish.ps1` | сборка и тесты по инструкции (раздел 1) | реализовано, проверено | — | — |
| 9.5 | Тестовые изображения и эталоны | `testdata/` (3 изображения, 3 листа), `testdata/reference/` (192 файла и `index.md`) | `ReferenceFileTests`, `TestPatternTests`, `FontSheetTests` | реализовано, проверено | — | — |
| 10.1 | Таблица этапов договора | — (K-01) | — | не применяется | — | — |
| 11.1.1 | Сравнение с эталонами по пресетам и комбинациям | `testdata/reference/`, `ReferenceFileTests.cs:20–71` | 6 входов × 16 комбинаций. Для изображений сравнивается только упаковщик: PNG бинаризует тест (`ReferenceFileTests.cs:102–118`), а не конвейер и генератор программы | частично | major | AU-01 |
| 11.1.2 | Эталоны независимы и согласованы | `tests/Image2Gdram.Reference/` без ссылки на Core | `ReferenceImplementationTests.Reference_assembly_does_not_reference_core` | реализовано, проверено; согласование — за заказчиком (Q-08, `docs/pmi.md`, п. 4.1) | — | — |
| 11.2.1 | C51 в Keil C51 | генератор C51 | GT | за заказчиком (`docs/pmi.md`, п. 3.3, 4.2) | — | — |
| 11.2.2 | A51 в обоих вариантах | генераторы A51 | GT | за заказчиком (`docs/pmi.md`, п. 3.3, 4.2) | — | — |
| 11.2.3 | STM32 в трёх средах | генератор STM32 | GT; `compile-check.ps1` | за заказчиком (`docs/pmi.md`, п. 3.3, 4.2) | — | — |
| 11.3.1 | Таблица настройки и строка на дисплеях | `docs/pmi.md`, п. 3.4 | — | за заказчиком (`docs/pmi.md`, п. 3.4, 4.3) | — | — |
| 11.3.2 | SSD1306 и WG240128A обязательны; прочие — эталоном | `docs/pmi.md`, п. 4.3 | ДОК | за заказчиком (`docs/pmi.md`, п. 4.3) | — | — |
| 11.4 | Критерий приёмки | предпросмотр показывает итоговые биты (N-08) | `GridAndPaintTests.Displayed_bit_flips_with_inversion`, `ReferenceFileTests` | за заказчиком (`docs/pmi.md`, п. 2, 4.4) | — | — |
| 11.5 | Исходник прошивки в CP1251 | `docs/user_guide.md`, `docs/pmi.md`, п. 2, 4.3 | ДОК | реализовано, проверено | — | — |

### 3.10. Приложения А–В

| ID чек-листа | Пункт ТЗ | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| А.1 | WG240128A: горизонтально, MSB, 8 бит, 3840, ЖКИ | `presets.json:5` | `PresetCatalogTests.Built_in_presets_match_d01`, `SizeTests.Preset_screen_buffers_have_expected_size` | реализовано, проверено | — | — |
| А.2 | W0240128 (UC1608) | `presets.json` | то же | реализовано, проверено; Q-01 — за заказчиком (`docs/pmi.md`, п. 4.3) | — | — |
| А.3 | RG12864F (NT7108) | `presets.json` | то же | реализовано, проверено; Q-01 — за заказчиком (`docs/pmi.md`, п. 4.3) | — | — |
| А.4 | RET012864DGPP3N (SSD1305) | `presets.json` | то же | реализовано, проверено; Q-01 — за заказчиком (`docs/pmi.md`, п. 4.3) | — | — |
| А.5 | OLED128X64-0.96 (SSD1306) | `presets.json` | то же | реализовано, проверено | — | — |
| А.6 | HT1.3-OLED-BW (SH1106) | `presets.json` | то же | реализовано, проверено | — | — |
| А.7 | «Пользовательский» не задаёт параметров | `PresetApplication.cs:12,34`, `PresetSelectorViewModel.cs:78` | `PresetCatalogTests.Image_preset_sets_size_packing_and_scheme_and_custom_does_not`, `Font_preset_does_not_change_the_cell` | реализовано, проверено | minor | статус чек-листа «частично, этап 6» устарел (AU-14) |
| А.8 | Сверка с datasheet | D-01 | запись сверки | реализовано, проверено; подтверждение — за заказчиком (Q-01) | — | — |
| А.9 | SH1106: 128 столбцов, смещение — драйвер | `docs/user_guide.md`, раздел 7 | ДОК | реализовано, проверено | — | — |
| Б.1 | `test_pattern_240x128.png`, `test_pattern_128x64.png` | `tools/TestAssetsGenerator/TestPattern.cs:26` | `TestPatternTests.Screen_has_a_frame_markers_ticks_and_separated_blocks` (генератор отказывается при наложении элементов) | реализовано, проверено | — | — |
| Б.1.1–Б.1.7 | Рамка, маркеры, метки, диагонали, шахматы, прямоугольники, надпись 5×7 | `TestPattern.cs`, `Font5x7.cs` | тот же тест; снимок сетки с таблицей настройки (раздел 1) | реализовано, проверено | — | — |
| Б.2 | `test_sprite_13x11.png` | `TestPattern.cs:55` | `TestPatternTests.Sprite_is_13_by_11_and_asymmetric`, `WicImageDecoderTests.Generated_png_matches_the_test_pattern` | реализовано, проверено | — | — |
| Б.3 | Согласование тестовых изображений | — (Q-11) | — | за заказчиком (Q-11); строки протокола в `docs/pmi.md` нет | minor | AU-17 |
| В.1 | C51, шрифт 12×16 | `C51CGenerator.cs`, `CGeneratorBase.cs` | `CGeneratorGoldenTests.C51_font_12x16_matches_appendix_v1` (построчно, с поправками D-02, D-03, N-02, K-06) | реализовано, проверено | — | — |
| В.2 | A51, модуль 6×8 | `A51ModuleGenerator` | `A51GeneratorGoldenTests.Module_font_6x8_matches_appendix_v2` | реализовано, проверено | — | — |
| В.3 | A51, фрагмент `$INCLUDE` | `A51IncludeGenerator` | `A51GeneratorGoldenTests.Include_fragment_matches_appendix_v3` | реализовано, проверено | — | — |
| В.4 | STM32, изображение 128×64 | `Stm32CGenerator` | `CGeneratorGoldenTests.Stm32_image_128x64_matches_appendix_v4` | реализовано, проверено | — | — |

## 4. Проверяемые требования `agents.md`

| ID | Пункт `agents.md` | Реализация | Тесты и проверка | Статус | Серьёзность | Что нужно сделать |
|---|---|---|---|---|---|---|
| AG-1.1 | Строго .NET 8: `net8.0-windows` для приложения, `net8.0` для библиотек | все 12 `*.csproj` | сборка (раздел 1) | реализовано, проверено | — | — |
| AG-1.2 | `CommunityToolkit.Mvvm` 8.4.2; ImageSharp не подключать | `App/image2gdram_converter.csproj:12–13` | поиск `PackageReference` | реализовано, проверено | — | — |
| AG-1.3 | Дымовая компиляция, если есть `gcc`/`clang`/`arm-none-eabi-gcc` | `tools/compile-check.ps1:34–75` | запуск: компиляторов нет, пропуск с кодом 0 | реализовано, проверено | — | — |
| AG-1.4 | Вне рамок 1.0: цвет только архитектурно | `PackingEnums.cs:4,14`; в интерфейсе цвета нет | снимки; `ValidationAndRegistryTests.Unregistered_format_gives_clear_error` | реализовано, проверено | — | — |
| AG-2.1 | Решения по неоднозначностям — в `decisions.md` в формате п. 2.1 | `docs/decisions.md` | ДОК | реализовано, проверено | — | — |
| AG-2.3 | Никаких заглушек | — | поиск (раздел 1) | реализовано, проверено | — | — |
| AG-2.4 | 0 ошибок и 0 предупреждений, тесты зелёные | `Directory.Build.props` (`TreatWarningsAsErrors`) | раздел 1 | реализовано, проверено | — | — |
| AG-2.5 | Git, `.gitignore`, коммит на этап | `.gitignore` (`[Bb]in/`, `[Oo]bj/`, `.vs/`, `*.user`, `publish/`, `TestResults/`, `artifacts/`) | `git log` | реализовано, проверено | — | — |
| AG-2.6 | Только MIT/Apache-2.0/BSD, без сети | пакеты раздела 1 | `README.md`, раздел «Лицензии» | реализовано, проверено | — | — |
| AG-2.7 | Ядро без WPF, MVVM, в code-behind только визуальная логика | `Core` (`net8.0`); code-behind: `MainWindow.xaml.cs` (рабочая область монитора, закрытие), `ImageConverterView.xaml.cs`, `FontGeneratorView.xaml.cs` (передача штриха и наведения), `CodeView.xaml.cs` | прочитаны все пять файлов code-behind | реализовано, проверено | — | — |
| AG-2.8 | API WIC и WPF проверены компиляцией | сборка | раздел 1 | реализовано, проверено | — | — |
| AG-2.9 | Детерминизм: `InvariantCulture`, без порядка `Dictionary`, без случайных чисел без seed | вывод — раздел 3.8 (6.5); `PackerRegistry.Formats` сортирует (`PackerRegistry.cs:22`); `TestBitmaps.SeedFor` | тройной запуск `OutputSamples`; явных культур в тестах нет | реализовано, проверено | minor | AU-12 |
| AG-3 | Файлы состояния ведутся в репозитории | `docs/implementation-plan.md`, `requirements_checklist.md`, `decisions.md`, `architecture.md` | ДОК; часть статусов чек-листа устарела, в `architecture.md` раздел 10 пишет «Core: Output — к интерфейсу не подключено» | частично | minor | AU-14 |
| AG-4 | Структура решения | `image2gdram_converter.sln`, `src/`, `tests/`, `tools/`, `testdata/`, `docs/` | `git ls-files` | реализовано, проверено | — | — |
| AG-5.1 | `IImageDecoder` в Core, WIC в `net8.0-windows`, размер до копирования, > 8192 — сообщение, GIF собирает свой код и это проверено тестом | `Core/Imaging/IImageDecoder.cs:6`, `Wic/WicImageDecoder.cs:34–35,93–143` | `WicImageDecoderTests.Side_8192…`, `Animated_gif_composites_disposal`, `Disposal_restore_previous…` | реализовано, проверено | — | — |
| AG-5.2 | Шаги — отдельные классы; поворот, масштаб, серое и дизеринг — свой код | `Core/Processing/*.cs` | раздел 3.2 | реализовано, проверено | — | — |
| AG-5.3 | `IPacker`: `Pack`, `Unpack`, `Locate`, `GetSize` | `Core/Packing/IPacker.cs:7`, `Mono1bppPacker.cs:13,20,41,63` | `LocateAndUnpackTests`, `SizeTests` | реализовано, проверено | — | — |
| AG-5.4 | Генератор возвращает тексты и карту «байт → позиция» | `Core/Output/OutputDocument.cs` | `OutputBehaviourTests.Byte_map_points_at_each_byte_literal` | реализовано, проверено | — | — |
| AG-5.5 | Формат пикселя и порядок байтов в `PackingOptions`; реестр | `PackingOptions.cs:13,27`, `PackerRegistry.cs:9` | `ValidationAndRegistryTests.New_packer_is_added_by_registration_only` | реализовано, проверено | — | — |
| AG-6.1 | Семантика упаковки и контрольные примеры (0x01/0xFE, 0x80/0x7F, 0x80/0x7F, 0x20/0xDF), 1536, 6144, 240 = 40, 13×11 | `Mono1bppPacker.cs` | `ControlExampleTests.Single_pixel_at_origin_gives_expected_first_byte`, `SizeTests` (прогон раздела 1) | реализовано, проверено | — | — |
| AG-6.2 | Формулы конвейера, граница порога, дизеринг, правки по кадрам, откат параметра | `Core/Processing/*.cs`, `FramePixelOverrides.cs`, `ImageConverterViewModel.cs:911` | раздел 3.2; `ImageConverterViewModelTests.Refusing_to_reset_edits_restores_the_parameter` | реализовано, проверено | — | — |
| AG-7 | Решения D-01…D-17 | см. `decisions.md`, раздел A | разделы 3.3–3.5 этого файла | реализовано, проверено | major | D-13 нарушается после выбора пресета (AU-02) |
| AG-8 | Совместимость вывода: C89, `code`, `const uint8_t`, A51 верхний регистр, валидатор, CP1251/UTF-8 без BOM, CRLF, дата, детерминизм, подтверждение перезаписи | раздел 3.5 | раздел 3.5; разбор 77 файлов | реализовано, проверено | — | — |
| AG-9.1 | Окно ≥ 1024×680, PerMonitorV2, ничего не обрезано | раздел 3.7 (5.1.4, 5.1.5) | `UiProbe dpi` | реализовано, проверено | — | — |
| AG-9.2 | Строки только во внешних словарях; язык из `settings.json`, без пересборки | `LocalizationService.cs:59–83` | см. 5.1.3 | частично | major | AU-05, AU-11 |
| AG-9.3 | Сетка в `WriteableBitmap`, не элемент на пиксель; `NearestNeighbor`, `UseLayoutRounding`; штрих — одно действие | `PixelGridControl.cs:82,98,203` | `GridAndPaintTests`, `DpiTests` | реализовано, проверено | — | — |
| AG-9.4 | Окно кода: только чтение, моноширинный шрифт, ~1 МБ | `CodeView.xaml.cs:25–26` (AvalonEdit, `IsReadOnly`, Consolas) | `UiProbe perf` 1024×1024 (вывод около 800 КБ) — до 195 мс | реализовано, проверено | — | — |
| AG-9.5 | Пересчёт асинхронный, отмена, пауза 30–50 мс | `RecalcScheduler.cs:19` (40 мс) | `RecalcSchedulerTests` | реализовано, проверено | — | — |
| AG-9.6 | Валидация через `INotifyDataErrorInfo` | `ObservableValidator` | `ValidationTests` | реализовано, проверено | — | — |
| AG-9.7 | Три глобальных обработчика | `App/App.xaml.cs:19–22` | `UnhandledExceptionReporterTests` | реализовано, проверено | — | — |
| AG-9.8 | «Сохранить / Не сохранять / Отмена» | `MainViewModel.cs:320–337` | `MainViewModelTests`; «Отмена» из меню «Выход» не работает | частично | major | AU-03 |
| AG-9.9 | Вкладка шрифтов: таблица, редактор, источники, предпросмотр строки | раздел 3.7 (5.4) | раздел 3.7 | реализовано, проверено | — | — |
| AG-9.10 | Предупреждения — в неблокирующей панели | `ImageConverterView.xaml:363`, `FontGeneratorView.xaml:466–468`, `MainWindow.xaml:48` | ядро — `OutputBehaviourTests` (предупреждение 64 КБ). Что предупреждение доходит до панели вкладки, тестом не проверено | частично | minor | тест: C51 для 1024×1024 даёт текст в `WarningText` (AU-16) |
| AG-10.1 | `settings.json` через `AppContext.BaseDirectory`, реальная попытка записи; повреждённый — умолчания и сообщение | `SettingsPathResolver.cs:13–60`, `SettingsService.cs:19,42`, `MainViewModel.cs:99–102` | `SettingsServiceTests`; запуск exe | частично | major | сообщение окна при повреждённом файле не проверено (AU-10) |
| AG-10.2 | Пользовательские пресеты; встроенные неизменяемы | `UserPresetStore.cs:8` | `UserPresetStoreTests` | частично | major | AU-09 |
| AG-10.3 | Проект `.iiu` через DTO, перечисления строками | `Core/Persistence/FileDtos.cs`, `ModelMapping.cs`, `JsonFormat.cs:59` | `ProjectSerializerTests` | частично | major | AU-10 (путь через окно) |
| AG-11.1 | Модульные тесты ядра по списку раздела 11 | `tests/Image2Gdram.Core.Tests` | 2347 тестов; содержимое проверено выборочно (раздел 3) | реализовано, проверено | — | — |
| AG-11.2 | Независимая эталонная реализация упаковки | `tests/Image2Gdram.Reference/ReferencePacker.cs:10–128` (строки бит, `Convert.ToByte`) | `ReferenceImplementationTests`, `ReferenceComparisonTests` | реализовано, проверено | — | — |
| AG-11.3 | Тесты ViewModel: пресет → «Пользовательский (на основе …)», сброс с подтверждением и откатом, undo/redo | `App.Tests` | `ImageConverterViewModelTests`, `FontGeneratorViewModelTests` | реализовано, проверено | — | — |
| AG-11.4 | Тестовые изображения и листы шрифтов генерируются утилитой | `tools/TestAssetsGenerator/` | `TestPatternTests`, `FontSheetTests` | реализовано, проверено | — | — |
| AG-11.5 | Эталоны по всем пресетам, спрайту и трём шрифтам; `index.md`; тест побайтно сравнивает **вывод ядра** | `testdata/reference/` | `ReferenceFileTests` сравнивает упаковщик, а не путь программы от PNG | частично | major | AU-01 |
| AG-11.6 | Запуск собранного приложения | — | запуск exe (раздел 1) | реализовано, проверено | — | — |
| AG-12.1 | Документы раздела 9 ТЗ, `decisions.md`, `requirements_checklist.md` | `docs/` | ДОК | реализовано, проверено | minor | AU-13, AU-14, AU-17 |
| AG-12.2 | `README.md`: назначение, сборка, тесты, публикация, лицензии | `README.md` | ДОК | реализовано, проверено | — | — |
| AG-12.3 | `publish.ps1` с нужными ключами; exe запускается, без админа, `settings.json` рядом; 1.0.0, Product | `publish.ps1:6–12`, `Directory.Build.props:10–13` | запуск копии exe | реализовано, проверено | minor | пересобрать из итогового коммита (AU-15) |
| AG-14.1 | `dotnet build -c Release` — 0/0; `dotnet test` зелёный | — | раздел 1 | реализовано, проверено | — | — |
| AG-14.2 | Каждый пункт разделов 4–6, 8, 9 ТЗ реализован и проверен | — | раздел 3 | частично | major | закрыть AU-01…AU-10 |
| AG-14.3 | Вывод для приложения В совпадает по оформлению | — | GT В.1–В.4 | реализовано, проверено | — | — |
| AG-14.4 | Все побайтные сравнения с эталонами проходят | `testdata/reference/` | `ReferenceFileTests` | реализовано, проверено | — | — |
| AG-14.5 | Временные требования п. 5.1 и замеры | — | `UiProbe perf` | реализовано, проверено | — | — |
| AG-14.6 | Опубликованный single-file exe запускается | — | запуск | реализовано, проверено | — | — |
| AG-14.7 | Документация раздела 9 ТЗ | `docs/` | ДОК | реализовано, проверено | — | — |

## 5. Открытые пропуски

Critical-пропусков нет: неверных байтов и вывода, противоречащего ТЗ, аудит не нашёл (побайтные сравнения, золотые тесты и тройная генерация совпадают).

| № | Серьёзность | Пункты | Что не так | Что сделать |
|---|---|---|---|---|
| AU-01 | major | 2.0, 11.1.1, AG-11.5 | Для изображений `ReferenceFileTests` сравнивает только упаковщик: растр из PNG строит сам тест (`ReferenceFileTests.cs:102–118`). Путь программы «декодер → `ImagePipeline` с параметрами пресета или „по исходному“ → генератор» с эталонами не сверяется | тест: каждый PNG через `WicImageDecoder`, `ImagePipeline` (размер пресета, для спрайта — «по исходному») и `BinGenerator` для всех 16 комбинаций равен `.bin`; шрифты — через `SheetGlyphSource`, `FontTable.ToOutputData` и `BinGenerator` |
| AU-02 | major | 4.4.2.1, 4.3.0.2, AG-7 (D-13) | После выбора пресета с другим размером имя массива по умолчанию сохраняет суффикс прежнего размера и перестаёт обновляться (`ImageConverterViewModel.cs:372`, `744–756`) | при применении пресета сохранять признак автоматического имени и пересчитывать имя; тест |
| AU-03 | major | 6.4, AG-9.8 | «Файл → Выход» вызывает `Application.Shutdown()` (`MainViewModel.cs:237–238`), при котором `Cancel` в `Closing` игнорируется: «Отмена» в запросе сохранения не отменяет выход | команда «Выход» просит окно закрыться (`Close`), закрытие проходит через `TryClose`; тест |
| AU-04 | major | 6.3 | Вкладки защищают только свои исходники (`ImageConverterViewModel.cs:594`, `FontGeneratorViewModel.cs:540–557`): вывод одной вкладки может перезаписать исходник другой | обе вкладки передают в `OutputWriter` исходники обеих вкладок; тест |
| AU-05 | major | 5.1.3, 6.1, AG-9.2 | Испорченный `Languages\<язык>.xaml` бросает исключение в `OnStartup` до создания окна (`LocalizationService.cs:74–75`); имя языка из `settings.json` не проверяется; загрузка словаря не покрыта тестом | перехват ошибок чтения и разбора, проверка имени языка, сообщение в строке состояния; тесты: внешний словарь подменяет строку, испорченный файл и чужое имя языка не ломают запуск |
| AU-06 | major | 4.1.1.4, 4.1.1.5 | Вкладка картинок с многокадровым GIF не проверена: кадр по умолчанию, смена кадра, «все кадры» | тест вкладки на декодированном изображении из трёх кадров |
| AU-07 | major | 4.4.1.2 | Команда «Копировать» не проверена | тест обеих вкладок: в буфере текст выбранного файла с CRLF |
| AU-08 | major | 4.4.1.3, 4.4.1.6 | Команда «Сохранить код» не проверена: пара файлов, отказ и согласие на перезапись | тест команды во временной папке |
| AU-09 | major | 4.5.2, AG-10.2 | Команды пользовательских пресетов на вкладках не проверены | тест: сохранить, переименовать, удалить; у встроенного пресета команды недоступны |
| AU-10 | major | 4.5.1, 4.5.3, AG-10.1, AG-10.3 | Через окно не проверены: восстановление параметров из `settings.json`, сообщение о повреждённом файле, сохранение и открытие проекта | тесты `MainViewModel` |
| AU-11 | minor | 5.1.3 | Текст `IoError` в сообщении удваивает префикс «Не удалось прочитать файл:» (`UserText.cs:30` + `WicImageDecoder.cs:446`); русские тексты исключений в `WicImageDecoder.cs:426–446`, `ImageHeaderReader.cs:342–345`, `PipelineException.cs:13` | в словарь передавать причину без префикса; тексты исключений — нейтральные английские для разработчика |
| AU-12 | minor | 6.5, AG-2.9 | Тесты не фиксируют культуру, детерминизм подтверждён только культурой машины | тест: все форматы с включённой датой в ru-RU и th-TH равны выводу в `InvariantCulture` |
| AU-13 | minor | 5.1.3, 7.5, 9.1 | В руководстве не описано, как добавить язык интерфейса | раздел в `docs/user_guide.md` |
| AU-14 | minor | 3.1, 4.3.3.3, 4.3.7.2, 4.4.2.1, 4.4.2.2, 4.4.8.1, 4.5.2, 4.6.1, 5.1.1, 6.3, 6.5, 8.3, А.7; `architecture.md` раздел 10 | Устаревшие статусы и ссылки «этап 6/7» в чек-листе; «к интерфейсу не подключено» в `architecture.md` | привести файлы состояния к факту |
| AU-15 | minor | 1.1, AG-12.3 | `publish/` собран до последнего коммита этапа 9 | пересобрать `publish.ps1` в конце этапа |
| AU-16 | minor | AG-9.10 | Показ предупреждения 64 КБ в панели вкладки не проверен | тест вкладки |
| AU-17 | minor | 7.2, 9.3, Б.3 | В ПМИ нет испытания на минимальной конфигурации и строк протокола для масштаба монитора и согласования тестовых изображений (Q-11) | дополнить `docs/pmi.md` |

## 6. Пункты ТЗ, которых нет в чек-листе

| Пункт ТЗ | Требование | Где отражено | Что сделать |
|---|---|---|---|
| 11 (вводный абзац) | «Испытания проводятся по ПМИ, согласованной с заказчиком» | `docs/pmi.md` подготовлена, согласование не отмечено нигде | добавить строку 11.0 в чек-лист (за заказчиком) и отметку согласования ПМИ в протокол (AU-17) |

Остальные утверждения ТЗ, включая вводные абзацы разделов 2, 4.1.2, 4.3 и примечания приложения А, в чек-листе есть.

## 7. Открытые вопросы к заказчику (раздел E `decisions.md`)

| ID | Тема | Действующее решение | Влияние на этап 10 |
|---|---|---|---|
| Q-01 | Пресеты UC1608, NT7108, SSD1305; смещение столбцов RET012864DGPP3N | D-01 | не блокирует; протокол `docs/pmi.md`, п. 4.3 |
| Q-02 | Название в заголовке | D-02 | не блокирует |
| Q-03 | `x` вместо «×» | D-03 | не блокирует |
| Q-04 | Единая структура заголовка | N-02 | не блокирует |
| Q-05 | Заголовок во фрагменте `$INCLUDE` | N-03 | не блокирует |
| Q-06 | Предпросмотр итоговых бит при инверсии | N-08 | не блокирует |
| Q-07 | Длина имени модуля A51 27 символов | N-05 | не блокирует |
| Q-08 | Согласование эталонов | N-28, N-60 | не блокирует; `docs/pmi.md`, п. 4.1 |
| Q-10 | Направление поворота и отражения | N-10 | не блокирует |
| Q-11 | Согласование тестовых изображений | N-40 | не блокирует; строку протокола добавить (AU-17) |
| Q-13 | Заголовок в `.h` | K-06 | не блокирует |

Q-09 и Q-12 закрыты решением N-39.

## 8. Повторная проверка после исправлений (фаза Б)

Заполняется в фазе Б.

## 9. Файлы тестовых классов

| Класс | Файл |
|---|---|
| `A51GeneratorGoldenTests`, `CGeneratorGoldenTests`, `FormattingTests`, `HeaderCommentTests`, `NameTests`, `OutputBehaviourTests`, `OutputWriterTests` | `tests/Image2Gdram.Core.Tests/Output/` |
| `ArrayImportParserTests`, `CharRangeSetTests`, `FontSourceIntegrationTests`, `FontTableTests`, `GlyphEditingTests`, `ImportGlyphSourceTests`, `PolygonRasterizerTests`, `SheetGlyphSourceTests`, `TextFileReaderTests`, `TrueTypeGlyphSourceTests` | `tests/Image2Gdram.Core.Tests/Fonts/` |
| `ControlExampleTests`, `LayoutTests`, `LocateAndUnpackTests`, `MonoBitmapTests`, `ReferenceComparisonTests`, `SizeTests`, `ValidationAndRegistryTests` | `tests/Image2Gdram.Core.Tests/Packing/` |
| `BackgroundCompositorTests`, `GrayscaleAndBinarizerTests`, `ImagePipelineTests`, `PixelOverrideTests`, `ResizeStepTests`, `RotateFlipTests` | `tests/Image2Gdram.Core.Tests/Processing/` |
| `PresetCatalogTests`, `UserPresetStoreTests` | `tests/Image2Gdram.Core.Tests/Presets/` |
| `SettingsServiceTests`; `ProjectSerializerTests`; `EditHistoryTests`; `ImageLimitsTests` | `tests/Image2Gdram.Core.Tests/Settings/`, `Projects/`, `Editing/`, `Imaging/` |
| `Cp1251Tests`, `RussianPluralTests`; `FontSheetTests`, `TestPatternTests`; `ProductInfoTests`, `ReferenceImplementationTests` | `tests/Image2Gdram.Core.Tests/Text/`, `TestAssets/`, корень проекта |
| `WicImageDecoderTests`, `ReferenceFileTests`, `SheetGlyphSourceDecoderTests` | `tests/Image2Gdram.Imaging.Wic.Tests/` |
| `WpfGlyphOutlineProviderTests` | `tests/Image2Gdram.Fonts.Wpf.Tests/` |
| `DpiTests`, `FontGeneratorViewModelTests`, `GridAndPaintTests`, `ImageConverterViewModelTests`, `MainViewModelTests`, `PerformanceTests`, `RecalcSchedulerTests`, `UiStringTests`, `UnhandledExceptionReporterTests`, `ValidationTests` | `tests/Image2Gdram.App.Tests/` |
