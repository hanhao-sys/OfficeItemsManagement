using System.Text.Json;
using OfficeItemsManagement.Common.DTOs;

namespace OfficeItemsManagement.Common.Services;

/// <summary>
/// JSON 文件用户存储服务
/// 实验要求：用户信息存在文件中（JSON）
/// 
/// 提供对普通用户的 CRUD 操作：
/// - 管理员通过桌面端维护用户
/// - Web 端登录时验证用户名密码
///
/// 文件不存在时自动创建默认用户（user01, user02）
/// </summary>
public class JsonUserService
{
    /// <summary>JSON 文件路径</summary>
    private readonly string _filePath;

    /// <summary>内存中的用户列表缓存</summary>
    private List<AppUserDto> _users;

    /// <summary>
    /// 构造函数 — 指定 JSON 文件路径并加载数据
    /// </summary>
    /// <param name="filePath">Users.json 的完整路径</param>
    public JsonUserService(string filePath)
    {
        _filePath = filePath;
        _users = LoadUsers();
    }

    /// <summary>获取所有用户列表</summary>
    public List<AppUserDto> GetAllUsers() => _users;

    /// <summary>按用户 ID 查找单个用户，不存在返回 null</summary>
    public AppUserDto? GetUserById(string userId) =>
        _users.FirstOrDefault(u => u.UserId == userId);

    /// <summary>验证用户名密码是否匹配</summary>
    /// <returns>true=验证通过, false=用户名或密码错误</returns>
    public bool ValidateUser(string userId, string password) =>
        _users.Any(u => u.UserId == userId && u.Password == password);

    /// <summary>新增用户，ID 重复时抛出异常</summary>
    public void AddUser(AppUserDto user)
    {
        if (_users.Any(u => u.UserId == user.UserId))
            throw new InvalidOperationException($"用户ID '{user.UserId}' 已存在。");
        _users.Add(user);
        SaveUsers(); // 立即持久化到文件
    }

    /// <summary>更新用户信息，ID 不存在时抛出异常</summary>
    public void UpdateUser(AppUserDto user)
    {
        var existing = _users.FirstOrDefault(u => u.UserId == user.UserId)
            ?? throw new InvalidOperationException($"用户ID '{user.UserId}' 不存在。");

        // 逐字段更新（保留原有 UserId）
        existing.Password = user.Password;
        existing.Name = user.Name;
        existing.Gender = user.Gender;
        existing.BirthDate = user.BirthDate;
        existing.Phone = user.Phone;
        SaveUsers();
    }

    /// <summary>删除用户，ID 不存在时抛出异常</summary>
    public void DeleteUser(string userId)
    {
        var user = _users.FirstOrDefault(u => u.UserId == userId)
            ?? throw new InvalidOperationException($"用户ID '{userId}' 不存在。");
        _users.Remove(user);
        SaveUsers();
    }

    /// <summary>
    /// 从 JSON 文件加载用户列表
    /// 文件不存在时返回默认用户（user01, user02）
    /// </summary>
    private List<AppUserDto> LoadUsers()
    {
        if (!File.Exists(_filePath))
        {
            // 首次运行时创建默认用户
            return new List<AppUserDto>
            {
                new()
                {
                    UserId = "user01", Password = "1234", Name = "张三", Gender = "男",
                    BirthDate = new DateTime(1990, 5, 15), Phone = "13800138001"
                },
                new()
                {
                    UserId = "user02", Password = "1234", Name = "李四", Gender = "女",
                    BirthDate = new DateTime(1992, 8, 20), Phone = "13800138002"
                }
            };
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<AppUserDto>>(json) ?? new List<AppUserDto>();
    }

    /// <summary>
    /// 将用户列表序列化写回 JSON 文件
    /// 使用 Unicode 不转义（中文可读）+ 缩进美化
    /// </summary>
    private void SaveUsers()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(_users, options);
        File.WriteAllText(_filePath, json);
    }
}
