using System;
using System.Collections.Generic;

namespace Cartographer.Core.Diagnostics;

public class MappingDiagnostics
{
    private readonly List<MappingDiagnosticEntry> _entries = new();
    private readonly object _lock = new();

    public bool Enabled { get; set; }

    public IReadOnlyList<MappingDiagnosticEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToArray();
            }
        }
    }

    public int TotalMappings
    {
        get
        {
            lock (_lock)
            {
                return _entries.Count;
            }
        }
    }

    public int FailedMappings
    {
        get
        {
            lock (_lock)
            {
                return _entries.Count(e => !e.IsSuccess);
            }
        }
    }

    public void Record(Type sourceType, Type destinationType, TimeSpan elapsed, bool isSuccess, string? errorMessage = null)
    {
        if (!Enabled) return;

        lock (_lock)
        {
            _entries.Add(new MappingDiagnosticEntry(sourceType, destinationType, elapsed, isSuccess, errorMessage));
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }
}
