using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ToolKit
{
    /// <summary>
    /// 与游戏逻辑无关的底层杂项工具：外部进程调用、字节比较、区间数字补全
    /// <para>用法：全部是静态方法，直接调用；不依赖 Unity 生命周期，可在编辑器脚本中复用</para>
    /// <code>
    /// // 执行命令并拿到标准输出，常用于取版本号、跑构建脚本
    /// string revision = Tool.ExecuteCommand("git rev-parse --short HEAD");
    ///
    /// // 启动外部程序，输出会实时转发到 Console
    /// Tool.StartProcess("python.exe", "build.py");
    ///
    /// // 比较两个字节数组内容（比如校验补丁包是否已更新）
    /// bool changed = !Tool.CompareBytes(localHash, remoteHash);
    ///
    /// // 补全缺失的编号（比如已占用的关卡 ID）
    /// int[] free = Tool.GetMissingNumbersInRange(usedIds, 1, 100);
    /// </code>
    /// <para>内部报错走 <c>UnityEngine.Debug.LogError</c> 而非 <c>DLogger</c>，</para>
    /// <para>因为本类刻意不依赖工具包的其他部分，便于单独抽出去用</para>
    /// </summary>
    /// <remarks>
    /// <para>关于进程调用：</para>
    /// <list type="bullet">
    /// <item>两个方法都是同步阻塞的，会卡住调用线程，不要在主线程每帧调用</item>
    /// <item>Windows 下走 <c>cmd.exe /c</c>，macOS/Linux 下走 <c>/bin/bash -c</c>，不支持的平台直接返回空串</item>
    /// <item>标准输出与错误输出都按 UTF-8 解码，与系统的默认代码页无关</item>
    /// <item>失败时不会抛异常，而是记录错误日志并返回 <c>string.Empty</c>，所以调用方必须自行判断空串</item>
    /// </list>
    /// <para>关于 <see cref="CompareBytes"/>：声明为 <c>unsafe</c>，但调用方无需开启 unsafe 上下文。</para>
    /// </remarks>
    public static class Tool
    {
        #region Process

        /// <summary>
        /// 执行一条命令并同步取回标准输出
        /// <para>根据平台自动选择 <c>cmd.exe /c</c>（Windows）或 <c>/bin/bash -c</c>（Unix）</para>
        /// <code>
        /// string output = Tool.ExecuteCommand("git rev-parse HEAD", timeoutMs: 5000);
        ///
        /// // 失败统一返回空串，需要显式判空而不是依赖异常
        /// if (string.IsNullOrEmpty(output))
        /// {
        ///     DLogger.LogWarning("Build", "取版本号失败");
        /// }
        /// </code>
        /// </summary>
        /// <param name="command">要执行的完整命令行（可含管道、重定向等 shell 语法）</param>
        /// <param name="timeoutMs">超时时间（毫秒），默认 30000；超时会杀掉进程并返回空串</param>
        /// <returns>标准输出内容；平台不支持、超时或抛出异常时返回 <c>string.Empty</c></returns>
        /// <remarks>
        /// <para>关于超时机制：</para>
        /// <list type="bullet">
        /// <item>超时判断发生在读取输出之后，因此「大量输出 + 长时间运行」的命令不会被及时打断</item>
        /// <item>标准输出与错误输出是串行读取的：若命令持续向 stderr 写入大量内容而 stdout 未关闭，
        /// 管道缓冲区写满后子进程会阻塞，直到超时才会被清理</item>
        /// <item>需要长时间运行或输出量很大的命令，改用 <see cref="StartProcess"/>，它用异步事件回调读输出</item>
        /// </list>
        /// <para>退出码非 0 且 stderr 非空时只记录一条错误日志，不作为异常抛出，返回值仍是标准输出。</para>
        /// </remarks>
        public static string ExecuteCommand(string command, int timeoutMs = 30000)
        {
            try
            {
                using Process process = new Process();

                // 跨平台支持
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {command}";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command}\"";
#else
                UnityEngine.Debug.LogError("不支持的平台");
                return string.Empty;
#endif

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // 超时控制
                if (!process.WaitForExit(timeoutMs))
                {
                    process.Kill();
                    UnityEngine.Debug.LogError($"命令执行超时: {command}");
                    return string.Empty;
                }

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError($"命令执行错误 (ExitCode={process.ExitCode}): {error}");
                }

                return output;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"执行命令异常: {e}");
                return string.Empty;
            }
        }


        /// <summary>
        /// 启动一个外部程序，并把它的标准输出与错误输出实时转发到 Unity Console
        /// <para>与 <see cref="ExecuteCommand"/> 不同，本方法只负责运行和转发日志，不返回输出内容</para>
        /// <code>
        /// // 跑一个耗时较长的构建脚本，进度会实时打印出来
        /// Tool.StartProcess("python.exe", "build.py --target android", timeoutMs: 0);
        ///
        /// // 非阻塞地拉起外部工具（例如打开资源管理器）
        /// Tool.StartProcess("explorer.exe", Application.persistentDataPath);
        /// </code>
        /// </summary>
        /// <param name="fileName">可执行文件路径或名称（依赖系统 PATH 查找）</param>
        /// <param name="arguments">启动参数字符串，原样传给子进程</param>
        /// <param name="timeoutMs">超时时间（毫秒），0 表示无限等待；超时会杀掉进程并记录错误</param>
        /// <remarks>
        /// <para>关于输出读取：</para>
        /// <list type="bullet">
        /// <item>使用 <c>BeginOutputReadLine</c> 异步回调，因此不会出现 <c>ExecuteCommand</c> 那样的管道阻塞</item>
        /// <item>回调在 .NET 线程池线程上触发，其中调用 <c>UnityEngine.Debug.Log</c> 而非 <c>DLogger</c>，
        /// 避免依赖主线程状态</item>
        /// <item>超时用 <c>WaitForExit(timeoutMs)</c>，该重载不保证异步输出回调已全部回调完毕；
        /// 需要完整日志时会无限等待（传 0）后自然退出</item>
        /// </list>
        /// <para>退出码非 0 时记录错误日志，但同样不抛异常；启动失败（如文件不存在）也只记录日志。</para>
        /// </remarks>
        public static void StartProcess(string fileName, string arguments, int timeoutMs = 0)
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

                // 超时控制
                if (timeoutMs > 0)
                {
                    if (!process.WaitForExit(timeoutMs))
                    {
                        process.Kill();
                        UnityEngine.Debug.LogError($"进程执行超时: {fileName}");
                        return;
                    }
                }
                else
                {
                    process.WaitForExit();
                }

                if (process.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError(
                        $"Process exited with code: {process.ExitCode}");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"启动进程异常: {e}");
            }
        }

        #endregion


        #region Byte

        /// <summary>
        /// 比较两个字节数组的内容是否完全一致
        /// <para>逐字节语义，与数组的引用无关；内部按 8 字节对齐比较以提高吞吐</para>
        /// <code>
        /// byte[] local = ToolFile.ReadBytes(localPatchPath);
        /// byte[] remote = downloadedBytes;
        ///
        /// if (!Tool.CompareBytes(local, remote))
        /// {
        ///     DLogger.Log("Patch", "补丁内容有变化，需要重新写入");
        /// }
        /// </code>
        /// </summary>
        /// <param name="x">数组1，允许为 null</param>
        /// <param name="y">数组2，允许为 null</param>
        /// <returns>长度与内容都相同返回 true；两者为同一引用、或两者都为 null、或两者都为空数组时也返回 true</returns>
        /// <remarks>
        /// <para>关于短数组：</para>
        /// <para>不足 8 字节的部分退化为逐字节比较，所以比较 1~7 字节的数组时优势不明显，</para>
        /// <para>但也不会比 <c>SequenceEqual</c> 更慢。适合用于校验补丁包、存档等长时间运行的场景。</para>
        /// </remarks>
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

            if (x.Length == 0)
            {
                return true;
            }

            fixed (byte* xPtr = x)
            fixed (byte* yPtr = y)
            {
                return CompareBytesInternal(xPtr, yPtr, x.Length);
            }
        }


        /// <summary>
        /// 内部字节比较实现
        /// <para>先按 8 字节（<c>ulong</c>）步进比较，尾部不足 8 字节的部分逐字节收尾</para>
        /// </summary>
        /// <param name="xPtr">数组1 的首地址</param>
        /// <param name="yPtr">数组2 的首地址</param>
        /// <param name="length">要比较的字节数，调用方需保证两个指针都有这么多有效字节</param>
        /// <returns>全部相同返回 true，遇到首个不同的字节立即返回 false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool CompareBytesInternal(byte* xPtr, byte* yPtr, int length)
        {
            byte* end = xPtr + length;

            // 8 字节对齐比较（更安全的实现）
            while (xPtr + 8 <= end)
            {
                if (*(ulong*)xPtr != *(ulong*)yPtr)
                    return false;

                xPtr += 8;
                yPtr += 8;
            }

            // 剩余字节逐字节比较
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
        /// 计算闭区间内未被占用的整数集合
        /// <para>用于分配不冲突的编号，例如新增关卡时挑一个没被占用的 ID</para>
        /// <code>
        /// List&lt;int&gt; usedIds = new List&lt;int&gt; { 1, 2, 4, 7 };
        ///
        /// // 返回 { 3, 5, 6, 8, 9, 10 }
        /// int[] free = Tool.GetMissingNumbersInRange(usedIds, 1, 10);
        /// </code>
        /// </summary>
        /// <param name="data">已占用的数字集合，允许为 null（视为全空）</param>
        /// <param name="rangeStart">范围起点，包含在结果内</param>
        /// <param name="rangeEnd">范围终点，包含在结果内</param>
        /// <returns>缺失数字数组，按从小到大排序；区间内无缺失时返回空数组</returns>
        /// <remarks>
        /// <para>关于性能：</para>
        /// <para>实现会遍历整个区间并逐个查 <c>HashSet</c>，复杂度为 O(区间长度)。</para>
        /// <para>区间跨度远大于 <paramref name="data"/> 的规模时（例如在大范围里找几个空位）开销会明显上升。</para>
        /// </remarks>
        public static int[] GetMissingNumbersInRange(List<int> data, int rangeStart, int rangeEnd)
        {
            return GetMissingNumbersInRangeCore(data, rangeStart, rangeEnd);
        }


        /// <summary>
        /// 计算闭区间内未被占用的整数集合（数组重载）
        /// <para>行为与 <see cref="GetMissingNumbersInRange(List{int}, int, int)"/> 完全一致</para>
        /// </summary>
        /// <param name="data">已占用的数字数组，允许为 null（视为全空）</param>
        /// <param name="rangeStart">范围起点，包含在结果内</param>
        /// <param name="rangeEnd">范围终点，包含在结果内</param>
        /// <returns>缺失数字数组，按从小到大排序；区间内无缺失时返回空数组</returns>
        public static int[] GetMissingNumbersInRange(int[] data, int rangeStart, int rangeEnd)
        {
            return GetMissingNumbersInRangeCore(data, rangeStart, rangeEnd);
        }


        /// <summary>
        /// 缺失数字计算的核心实现，两个公开重载共用
        /// </summary>
        /// <param name="data">已占用的数字集合，允许为 null</param>
        /// <param name="rangeStart">范围起点（包含）</param>
        /// <param name="rangeEnd">范围终点（包含）</param>
        /// <returns>缺失数字数组</returns>
        /// <exception cref="ArgumentException"><paramref name="rangeEnd"/> 小于 <paramref name="rangeStart"/> 时抛出</exception>
        private static int[] GetMissingNumbersInRangeCore(IEnumerable<int> data, int rangeStart, int rangeEnd)
        {
            if (rangeEnd < rangeStart)
            {
                throw new ArgumentException("rangeEnd 不能小于 rangeStart.");
            }

            HashSet<int> numberSet =
                data == null
                ? new HashSet<int>()
                : new HashSet<int>(data);

            List<int> missing = new List<int>();

            for (int i = rangeStart; i <= rangeEnd; i++)
            {
                if (!numberSet.Contains(i))
                {
                    missing.Add(i);
                }
            }

            return missing.ToArray();
        }

        #endregion
    }
}
