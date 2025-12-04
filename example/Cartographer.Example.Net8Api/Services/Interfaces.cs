using Cartographer.Example.Net8Api.Models;

namespace Cartographer.Example.Net8Api.Services;

public interface ICustomerDirectory
{
    IEnumerable<Person> GetAll();
    Person? Get(Guid id);
    Person Add(Person person);
    void Update(Guid id, Person person);
}

public interface IOrderBoard
{
    IEnumerable<Order> GetAll();
    Order? Get(Guid id);
    Order Add(Order order);
    void Update(Guid id, Order order);
}
