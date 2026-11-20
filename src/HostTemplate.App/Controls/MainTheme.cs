using System.Drawing;
using HostTemplate.Core.Configuration;
using Sunny.UI;

namespace HostTemplate.App.Controls
{
    /// <summary>
    /// 主界面日间/夜间配色。夜间沿用现有深色，不改成 SunnyUI 的纯黑。
    /// SunnyUI 控件用 <see cref="SunnyStyle"/> 作为底，再盖上这里的颜色，保证整窗一致。
    /// </summary>
    public sealed class MainTheme
    {
        private MainTheme()
        {
        }

        public static MainTheme Night { get; } = CreateNight();

        public static MainTheme Day { get; } = CreateDay();

        public static MainTheme For(UiThemeMode mode)
        {
            return mode == UiThemeMode.日间 ? Day : Night;
        }

        public UiThemeMode Mode { get; private set; }

        public bool IsNight => Mode == UiThemeMode.夜间;

        public UIStyle SunnyStyle { get; private set; }

        public Color WindowBack { get; private set; }

        public Color BarBack { get; private set; }

        public Color StripBack { get; private set; }

        public Color TitleFore { get; private set; }

        public Color SubFore { get; private set; }

        public Color MutedFore { get; private set; }

        public Color HintFore { get; private set; }

        public Color WordFore { get; private set; }

        public Color AlarmIdleBack { get; private set; }

        public Color AlarmIdleFore { get; private set; }

        public Color AlarmActiveBack { get; private set; }

        public Color AlarmActiveFore { get; private set; }

        public Color LightFore { get; private set; }

        public Color LightInactive { get; private set; }

        public Color ButtonSecondaryFill { get; private set; }

        public Color ButtonSecondaryRect { get; private set; }

        public Color ButtonSecondaryFore { get; private set; }

        public Color StartFill { get; private set; }

        public Color StopFill { get; private set; }

        public Color KeyboardFill { get; private set; }

        public Color LogBack { get; private set; }

        public Color LogInfo { get; private set; }

        public Color LogWarn { get; private set; }

        public Color LogError { get; private set; }

        public Color LogDebug { get; private set; }

        public Color TabBack { get; private set; }

        public Color TabSelected { get; private set; }

        public Color TabSelectedFore { get; private set; }

        public Color TabUnselectedFore { get; private set; }

        public Color TabAccent { get; private set; }

        public Color PageBack { get; private set; }

        public Color GridBack { get; private set; }

        public Color GridHeaderBack { get; private set; }

        public Color GridHeaderFore { get; private set; }

        public Color GridText { get; private set; }

        public Color GridSelectBack { get; private set; }

        public Color GridSelectFore { get; private set; }

        public Color GridStripe { get; private set; }

        public Color GridLine { get; private set; }

        public Color ScrollBar { get; private set; }

        public Color ScrollTrack { get; private set; }

        public UIChartStyleType ChartStyle { get; private set; }

        public Color VerdictIdleBack { get; private set; }

        public Color VerdictIdleFore { get; private set; }

        public Color VerdictIdleReason { get; private set; }

        public Color VerdictOkBack { get; private set; }

        public Color VerdictOkFore { get; private set; }

        public Color VerdictOkReason { get; private set; }

        public Color VerdictNgBack { get; private set; }

        public Color VerdictNgFore { get; private set; }

        public Color VerdictNgReason { get; private set; }

        public Color PassFore { get; private set; }

        public Color FailFore { get; private set; }

        public Color ThemeButtonFill { get; private set; }

        public Color ThemeButtonFore { get; private set; }

        public Color ThemeButtonRect { get; private set; }

        public string ThemeButtonText { get; private set; }

        public string ThemeButtonTip { get; private set; }

