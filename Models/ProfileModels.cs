using System.ComponentModel.DataAnnotations;
namespace AnonymousUserMvcApi.Models;
public sealed class UserProfile
{
 public Guid UserId { get; set; }
 public string DisplayName { get; set; } = "";
 public string Bio { get; set; } = "";
 public string Location { get; set; } = "";
 public string Website { get; set; } = "";
 public Guid? AvatarPictureId { get; set; }
}
public sealed class ProfileInput
{
 [StringLength(80)] public string DisplayName { get; set; } = "";
 [StringLength(1000)] public string Bio { get; set; } = "";
 [StringLength(100)] public string Location { get; set; } = "";
 [StringLength(250), Url] public string? Website { get; set; }
}
public sealed class ProfilePicture
{
 public Guid Id { get; set; } = Guid.NewGuid();
 public Guid UserId { get; set; }
 public string FileName { get; set; } = "";
 public string ContentType { get; set; } = "";
 public long Size { get; set; }
 public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
 public string Url => $"/api/users/{UserId}/pictures/{Id}/file";
}
