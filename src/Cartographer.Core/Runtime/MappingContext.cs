using System.Collections.Generic;
using Cartographer.Core.Configuration;

namespace Cartographer.Core.Runtime;

/// <summary>
/// Runtime context used during mapping to track depth and references.
/// </summary>
public class MappingContext
{
    public MappingContext(MappingOptions options)
    {
        Options = options;
    }

    public MappingOptions Options { get; }

    public int Depth { get; private set; }

    private Dictionary<object, object>? _references;

    public IDisposable Push()
    {
        Depth++;
        return new DepthScope(this);
    }

    public bool TryGetReference(object source, out object? destination)
    {
        destination = null;
        if (!Options.PreserveReferences)
        {
            return false;
        }

        if (_references != null && _references.TryGetValue(source, out destination))
        {
            return true;
        }

        return false;
    }

    public void Track(object source, object destination)
    {
        if (!Options.PreserveReferences)
        {
            return;
        }

        _references ??= new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        _references[source] = destination;
    }

    private void Pop()
    {
        Depth--;
    }

    private sealed class DepthScope : IDisposable
    {
        private readonly MappingContext _context;
        private bool _disposed;

        public DepthScope(MappingContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _context.Pop();
            _disposed = true;
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object?>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
