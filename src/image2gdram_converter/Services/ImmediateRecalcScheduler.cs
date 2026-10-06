namespace image2gdram_converter.Services;

/// <summary>Считает и применяет результат сразу, в вызывающем потоке. Нужен тестам.</summary>
public sealed class ImmediateRecalcScheduler : IRecalcScheduler
{
    public void Schedule(Func<CancellationToken, object?> compute, Action<object?> apply, Action<Exception> onError)
    {
        ArgumentNullException.ThrowIfNull(compute);
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(onError);
        try
        {
            apply(compute(CancellationToken.None));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            onError(ex);
        }
    }
}
