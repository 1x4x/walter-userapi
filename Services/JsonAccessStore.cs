using System.Text.Json;
using AnonymousUserMvcApi.Models;

namespace AnonymousUserMvcApi.Services;

public sealed class JsonAccessStore : IAccessStore
{
    private readonly string _rolesPath, _permissionsPath;
    private readonly IUserStore _users;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public JsonAccessStore(IWebHostEnvironment env, IUserStore users)
    {
        var directory = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(directory);
        _rolesPath = Path.Combine(directory, "roles.json");
        _permissionsPath = Path.Combine(directory, "permissions.json");
        _users = users;
    }

    private static async Task<List<T>> ReadAsync<T>(string path)
    {
        if (!File.Exists(path)) return new();
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, Options) ?? new();
    }

    private static async Task WriteAsync<T>(string path, List<T> records)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = File.Create(temp))
                await JsonSerializer.SerializeAsync(stream, records, Options);
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private async Task<List<Role>> ReadRolesAsync()
    {
        var roles = await ReadAsync<Role>(_rolesPath);
        if (roles.Count == 0 && !File.Exists(_rolesPath))
            return new() { new Role { Name = "User", Description = "Default user role" }, new Role { Name = "Admin", Description = "Administrative role (metadata only)" } };
        return roles;
    }

    public async Task<IReadOnlyList<Role>> RolesAsync()
    {
        await _gate.WaitAsync();
        try { return (await ReadRolesAsync()).OrderBy(r => r.Name).ToList(); }
        finally { _gate.Release(); }
    }

    public async Task<Role?> RoleAsync(Guid id)
    {
        await _gate.WaitAsync();
        try { return (await ReadRolesAsync()).FirstOrDefault(r => r.Id == id); }
        finally { _gate.Release(); }
    }

    public async Task<bool> RoleExistsAsync(string name)
    {
        await _gate.WaitAsync();
        try { return (await ReadRolesAsync()).Any(r => r.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)); }
        finally { _gate.Release(); }
    }

    public async Task<SaveResult<Role>> SaveRoleAsync(Guid? id, RoleInput input)
    {
        await _gate.WaitAsync();
        try
        {
            var roles = await ReadRolesAsync();
            var name = input.Name.Trim();
            if (name.Length == 0) return new(SaveStatus.Invalid, Error: "Role name is required");
            var role = id.HasValue ? roles.FirstOrDefault(r => r.Id == id) : null;
            if (id.HasValue && role is null) return new(SaveStatus.NotFound);
            if (roles.Any(r => r.Id != id && r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return new(SaveStatus.Conflict, Error: "Role name already exists");
            var ids = (input.PermissionIds ?? new()).Distinct().ToList();
            var permissions = await ReadAsync<Permission>(_permissionsPath);
            if (ids.Any(pid => permissions.All(p => p.Id != pid)))
                return new(SaveStatus.Invalid, Error: "One or more permissions do not exist");
            if (role is not null && !role.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                (await _users.GetAllAsync()).Any(u => u.Role.Equals(role.Name, StringComparison.OrdinalIgnoreCase)))
                return new(SaveStatus.InUse, Error: "Cannot rename a role assigned to users");
            role ??= new Role();
            role.Name = name;
            role.Description = input.Description?.Trim() ?? "";
            role.PermissionIds = ids;
            if (!id.HasValue) roles.Add(role);
            await WriteAsync(_rolesPath, roles);
            return new(SaveStatus.Success, role);
        }
        finally { _gate.Release(); }
    }

    public async Task<SaveResult<Role>> DeleteRoleAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            var roles = await ReadRolesAsync();
            var role = roles.FirstOrDefault(r => r.Id == id);
            if (role is null) return new(SaveStatus.NotFound);
            if ((await _users.GetAllAsync()).Any(u => u.Role.Equals(role.Name, StringComparison.OrdinalIgnoreCase)))
                return new(SaveStatus.InUse, Error: "Role is assigned to users");
            roles.Remove(role);
            await WriteAsync(_rolesPath, roles);
            return new(SaveStatus.Success, role);
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<Permission>> PermissionsAsync()
    {
        await _gate.WaitAsync();
        try { return (await ReadAsync<Permission>(_permissionsPath)).OrderBy(p => p.Key).ToList(); }
        finally { _gate.Release(); }
    }

    public async Task<Permission?> PermissionAsync(Guid id)
    {
        await _gate.WaitAsync();
        try { return (await ReadAsync<Permission>(_permissionsPath)).FirstOrDefault(p => p.Id == id); }
        finally { _gate.Release(); }
    }

    public async Task<SaveResult<Permission>> SavePermissionAsync(Guid? id, PermissionInput input)
    {
        await _gate.WaitAsync();
        try
        {
            var permissions = await ReadAsync<Permission>(_permissionsPath);
            var key = input.Key.Trim().ToLowerInvariant();
            var permission = id.HasValue ? permissions.FirstOrDefault(p => p.Id == id) : null;
            if (id.HasValue && permission is null) return new(SaveStatus.NotFound);
            if (permissions.Any(p => p.Id != id && p.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                return new(SaveStatus.Conflict, Error: "Permission key already exists");
            permission ??= new Permission();
            permission.Key = key;
            permission.Description = input.Description?.Trim() ?? "";
            if (!id.HasValue) permissions.Add(permission);
            await WriteAsync(_permissionsPath, permissions);
            return new(SaveStatus.Success, permission);
        }
        finally { _gate.Release(); }
    }

    public async Task<SaveResult<Permission>> DeletePermissionAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            var permissions = await ReadAsync<Permission>(_permissionsPath);
            var permission = permissions.FirstOrDefault(p => p.Id == id);
            if (permission is null) return new(SaveStatus.NotFound);
            var roles = await ReadRolesAsync();
            foreach (var role in roles) role.PermissionIds.RemoveAll(pid => pid == id);
            permissions.Remove(permission);
            await WriteAsync(_rolesPath, roles);
            await WriteAsync(_permissionsPath, permissions);
            return new(SaveStatus.Success, permission);
        }
        finally { _gate.Release(); }
    }
}
