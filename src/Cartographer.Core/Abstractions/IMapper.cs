using System;

namespace Cartographer.Core.Abstractions;

public interface IMapper
{
    TDestination Map<TDestination>(object source);
    object Map(object source, Type sourceType, Type destinationType);
}
