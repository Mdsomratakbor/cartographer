using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Core.Configuration.Attributes;
using Cartographer.Core.Configuration.Converters;
using Cartographer.Core.Configuration.Naming;
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
            Friend = CreateFriendGraph(),
            BestFriend = CreateFriendGraph(),
            Address = new Address { Line1 = "123 Logic Way", City = "London" },
            postal_code = "90210",
            Orders = null
        };

        var dto = mapper.Map<UserDto>(user);
        BaseUser baseRef = new AdminUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Chief",
            LastName = "Architect",
            Email = "chief@example.com",
            Title = "CTO"
        };
        var adminDto = mapper.Map<BaseUserDto>(baseRef);

        Console.WriteLine($"Mapped: {dto.DisplayName} ({dto.Email})");
        Console.WriteLine($"Address: {dto.Address?.Line1}, {dto.Address?.City}");
        Console.WriteLine($"Orders: {(dto.Orders.Any() ? string.Join(", ", dto.Orders.Select(o => $"{o.Sku} x{o.Quantity}")) : "<empty>")}");
        Console.WriteLine($"Conditional (PreCondition): {dto.OptionalNote ?? "<skipped>"}");
        Console.WriteLine($"Conditional (Condition): {dto.OptionalNote2 ?? "<skipped>"}");
        Console.WriteLine($"Hooks: Before={dto.BeforeHookCalled}, After={dto.AfterHookCalled}");
        Console.WriteLine($"Friend: {dto.Friend?.DisplayName} (Friend.Friend null? {dto.Friend?.Friend is null})");
        Console.WriteLine($"PreserveReferences (BestFriend == Friend): {ReferenceEquals(dto.BestFriend, dto.Friend)}");
        Console.WriteLine($"Inheritance: adminDto runtime type = {adminDto.GetType().Name}, Title={(adminDto as AdminUserDto)?.Title}");

        // Demonstrate mapping into an existing instance (patch/update scenario)
        var existingDto = new UserDto { DisplayName = "Existing Value" };
        mapper.Map(user, existingDto);
        Console.WriteLine($"Existing instance updated: {existingDto.DisplayName} ({existingDto.Email}) ({existingDto.Address?.Line1}) PostalCode={existingDto.PostalCode}");
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Registers Cartographer with naming conventions, global options, and profile scanning in this assembly
        services.AddCartographer(cfg =>
        {
            cfg.SourceNamingConvention = new SnakeCaseNamingConvention();
            cfg.DestinationNamingConvention = new PascalCaseNamingConvention();
            cfg.MaxDepth = 3;
            cfg.PreserveReferences = true;
            cfg.NullCollectionStrategy = NullCollectionStrategy.UseEmptyCollection;
            new UserProfile().Apply(cfg);
        });

        return services.BuildServiceProvider();
    }

    private static User CreateFriendGraph()
    {
        var friend = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@example.com"
        };

        // This deeper friend will be trimmed by MaxDepth
        friend.Friend = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Deep",
            LastName = "Friend",
            Email = "deep@example.com"
        };

        return friend;
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
    public string postal_code { get; set; } = string.Empty;
    public string AgeText { get; set; } = "37";
    public string IgnoredFromAttribute { get; set; } = "ShouldNotMap";
    public User? Friend { get; set; }
    public User? BestFriend { get; set; }
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
    [MapFrom("Email")]
    public string Email { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
    public List<OrderDto> Orders { get; set; } = new();
    public string? OptionalNote { get; set; }
    public string? OptionalNote2 { get; set; }
    public bool BeforeHookCalled { get; set; }
    public bool AfterHookCalled { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public int Age { get; set; }
    [IgnoreMap]
    public string IgnoredFromAttribute { get; set; } = string.Empty;
    public UserDto? Friend { get; set; }
    public UserDto? BestFriend { get; set; }
}

internal class BaseUser : User
{
}

internal class AdminUser : BaseUser
{
    public string Title { get; set; } = string.Empty;
}

internal class BaseUserDto : UserDto
{
}

internal class AdminUserDto : BaseUserDto
{
    public string Title { get; set; } = string.Empty;
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
            .ForMember(d => d.Age, o => o.ConvertUsing(new StringToIntConverter(), s => s.AgeText))
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

        cfg.CreateMap<BaseUser, BaseUserDto>()
            .Include<AdminUser, AdminUserDto>();

        cfg.CreateMap<AdminUser, AdminUserDto>();

        cfg.CreateMap<Address, AddressDto>()
            .ReverseMap();

        cfg.CreateMap<Order, OrderDto>()
            .ConvertUsing(new OrderTypeConverter())
            .ReverseMap();
    }
}

internal class StringToIntConverter : IValueConverter<string, int>
{
    public int Convert(string sourceMember) => int.TryParse(sourceMember, out var n) ? n : 0;
}

internal class OrderTypeConverter : ITypeConverter<Order, OrderDto>
{
    public OrderDto Convert(Order source) => new OrderDto
    {
        Sku = source.Sku.ToUpperInvariant(),
        Quantity = source.Quantity
    };
}
