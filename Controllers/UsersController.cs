using AnonymousUserMvcApi.Models;
using AnonymousUserMvcApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousUserMvcApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/users")]
public sealed class UsersController(IUserStore store, IAccessStore access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<User>>> GetAll()
        => Ok(await store.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<User>> Get(Guid id)
    {
        var user = await store.GetAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> Create([FromBody] UserInput input)
    {
        if (!await access.RoleExistsAsync(input.Role ?? ""))
            return BadRequest(new { error = "Role does not exist" });
        var user = await store.CreateAsync(input);
        return user is null
            ? Conflict(new { error = "Email already exists" })
            : CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserInput input)
    {
        if (!await access.RoleExistsAsync(input.Role ?? ""))
            return BadRequest(new { error = "Role does not exist" });
        var result = await store.UpdateAsync(id, input);
        if (!result.Found) return NotFound();
        if (result.Duplicate) return Conflict(new { error = "Email already exists" });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await store.DeleteAsync(id) ? NoContent() : NotFound();
}