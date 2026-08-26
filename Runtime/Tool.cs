using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ToolKit
{
    /// <summary>
    /// 通用工具类
    /// </summary>
    public static class Tool
    {
        #region Process

        /// <summary>
        /// 执行命令行并获取输出结果。
        /// 默认通过 cmd.exe 执行。
        /// </summary>
        /// <param name="command">执行命令</param>
        /// <returns>标准输出内容</returns>
        public static string ExecuteCommand(string command)
        {
            try
            {
                using Process process = new Process();

                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {command}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError(error);
                }

                return output;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                return string.Empty;
            }
        }


        /// <summary>
        /// 启动外部程序。
        /// 支持实时输出日志。
        /// </summary>
        /// <param name="fileName">程序名称</param>
        /// <param name="arguments">启动参数</param>
        public static void StartProcess(string fileName, string arguments)
        {
            try
            {
                using Process process = new Process();

                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        UnityEngine.Debug.Log(e.Data);
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        UnityEngine.Debug.LogError(e.Data);
                    }
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError(
                        $"Process exited with code:{process.ExitCode}");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
        }

        #endregion


        #region Byte

        /// <summary>
        /// 比较两个 byte 数组内容是否完全一致。
        /// 使用 unsafe 指针方式提高大量数据比较性能。
        /// </summary>
        /// <param name="x">数组1</param>
        /// <param name="y">数组2</param>
        /// <returns>是否相同</returns>
        public static unsafe bool CompareBytes(byte[] x, byte[] y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            fixed (byte* xPtr = x)
            fixed (byte* yPtr = y)
            {
                return CompareBytesInternal(xPtr, yPtr, x.Length);
            }
        }


        /// <summary>
        /// 内部字节比较实现。
        /// 每次比较 32 字节，提高 CPU 缓存利用率。
        /// </summary>
        private static unsafe bool CompareBytesInternal( byte* xPtr, byte* yPtr, int length) {
            byte* end = xPtr + length;

            while (xPtr + 32 <= end)
            {
                if (*(ulong*)xPtr != *(ulong*)yPtr)
                    return false;

                if (*(ulong*)(xPtr + 8) != *(ulong*)(yPtr + 8))
                    return false;

                if (*(ulong*)(xPtr + 16) != *(ulong*)(yPtr + 16))
                    return false;

                if (*(ulong*)(xPtr + 24) != *(ulong*)(yPtr + 24))
                    return false;

                xPtr += 32;
                yPtr += 32;
            }

            while (xPtr < end)
            {
                if (*xPtr != *yPtr)
                    return false;

                xPtr++;
                yPtr++;
            }

            return true;
        }

        #endregion


        #region Collection

        /// <summary>
        /// 获取指定数字范围内缺失的数字。
        /// </summary>
        /// <param name="data">已有数字集合</param>
        /// <param name="rangeStart">开始范围</param>
        /// <param name="rangeEnd">结束范围</param>
        /// <returns>缺失数字数组</returns>
        public static int[] GetMissingNumbersInRange( List<int> data, int rangeStart, int rangeEnd) {
            return GetMissingNumbersInRangeCore(
                data,
                rangeStart,
                rangeEnd);
        }


        /// <summary>
        /// 获取指定数字范围内缺失的数字。
        /// </summary>
        public static int[] GetMissingNumbersInRange(
            int[] data,
            int rangeStart,
            int rangeEnd)
        {
            return GetMissingNumbersInRangeCore(
                data,
                rangeStart,
                rangeEnd);
        }


        /// <summary>
        /// 获取缺失数字核心实现。
        /// </summary>
        private static int[] GetMissingNumbersInRangeCore( IEnumerable<int> data, int rangeStart, int rangeEnd) {
            if (rangeEnd < rangeStart)
            {
                throw new ArgumentException(
                    "rangeEnd cannot be smaller than rangeStart.");
            }

            HashSet<int> numberSet =
                data == null
                ? new HashSet<int>()
                : new HashSet<int>(data);


            List<int> missing = new List<int>();

            for (int i = rangeStart; ; i++)
            {
                if (!numberSet.Contains(i))
                {
                    missing.Add(i);
                }

                if (i == rangeEnd)
                {
                    break;
                }
            }

            return missing.ToArray();
        }

        #endregion
    }
}