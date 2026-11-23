using AnonymousUserMvcApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AnonymousUserMvcApi.Controllers;
[ApiController,AllowAnonymous,Route("api/users/{userId:guid}/pictures")]
public sealed class ProfilePicturesController(IProfileStore profiles) : ControllerBase
{
 private const long MaxBytes=5*1024*1024;
 [HttpGet]public async Task<IActionResult> List(Guid userId)
  =>await profiles.PicturesAsync(userId) is { } pictures?Ok(pictures):NotFound();
 [HttpGet("{pictureId:guid}")]public async Task<IActionResult> Get(Guid userId,Guid pictureId)
  =>await profiles.PictureAsync(userId,pictureId) is { } p?Ok(p):NotFound();
 [HttpGet("{pictureId:guid}/file")]
 public async Task<IActionResult> FileContent(Guid userId,Guid pictureId)
 {
  var data=await profiles.ReadPictureAsync(userId,pictureId);
  if(data is null)return NotFound();
  Response.Headers["X-Content-Type-Options"]="nosniff";
  Response.Headers["Content-Security-Policy"]="default-src 'none'; sandbox";
  return File(data.Value.Bytes,data.Value.ContentType);
 }
 [HttpPost,RequestSizeLimit(MaxBytes+1024*64)]
 public async Task<IActionResult> Upload(Guid userId,IFormFile file)
 {
  if(file is null||file.Length==0||file.Length>MaxBytes)return BadRequest(new{error="Choose an image up to 5 MB"});
  // Verify actual file signatures rather than trusting MIME types or extensions.
  var head=new byte[12];
  await using var stream=file.OpenReadStream();
  var count=await stream.ReadAsync(head);
  stream.Position=0;
  var png=count>=8&&head.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10});
  var jpeg=count>=3&&head[0]==255&&head[1]==216&&head[2]==255;
  var gif=count>=6&&System.Text.Encoding.ASCII.GetString(head,0,6) is "GIF87a" or "GIF89a";
  var webp=count>=12&&System.Text.Encoding.ASCII.GetString(head,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(head,8,4)=="WEBP";
  if(!png&&!jpeg&&!gif&&!webp)return BadRequest(new{error="Only PNG, JPEG, GIF and WebP images are supported"});
  var type=png?"image/png":jpeg?"image/jpeg":gif?"image/gif":"image/webp";
  var ext=png?".png":jpeg?".jpg":gif?".gif":".webp";
  var picture=await profiles.AddPictureAsync(userId,stream,type,ext);
  return picture is null?NotFound():CreatedAtAction(nameof(Get),new{userId,pictureId=picture.Id},picture);
 }
 [HttpPut("{pictureId:guid}/avatar")]
 public async Task<IActionResult> SetAvatar(Guid userId,Guid pictureId)
  =>await profiles.SetAvatarAsync(userId,pictureId)?NoContent():NotFound();
 [HttpDelete("{pictureId:guid}")]
 public async Task<IActionResult> Delete(Guid userId,Guid pictureId)
  =>await profiles.DeletePictureAsync(userId,pictureId)?NoContent():NotFound();
}
