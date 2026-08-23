using ShotgunKeyboard.Core;
using Xunit;

namespace ShotgunKeyboard.Tests;

public class ProjectInfoTests
{
    [Fact]
    public void ProjectName_IsKeyShot()
    {
        Assert.Equal("KeyShot", ProjectInfo.Name);
    }
}
