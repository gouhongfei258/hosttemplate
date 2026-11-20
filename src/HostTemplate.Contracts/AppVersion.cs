using System;

namespace HostTemplate.Contracts
{
    /// <summary>
    /// 版本号比较与主界面标题用的显示格式。比较时丢掉 + 后面的提交号。
    /// </summary>
    public static class AppVersion
    {
        public static bool TryParse(string text, out Version version)
        {
            version = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var value = text.Trim();
            if (value.Length > 1 && (value[0] == 'v' || value[0] == 'V'))
            {
                value = value.Substring(1);
            }

            var plus = value.IndexOf('+');
            if (plus >= 0)
            {
                value = value.Substring(0, plus);
            }

            var dash = value.IndexOf('-');
            if (dash >= 0)
            {
                value = value.Substring(0, dash);
            }

            var parts = value.Split('.');
            if (parts.Length < 2 || parts.Length > 4)
            {
                return false;
            }

            if (parts.Length == 2)
            {
                value += ".0";
            }

            return Version.TryParse(value, out version);
        }

        /// <summary>candidate 比 current 新则返回正数。</summary>
        public static int Compare(string left, string right)
        {
            var leftOk = TryParse(left, out var leftVersion);
            var rightOk = TryParse(right, out var rightVersion);

            if (!leftOk && !rightOk)
            {
                return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
            }

            if (!leftOk)
            {
                return -1;
            }

            if (!rightOk)
            {
                return 1;
            }

            return leftVersion.CompareTo(rightVersion);
        }

        public static bool IsNewer(string candidate, string current)
        {
            return Compare(candidate, current) > 0;
        }

        /// <summary>
        /// 与主界面原有标题规则一致：无提交号则原样显示；有提交号则只留前 7 位。
        /// </summary>
        public static string FormatDisplay(string informational, Version assemblyVersion)
        {
            if (string.IsNullOrWhiteSpace(informational))
            {
                return assemblyVersion == null ? "1.0.0" : assemblyVersion.ToString(3);
            }

            var plus = informational.IndexOf('+');
            if (plus < 0)
            {
                return informational;
            }

            var head = informational.Substring(0, plus);
            var tail = informational.Substring(plus + 1);
            if (tail.Length > 7)
            {
                tail = tail.Substring(0, 7);
            }

            return string.IsNullOrEmpty(tail) ? head : head + "+" + tail;
        }
    }
}
