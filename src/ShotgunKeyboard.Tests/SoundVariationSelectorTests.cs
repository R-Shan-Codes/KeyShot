using ShotgunKeyboard.Core.Audio;

namespace ShotgunKeyboard.Tests;

public sealed class SoundVariationSelectorTests
{
    [Fact]
    public void DoesNotImmediatelyRepeatWhenMultipleVariationsExist()
    {
        var selector = new SoundVariationSelector(new Random(1234));
        var previous = selector.Select(5);

        for (var i = 0; i < 100; i++)
        {
            var current = selector.Select(5);
            Assert.NotEqual(previous, current);
            previous = current;
        }
    }

    [Fact]
    public void SingleVariationAlwaysReturnsZero()
    {
        var selector = new SoundVariationSelector(new Random(1234));

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(0, selector.Select(1));
        }
    }
}
