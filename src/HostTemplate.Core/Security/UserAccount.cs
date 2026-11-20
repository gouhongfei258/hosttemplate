using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace HostTemplate.Core.Security
{
    /// <summary>
    /// 权限等级。数值越大权限越高，HasRole 用 &gt;= 判断，新增角色只需插在合适位置。
    /// </summary>
    public enum UserRole
    {
        操作员 = 0,
        工程师 = 1,
        管理员 = 2,
    }

    /// <summary>
    /// 账号。只存 PBKDF2 派生值 + 盐，不存明文密码。
    /// </summary>
    public sealed class UserAccount
    {
        [XmlAttribute]
        public string UserName { get; set; } = string.Empty;

        [XmlAttribute]
        public string DisplayName { get; set; } = string.Empty;

        [XmlAttribute]
        public UserRole Role { get; set; } = UserRole.操作员;

        [XmlAttribute]
        public bool Enabled { get; set; } = true;

        /// <summary>首次登录后强制改密。默认账号置 true。</summary>
        [XmlAttribute]
        public bool MustChangePassword { get; set; }

        [XmlAttribute]
        public string Salt { get; set; } = string.Empty;

        [XmlAttribute]
        public string PasswordHash { get; set; } = string.Empty;

        [XmlIgnore]
        public string LastLogin { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{UserName}（{Role}）";
        }
    }

    /// <summary>
    /// users.xml 的根节点。
    /// </summary>
    [XmlRoot("Users")]
    public sealed class UserConfig
    {
        [XmlElement("User")]
        public List<UserAccount> Users { get; set; } = new List<UserAccount>();
    }

    /// <summary>
    /// 登录后的会话。UI 与业务用它判断权限，不直接读配置文件。
    /// </summary>
    public sealed class UserSession
    {
        /// <summary>未启用登录时使用的匿名会话，等同管理员，保证功能不被拦。</summary>
        public static UserSession Anonymous { get; } = new UserSession
        {
            UserName = "anonymous",
            DisplayName = "未登录",
            Role = UserRole.管理员,
            IsAuthenticated = false,
        };

        public string UserName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.操作员;

        public bool IsAuthenticated { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public bool HasRole(UserRole required)
        {
            return Role >= required;
        }
    }
}
