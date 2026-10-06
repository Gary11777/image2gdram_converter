namespace image2gdram_converter.Services;

/// <summary>Пересчёт предпросмотра. Реализация может откладывать запуск и отменять предыдущий.</summary>
public interface IRecalcScheduler
{
    void Schedule(Func<CancellationToken, object?> compute, Action<object?> apply, Action<Exception> onError);
}
