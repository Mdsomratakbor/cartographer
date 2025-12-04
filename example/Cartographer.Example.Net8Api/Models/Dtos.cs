namespace Cartographer.Example.Net8Api.Models;

public class PersonDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CustomerDto : PersonDto
{
    public string LoyaltyTier { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
}

public class StaffDto : PersonDto
{
    public string Role { get; set; } = string.Empty;
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
    public List<OrderLineDto> Lines { get; set; } = new();
    public decimal Total { get; set; }
}

public class OrderLineDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
