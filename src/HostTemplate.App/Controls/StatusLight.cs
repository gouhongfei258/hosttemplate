using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HostTemplate.App.Controls
{
    /// <summary>
    /// 极简状态灯：一个圆点 + 一行文字。刻意不依赖第三方控件，保证任何主题下都能显示。
    /// </summary>
    public sealed class StatusLight : Control
    {
        private bool _active;
        private Color _activeColor = Color.FromArgb(34, 197, 94);
        private Color _inactiveColor = Color.FromArgb(107, 114, 128);

        public StatusLight()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Size = new Size(96, 28);
            ForeColor = Color.FromArgb(209, 213, 219);
        }

        public bool Active
        {
            get => _active;
            set
            {
                if (_active == value)
                {
                    return;
                }

                _active = value;
                Invalidate();
            }
        }

        public Color ActiveColor
        {
            get => _activeColor;
            set
            {
                _activeColor = value;
                Invalidate();
            }
        }

        public Color InactiveColor
        {
            get => _inactiveColor;
            set
            {
                _inactiveColor = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var diameter = 12;
            var top = (Height - diameter) / 2;

            using (var brush = new SolidBrush(Active ? ActiveColor : InactiveColor))
            {
                graphics.FillEllipse(brush, 2, top, diameter, diameter);
            }

            using (var textBrush = new SolidBrush(ForeColor))
            using (var format = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                graphics.DrawString(Text, Font, textBrush, new RectangleF(diameter + 8, 0, Width - diameter - 8, Height), format);
            }
        }
    }
}
