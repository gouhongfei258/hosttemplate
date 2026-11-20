using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Configuration.Store;

namespace HostTemplate.Core.Security
{
    /// <summary>
    /// 账号校验。密码用 PBKDF2-SHA256（10 万次迭代）加盐派生，比对走恒定时间。
    ///
    /// 目前默认不启用（AppConfig.EnableLogin = false），此服务与 LoginForm 已就绪，
    /// 需要时把开关打开即可，业务代码无需改动。
    /// </summary>
    public sealed class AuthService
    {
        public const int Iterations = 100_000;
        public const int SaltBytes = 16;
        public const int HashBytes = 32;

        public const string DefaultAdminName = "admin";
        public const string DefaultAdminPassword = "admin123";

        private readonly string _filePath;
        private UserConfig _config = new UserConfig();

        public AuthService(string usersFilePath)
        {
            _filePath = usersFilePath ?? throw new ArgumentNullException(nameof(usersFilePath));
        }

        public string FilePath => _filePath;

        public IReadOnlyList<UserAccount> Users => _config.Users;

        /// <summary>
        /// 读取 users.xml。文件缺失时生成默认管理员（admin / admin123）并落盘。
        /// </summary>
        public bool Load(out string message)
        {
            message = string.Empty;

            if (!XmlConfigStore.Load(_filePath, out UserConfig loaded, out _) || loaded?.Users == null || loaded.Users.Count == 0)
            {
                _config = new UserConfig();
                var admin = new UserAccount
                {
                    UserName = DefaultAdminName,
                    DisplayName = "超级管理员",
                    Role = UserRole.管理员,
                    Enabled = true,
                    MustChangePassword = true,
                };

                SetPassword(admin, DefaultAdminPassword);
                _config.Users.Add(admin);

                if (!Save(out var error))
                {
                    message = $"生成默认账号失败：{error}";
                    return false;
                }

                message = $"已生成默认账号 {DefaultAdminName} / {DefaultAdminPassword}，请登录后立即修改密码。";
                return true;
            }

            _config = loaded;
            return true;
        }

        public bool Save(out string error)
        {
            return XmlConfigStore.Save(_filePath, _config, out error);
        }

        public UserAccount Find(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            foreach (var user in _config.Users)
            {
                if (string.Equals(user.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return user;
                }
            }

            return null;
        }

        /// <summary>
        /// 校验账号密码。失败统一返回「账号或密码错误」，不区分账号是否存在。
        /// </summary>
        public bool Verify(string userName, string password, out UserSession session, out string error)
        {
            session = null;
            error = string.Empty;

            var user = Find(userName);
            if (user == null)
            {
                error = "账号或密码错误。";
                return false;
            }

            if (!user.Enabled)
            {
                error = "该账号已停用，请联系管理员。";
                return false;
            }

            if (!VerifyPassword(password ?? string.Empty, user.Salt, user.PasswordHash))
            {
                error = "账号或密码错误。";
                return false;
            }

            user.LastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            session = new UserSession
            {
                UserName = user.UserName,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName,
                Role = user.Role,
                IsAuthenticated = true,
                LoginTime = DateTime.Now,
            };

            return true;
        }

        /// <summary>
        /// 修改密码。旧密码不对或新密码过短都返回 false。
        /// </summary>
        public bool ChangePassword(string userName, string oldPassword, string newPassword, out string error)
        {
            error = string.Empty;

            var user = Find(userName);
            if (user == null)
            {
                error = "账号不存在。";
                return false;
            }

            if (!VerifyPassword(oldPassword ?? string.Empty, user.Salt, user.PasswordHash))
            {
                error = "原密码不正确。";
                return false;
            }

            if (!IsAcceptable(newPassword, out error))
            {
                return false;
            }

            SetPassword(user, newPassword);
            user.MustChangePassword = false;

            return Save(out error);
        }

        /// <summary>
        /// 新增或更新账号（管理员用）。password 为空表示不修改密码。
        /// </summary>
        public bool Upsert(string userName, string displayName, UserRole role, string password, out string error)
        {
            return Upsert(userName, displayName, role, password, true, out error);
        }

        /// <summary>
        /// 新增或更新账号。enabled 为 false 时会校验「不能停用最后一个管理员」。
        /// </summary>
        public bool Upsert(string userName, string displayName, UserRole role, string password, bool enabled, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(userName))
            {
                error = "账号不能为空。";
                return false;
            }

            var user = Find(userName);
            var isNew = user == null;

            if (isNew)
            {
                if (!IsAcceptable(password, out error))
                {
                    return false;
                }

                user = new UserAccount { UserName = userName.Trim() };
                _config.Users.Add(user);
            }
            else if (IsLastEnabledAdmin(user) && (!enabled || role != UserRole.管理员))
            {
                error = "这是最后一个启用的管理员，不能停用或降级。";
                return false;
            }

            user.DisplayName = displayName ?? string.Empty;
            user.Role = role;
            user.Enabled = enabled;

            if (!string.IsNullOrEmpty(password))
            {
                SetPassword(user, password);
                user.MustChangePassword = isNew;
            }

            return Save(out error);
        }

        /// <summary>删除账号。最后一个启用的管理员不允许删除。</summary>
        public bool Remove(string userName, out string error)
        {
            error = string.Empty;

            var user = Find(userName);
            if (user == null)
            {
                error = "账号不存在。";
                return false;
            }

            if (IsLastEnabledAdmin(user))
            {
                error = "这是最后一个启用的管理员，不能删除。";
                return false;
            }

            _config.Users.Remove(user);
            return Save(out error);
        }

        /// <summary>该账号是否为「最后一个启用的管理员」。用于防止把系统锁死。</summary>
        public bool IsLastEnabledAdmin(UserAccount user)
        {
            if (user == null || !user.Enabled || user.Role != UserRole.管理员)
            {
                return false;
            }

            var count = 0;
            foreach (var item in _config.Users)
            {
                if (item.Enabled && item.Role == UserRole.管理员)
                {
                    count++;
                }
            }

            return count <= 1;
        }

        public bool SetEnabled(string userName, bool enabled, out string error)
        {
            error = string.Empty;

            var user = Find(userName);
            if (user == null)
            {
                error = "账号不存在。";
                return false;
            }

            if (!enabled && IsLastEnabledAdmin(user))
            {
                error = "这是最后一个启用的管理员，不能停用。";
                return false;
            }

            user.Enabled = enabled;
            return Save(out error);
        }

        public static bool IsAcceptable(string password, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                error = "密码长度至少 6 位。";
                return false;
            }

            return true;
        }

        public static void SetPassword(UserAccount user, string password)
        {
            if (user == null)
            {
                return;
            }

            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            user.Salt = Convert.ToBase64String(salt);
            user.PasswordHash = Convert.ToBase64String(Derive(password, salt));
        }

        public static bool VerifyPassword(string password, string saltBase64, string hashBase64)
        {
            if (string.IsNullOrEmpty(saltBase64) || string.IsNullOrEmpty(hashBase64))
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(saltBase64);
                var expected = Convert.FromBase64String(hashBase64);
                var actual = Derive(password, salt);

                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] Derive(string password, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                password ?? string.Empty,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashBytes);
        }
    }
}
