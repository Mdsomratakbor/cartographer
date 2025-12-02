namespace Cartographer.Core.Configuration;

/// <summary>
/// Global mapping options.
/// </summary>
public class MappingOptions
{
    /// <summary>
    /// Maximum mapping depth; when exceeded nested mapping yields null/default.
    /// </summary>
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Whether to preserve references when mapping object graphs.
    /// </summary>
    public bool PreserveReferences { get; set; }

    /// <summary>
    /// How null source collections are handled.
    /// </summary>
    public NullCollectionStrategy NullCollectionStrategy { get; set; } = NullCollectionStrategy.PreserveNull;
}