        private static MainTheme CreateNight()
        {
            return new MainTheme
            {
                Mode = UiThemeMode.夜间,
                SunnyStyle = UIStyle.Black,
                WindowBack = Color.FromArgb(11, 18, 32),
                BarBack = Color.FromArgb(17, 24, 39),
                StripBack = Color.FromArgb(15, 23, 42),
                TitleFore = Color.FromArgb(249, 250, 251),
                SubFore = Color.FromArgb(156, 163, 175),
                MutedFore = Color.FromArgb(156, 163, 175),
                HintFore = Color.FromArgb(107, 114, 128),
                WordFore = Color.FromArgb(56, 189, 248),
                AlarmIdleBack = Color.FromArgb(17, 24, 39),
                AlarmIdleFore = Color.FromArgb(74, 222, 128),
                AlarmActiveBack = Color.FromArgb(127, 29, 29),
                AlarmActiveFore = Color.FromArgb(254, 242, 242),
                LightFore = Color.FromArgb(209, 213, 219),
                LightInactive = Color.FromArgb(107, 114, 128),
                ButtonSecondaryFill = Color.FromArgb(55, 65, 81),
                ButtonSecondaryRect = Color.FromArgb(75, 85, 99),
                ButtonSecondaryFore = Color.White,
                StartFill = Color.FromArgb(22, 163, 74),
                StopFill = Color.FromArgb(185, 28, 28),
                KeyboardFill = Color.FromArgb(37, 99, 235),
                LogBack = Color.FromArgb(11, 18, 32),
                LogInfo = Color.FromArgb(209, 213, 219),
                LogWarn = Color.FromArgb(251, 191, 36),
                LogError = Color.FromArgb(248, 113, 113),
                LogDebug = Color.FromArgb(107, 114, 128),
                TabBack = Color.FromArgb(17, 24, 39),
                TabSelected = Color.FromArgb(15, 23, 42),
                TabSelectedFore = Color.FromArgb(56, 189, 248),
                TabUnselectedFore = Color.FromArgb(156, 163, 175),
                TabAccent = Color.FromArgb(56, 189, 248),
                PageBack = Color.FromArgb(11, 18, 32),
                GridBack = Color.FromArgb(11, 18, 32),
                GridHeaderBack = Color.FromArgb(17, 24, 39),
                GridHeaderFore = Color.FromArgb(156, 163, 175),
                GridText = Color.FromArgb(229, 231, 235),
                GridSelectBack = Color.FromArgb(30, 41, 59),
                GridSelectFore = Color.FromArgb(229, 231, 235),
                GridStripe = Color.FromArgb(15, 23, 42),
                GridLine = Color.FromArgb(31, 41, 55),
                ScrollBar = Color.FromArgb(75, 85, 99),
                ScrollTrack = Color.FromArgb(17, 24, 39),
                ChartStyle = UIChartStyleType.Dark,
                VerdictIdleBack = Color.FromArgb(15, 23, 42),
                VerdictIdleFore = Color.FromArgb(107, 114, 128),
                VerdictIdleReason = Color.FromArgb(156, 163, 175),
                VerdictOkBack = Color.FromArgb(6, 48, 32),
                VerdictOkFore = Color.FromArgb(74, 222, 128),
                VerdictOkReason = Color.FromArgb(187, 247, 208),
                VerdictNgBack = Color.FromArgb(69, 10, 10),
                VerdictNgFore = Color.FromArgb(248, 113, 113),
                VerdictNgReason = Color.FromArgb(254, 202, 202),
                PassFore = Color.FromArgb(74, 222, 128),
                FailFore = Color.FromArgb(248, 113, 113),
                ThemeButtonFill = Color.FromArgb(241, 245, 249),
                ThemeButtonFore = Color.FromArgb(15, 23, 42),
                ThemeButtonRect = Color.FromArgb(226, 232, 240),
                ThemeButtonText = "日间",
                ThemeButtonTip = "当前是夜间界面，点击切换为日间",
            };
        }

