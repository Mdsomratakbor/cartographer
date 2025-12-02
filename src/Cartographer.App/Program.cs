using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cartographer.App;

internal class Program
{
    private static void Main()
    {
        using var provider = BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        var configuration = provider.GetRequiredService<MapperConfiguration>();
       // configuration.AssertConfigurationIsValid();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Address = new Address { Line1 = "123 Logic Way", City = "London" },
            Orders = new[]
            {
                new Order { Sku = "ABC-001", Quantity = 2 },
                new Order { Sku = "XYZ-999", Quantity = 1 }
            }
        };

        var dto = mapper.Map<UserDto>(user);

        Console.WriteLine($"Mapped: {dto.DisplayName} ({dto.Email})");
        Console.WriteLine($"Address: {dto.Address?.Line1}, {dto.Address?.City}");
        Console.WriteLine($"Orders: {string.Join(", ", dto.Orders.Select(o => $"{o.Sku} x{o.Quantity}"))}");
        Console.WriteLine($"Conditional (PreCondition): {dto.OptionalNote ?? "<skipped>"}");
        Console.WriteLine($"Conditional (Condition): {dto.OptionalNote2 ?? "<skipped>"}");
        Console.WriteLine($"Hooks: Before={dto.BeforeHookCalled}, After={dto.AfterHookCalled}");
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Registers Cartographer using profile scanning in this assembly
        services.AddCartographer(typeof(UserProfile).Assembly);

        return services.BuildServiceProvider();
    }
}

internal class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public IEnumerable<Order> Orders { get; set; } = Array.Empty<Order>();
    public bool IncludeNote { get; set; } = true;
}

internal class Address
{
    public string Line1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

internal class Order
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

internal class UserDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
    public List<OrderDto> Orders { get; set; } = new();
    public string? OptionalNote { get; set; }
    public string? OptionalNote2 { get; set; }
    public bool BeforeHookCalled { get; set; }
    public bool AfterHookCalled { get; set; }
}

internal class AddressDto
{
    public string Line1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

internal class OrderDto
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

internal class UserProfile : Profile
{
    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<User, UserDto>()
            .ForMember(d => d.DisplayName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.OptionalNote, o =>
            {
                o.PreCondition(s => s.IncludeNote);
                o.MapFrom(s => $"PreCondition note for {s.FirstName}");
            })
            .ForMember(d => d.OptionalNote2, o =>
            {
                o.Condition(s => s.IncludeNote);
                o.MapFrom(s => $"Condition note for {s.LastName}");
            })
            .BeforeMap((s, d) => d.BeforeHookCalled = true)
            .AfterMap((s, d) => d.AfterHookCalled = true)
            .ReverseMap();

        cfg.CreateMap<Address, AddressDto>()
            .ReverseMap();

        cfg.CreateMap<Order, OrderDto>()
            .ReverseMap();
    }
}
