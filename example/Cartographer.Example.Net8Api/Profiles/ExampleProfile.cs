using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Example.Net8Api.Models;

namespace Cartographer.Example.Net8Api.Profiles;

public class ExampleProfile : Profile
{
    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<Person, PersonDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .Include<Customer, CustomerDto>()
            .Include<Staff, StaffDto>();

        cfg.CreateMap<Customer, CustomerDto>();
        cfg.CreateMap<Staff, StaffDto>();

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
