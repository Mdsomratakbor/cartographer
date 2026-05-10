using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Example.Api.Models;
using Cartographer.Example.Api.Services;

namespace Cartographer.Example.Api.Profiles;

public class ApiProfile : Profile
{
    private readonly IFullNameFormatter _fullNameFormatter;

    public ApiProfile(IFullNameFormatter fullNameFormatter)
    {
        _fullNameFormatter = fullNameFormatter;
    }

    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<Person, PersonDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => _fullNameFormatter.Format(s.FirstName, s.LastName)))
            .Include<Customer, CustomerDto>()
            .Include<VipCustomer, VipCustomerDto>();

        cfg.CreateMap<Customer, CustomerDto>()
            .IncludeBase<Person, PersonDto>()
            .ForMember(d => d.ClientCode, o => o.MapFrom(s => s.CustomerCode))
            .Include<VipCustomer, VipCustomerDto>()
            .ReverseMap();

        cfg.CreateMap<VipCustomer, VipCustomerDto>()
            .IncludeBase<Customer, CustomerDto>()
            .ReverseMap();

        cfg.CreateMap<Address, AddressDto>()
            .ReverseMap();

        cfg.CreateMap<Product, ProductDto>();

        cfg.CreateMap<OrderItem, OrderItemDto>()
            .ReverseMap();

        cfg.CreateMap<Order, OrderDto>()
            .ReverseMap();
    }
}
