using AutoPvpSeriesGrind.Core.Tasks;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class ChatCommandBlockTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\n  \r\n")]
    public void BlankBlockHasNoLines(string commandBlock)
    {
        Assert.Empty(ChatCommandBlock.Lines(commandBlock));
    }

    [Fact]
    public void LinesAreTrimmedAndBlankOnesDropped()
    {
        var lines = ChatCommandBlock.Lines("  /li auto  \r\n\r\n/ays m\n   \n\t/echo done\t");
        Assert.Equal(["/li auto", "/ays m", "/echo done"], lines);
    }

    [Fact]
    public void LineOrderIsKept()
    {
        var lines = ChatCommandBlock.Lines("/second\n/first\n/third");
        Assert.Equal(["/second", "/first", "/third"], lines);
    }

    [Theory]
    [InlineData("/li home", true)]
    [InlineData("/ays m", true)]
    [InlineData("li home", false)]
    [InlineData("hello everyone", false)]
    [InlineData("\\li home", false)]
    public void OnlySlashLinesAreCommands(string line, bool expected)
    {
        Assert.Equal(expected, ChatCommandBlock.IsCommand(line));
    }

    [Fact]
    public void PlainChatLineIsKeptSoItCanBeReportedAsSkipped()
    {
        var lines = ChatCommandBlock.Lines("say hi\n/li auto");
        Assert.Equal(2, lines.Length);
        Assert.False(ChatCommandBlock.IsCommand(lines[0]));
        Assert.True(ChatCommandBlock.IsCommand(lines[1]));
    }
}
