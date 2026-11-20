using System;

namespace HostTemplate.Contracts
{
    /// <summary>
    /// 工站配置包里的文件名。地址表只在 plc.xml 里，不另做一份状态表。
    /// </summary>
    public static class StationFiles
    {
        public const string App = "app.xml";
        public const string Plc = "plc.xml";
        public const string Api = "api.xml";
        public const string Users = "users.xml";

        public static readonly string[] Required = { App, Plc, Api };

        public static readonly string[] Package = { App, Plc, Api, Users };

        public static bool IsPackageFile(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var file = System.IO.Path.GetFileName(name);
            foreach (var known in Package)
            {
                if (string.Equals(file, known, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSafeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 80)
            {
                return false;
            }

            if (id == "." || id == "..")
            {
                return false;
            }

            foreach (var c in id)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
