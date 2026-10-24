using FluentAssertions;

namespace Requests.Tests;

public class PlaceholderTests
{
    [Fact]
    public void Placeholder_Passes()
    {
        true.Should().BeTrue();
    }
}
