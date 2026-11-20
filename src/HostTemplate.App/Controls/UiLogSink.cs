using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly List<LogEventArgs> _history = new List<LogEventArgs>();
        private readonly Timer _timer;
        private Color _info = Color.FromArgb(209, 213, 219);
        private Color _warn = Color.FromArgb(251, 191, 36);
        private Color _error = Color.FromArgb(248, 113, 113);
        private Color _debug = Color.FromArgb(107, 114, 128);

        public UiLogSink(RichTextBox target, int flushIntervalMs = 200)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));

            _timer = new Timer { Interval = Math.Max(50, flushIntervalMs) };
            _timer.Tick += (s, e) => Flush();
            _timer.Start();
        }

        /// <summary>换日间/夜间时重画已经显示的行，级别颜色跟着变。</summary>
        public void ApplyColors(Color info, Color warn, Color error, Color debug)
        {
            _info = info;
            _warn = warn;
            _error = error;
            _debug = debug;

            if (_target.IsDisposed)
            {
                return;
            }

            Rebuild();
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
                var rebuilt = false;

                while (_queue.TryDequeue(out var item))
                {
                    _history.Add(item);

                    if (_history.Count > MaxLines)
                    {
                        _history.RemoveRange(0, _history.Count - MaxLines);
                        rebuilt = true;
                    }
                    else
                    {
                        Write(item);
                    }
                }

                if (rebuilt)
                {
                    Rebuild();
                }

                _target.SelectionStart = _target.TextLength;
                _target.ScrollToCaret();
            }
            finally
            {
                _target.ResumeLayout();
            }
        }

        private void Rebuild()
        {
            _target.Clear();

            foreach (var item in _history)
            {
                Write(item);
            }

            _target.SelectionStart = _target.TextLength;
            _target.ScrollToCaret();
        }

        private void Write(LogEventArgs item)
        {
            var color = item.Level switch
            {
                LogLevel.Error => _error,
                LogLevel.Fatal => _error,
                LogLevel.Warn => _warn,
                LogLevel.Debug => _debug,
                _ => _info,
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
