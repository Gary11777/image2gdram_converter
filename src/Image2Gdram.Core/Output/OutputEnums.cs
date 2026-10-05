namespace Image2Gdram.Core.Output;

/// <summary>Формат вывода (п. 4.4.1, 4.4.5, 4.4.6 ТЗ).</summary>
public enum OutputFormat
{
    /// <summary>Язык C для Keil C51: <c>unsigned char code</c>, пара <c>.c</c>/<c>.h</c>.</summary>
    CKeilC51,

    /// <summary>Язык C для STM32: <c>const uint8_t</c> или <c>const unsigned char</c>, пара <c>.c</c>/<c>.h</c>.</summary>
    CStm32,

    /// <summary>Keil A51, самостоятельный модуль: <c>PUBLIC</c>, <c>SEGMENT CODE</c>, <c>RSEG</c>, <c>END</c>.</summary>
    A51Module,

    /// <summary>Keil A51, фрагмент для <c>$INCLUDE</c>: только метка и строки <c>DB</c>.</summary>
    A51Include,

    /// <summary>Двоичный файл: только байты массива.</summary>
    Bin,
}

/// <summary>Кодировка текстовых файлов (п. 4.4.8 ТЗ).</summary>
public enum OutputEncoding
{
    Cp1251,
    Utf8NoBom,
}

/// <summary>Формат чисел в ассемблере (п. 4.4.4 ТЗ, решение D-09).</summary>
public enum AsmNumberFormat
{
    /// <summary><c>0FFh</c>.</summary>
    Hex,

    /// <summary><c>11111111b</c>.</summary>
    Binary,
}

/// <summary>Тип элемента массива для STM32 (п. 4.4.6 ТЗ).</summary>
public enum Stm32ElementType
{
    /// <summary><c>uint8_t</c> из <c>&lt;stdint.h&gt;</c>.</summary>
    Uint8T,

    UnsignedChar,
}

/// <summary>Расширение файла ассемблера (п. 4.4.1 ТЗ).</summary>
public enum AsmFileExtension
{
    A51,
    Asm,
}

public enum OutputFileKind
{
    CSource,
    CHeader,
    Assembly,
    Binary,
}

/// <summary>Поле заголовка-комментария со значением, заданным пользователем.</summary>
public enum HeaderField
{
    SourceFile,
    FontFamily,
    ImportedArrayName,
    Preset,
}
