using AnonymousUserMvcApi.Models;
using AnonymousUserMvcApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousUserMvcApi.Controllers;

[ApiController, AllowAnonymous, Route("api/permissions")]
public sealed class PermissionsController(IAccessStore store) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> All() => Ok(await store.PermissionsAsync());
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id)
        => await store.PermissionAsync(id) is { } permission ? Ok(permission) : NotFound();
    [HttpPost] public async Task<IActionResult> Create([FromBody] PermissionInput input)
        => Respond(await store.SavePermissionAsync(null, input), true);
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] PermissionInput input)
        => Respond(await store.SavePermissionAsync(id, input), false);
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id)
        => Respond(await store.DeletePermissionAsync(id), false, true);

    private IActionResult Respond(SaveResult<Permission> result, bool created = false, bool deleted = false)
        => result.Status switch
        {
            SaveStatus.Success when created => CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value),
            SaveStatus.Success when deleted => NoContent(),
            SaveStatus.Success => NoContent(),
            SaveStatus.NotFound => NotFound(),
            SaveStatus.Invalid => BadRequest(new { error = result.Error }),
            SaveStatus.Conflict or SaveStatus.InUse => Conflict(new { error = result.Error }),
            _ => StatusCode(500)
        };
}
