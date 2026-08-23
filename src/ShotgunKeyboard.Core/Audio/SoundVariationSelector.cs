namespace ShotgunKeyboard.Core.Audio;

public sealed class SoundVariationSelector
{
    private readonly Random _random;
    private int _lastIndex = -1;

    public SoundVariationSelector(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public int Select(int variationCount)
    {
        if (variationCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(variationCount));
        }

        if (variationCount == 1)
        {
            _lastIndex = 0;
            return 0;
        }

        var index = _random.Next(variationCount - 1);
        if (index >= _lastIndex)
        {
            index++;
        }

        _lastIndex = index;
        return index;
    }
}
