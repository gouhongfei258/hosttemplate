using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace HostTemplate.App
{
    /// <summary>
    /// 主界面产品标志。图片是嵌入的白底横幅，只负责显示。
    /// </summary>
    internal static class ProductLogo
    {
        public const string ResourceName = "HostTemplate.App.Assets.logo-byd-shisibu.png";

        public static bool TryApply(PictureBox box)
        {
            if (box == null)
            {
                return false;
            }

            try
            {
                var image = Load();
                if (image == null)
                {
                    box.Visible = false;
                    return false;
                }

                box.Image = image;
                box.AccessibleName = "十四事业部装备中心数采系统";
                box.Visible = true;
                return true;
            }
            catch
            {
                box.Visible = false;
                return false;
            }
        }

        private static Image Load()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                return null;
            }

            using (stream)
            using (var source = Image.FromStream(stream))
            {
                return new Bitmap(source);
            }
        }
    }
}
