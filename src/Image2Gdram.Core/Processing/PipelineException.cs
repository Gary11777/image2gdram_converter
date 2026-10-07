namespace Image2Gdram.Core.Processing;

/// <summary>Ошибка конвейера, которую видит пользователь (текст подставит интерфейс по коду).</summary>
public enum PipelineErrorCode
{
    /// <summary>Режим «размер по исходному» дал сторону больше 1024 (решение D-07).</summary>
    SourceLargerThan1024,
}

public sealed class PipelineException : Exception
{
    public PipelineException(PipelineErrorCode code, int width, int height)
        : base($"Source image {width}x{height} is larger than 1024 on one side; set the target size manually.")
    {
        Code = code;
        Width = width;
        Height = height;
    }

    public PipelineErrorCode Code { get; }

    public int Width { get; }

    public int Height { get; }
}
