using System;

namespace HostTemplate.Core.Abstractions
{
    /// <summary>
    /// PLC 通信抽象。引擎只依赖此接口，换 PLC 品牌只换实现。
    /// 所有读写都返回 bool 并输出 error，不抛异常——主循环不能被单次通信失败打断。
    /// </summary>
    public interface IPlcClient : IDisposable
    {
        string Address { get; }

        bool IsConnected { get; }

        bool Open(out string error);

        void Close();

        bool ReadBool(string address, out bool value, out string error);

        bool ReadBoolArray(string address, int count, out bool[] values, out string error);

        bool ReadWord(string address, out ushort value, out string error);

        bool ReadWordArray(string address, int count, out ushort[] values, out string error);

        bool ReadInt32(string address, out int value, out string error);

        bool ReadReal(string address, out float value, out string error);

        bool ReadString(string address, int length, out string value, out string error);

        bool WriteBool(string address, bool value, out string error);

        bool WriteWord(string address, ushort value, out string error);

        bool WriteInt32(string address, int value, out string error);

        bool WriteReal(string address, float value, out string error);

        bool WriteString(string address, string value, out string error);
    }
}
