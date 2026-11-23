using System.Text.Json;
using AnonymousUserMvcApi.Models;
namespace AnonymousUserMvcApi.Services;
public sealed class JsonProfileStore : IProfileStore
{
 private readonly string _profiles, _pictures, _images;
 private readonly IUserStore _users;
 private readonly SemaphoreSlim _gate = new(1,1);
 private static readonly JsonSerializerOptions Options = new() { WriteIndented=true, PropertyNameCaseInsensitive=true };
 public JsonProfileStore(IWebHostEnvironment env, IUserStore users)
 {
  var root=Path.Combine(env.ContentRootPath,"Data");
  Directory.CreateDirectory(root);
  _profiles=Path.Combine(root,"profiles.json");
  _pictures=Path.Combine(root,"profile-pictures.json");
  _images=Path.Combine(root,"profile-images");
  Directory.CreateDirectory(_images);
  _users=users;
 }
 private static async Task<List<T>> ReadAsync<T>(string path)
 {
  if(!File.Exists(path))return new();
  await using var s=File.OpenRead(path);
  return await JsonSerializer.DeserializeAsync<List<T>>(s,Options) ?? new();
 }
 private static async Task WriteAsync<T>(string path,List<T> records)
 {
  var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try { await using(var s=File.Create(temp))await JsonSerializer.SerializeAsync(s,records,Options);File.Move(temp,path,true); }
  finally { if(File.Exists(temp))File.Delete(temp); }
 }
 public async Task<IReadOnlyList<UserProfile>> AllAsync()
 {
  await _gate.WaitAsync();
  try {return await ReadAsync<UserProfile>(_profiles);} finally {_gate.Release();}
 }
 public async Task<UserProfile?> GetAsync(Guid userId)
 {
  await _gate.WaitAsync();
  try {return (await ReadAsync<UserProfile>(_profiles)).FirstOrDefault(x=>x.UserId==userId);}
  finally {_gate.Release();}
 }
 public async Task<UserProfile?> SaveAsync(Guid userId,ProfileInput input)
 {
  if(await _users.GetAsync(userId) is null)return null;
  await _gate.WaitAsync();
  try {
   var list=await ReadAsync<UserProfile>(_profiles);
   var profile=list.FirstOrDefault(x=>x.UserId==userId);
   if(profile is null){profile=new UserProfile{UserId=userId};list.Add(profile);}
   profile.DisplayName=input.DisplayName?.Trim()??"";
   profile.Bio=input.Bio?.Trim()??"";
   profile.Location=input.Location?.Trim()??"";
   profile.Website=input.Website?.Trim()??"";
   await WriteAsync(_profiles,list);return profile;
  } finally {_gate.Release();}
 }
 public async Task<bool> DeleteAsync(Guid userId)
 {
  await _gate.WaitAsync();
  try {
   var list=await ReadAsync<UserProfile>(_profiles);
   if(list.RemoveAll(x=>x.UserId==userId)==0)return false;
   await WriteAsync(_profiles,list);return true;
  }finally{_gate.Release();}
 }
 public async Task<IReadOnlyList<ProfilePicture>?> PicturesAsync(Guid userId)
 {
  if(await _users.GetAsync(userId) is null)return null;
  await _gate.WaitAsync();
  try {return (await ReadAsync<ProfilePicture>(_pictures)).Where(x=>x.UserId==userId).OrderByDescending(x=>x.UploadedUtc).ToList();}
  finally{_gate.Release();}
 }
 public async Task<ProfilePicture?> PictureAsync(Guid userId,Guid pictureId)
 {
  await _gate.WaitAsync();
  try {return (await ReadAsync<ProfilePicture>(_pictures)).FirstOrDefault(x=>x.UserId==userId&&x.Id==pictureId);}
  finally{_gate.Release();}
 }
 public async Task<ProfilePicture?> AddPictureAsync(Guid userId,Stream input,string contentType,string extension)
 {
  if(await _users.GetAsync(userId) is null)return null;
  await _gate.WaitAsync();
  try {
   var pictures=await ReadAsync<ProfilePicture>(_pictures);
   var profileList=await ReadAsync<UserProfile>(_profiles);
   var profile=profileList.FirstOrDefault(x=>x.UserId==userId);
   if(profile is null){profile=new UserProfile{UserId=userId};profileList.Add(profile);}
   var picture=new ProfilePicture{UserId=userId,ContentType=contentType};
   picture.FileName=picture.Id.ToString("N")+extension;
   var path=Path.Combine(_images,picture.FileName);
   try {
    await using(var output=File.Create(path))await input.CopyToAsync(output);
    picture.Size=new FileInfo(path).Length;
    pictures.Add(picture);
    if(profile.AvatarPictureId is null)profile.AvatarPictureId=picture.Id;
    await WriteAsync(_pictures,pictures);
    await WriteAsync(_profiles,profileList);
    return picture;
   }catch{if(File.Exists(path))File.Delete(path);throw;}
  }finally{_gate.Release();}
 }
 public async Task<bool> DeletePictureAsync(Guid userId,Guid pictureId)
 {
  await _gate.WaitAsync();
  try {
   var list=await ReadAsync<ProfilePicture>(_pictures);
   var picture=list.FirstOrDefault(x=>x.UserId==userId&&x.Id==pictureId);
   if(picture is null)return false;
   list.Remove(picture);
   var profiles=await ReadAsync<UserProfile>(_profiles);
   var profile=profiles.FirstOrDefault(x=>x.UserId==userId);
   if(profile?.AvatarPictureId==pictureId)
    profile.AvatarPictureId=list.Where(x=>x.UserId==userId).OrderByDescending(x=>x.UploadedUtc).Select(x=>(Guid?)x.Id).FirstOrDefault();
   await WriteAsync(_pictures,list);
   await WriteAsync(_profiles,profiles);
   var path=Path.Combine(_images,picture.FileName);
   if(File.Exists(path))File.Delete(path);
   return true;
  }finally{_gate.Release();}
 }
 public async Task<bool> SetAvatarAsync(Guid userId,Guid pictureId)
 {
  await _gate.WaitAsync();
  try {
   if(!(await ReadAsync<ProfilePicture>(_pictures)).Any(x=>x.UserId==userId&&x.Id==pictureId))return false;
   var profiles=await ReadAsync<UserProfile>(_profiles);
   var profile=profiles.FirstOrDefault(x=>x.UserId==userId);
   if(profile is null){profile=new UserProfile{UserId=userId};profiles.Add(profile);}
   profile.AvatarPictureId=pictureId;
   await WriteAsync(_profiles,profiles);return true;
  }finally{_gate.Release();}
 }
 public async Task<(byte[] Bytes,string ContentType)?> ReadPictureAsync(Guid userId,Guid pictureId)
 {
  await _gate.WaitAsync();
  try {
   var p=(await ReadAsync<ProfilePicture>(_pictures)).FirstOrDefault(x=>x.UserId==userId&&x.Id==pictureId);
   if(p is null)return null;
   var path=Path.Combine(_images,p.FileName);
   if(!File.Exists(path))return null;
   return (await File.ReadAllBytesAsync(path),p.ContentType);
  }finally{_gate.Release();}
 }
}
