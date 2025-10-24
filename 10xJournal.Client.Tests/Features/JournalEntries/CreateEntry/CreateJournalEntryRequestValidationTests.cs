using _10xJournal.Client.Features.JournalEntries.CreateEntry;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace _10xJournal.Client.Tests.Features.JournalEntries.CreateEntry;

/// <summary>
/// Unit tests for CreateJournalEntryRequest validation.
/// Tests the [Required] attribute behavior with various edge cases.
/// </summary>
public class CreateJournalEntryRequestValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
        return validationResults;
    }

    [Fact]
    public void Validation_WithValidContent_Passes()
    {
        var request = new CreateJournalEntryRequest { Content = "Valid journal entry content" };
        
        var results = ValidateModel(request);
        
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validation_WithNullOrEmpty_Fails()
    {
        var nullRequest = new CreateJournalEntryRequest { Content = null! };
        var emptyRequest = new CreateJournalEntryRequest { Content = "" };
        
        var nullResults = ValidateModel(nullRequest);
        var emptyResults = ValidateModel(emptyRequest);
        
        nullResults.Should().ContainSingle().Which.ErrorMessage.Should().Be("Content is required.");
        emptyResults.Should().ContainSingle().Which.ErrorMessage.Should().Be("Content is required.");
    }

    [Fact]
    public void Validation_WithWhitespaceOnly_Fails()
    {
        // [Required] attribute validates whitespace as empty in .NET
        var request = new CreateJournalEntryRequest { Content = "   " };
        
        var results = ValidateModel(request);
        
        results.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Content is required.");
    }

    [Fact]
    public void Constructor_InitializesContentAsEmptyString()
    {
        var request = new CreateJournalEntryRequest();
        
        request.Content.Should().NotBeNull().And.BeEmpty();
        ValidateModel(request).Should().NotBeEmpty("default empty string should fail validation");
    }
}
