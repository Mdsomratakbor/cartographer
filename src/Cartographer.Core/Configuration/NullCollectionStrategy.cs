namespace Cartographer.Core.Configuration;

/// <summary>
/// Controls how null collections are handled during mapping.
/// </summary>
public enum NullCollectionStrategy
{
    /// <summary>
    /// Preserve null collection values.
    /// </summary>
    PreserveNull = 0,

    /// <summary>
    /// Substitute an empty collection when the source collection is null.
    /// </summary>
    UseEmptyCollection = 1
}
