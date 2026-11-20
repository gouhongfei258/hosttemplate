using System;
using System.Collections.Generic;

namespace HostTemplate.Core.Data
{
    /// <summary>
    /// 一件产品的测试主记录。实时界面不读这张表，只在步骤里落库。
    /// </summary>
    public sealed class TestDataRecord
    {
        public long Id { get; set; }

        public string LineCode { get; set; }

        public string StationCode { get; set; }

        public string StationName { get; set; }

        public string SnNumber { get; set; }

        public string TrayNo { get; set; }

        public string TestType { get; set; }

        public bool Result { get; set; }

        public string WarningMessage { get; set; }

        /// <summary>可选附件说明，例如曲线文件名或原始报文。没有就留空。</summary>
        public string Attachment { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public List<TestDataItemRecord> Items { get; set; } = new List<TestDataItemRecord>();
    }

    /// <summary>
    /// 一条测试项。字段与实时表格一致：项目 / 标准 / 实测 / 合格。
    /// </summary>
    public sealed class TestDataItemRecord
    {
        public long Id { get; set; }

        public long RecordId { get; set; }

        public string ItemName { get; set; }

        public string Standard { get; set; }

        public string Value { get; set; }

        public bool Result { get; set; }

        public string Remark { get; set; }

        public TestDataRecord Record { get; set; }
    }

    /// <summary>程序报警落库。和产品 OK/NG 分开。</summary>
    public sealed class AlarmRecord
    {
        public long Id { get; set; }

        public string StationCode { get; set; }

        public string AlarmKey { get; set; }

        public string Content { get; set; }

        public DateTime RaisedAt { get; set; }

        public DateTime? ClearedAt { get; set; }
    }
}
