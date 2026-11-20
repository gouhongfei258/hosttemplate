using System;
using System.Collections.Generic;
using System.Text;

namespace HostTemplate.Core.Flow.Process
{
    /// <summary>
    /// 当前产品的流程轨迹。用于出站前校验「该走的步骤是否都走过」，
    /// 逻辑来自原项目 AutoProcessHelper，但去掉了界面依赖。
    /// </summary>
    public sealed class ProcessTrace
    {
        private readonly List<ushort> _words = new List<ushort>();
        private readonly object _gate = new object();

        /// <summary>追加流程字，末尾去重（PLC 抖动时不重复记录）。</summary>
        public void Add(ushort word)
        {
            lock (_gate)
            {
                if (_words.Count == 0 || _words[_words.Count - 1] != word)
                {
                    _words.Add(word);
                }
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _words.Clear();
            }
        }

        public IReadOnlyList<ushort> Words
        {
            get
            {
                lock (_gate)
                {
                    return new List<ushort>(_words);
                }
            }
        }

        /// <summary>形如 10-20-30-40 的轨迹文本，直接显示在界面上。</summary>
        public string Text
        {
            get
            {
                lock (_gate)
                {
                    if (_words.Count == 0)
                    {
                        return string.Empty;
                    }

                    var builder = new StringBuilder();
                    for (var i = 0; i < _words.Count; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append('-');
                        }

                        builder.Append(_words[i]);
                    }

                    return builder.ToString();
                }
            }
        }

        /// <summary>
        /// 校验必经流程。required 为空表示不校验。
        /// </summary>
        public bool Check(IEnumerable<ushort> required, out string error)
        {
            error = string.Empty;

            if (required == null)
            {
                return true;
            }

            List<ushort> snapshot;
            lock (_gate)
            {
                snapshot = new List<ushort>(_words);
            }

            var ok = true;
            foreach (var word in required)
            {
                if (!snapshot.Contains(word))
                {
                    ok = false;
                    error += $"缺少流程 {word}（{ProcessWords.Describe(word)}）；";
                }
            }

            return ok;
        }
    }
}
