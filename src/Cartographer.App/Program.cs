using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;

namespace Cartographer.App;

internal class Program
{
    private static void Main()
    {
        var mapper = BuildMapper();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com"
        };

        var dto = mapper.Map<UserDto>(user);

        Console.WriteLine($"Mapped: {dto.DisplayName} ({dto.Email})");
    }

    private static IMapper BuildMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            new UserProfile().Apply(cfg);
        });

        return config.CreateMapper();
    }
}

internal class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

internal class UserDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

internal class UserProfile : Profile
{
    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<User, UserDto>()
            .ForMember(d => d.DisplayName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"));
    }
}
