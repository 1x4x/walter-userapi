using System.ComponentModel.DataAnnotations;

namespace AnonymousUserMvcApi.Models;

public sealed class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class PermissionInput
{
    [Required, RegularExpression(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$", ErrorMessage = "Use a dotted key like users.read")]
    [StringLength(100)] public string Key { get; set; } = "";
    [StringLength(300)] public string Description { get; set; } = "";
}

public sealed class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Guid> PermissionIds { get; set; } = new();
}

public sealed class RoleInput
{
    [Required, StringLength(50)] public string Name { get; set; } = "";
    [StringLength(300)] public string Description { get; set; } = "";
    public List<Guid> PermissionIds { get; set; } = new();
}

public enum SaveStatus { Success, NotFound, Conflict, Invalid, InUse }
public sealed record SaveResult<T>(SaveStatus Status, T? Value = default, string? Error = null);
