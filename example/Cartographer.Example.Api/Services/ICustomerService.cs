using Cartographer.Example.Api.Models;

namespace Cartographer.Example.Api.Services;

public interface ICustomerService
{
    IEnumerable<Person> GetAll();
    Person? Get(Guid id);
    Person Add(Person person);
    void Update(Guid id, Person person);
}

public interface IOrderService
{
    IEnumerable<Order> GetAll();
    Order? Get(Guid id);
    Order Add(Order order);
    void Update(Guid id, Order order);
}
