using AnonymousUserMvcApi.Models;

namespace AnonymousUserMvcApi.Services;

public interface IUserStore
{
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<User?> GetAsync(Guid id);
    Task<User?> CreateAsync(UserInput input);
    Task<(bool Found, bool Duplicate)> UpdateAsync(Guid id, UserInput input);
    Task<bool> DeleteAsync(Guid id);
}