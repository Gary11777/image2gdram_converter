namespace Image2Gdram.Core.Output;

/// <summary>
/// Генератор вывода одного формата (п. 4.4 ТЗ). Новый формат добавляется новой реализацией и регистрацией
/// в <see cref="OutputGeneratorRegistry"/>.
/// </summary>
public interface IOutputGenerator
{
    OutputFormat Format { get; }

    /// <summary>
    /// Генерирует файлы. Имя массива должно проходить <see cref="NameValidator"/> для <see cref="Format"/>,
    /// иначе — <see cref="ArgumentException"/>: интерфейс проверяет имя до генерации.
    /// </summary>
    OutputDocument Generate(OutputData data, OutputOptions options);
}
