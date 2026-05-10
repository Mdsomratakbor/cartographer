using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Example.Net8Api.Models;
using Cartographer.Example.Net8Api.Services;

namespace Cartographer.Example.Net8Api.Profiles;

public class ExampleProfile : Profile
{
    private readonly IFullNameFormatter _fullNameFormatter;

    public ExampleProfile(IFullNameFormatter fullNameFormatter)
    {
        _fullNameFormatter = fullNameFormatter;
    }

    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<Person, PersonDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => _fullNameFormatter.Format(s.FirstName, s.LastName)))
            .Include<Customer, CustomerDto>()
            .Include<Staff, StaffDto>();

        cfg.CreateMap<Customer, CustomerDto>()
            .IncludeBase<Person, PersonDto>()
            .ForMember(d => d.ClientCode, o => o.MapFrom(s => s.CustomerCode))
            .ReverseMap();

        cfg.CreateMap<Staff, StaffDto>()
            .IncludeBase<Person, PersonDto>()
            .ReverseMap();

        cfg.CreateMap<Address, AddressDto>()
            .ReverseMap();

        cfg.CreateMap<Product, ProductDto>()
            .ReverseMap();

        cfg.CreateMap<OrderLine, OrderLineDto>()
            .ReverseMap();

        cfg.CreateMap<Order, OrderDto>()
            .ReverseMap();
    }
}
