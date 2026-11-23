using System.ComponentModel.DataAnnotations;

namespace AnonymousUserMvcApi.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [StringLength(50)] public string Role { get; set; } = "User";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class UserInput
{
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [StringLength(50)] public string Role { get; set; } = "User";
}