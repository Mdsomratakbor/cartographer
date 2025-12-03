using Cartographer.Core.Abstractions;
using Cartographer.Example.Api.Models;
using Cartographer.Example.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cartographer.Example.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;
    private readonly IMapper _mapper;

    public CustomersController(ICustomerService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PersonDto>> GetAll()
    {
        var people = _service.GetAll();
        var dtos = people.Select(p => _mapper.Map<PersonDto>(p)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PersonDto> GetById(Guid id)
    {
        var person = _service.Get(id);
        if (person == null) return NotFound();
        return Ok(_mapper.Map<PersonDto>(person));
    }

    [HttpPost]
    public ActionResult<PersonDto> Create(CustomerDto dto)
    {
        var person = dto.LoyaltyLevel == "VIP"
            ? _mapper.Map<VipCustomer>(dto)
            : _mapper.Map<Customer>(dto);

        var created = _service.Add(person);
        var createdDto = _mapper.Map<PersonDto>(created);
        return CreatedAtAction(nameof(GetById), new { id = createdDto.Id }, createdDto);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, CustomerDto dto)
    {
        var person = _mapper.Map<Customer>(dto);
        _service.Update(id, person);
        return NoContent();
    }
}
