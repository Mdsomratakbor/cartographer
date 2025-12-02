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
            .ReverseMap();

        cfg.CreateMap<Address, AddressDto>()
            .ReverseMap();

        cfg.CreateMap<Order, OrderDto>()
            .ReverseMap();
    }
}
