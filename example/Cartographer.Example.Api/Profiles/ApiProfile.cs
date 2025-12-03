using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Example.Api.Models;

namespace Cartographer.Example.Api.Profiles;

public class ApiProfile : Profile
{
    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<Person, PersonDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .Include<Customer, CustomerDto>()
            .Include<VipCustomer, VipCustomerDto>();

        cfg.CreateMap<Customer, CustomerDto>()
            .Include<VipCustomer, VipCustomerDto>();

        cfg.CreateMap<VipCustomer, VipCustomerDto>();

        cfg.CreateMap<Address, AddressDto>()
            .ReverseMap();

        cfg.CreateMap<Product, ProductDto>();

        cfg.CreateMap<OrderItem, OrderItemDto>()
            .ReverseMap();

        cfg.CreateMap<Order, OrderDto>()
            .ReverseMap();
    }
}
