using Cartographer.Example.Api.Models;

namespace Cartographer.Example.Api.Services;

public class InMemoryCustomerService : ICustomerService
{
    private readonly List<Person> _people = new();

    public InMemoryCustomerService()
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

        if (existing is Customer existingCustomer && person is Customer updatedCustomer)
        {
            existingCustomer.LoyaltyLevel = updatedCustomer.LoyaltyLevel;
            existingCustomer.Address = updatedCustomer.Address;
        }

        if (existing is VipCustomer existingVip && person is VipCustomer updatedVip)
        {
            existingVip.AccountManager = updatedVip.AccountManager;
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
            LoyaltyLevel = "Gold",
            Address = new Address { Line1 = "123 Logic Way", City = "London", Country = "UK", PostalCode = "SW1A1AA" }
        });

        _people.Add(new VipCustomer
        {
            Id = Guid.NewGuid(),
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@example.com",
            LoyaltyLevel = "Platinum",
            AccountManager = "Manager One",
            Address = new Address { Line1 = "456 Compiler St", City = "Arlington", Country = "USA", PostalCode = "22203" }
        });
    }
}

public class InMemoryOrderService : IOrderService
{
    private readonly List<Order> _orders = new();
    private readonly List<Product> _products = new();

    public InMemoryOrderService()
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
        existing.Items = order.Items;
    }

    private void Seed()
    {
        var productA = new Product { Id = Guid.NewGuid(), Name = "Widget", Price = 25m };
        var productB = new Product { Id = Guid.NewGuid(), Name = "Gadget", Price = 15m };
        _products.AddRange(new[] { productA, productB });

        _orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = productA.Id, ProductName = productA.Name, Quantity = 2, UnitPrice = productA.Price },
                new OrderItem { ProductId = productB.Id, ProductName = productB.Name, Quantity = 1, UnitPrice = productB.Price }
            }
        });
    }
}
