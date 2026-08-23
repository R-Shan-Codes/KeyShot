using Xunit;
using ShotgunKeyboard.Core.Input;

namespace ShotgunKeyboard.Tests;

public sealed class GlobalKeyEventArgsTests
{
    [Fact]
    public void StoresVirtualKeyCode()
    {
        var args = new GlobalKeyEventArgs(0x41);

        Assert.Equal(0x41u, args.VirtualKeyCode);
    }
}
