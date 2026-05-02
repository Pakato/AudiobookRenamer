using AudioBookManager.Core;
using FluentAssertions;

namespace AudioBookManager.Core.Tests.Util;

public class StringHelperTests
{
    [Fact]
    public void ToSafeFileName_ColonSpace_BecomesDash()
    {
        "Hyperion: The Fall".ToSafeFileName().Should().Be("Hyperion - The Fall");
    }

    [Theory]
    [InlineData("a<b", "a_b")]
    [InlineData("a>b", "a_b")]
    [InlineData("a:b", "a_b")]
    [InlineData("a\"b", "a_b")]
    [InlineData("a/b", "a_b")]
    [InlineData("a\\b", "a_b")]
    [InlineData("a|b", "a_b")]
    [InlineData("a?b", "a_b")]
    [InlineData("a*b", "a_b")]
    public void ToSafeFileName_ReplacesWindowsInvalidChars(string input, string expected)
    {
        input.ToSafeFileName().Should().Be(expected);
    }

    [Fact]
    public void ToSafeFileName_TrimsTrailingDotsAndSpaces()
    {
        "Book Title.  ".ToSafeFileName().Should().Be("Book Title");
    }

    [Fact]
    public void ToSafeFileName_ReservedDeviceName_IsPrefixed()
    {
        "CON".ToSafeFileName().Should().Be("_CON");
        "nul.txt".ToSafeFileName().Should().Be("_nul.txt");
    }

    [Fact]
    public void ToSafeFileName_NullOrEmpty_ReturnsUnderscore()
    {
        ((string)null!).ToSafeFileName().Should().Be("_");
        string.Empty.ToSafeFileName().Should().Be("_");
    }

    [Fact]
    public void ToSafeFileName_StringOfOnlyInvalidChars_TrimsToUnderscore()
    {
        "...".ToSafeFileName().Should().Be("_");
    }

    [Fact]
    public void ToSafeFileName_PreservesValidUnicode()
    {
        "Récit de l'année".ToSafeFileName().Should().Be("Récit de l'année");
    }
}
