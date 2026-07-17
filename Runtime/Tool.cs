using System.Collections.Generic;
using System.Diagnostics;

namespace ToolKit
{
    public static class Tool
    {
        public static string Cmd(string arguments)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
                    process.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
                    process.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                    process.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                    process.StartInfo.CreateNoWindow = true;//不显示程序窗口

                    process.Start();//启动程序

                    return process.StandardOutput.ReadToEnd();
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
            return "";
        }

        public static void RunProcess(string command, string arguments)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
                    process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                    process.StartInfo.StandardInputEncoding = System.Text.Encoding.UTF8;
                    process.StartInfo.FileName = command;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
                    process.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
                    process.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                    process.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                    process.StartInfo.CreateNoWindow = true;//不显示程序窗口
                    process.OutputDataReceived += (sender, str) =>
                    {
                        if (!string.IsNullOrEmpty(str.Data))
                        {
                            UnityEngine.Debug.Log(str.Data);
                        }
                    };
                    process.Exited += (sender, e) => UnityEngine.Debug.LogError(e.ToString());
                    process.Start();//启动程序
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
        }

        public static unsafe bool CompareBytes(byte[] x, byte[] y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.Length != y.Length) return false;

            fixed (byte* xPtr = x, yPtr = y)
            {
                return UlongCompareInternal(xPtr, yPtr, x.Length);
            }
        }

        private static unsafe bool UlongCompareInternal(byte* xPtr, byte* yPtr, int length)
        {
            // 指针+偏移量计算出数组最后一个元素地址
            byte* lastAddr = xPtr + length;
            byte* lastAddrMinus32 = lastAddr - 32;
            while (xPtr < lastAddrMinus32) // 我们一次循环比较32字节，也就是256位
            {
                // 一次判断比较前64位
                if (*(ulong*)xPtr != *(ulong*)yPtr) return false;
                // 第二次从64为开始，比较接下来的64位，需要指针偏移64位，一个byte指针是8为，所以需要偏移8个位置才能到下一轮起始位置
                // 所以代码就是xPtr+8
                if (*(ulong*)(xPtr + 8) != *(ulong*)(yPtr + 8)) return false;
                // 同上面一样，第三次从第128位开始比较64位
                if (*(ulong*)(xPtr + 16) != *(ulong*)(yPtr + 16)) return false;
                // 第四次从第192位开始比较64位
                if (*(ulong*)(xPtr + 24) != *(ulong*)(yPtr + 24)) return false;
                // 一轮总共比较了256位，让指针偏移256位
                xPtr += 32;
                yPtr += 32;
            }
            // 因为上面是一次性比较32字节(256位)，可能数组不能为32整除，最后只留下比如30字节，20字节
            // 最后的几个字节，我们用循环来逐字节比较
            while (xPtr < lastAddr)
            {
                if (*xPtr != *yPtr) return false;
                xPtr++;
                yPtr++;
            }
            return true;
        }

        /// <summary>
        /// 获取顺序缺失数字
        /// </summary>
        /// <param name="data"></param>
        /// <param name="rangeStart"></param>
        /// <param name="rangeEnd"></param>
        public static int[] GetSequentialMissings(List<int> data, int rangeStart, int rangeEnd)
        {
            return GetSequentialMissingsCore(data, rangeStart, rangeEnd);
        }

        /// <summary>
        /// 获取顺序缺失数字
        /// </summary>
        /// <param name="data"></param>
        /// <param name="rangeStart"></param>
        /// <param name="rangeEnd"></param>
        public static int[] GetSequentialMissings(int[] data, int rangeStart, int rangeEnd)
        {
            return GetSequentialMissingsCore(data, rangeStart, rangeEnd);
        }

        private static int[] GetSequentialMissingsCore(IEnumerable<int> data, int rangeStart, int rangeEnd)
        {
            if (rangeEnd < rangeStart)
            {
                TLog.LogError($"参数范围错误：rangeStart({rangeStart}) > rangeEnd({rangeEnd})");
                return System.Array.Empty<int>();
            }

            if (data == null)
            {
                data = System.Array.Empty<int>();
            }

            HashSet<int> numbers = new HashSet<int>(data);
            List<int> missing = new List<int>();

            for (int i = rangeStart; i <= rangeEnd; i++)
            {
                if (!numbers.Contains(i))
                {
                    missing.Add(i);
                }
            }

            return missing.ToArray();
        }
    }
}