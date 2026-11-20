using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace HostTemplate.Core.Configuration.Store
{
    /// <summary>
    /// XML 配置读写。写入采用「临时文件 + 原子替换 + 备份」，避免掉电写坏配置。
    /// </summary>
    public static class XmlConfigStore
    {
        public static bool Load<T>(string filePath, out T value, out string error) where T : class
        {
            error = string.Empty;
            value = null;

            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    error = "配置文件路径为空。";
                    return false;
                }

                if (!File.Exists(filePath))
                {
                    error = "配置文件不存在：" + filePath;
                    return false;
                }

                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    value = serializer.Deserialize(reader) as T;
                }

                if (value == null)
                {
                    error = "配置文件反序列化结果为空：" + filePath;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 深拷贝。配置编辑页用它在副本上改，点「取消」时原对象不受影响。
        /// 走序列化往返，新增字段不用改这里。
        /// </summary>
        public static T Clone<T>(T value) where T : class
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    var serializer = new XmlSerializer(typeof(T));
                    serializer.Serialize(stream, value);
                    stream.Position = 0;
                    return serializer.Deserialize(stream) as T;
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool Save<T>(string filePath, T value, out string error) where T : class
        {
            error = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    error = "配置文件路径为空。";
                    return false;
                }

                if (value == null)
                {
                    error = "待保存的配置对象为空。";
                    return false;
                }

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempFile = filePath + ".tmp";
                var backupFile = filePath + ".bak";

                using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    serializer.Serialize(stream, value);
                    stream.Flush(true);
                }

                if (!File.Exists(filePath))
                {
                    File.Move(tempFile, filePath);
                }
                else
                {
                    File.Replace(tempFile, filePath, backupFile);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
