using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Windows.Forms;
using HostTemplate.Core.Diagnostics;

namespace HostTemplate.App.Controls
{
    /// <summary>
    /// 把日志推到 RichTextBox。
    /// 采用「队列 + 定时批量刷」，避免高频日志卡住 UI 线程（原项目 LogShowHelper 的同类思路）。
    /// </summary>
    public sealed class UiLogSink : IDisposable
    {
        private const int MaxLines = 800;

        private readonly RichTextBox _target;
        private readonly ConcurrentQueue<LogEventArgs> _queue = new ConcurrentQueue<LogEventArgs>();
        private readonly Timer _timer;

        public UiLogSink(RichTextBox target, int flushIntervalMs = 200)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));

            _timer = new Timer { Interval = Math.Max(50, flushIntervalMs) };
            _timer.Tick += (s, e) => Flush();
            _timer.Start();
        }

        public void Enqueue(LogEventArgs args)
        {
            if (args == null)
            {
                return;
            }

            _queue.Enqueue(args);

            // 队列积压时丢弃最旧的，防止内存膨胀。
            while (_queue.Count > 2000 && _queue.TryDequeue(out _))
            {
            }
        }

        private void Flush()
        {
            if (_queue.IsEmpty || _target.IsDisposed)
            {
                return;
            }

            _target.SuspendLayout();

            try
            {
                while (_queue.TryDequeue(out var item))
                {
                    Append(item);
                }

                while (_target.Lines.Length > MaxLines)
                {
                    var keep = new string[MaxLines];
                    Array.Copy(_target.Lines, _target.Lines.Length - MaxLines, keep, 0, MaxLines);
                    _target.Lines = keep;
                }

                _target.SelectionStart = _target.TextLength;
                _target.ScrollToCaret();
            }
            finally
            {
                _target.ResumeLayout();
            }
        }

        private void Append(LogEventArgs item)
        {
            var color = item.Level switch
            {
                LogLevel.Error => Color.FromArgb(248, 113, 113),
                LogLevel.Fatal => Color.FromArgb(248, 113, 113),
                LogLevel.Warn => Color.FromArgb(251, 191, 36),
                LogLevel.Debug => Color.FromArgb(107, 114, 128),
                _ => Color.FromArgb(209, 213, 219),
            };

            var start = _target.TextLength;
            _target.AppendText($"{item.Time:HH:mm:ss.fff}  {item.Message}{Environment.NewLine}");
            _target.Select(start, _target.TextLength - start);
            _target.SelectionColor = color;
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
            Flush();
        }
    }
}
