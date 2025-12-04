using Cartographer.Core.Abstractions;
using Cartographer.Example.Net8Api.Models;
using Cartographer.Example.Net8Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cartographer.Example.Net8Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderBoard _orders;
    private readonly IMapper _mapper;

    public OrdersController(IOrderBoard orders, IMapper mapper)
    {
        _orders = orders;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<OrderDto>> GetAll()
    {
        var data = _orders.GetAll().Select(o => _mapper.Map<OrderDto>(o)).ToList();
        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<OrderDto> Get(Guid id)
    {
        var order = _orders.Get(id);
        if (order == null) return NotFound();
        return Ok(_mapper.Map<OrderDto>(order));
    }

    [HttpPost]
    public ActionResult<OrderDto> Create(OrderDto dto)
    {
        var order = _mapper.Map<Order>(dto);
        var created = _orders.Add(order);
        var createdDto = _mapper.Map<OrderDto>(created);
        return CreatedAtAction(nameof(Get), new { id = createdDto.Id }, createdDto);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, OrderDto dto)
    {
        var order = _mapper.Map<Order>(dto);
        _orders.Update(id, order);
        return NoContent();
    }
}
