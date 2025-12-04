using Cartographer.Core.Abstractions;
using Cartographer.Example.Net8Api.Models;
using Cartographer.Example.Net8Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cartographer.Example.Net8Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerDirectory _directory;
    private readonly IMapper _mapper;

    public CustomersController(ICustomerDirectory directory, IMapper mapper)
    {
        _directory = directory;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PersonDto>> GetAll()
    {
        var people = _directory.GetAll()
            .Select(p => _mapper.Map<PersonDto>(p))
            .ToList();

        return Ok(people);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PersonDto> Get(Guid id)
    {
        var person = _directory.Get(id);
        if (person == null) return NotFound();
        return Ok(_mapper.Map<PersonDto>(person));
    }

    [HttpPost]
    public ActionResult<PersonDto> Create(CustomerDto dto)
    {
        var person = _mapper.Map<Customer>(dto);
        var created = _directory.Add(person);
        var createdDto = _mapper.Map<PersonDto>(created);
        return CreatedAtAction(nameof(Get), new { id = createdDto.Id }, createdDto);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, CustomerDto dto)
    {
        var person = _mapper.Map<Customer>(dto);
        _directory.Update(id, person);
        return NoContent();
    }
}
