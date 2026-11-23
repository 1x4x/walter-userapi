using System.Text.Json;
using AnonymousUserMvcApi.Models;

namespace AnonymousUserMvcApi.Services;

public sealed class JsonUserStore : IUserStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonUserStore(IWebHostEnvironment env)
    {
        var directory = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "users.json");
    }

    private async Task<List<User>> ReadAsync()
    {
        if (!File.Exists(_path)) return new();
        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<List<User>>(stream, Options) ?? new();
    }

    private async Task WriteAsync(List<User> users)
    {
        var temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = File.Create(temp))
                await JsonSerializer.SerializeAsync(stream, users, Options);
            File.Move(temp, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try { return (await ReadAsync()).OrderBy(x => x.Name).ToList(); }
        finally { _gate.Release(); }
    }

    public async Task<User?> GetAsync(Guid id)
    {
        await _gate.WaitAsync();
        try { return (await ReadAsync()).FirstOrDefault(x => x.Id == id); }
        finally { _gate.Release(); }
    }

    public async Task<User?> CreateAsync(UserInput input)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAsync();
            var email = input.Email.Trim();
            if (users.Any(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                return null;
            var user = new User
            {
                Name = input.Name.Trim(), Email = email,
                Role = input.Role?.Trim() ?? "User"
            };
            users.Add(user);
            await WriteAsync(users);
            return user;
        }
        finally { _gate.Release(); }
    }

    public async Task<(bool Found, bool Duplicate)> UpdateAsync(Guid id, UserInput input)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAsync();
            var user = users.FirstOrDefault(x => x.Id == id);
            if (user is null) return (false, false);
            var email = input.Email.Trim();
            if (users.Any(x => x.Id != id && x.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                return (true, true);
            user.Name = input.Name.Trim();
            user.Email = email;
            user.Role = input.Role?.Trim() ?? "User";
            await WriteAsync(users);
            return (true, false);
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAsync();
            if (users.RemoveAll(x => x.Id == id) == 0) return false;
            await WriteAsync(users);
            return true;
        }
        finally { _gate.Release(); }
    }
}