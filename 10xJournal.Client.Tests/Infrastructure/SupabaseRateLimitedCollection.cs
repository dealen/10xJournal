namespace _10xJournal.Client.Tests.Infrastructure;

/// <summary>
/// Collection definition for tests that interact with Supabase and need to avoid rate limiting.
/// Tests in this collection will run sequentially rather than in parallel.
/// </summary>
[CollectionDefinition("SupabaseRateLimited", DisableParallelization = true)]
public class SupabaseRateLimitedCollection
{
    // This class is never instantiated.
    // It exists only to define the collection and its configuration.
}
