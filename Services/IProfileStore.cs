using AnonymousUserMvcApi.Models;
namespace AnonymousUserMvcApi.Services;
public interface IProfileStore
{
 Task<IReadOnlyList<UserProfile>> AllAsync();
 Task<UserProfile?> GetAsync(Guid userId);
 Task<UserProfile?> SaveAsync(Guid userId, ProfileInput input);
 Task<bool> DeleteAsync(Guid userId);
 Task<IReadOnlyList<ProfilePicture>?> PicturesAsync(Guid userId);
 Task<ProfilePicture?> PictureAsync(Guid userId, Guid pictureId);
 Task<ProfilePicture?> AddPictureAsync(Guid userId, Stream input, string contentType, string extension);
 Task<bool> DeletePictureAsync(Guid userId, Guid pictureId);
 Task<bool> SetAvatarAsync(Guid userId, Guid pictureId);
 Task<(byte[] Bytes, string ContentType)?> ReadPictureAsync(Guid userId, Guid pictureId);
}
