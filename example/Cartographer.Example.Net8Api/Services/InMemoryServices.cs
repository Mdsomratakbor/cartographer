using Cartographer.Example.Net8Api.Models;

namespace Cartographer.Example.Net8Api.Services;

public class InMemoryCustomerDirectory : ICustomerDirectory
{
    private readonly List<Person> _people = new();

    public InMemoryCustomerDirectory()
    {
        Seed();
    }

    public IEnumerable<Person> GetAll() => _people;

    public Person? Get(Guid id) => _people.FirstOrDefault(p => p.Id == id);

    public Person Add(Person person)
    {
        person.Id = Guid.NewGuid();
        _people.Add(person);
        return person;
    }

    public void Update(Guid id, Person person)
    {
        var existing = Get(id);
        if (existing == null) return;

        existing.FirstName = person.FirstName;
        existing.LastName = person.LastName;
        existing.Email = person.Email;

        if (existing is Customer ec && person is Customer pc)
        {
            ec.LoyaltyTier = pc.LoyaltyTier;
            ec.Address = pc.Address;
        }
    }

    private void Seed()
    {
        _people.Add(new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            LoyaltyTier = "Gold",
            Address = new Address { Line1 = "123 Logic Way", City = "London", Country = "UK", PostalCode = "SW1A1AA" }
        });

        _people.Add(new Staff
        {
            Id = Guid.NewGuid(),
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@example.com",
            Role = "Support"
        });
    }
}

public class InMemoryOrderBoard : IOrderBoard
{
    private readonly List<Order> _orders = new();

    public InMemoryOrderBoard()
    {
        Seed();
    }

    public IEnumerable<Order> GetAll() => _orders;

    public Order? Get(Guid id) => _orders.FirstOrDefault(o => o.Id == id);

    public Order Add(Order order)
    {
        order.Id = Guid.NewGuid();
        _orders.Add(order);
        return order;
    }

    public void Update(Guid id, Order order)
    {
        var existing = Get(id);
        if (existing == null) return;
        existing.CustomerId = order.CustomerId;
        existing.Lines = order.Lines;
    }

    private void Seed()
    {
        _orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Lines = new List<OrderLine>
            {
                new OrderLine { ProductId = Guid.NewGuid(), ProductName = "Widget", Quantity = 2, UnitPrice = 25m },
                new OrderLine { ProductId = Guid.NewGuid(), ProductName = "Gadget", Quantity = 1, UnitPrice = 15m }
            }
        });
    }
}
