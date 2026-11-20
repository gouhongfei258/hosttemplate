using System;
using System.Windows.Forms;
using HostTemplate.App.Steps;
using HostTemplate.Core.Runtime.Live;
using HostTemplate.Core.Runtime.State;

namespace HostTemplate.App.Startup
{
    /// <summary>
    /// 「实时数据」页的登记表。Program 按 app.xml 的测试类型来要页面，MainForm 只负责放进去。
    /// 自定义类型示例：
    /// LiveViewCatalog.Register("扭矩", ctx => new TorqueLiveView(ctx));
    /// 「默认」、空白和没登记的名字都使用 <see cref="DemoLiveView"/>。
    /// </summary>
    public static class LiveViewCatalog
    {
        public const string DefaultType = LiveViewRegistry<StationContext, Control>.DefaultType;

        private static readonly LiveViewRegistry<StationContext, Control> Registry =
            new LiveViewRegistry<StationContext, Control>();

        public static void Register(string testType, Func<StationContext, Control> create)
        {
            Registry.Register(testType, create);
        }

        public static string Resolve(string testType)
        {
            return Registry.Resolve(testType);
        }

        public static Control Create(StationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Registry.Create(context.App?.TestType, context, CreateDefault);
        }

        private static Control CreateDefault(StationContext context)
        {
            return new DemoLiveView(context.Live);
        }
    }
}
