using System;

namespace Cartographer.Core.Diagnostics;

public class MappingDiagnosticEntry
{
    public MappingDiagnosticEntry(Type sourceType, Type destinationType, TimeSpan elapsed, bool isSuccess, string? errorMessage)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
        Elapsed = elapsed;
        Timestamp = DateTime.UtcNow;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public Type SourceType { get; }
    public Type DestinationType { get; }
    public TimeSpan Elapsed { get; }
    public DateTime Timestamp { get; }
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
}
