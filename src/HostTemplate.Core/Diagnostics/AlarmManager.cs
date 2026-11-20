using System;
using System.Collections.Generic;

namespace HostTemplate.Core.Diagnostics
{
    public sealed class AlarmItem
    {
        public string Key { get; internal set; }

        public string Message { get; internal set; }

        public bool Active { get; internal set; }

        public DateTime RaisedAt { get; internal set; }

        public DateTime? ClearedAt { get; internal set; }
    }

    public sealed class AlarmChangedEventArgs : EventArgs
    {
        public AlarmChangedEventArgs(AlarmItem item, bool activated)
        {
            Item = item;
            Activated = activated;
        }

        public AlarmItem Item { get; }

        /// <summary>true 表示报警产生，false 表示报警恢复。</summary>
        public bool Activated { get; }
    }

    /// <summary>
    /// 报警管理。语义与原项目 Alarmer.Content("连接PLC失败", true) 一致：
    /// 只在状态发生「上升沿 / 下降沿」时通知界面，避免每轮询刷一次日志。
    /// </summary>
    public sealed class AlarmManager
    {
        private readonly Dictionary<string, AlarmItem> _items = new Dictionary<string, AlarmItem>(StringComparer.Ordinal);
        private readonly object _gate = new object();

        public event EventHandler<AlarmChangedEventArgs> Changed;

        public int ActiveCount
        {
            get
            {
                lock (_gate)
                {
                    var count = 0;
                    foreach (var item in _items.Values)
                    {
                        if (item.Active)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }
        }

        /// <summary>
        /// 设置报警状态。状态未变化时不触发事件。
        /// </summary>
        public void Set(string key, bool active, string message = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            AlarmChangedEventArgs args = null;

            lock (_gate)
            {
                if (!_items.TryGetValue(key, out var item))
                {
                    item = new AlarmItem
                    {
                        Key = key,
                        Message = message ?? key,
                        Active = false
                    };
                    _items[key] = item;
                }

                if (item.Active == active)
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        item.Message = message;
                    }

                    return;
                }

                item.Active = active;
                item.Message = message ?? item.Message;

                if (active)
                {
                    item.RaisedAt = DateTime.Now;
                    item.ClearedAt = null;
                }
                else
                {
                    item.ClearedAt = DateTime.Now;
                }

                args = new AlarmChangedEventArgs(item, active);
            }

            Changed?.Invoke(this, args);
        }

        public void Clear()
        {
            lock (_gate)
            {
                _items.Clear();
            }
        }

        public IReadOnlyList<AlarmItem> Snapshot()
        {
            lock (_gate)
            {
                return new List<AlarmItem>(_items.Values);
            }
        }
    }
}
