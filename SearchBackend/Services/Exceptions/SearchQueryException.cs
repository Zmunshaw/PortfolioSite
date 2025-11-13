using System.Text.RegularExpressions;

namespace SearchBackend.Services.Exceptions;

public enum SearchQueryExceptionType
{
    Unknown,
    EmptyQuery,
    InvalidQuery
}

/// <summary>
/// Thrown when a search query fails validation (empty, contains invalid characters, etc).
/// </summary>
public class SearchQueryException : Exception
{
    public string Query { get; }
    public DateTime OccurredAt { get; }
    public SearchQueryExceptionType ExceptionType { get; }
    
    private static readonly Regex ValidQueryPattern = 
        new(@"^[a-zA-Z0-9\s\-\.\,\!\?\']+$", RegexOptions.Compiled);

    public SearchQueryException(string message, string query, 
        SearchQueryExceptionType type = SearchQueryExceptionType.Unknown) : base(message)
    {
        Query = query;
        ExceptionType = type;
        OccurredAt = DateTime.Now;
    }

    public SearchQueryException(string message, string query, Exception innerException, 
        SearchQueryExceptionType type = SearchQueryExceptionType.Unknown) : base(message, innerException)
    {
        Query = query;
        ExceptionType = type;
        OccurredAt = DateTime.Now;
    }
    
    /// <summary>
    /// Validates that a search query is not empty and contains only valid characters.
    /// </summary>
    /// <param name="query">The query to validate</param>
    /// <param name="type">Optional exception type to throw. Defaults based on validation failure.</param>
    /// <exception cref="SearchQueryException">Thrown if query is invalid e.g, Non-English characters
    /// or Null/Empty query.</exception>
    public static void ThrowIfInvalid(string? query, SearchQueryExceptionType? type = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new SearchQueryException(
                "Search query cannot be empty or whitespace!", 
                query ?? string.Empty, 
                type ?? SearchQueryExceptionType.EmptyQuery);
        
        if (!ValidQueryPattern.IsMatch(query))
            throw new SearchQueryException(
                "Search query contains invalid characters.", 
                query, 
                type ?? SearchQueryExceptionType.InvalidQuery);
    }
}