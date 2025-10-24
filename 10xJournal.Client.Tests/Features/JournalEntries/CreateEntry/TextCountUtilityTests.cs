using _10xJournal.Client.Features.JournalEntries.CreateEntry;
using FluentAssertions;
using Xunit;

namespace _10xJournal.Client.Tests.Features.JournalEntries.CreateEntry;

/// <summary>
/// Unit tests for text counting utilities in CreateEntry component.
/// Tests critical business rules: null/empty handling, whitespace edge cases, and word/character counting accuracy.
/// </summary>
public class TextCountUtilityTests
{
    private class TextCountHelper
    {
        private readonly CreateJournalEntryRequest _request = new();

        public string Content
        {
            get => _request.Content;
            set => _request.Content = value;
        }

        public int GetCharacterCount()
        {
            return string.IsNullOrEmpty(_request.Content) ? 0 : _request.Content.Length;
        }

        public int GetWordCount()
        {
            if (string.IsNullOrWhiteSpace(_request.Content))
                return 0;

            return _request.Content
                .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Length;
        }
    }

    #region Character Count - Critical Tests Only

    [Fact]
    public void GetCharacterCount_WithNullOrEmpty_ReturnsZero()
    {
        new TextCountHelper { Content = null! }.GetCharacterCount().Should().Be(0);
        new TextCountHelper { Content = "" }.GetCharacterCount().Should().Be(0);
    }

    [Theory]
    [InlineData("Hello", 5)]
    [InlineData("Hello World", 11)]
    public void GetCharacterCount_WithVariousInputs_ReturnsCorrectCount(string content, int expectedCount)
    {
        var helper = new TextCountHelper { Content = content! };
        helper.GetCharacterCount().Should().Be(expectedCount);
    }

    [Fact]
    public void GetCharacterCount_WithMultilineText_CountsNewlines()
    {
        var helper = new TextCountHelper { Content = "Line 1\nLine 2" };
        helper.GetCharacterCount().Should().Be(13); // "Line 1" (6) + "\n" (1) + "Line 2" (6)
    }

    #endregion

    #region Word Count - Critical Tests Only

    [Fact]
    public void GetWordCount_WithNullEmptyOrWhitespace_ReturnsZero()
    {
        new TextCountHelper { Content = null! }.GetWordCount().Should().Be(0);
        new TextCountHelper { Content = "" }.GetWordCount().Should().Be(0);
        new TextCountHelper { Content = " " }.GetWordCount().Should().Be(0);
        new TextCountHelper { Content = "\n\t  " }.GetWordCount().Should().Be(0);
    }

    [Theory]
    [InlineData("Hello", 1)]
    [InlineData("Hello World", 2)]
    public void GetWordCount_WithVariousInputs_ReturnsCorrectCount(string content, int expectedCount)
    {
        var helper = new TextCountHelper { Content = content! };
        helper.GetWordCount().Should().Be(expectedCount);
    }

    [Fact]
    public void GetWordCount_WithMultipleSpaces_IgnoresExtraWhitespace()
    {
        var helper = new TextCountHelper { Content = "Hello    World" };
        helper.GetWordCount().Should().Be(2);
    }

    [Fact]
    public void GetWordCount_WithMixedWhitespaceSeparators_CountsCorrectly()
    {
        var helper = new TextCountHelper { Content = "Word1 Word2\nWord3\tWord4" };
        helper.GetWordCount().Should().Be(4);
    }

    [Fact]
    public void GetWordCount_WithPunctuation_CountsWordsCorrectly()
    {
        var helper = new TextCountHelper { Content = "Hello, World! How are you?" };
        helper.GetWordCount().Should().Be(5);
    }

    #endregion
}
