using System;
using System.Collections.Generic;

namespace HostTemplate.Core.Runtime.Live
{
    /// <summary>
    /// 按「测试类型」选择实时页面。
    /// 空白、默认、default，以及没有登记过的名字，都回到 <see cref="DefaultType"/>，由调用方提供默认页面。
    /// 其它名字由工站用 <see cref="Register"/> 登记，页面本身可以是任意控件，不限定表格或曲线。
    /// </summary>
    public sealed class LiveViewRegistry<TContext, TView>
    {
        public const string DefaultType = "默认";

        private readonly Dictionary<string, Func<TContext, TView>> _views =
            new Dictionary<string, Func<TContext, TView>>(StringComparer.OrdinalIgnoreCase);

        public void Register(string testType, Func<TContext, TView> create)
        {
            if (string.IsNullOrWhiteSpace(testType))
            {
                throw new ArgumentException("测试类型不能为空。", nameof(testType));
            }

            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            var name = testType.Trim();
            if (IsDefaultName(name))
            {
                throw new ArgumentException("「" + DefaultType + "」固定使用内置实时页面，请给自定义页面另起名字。", nameof(testType));
            }

            _views[name] = create;
        }

        public bool IsRegistered(string testType)
        {
            return !IsDefaultName(testType) && _views.ContainsKey(testType.Trim());
        }

        public string Resolve(string testType)
        {
            if (IsDefaultName(testType))
            {
                return DefaultType;
            }

            var name = testType.Trim();
            return _views.ContainsKey(name) ? name : DefaultType;
        }

        public TView Create(string testType, TContext context, Func<TContext, TView> defaultView)
        {
            if (defaultView == null)
            {
                throw new ArgumentNullException(nameof(defaultView));
            }

            var name = Resolve(testType);
            if (!string.Equals(name, DefaultType, StringComparison.OrdinalIgnoreCase)
                && _views.TryGetValue(name, out var create))
            {
                return create(context);
            }

            return defaultView(context);
        }

        public static bool IsDefaultName(string testType)
        {
            if (string.IsNullOrWhiteSpace(testType))
            {
                return true;
            }

            var name = testType.Trim();
            return string.Equals(name, DefaultType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "default", StringComparison.OrdinalIgnoreCase);
        }
    }
}
