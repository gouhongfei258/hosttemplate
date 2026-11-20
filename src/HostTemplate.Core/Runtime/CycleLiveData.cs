using System;
using System.Collections.Generic;

namespace HostTemplate.Core.Runtime
{
    /// <summary>
    /// 当前周期的产品结论。程序报警不走这里，走 AlarmManager。
    /// </summary>
    public enum ProductVerdict
    {
        /// <summary>本周期还没有产品结论。</summary>
        None = 0,

        Ok = 1,

        Ng = 2,
    }

    /// <summary>
    /// 实时表格的一行：项目、标准、实测、合格。只描述当前这一件，不落库。
    /// </summary>
    public sealed class LiveItemRow
    {
        public LiveItemRow(string itemName, string standard, string value, bool passed)
        {
            ItemName = itemName ?? string.Empty;
            Standard = standard ?? string.Empty;
            Value = value ?? string.Empty;
            Passed = passed;
        }

        public string ItemName { get; }

        public string Standard { get; }

        public string Value { get; }

        public bool Passed { get; }
    }

    /// <summary>曲线上的一个点。X 由工站自己定义（序号、时间、角度都可以）。</summary>
    public readonly struct LiveCurvePoint
    {
        public LiveCurvePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }
    }

    /// <summary>一条具名曲线。界面按名称区分系列。</summary>
    public sealed class LiveCurveSeries
    {
        public LiveCurveSeries(string name, IReadOnlyList<LiveCurvePoint> points)
        {
            Name = name ?? string.Empty;
            Points = points ?? Array.Empty<LiveCurvePoint>();
        }

        public string Name { get; }

        public IReadOnlyList<LiveCurvePoint> Points { get; }
    }

    /// <summary>
    /// 当前周期实时数据的只读快照。界面拿去画，不要再写回来。
    /// </summary>
    public sealed class CycleLiveSnapshot
    {
        public CycleLiveSnapshot(
            string serialNumber,
            ProductVerdict verdict,
            string reason,
            IReadOnlyList<LiveItemRow> rows,
            IReadOnlyList<LiveCurveSeries> series)
        {
            SerialNumber = serialNumber ?? string.Empty;
            Verdict = verdict;
            Reason = reason ?? string.Empty;
            Rows = rows ?? Array.Empty<LiveItemRow>();
            Series = series ?? Array.Empty<LiveCurveSeries>();
        }

        public string SerialNumber { get; }

        public ProductVerdict Verdict { get; }

        public string Reason { get; }

        public IReadOnlyList<LiveItemRow> Rows { get; }

        public IReadOnlyList<LiveCurveSeries> Series { get; }
    }

    /// <summary>
    /// 可选的当前周期数据袋，不是实时页面的形状。
    /// 引擎不调用它。工站想用表格行和曲线点时自己写入，并在新的一件开始时调用 <see cref="BeginCycle"/>。
    /// 页面可以是图像标注或任意组合，那种页面不必引用本类。
    /// </summary>
    public sealed class CycleLiveData
    {
        private readonly object _gate = new object();
        private readonly List<LiveItemRow> _rows = new List<LiveItemRow>();
        private readonly Dictionary<string, int> _rowIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<string> _seriesOrder = new List<string>();
        private readonly Dictionary<string, List<LiveCurvePoint>> _series =
            new Dictionary<string, List<LiveCurvePoint>>(StringComparer.Ordinal);

        private string _serialNumber = string.Empty;
        private ProductVerdict _verdict = ProductVerdict.None;
        private string _reason = string.Empty;

        /// <summary>数据变化。可能在引擎线程上触发，界面要自己切回 UI 线程。</summary>
        public event EventHandler Changed;

        /// <summary>
        /// 新的一件开始。清空表格、曲线、产品结论和记住的 SN。
        /// </summary>
        public void BeginCycle()
        {
            lock (_gate)
            {
                _serialNumber = string.Empty;
                _verdict = ProductVerdict.None;
                _reason = string.Empty;
                _rows.Clear();
                _rowIndex.Clear();
                _seriesOrder.Clear();
                _series.Clear();
            }

            Raise();
        }

        /// <summary>
        /// 记住本周期的 SN。出站把运行状态里的 SN 清掉之后，界面仍能显示这一件是谁。
        /// </summary>
        public void RememberSerialNumber(string serialNumber)
        {
            var text = serialNumber ?? string.Empty;

            lock (_gate)
            {
                if (string.Equals(_serialNumber, text, StringComparison.Ordinal))
                {
                    return;
                }

                _serialNumber = text;
            }

            Raise();
        }

        /// <summary>
        /// 写入或更新一行。同名项目覆盖，不追加重复行。
        /// </summary>
        public void PublishItem(string itemName, string standard, string value, bool passed)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return;
            }

            var row = new LiveItemRow(itemName.Trim(), standard, value, passed);

            lock (_gate)
            {
                if (_rowIndex.TryGetValue(row.ItemName, out var index))
                {
                    _rows[index] = row;
                }
                else
                {
                    _rowIndex[row.ItemName] = _rows.Count;
                    _rows.Add(row);
                }
            }

            Raise();
        }

        /// <summary>
        /// 给一条具名曲线追加一个点。系列不存在时自动创建。
        /// </summary>
        public void PublishPoint(string seriesName, double x, double y)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
            {
                return;
            }

            var name = seriesName.Trim();

            lock (_gate)
            {
                if (!_series.TryGetValue(name, out var points))
                {
                    points = new List<LiveCurvePoint>();
                    _series[name] = points;
                    _seriesOrder.Add(name);
                }

                points.Add(new LiveCurvePoint(x, y));
            }

            Raise();
        }

        /// <summary>
        /// 给出当前产品结论。本周期一旦判 NG，后面的 OK 不会把它盖掉。
        /// </summary>
        public void PublishVerdict(bool passed, string reason)
        {
            var text = reason ?? string.Empty;
            var next = passed ? ProductVerdict.Ok : ProductVerdict.Ng;

            lock (_gate)
            {
                if (passed && _verdict == ProductVerdict.Ng)
                {
                    return;
                }

                if (_verdict == next && string.Equals(_reason, text, StringComparison.Ordinal))
                {
                    return;
                }

                _verdict = next;
                _reason = text;
            }

            Raise();
        }

        public CycleLiveSnapshot Snapshot()
        {
            lock (_gate)
            {
                var series = new List<LiveCurveSeries>(_seriesOrder.Count);

                foreach (var name in _seriesOrder)
                {
                    var points = _series[name];
                    series.Add(new LiveCurveSeries(name, points.ToArray()));
                }

                return new CycleLiveSnapshot(
                    _serialNumber,
                    _verdict,
                    _reason,
                    _rows.ToArray(),
                    series);
            }
        }

        private void Raise()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
