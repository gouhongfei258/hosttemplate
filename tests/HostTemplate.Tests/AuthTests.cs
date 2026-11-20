using System;
using System.IO;
using HostTemplate.Core.Security;
using Xunit;

namespace HostTemplate.Tests
{
    /// <summary>
    /// 登录鉴权测试。功能默认不启用，但服务层必须可用，
    /// 否则现场一打开开关就出问题。
    /// </summary>
    public class AuthTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly string _usersPath;

        public AuthTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "HostTemplateTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            _usersPath = Path.Combine(_tempDirectory, "users.xml");
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // 清理失败不影响测试结论。
            }
        }

        private AuthService CreateService()
        {
            var service = new AuthService(_usersPath);
            service.Load(out _);
            return service;
        }

        [Fact]
        public void Load_ShouldCreateDefaultAdmin()
        {
            var service = CreateService();

            Assert.True(File.Exists(_usersPath));
            Assert.Single(service.Users);

            var admin = service.Find(AuthService.DefaultAdminName);
            Assert.NotNull(admin);
            Assert.Equal(UserRole.管理员, admin.Role);
            Assert.True(admin.MustChangePassword);
        }

        [Fact]
        public void Verify_DefaultAdminPassword_ShouldSucceed()
        {
            var service = CreateService();

            Assert.True(service.Verify(
                AuthService.DefaultAdminName,
                AuthService.DefaultAdminPassword,
                out var session,
                out var error), error);

            Assert.True(session.IsAuthenticated);
            Assert.Equal(UserRole.管理员, session.Role);
            Assert.True(session.HasRole(UserRole.工程师));
        }

        [Fact]
        public void Verify_WrongPassword_ShouldFail()
        {
            var service = CreateService();

            Assert.False(service.Verify(AuthService.DefaultAdminName, "wrong-password", out _, out var error));
            Assert.Contains("密码", error);
        }

        [Fact]
        public void Verify_UnknownUser_ShouldNotLeakExistence()
        {
            var service = CreateService();

            Assert.False(service.Verify("nobody", "whatever", out _, out var error));
            Assert.Equal("账号或密码错误。", error);
        }

        [Fact]
        public void Password_ShouldBeSalted()
        {
            var first = new UserAccount { UserName = "a" };
            var second = new UserAccount { UserName = "b" };

            AuthService.SetPassword(first, "same-password");
            AuthService.SetPassword(second, "same-password");

            Assert.NotEqual(first.Salt, second.Salt);
            Assert.NotEqual(first.PasswordHash, second.PasswordHash);
            Assert.DoesNotContain("same-password", first.PasswordHash);
        }

        [Fact]
        public void ChangePassword_ShouldRequireOldPassword()
        {
            var service = CreateService();

            Assert.False(service.ChangePassword(AuthService.DefaultAdminName, "bad-old", "newpass123", out var error));
            Assert.Contains("原密码", error);

            Assert.True(service.ChangePassword(
                AuthService.DefaultAdminName,
                AuthService.DefaultAdminPassword,
                "newpass123",
                out error), error);

            Assert.False(service.Verify(AuthService.DefaultAdminName, AuthService.DefaultAdminPassword, out _, out _));
            Assert.True(service.Verify(AuthService.DefaultAdminName, "newpass123", out var session, out _));
            Assert.False(service.Find(AuthService.DefaultAdminName).MustChangePassword);
            Assert.Equal(UserRole.管理员, session.Role);
        }

        [Fact]
        public void ChangePassword_ShouldRejectShortPassword()
        {
            var service = CreateService();

            Assert.False(service.ChangePassword(
                AuthService.DefaultAdminName,
                AuthService.DefaultAdminPassword,
                "123",
                out var error));

            Assert.Contains("至少 6 位", error);
        }

        [Fact]
        public void DisabledUser_ShouldFailToLogin()
        {
            var service = CreateService();

            Assert.True(service.Upsert("op01", "一号操作员", UserRole.操作员, "op123456", out var error), error);
            Assert.True(service.SetEnabled("op01", false, out error), error);

            Assert.False(service.Verify("op01", "op123456", out _, out error));
            Assert.Contains("停用", error);
        }

        [Fact]
        public void Upsert_ShouldPersistAcrossInstances()
        {
            var service = CreateService();
            Assert.True(service.Upsert("eng01", "工艺工程师", UserRole.工程师, "eng123456", out var error), error);

            var reloaded = new AuthService(_usersPath);
            reloaded.Load(out _);

            var user = reloaded.Find("eng01");
            Assert.NotNull(user);
            Assert.Equal(UserRole.工程师, user.Role);
            Assert.True(reloaded.Verify("eng01", "eng123456", out var session, out _));
            Assert.False(session.HasRole(UserRole.管理员));
        }

        [Fact]
        public void Verify_ShouldBeCaseInsensitiveOnUserName()
        {
            var service = CreateService();

            Assert.True(service.Verify("ADMIN", AuthService.DefaultAdminPassword, out var session, out var error), error);
            Assert.Equal(AuthService.DefaultAdminName, session.UserName);
        }

        [Fact]
        public void AnonymousSession_ShouldBeAdminButNotAuthenticated()
        {
            Assert.True(UserSession.Anonymous.HasRole(UserRole.管理员));
            Assert.False(UserSession.Anonymous.IsAuthenticated);
        }

        [Fact]
        public void Remove_ShouldRefuseLastAdmin()
        {
            var service = CreateService();

            Assert.False(service.Remove(AuthService.DefaultAdminName, out var error));
            Assert.Contains("最后一个", error);
            Assert.Single(service.Users);
        }

        [Fact]
        public void Remove_ShouldDeleteNormalUser()
        {
            var service = CreateService();
            Assert.True(service.Upsert("op02", "二号操作员", UserRole.操作员, "op123456", out var error), error);

            Assert.True(service.Remove("op02", out error), error);
            Assert.Null(service.Find("op02"));
        }

        [Fact]
        public void Upsert_ShouldRefuseDowngradeLastAdmin()
        {
            var service = CreateService();

            Assert.False(service.Upsert(
                AuthService.DefaultAdminName,
                "超级管理员",
                UserRole.操作员,
                string.Empty,
                true,
                out var error));

            Assert.Contains("不能停用或降级", error);
            Assert.Equal(UserRole.管理员, service.Find(AuthService.DefaultAdminName).Role);
        }

        [Fact]
        public void Upsert_ShouldAllowDowngradeWhenAnotherAdminExists()
        {
            var service = CreateService();
            Assert.True(service.Upsert("admin2", "二号管理员", UserRole.管理员, "admin23456", out var error), error);

            Assert.True(service.Upsert(
                AuthService.DefaultAdminName,
                "超级管理员",
                UserRole.工程师,
                string.Empty,
                true,
                out error), error);

            Assert.Equal(UserRole.工程师, service.Find(AuthService.DefaultAdminName).Role);
        }

        [Fact]
        public void Upsert_WithoutPassword_ShouldKeepOldPassword()
        {
            var service = CreateService();
            Assert.True(service.Upsert("eng02", "工艺员", UserRole.工程师, "eng123456", out var error), error);

            Assert.True(service.Upsert("eng02", "工艺员改名", UserRole.工程师, string.Empty, true, out error), error);

            Assert.Equal("工艺员改名", service.Find("eng02").DisplayName);
            Assert.True(service.Verify("eng02", "eng123456", out _, out _));
        }

        [Fact]
        public void SetEnabled_ShouldRefuseDisablingLastAdmin()
        {
            var service = CreateService();

            Assert.False(service.SetEnabled(AuthService.DefaultAdminName, false, out var error));
            Assert.Contains("最后一个", error);
            Assert.True(service.Find(AuthService.DefaultAdminName).Enabled);
        }
    }
}
