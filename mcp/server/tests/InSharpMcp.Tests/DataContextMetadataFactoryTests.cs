using InSharpMcp.Contracts;

namespace InSharpMcp.Tests;

public sealed class DataContextMetadataFactoryTests
{
    [Fact]
    public void Create_ReturnsPrimitiveAndStringPropertiesOnly()
    {
        var metadata = DataContextMetadataFactory.Create(new SampleViewModel(), new ToolLimits());

        Assert.Contains(nameof(SampleViewModel.Title), metadata.Properties.Keys);
        Assert.Contains(nameof(SampleViewModel.Count), metadata.Properties.Keys);
        Assert.DoesNotContain(nameof(SampleViewModel.Child), metadata.Properties.Keys);
    }

    [Fact]
    public void Create_RedactsSensitivePropertyNames()
    {
        var metadata = DataContextMetadataFactory.Create(new SampleViewModel(), new ToolLimits());

        Assert.Equal("<redacted>", metadata.Properties[nameof(SampleViewModel.ApiToken)]);
    }

    [Fact]
    public void Create_TruncatesLongStringsAndReportsTruncation()
    {
        var metadata = DataContextMetadataFactory.Create(
            new SampleViewModel { Title = "abcdef" },
            new ToolLimits { MaxTextCharacters = 3 });

        Assert.Equal("abc", metadata.Properties[nameof(SampleViewModel.Title)]);
        Assert.True(metadata.Truncated);
    }

    [Fact]
    public void Create_StopsAtPropertyLimitAndReportsTruncation()
    {
        var metadata = DataContextMetadataFactory.Create(new SampleViewModel(), new ToolLimits { MaxNodes = 1 });

        Assert.Single(metadata.Properties);
        Assert.True(metadata.Truncated);
    }

    private sealed class SampleViewModel
    {
        public string Title { get; init; } = "Sample";

        public int Count { get; init; } = 3;

        public string ApiToken { get; init; } = "secret-token";

        public object Child { get; } = new();
    }
}
