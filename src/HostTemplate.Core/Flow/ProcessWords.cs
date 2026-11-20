using System.Collections.Generic;

namespace HostTemplate.Core.Flow
{
    /// <summary>
    /// 标准流程字。与原项目 ProcessWord 完全一致，现场 PLC 程序无需改动。
    /// </summary>
    public static class ProcessWords
    {
        public const ushort Idle = 0;
        public const ushort InStation = 10;
        public const ushort TestStart = 20;
        public const ushort TestFinish = 30;
        public const ushort OutStation = 40;
        public const ushort ReTest = 50;

        private static readonly Dictionary<ushort, string> Names = new Dictionary<ushort, string>
        {
            { Idle, "等待进站" },
            { InStation, "进站" },
            { TestStart, "测试开始" },
            { TestFinish, "测试结束" },
            { OutStation, "出站" },
            { ReTest, "复测" },
        };

        public static string Describe(ushort word)
        {
            return Names.TryGetValue(word, out var name) ? name : $"未定义流程({word})";
        }

        public static IReadOnlyDictionary<ushort, string> All => Names;
    }
}
