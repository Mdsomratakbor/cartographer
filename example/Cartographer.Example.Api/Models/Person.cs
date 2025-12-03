namespace Cartographer.Example.Api.Models;

public class Person
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Customer : Person
{
    public string LoyaltyLevel { get; set; } = "Standard";
    public Address? Address { get; set; }
}

public class VipCustomer : Customer
{
    public string AccountManager { get; set; } = string.Empty;
}

public class Address
{
    public string Line1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "USA";
    public string PostalCode { get; set; } = string.Empty;
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Quantity * i.UnitPrice);
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
