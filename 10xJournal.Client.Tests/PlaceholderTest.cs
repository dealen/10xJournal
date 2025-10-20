using _10xJournal.Client.Features.JournalEntries.Models;
using _10xJournal.Client.Features.JournalEntries.Services;
using Microsoft.Extensions.DependencyInjection;

namespace _10xJournal.Client.Tests;

/// <summary>
/// This is a placeholder test class.
/// bUnit tests should inherit from TestContext.
/// </summary>
public class PlaceholderTest : TestContext
{
    [Fact]
    public void ListEntries_ShouldRenderCorrectly_WhenDataIsLoaded()
    {
        // Arrange
        var journalDataServiceMock = new Mock<IJournalDataService>();

        var journalEntries = new List<JournalEntry>
        {
            new JournalEntry { Id = Guid.NewGuid(), Content = "Content 1", CreatedAt = DateTime.Now },
            new JournalEntry { Id = Guid.NewGuid(), Content = "Content 2", CreatedAt = DateTime.Now.AddDays(-1) }
        };

        var userStreak = new UserStreak { UserId = Guid.NewGuid(), CurrentStreak = 5, LastEntryDate = DateTime.Now.Date };

        journalDataServiceMock.Setup(s => s.GetEntriesAsync()).ReturnsAsync(journalEntries);
        journalDataServiceMock.Setup(s => s.GetStreakAsync()).ReturnsAsync(userStreak);

        Services.AddSingleton<IJournalDataService>(journalDataServiceMock.Object);

        // Act
        var cut = RenderComponent<ListEntries>();

        // Assert
        var h1Element = cut.Find("h1");
        h1Element.MarkupMatches("<h1>Mój dziennik</h1>");

        var entriesList = cut.FindAll(".entries-list article");
        entriesList.Count.Should().Be(2);

        var streakIndicator = cut.Find(".streak-indicator");
        streakIndicator.TextContent.Should().Contain("5 dni z rzędu");
    }
}
