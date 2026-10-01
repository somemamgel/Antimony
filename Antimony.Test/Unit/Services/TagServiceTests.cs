using System.Globalization;
using Antimony.Core.Services;

namespace Antimony.Test.Unit.Services;

[Trait("Category", "Unit")]
public class TagServiceTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData(" \t\r\n ", "")]
    [InlineData(" .net ", ".NET")]
    [InlineData(" 编程 ", "编程")]
    [InlineData(" ASP.NET Core ", "ASP.NET CORE")]
    public void Normalize_TrimsAndUppercasesWithoutRemovingInternalSpaces(string input, string expected)
    {
        Assert.Equal(expected, TagService.Normalize(input));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void Normalize_UsesInvariantCasingRegardlessOfCurrentCulture(string culture)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

            Assert.Equal("INDIGO", TagService.Normalize(" indigo "));
            Assert.Equal(new[] { "indigo" }, TagService.Parse("indigo,INDIGO"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",，")]
    [InlineData(" , ， , ")]
    public void Parse_EmptyInput_ReturnsNoTags(string? input)
    {
        Assert.Empty(TagService.Parse(input));
    }

    [Theory]
    [InlineData("编程,生活,.NET")]
    [InlineData("编程，生活，.NET")]
    [InlineData(" 编程, 生活，.NET,， ")]
    public void Parse_ChineseAndEnglishSeparators_ReturnsTrimmedTags(string input)
    {
        Assert.Equal(new[] { "编程", "生活", ".NET" }, TagService.Parse(input));
    }

    [Fact]
    public void Parse_DuplicateTags_PreservesFirstSpellingAndOrder()
    {
        var tags = TagService.Parse(".NET,编程,.net,生活, 编程 , .Net");

        Assert.Equal(new[] { ".NET", "编程", "生活" }, tags);
    }

    [Fact]
    public void Parse_MultiWordTag_PreservesSpacesInsideName()
    {
        Assert.Equal(new[] { "ASP.NET Core", "读书 笔记" },
            TagService.Parse(" ASP.NET Core , 读书 笔记 "));
    }
}
