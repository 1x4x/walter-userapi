using AnonymousUserMvcApi.Models;

namespace AnonymousUserMvcApi.Services;

public interface IAccessStore
{
    Task<IReadOnlyList<Role>> RolesAsync();
    Task<Role?> RoleAsync(Guid id);
    Task<SaveResult<Role>> SaveRoleAsync(Guid? id, RoleInput input);
    Task<SaveResult<Role>> DeleteRoleAsync(Guid id);
    Task<IReadOnlyList<Permission>> PermissionsAsync();
    Task<Permission?> PermissionAsync(Guid id);
    Task<SaveResult<Permission>> SavePermissionAsync(Guid? id, PermissionInput input);
    Task<SaveResult<Permission>> DeletePermissionAsync(Guid id);
    Task<bool> RoleExistsAsync(string name);
}
