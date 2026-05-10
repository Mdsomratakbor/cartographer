using System;
using System.Collections.Generic;
using Cartographer.Core.Diagnostics;

namespace Cartographer.Core.Abstractions;

public interface IMapper
{
    TDestination Map<TDestination>(object source);

    object Map(object source, Type sourceType, Type destinationType);

    TDestination Map<TDestination>(object source, TDestination destination);

    IReadOnlyList<TypeMapDescriptor> GetMappingPlans();

    MappingDiagnostics Diagnostics { get; }
}
