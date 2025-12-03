namespace Cartographer.Example.Api.Models;

public class PersonDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CustomerDto : PersonDto
{
    public string LoyaltyLevel { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
}

public class VipCustomerDto : CustomerDto
{
    public string AccountManager { get; set; } = string.Empty;
}

public class AddressDto
{
    public string Line1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
