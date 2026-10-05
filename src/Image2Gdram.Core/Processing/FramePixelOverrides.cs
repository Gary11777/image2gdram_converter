namespace Image2Gdram.Core.Processing;

/// <summary>
/// Ручные правки анимированного GIF: отдельный набор на каждый кадр (решение F-11).
/// Номер кадра — с нуля, в том же порядке, что <c>DecodedImage.Frames</c>.
/// </summary>
public sealed class FramePixelOverrides
{
    private readonly Dictionary<int, PixelOverrides> _frames = new();

    public bool HasEdits
    {
        get
        {
            foreach (PixelOverrides frame in _frames.Values)
            {
                if (frame.HasEdits)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public PixelOverrides ForFrame(int frameIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frameIndex);
        if (!_frames.TryGetValue(frameIndex, out PixelOverrides? frame))
        {
            frame = new PixelOverrides();
            _frames.Add(frameIndex, frame);
        }

        return frame;
    }

    public bool TryGetFrame(int frameIndex, out PixelOverrides? overrides)
    {
        if (_frames.TryGetValue(frameIndex, out PixelOverrides? frame) && frame.HasEdits)
        {
            overrides = frame;
            return true;
        }

        overrides = null;
        return false;
    }

    public void Clear() => _frames.Clear();

    /// <summary>Номера кадров, в которых есть хотя бы одна правка, по возрастанию.</summary>
    public IReadOnlyList<int> EditedFrames()
    {
        var indexes = new List<int>();
        foreach ((int index, PixelOverrides frame) in _frames)
        {
            if (frame.HasEdits)
            {
                indexes.Add(index);
            }
        }

        indexes.Sort();
        return indexes;
    }
}
