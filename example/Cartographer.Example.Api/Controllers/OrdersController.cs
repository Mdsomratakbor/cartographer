using Cartographer.Core.Abstractions;
using Cartographer.Example.Api.Models;
using Cartographer.Example.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cartographer.Example.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly IMapper _mapper;

    public OrdersController(IOrderService orders, IMapper mapper)
    {
        _orders = orders;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<OrderDto>> GetAll()
    {
        var data = _orders.GetAll();
        var dtos = data.Select(o => _mapper.Map<OrderDto>(o)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<OrderDto> GetById(Guid id)
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
        return CreatedAtAction(nameof(GetById), new { id = createdDto.Id }, createdDto);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, OrderDto dto)
    {
        var order = _mapper.Map<Order>(dto);
        _orders.Update(id, order);
        return NoContent();
    }
}