        private static MainTheme CreateDay()
        {
            return new MainTheme
            {
                Mode = UiThemeMode.日间,
                SunnyStyle = UIStyle.Blue,
                WindowBack = Color.FromArgb(248, 250, 252),
                BarBack = Color.White,
                StripBack = Color.FromArgb(241, 245, 249),
                TitleFore = Color.FromArgb(15, 23, 42),
                SubFore = Color.FromArgb(71, 85, 105),
                MutedFore = Color.FromArgb(71, 85, 105),
                HintFore = Color.FromArgb(51, 65, 85),
                WordFore = Color.FromArgb(2, 132, 199),
                AlarmIdleBack = Color.FromArgb(240, 253, 244),
                AlarmIdleFore = Color.FromArgb(21, 128, 61),
                AlarmActiveBack = Color.FromArgb(254, 226, 226),
                AlarmActiveFore = Color.FromArgb(153, 27, 27),
                LightFore = Color.FromArgb(30, 41, 59),
                LightInactive = Color.FromArgb(148, 163, 184),
                ButtonSecondaryFill = Color.FromArgb(241, 245, 249),
                ButtonSecondaryRect = Color.FromArgb(203, 213, 225),
                ButtonSecondaryFore = Color.FromArgb(15, 23, 42),
                StartFill = Color.FromArgb(22, 163, 74),
                StopFill = Color.FromArgb(185, 28, 28),
                KeyboardFill = Color.FromArgb(29, 78, 216),
                LogBack = Color.White,
                LogInfo = Color.FromArgb(30, 41, 59),
                LogWarn = Color.FromArgb(180, 83, 9),
                LogError = Color.FromArgb(185, 28, 28),
                LogDebug = Color.FromArgb(100, 116, 139),
                TabBack = Color.FromArgb(241, 245, 249),
                TabSelected = Color.White,
                TabSelectedFore = Color.FromArgb(2, 132, 199),
                TabUnselectedFore = Color.FromArgb(71, 85, 105),
                TabAccent = Color.FromArgb(2, 132, 199),
                PageBack = Color.FromArgb(248, 250, 252),
                GridBack = Color.White,
                GridHeaderBack = Color.FromArgb(241, 245, 249),
                GridHeaderFore = Color.FromArgb(51, 65, 85),
                GridText = Color.FromArgb(15, 23, 42),
                GridSelectBack = Color.FromArgb(224, 242, 254),
                GridSelectFore = Color.FromArgb(15, 23, 42),
                GridStripe = Color.FromArgb(248, 250, 252),
                GridLine = Color.FromArgb(226, 232, 240),
                ScrollBar = Color.FromArgb(148, 163, 184),
                ScrollTrack = Color.FromArgb(241, 245, 249),
                ChartStyle = UIChartStyleType.Plain,
                VerdictIdleBack = Color.FromArgb(241, 245, 249),
                VerdictIdleFore = Color.FromArgb(100, 116, 139),
                VerdictIdleReason = Color.FromArgb(71, 85, 105),
                VerdictOkBack = Color.FromArgb(220, 252, 231),
                VerdictOkFore = Color.FromArgb(21, 128, 61),
                VerdictOkReason = Color.FromArgb(22, 101, 52),
                VerdictNgBack = Color.FromArgb(254, 226, 226),
                VerdictNgFore = Color.FromArgb(185, 28, 28),
                VerdictNgReason = Color.FromArgb(153, 27, 27),
                PassFore = Color.FromArgb(22, 163, 74),
                FailFore = Color.FromArgb(185, 28, 28),
                ThemeButtonFill = Color.FromArgb(15, 23, 42),
                ThemeButtonFore = Color.White,
                ThemeButtonRect = Color.FromArgb(15, 23, 42),
                ThemeButtonText = "夜间",
                ThemeButtonTip = "当前是日间界面，点击切换为夜间",
            };
        }
    }

    /// <summary>
    /// 跟主界面一起换日间/夜间。自定义实时页面可以选实现，不实现就保持自己的颜色。
    /// </summary>
    public interface IMainThemeAware
    {
        void ApplyMainTheme(MainTheme theme);
    }
}
