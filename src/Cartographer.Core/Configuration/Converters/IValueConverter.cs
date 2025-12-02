namespace Cartographer.Core.Configuration.Converters;

/// <summary>
/// Converts a source member value to a destination member value.
/// </summary>
/// <typeparam name="TSourceMember">Source member type.</typeparam>
/// <typeparam name="TDestinationMember">Destination member type.</typeparam>
public interface IValueConverter<in TSourceMember, out TDestinationMember>
{
    TDestinationMember Convert(TSourceMember sourceMember);
}

/// <summary>
/// Converts a source object to a destination object, bypassing member-by-member mapping.
/// </summary>
public interface ITypeConverter<in TSource, out TDestination>
{
    TDestination Convert(TSource source);
}
