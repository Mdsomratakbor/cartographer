namespace Cartographer.Example.Net8Api.Services;

public interface IFullNameFormatter
{
    string Format(string firstName, string lastName);
}

public sealed class FullNameFormatter : IFullNameFormatter
{
    public string Format(string firstName, string lastName) => $"{firstName} {lastName}";
}
