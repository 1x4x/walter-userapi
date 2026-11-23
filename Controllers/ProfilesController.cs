using AnonymousUserMvcApi.Models;
using AnonymousUserMvcApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AnonymousUserMvcApi.Controllers;
[ApiController,AllowAnonymous,Route("api/users")]
public sealed class ProfilesController(IProfileStore profiles) : ControllerBase
{
 [HttpGet("/api/profiles")]
 public async Task<IActionResult> All()=>Ok(await profiles.AllAsync());
 [HttpGet("{userId:guid}/profile")]
 public async Task<IActionResult> Get(Guid userId)=>await profiles.GetAsync(userId) is { } p?Ok(p):NotFound();
 [HttpPut("{userId:guid}/profile")]
 public async Task<IActionResult> Upsert(Guid userId,[FromBody]ProfileInput input)
 {
  var existing=await profiles.GetAsync(userId);
  var p=await profiles.SaveAsync(userId,input);
  return p is null?NotFound():existing is null?CreatedAtAction(nameof(Get),new{userId},p):Ok(p);
 }
 [HttpDelete("{userId:guid}/profile")]
 public async Task<IActionResult> Delete(Guid userId)=>await profiles.DeleteAsync(userId)?NoContent():NotFound();
}
