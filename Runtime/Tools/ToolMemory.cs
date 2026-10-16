using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ToolKit
{
    /// <summary>
    /// 结构体与非托管内存的互转，以及不依赖系统字节序的整数读写
    /// <para>用法：互转用于与原生插件传递结构体，字节序转换用于自定义二进制协议</para>
    /// <code>
    /// // 把结构体交给原生插件，用完必须释放
    /// IntPtr ptr = ToolMemory.StructToIntPtr(myStruct);
    /// try
    /// {
    ///     NativePlugin.Process(ptr);
    ///     myStruct = ToolMemory.IntPtrToStruct&lt;MyStruct&gt;(ptr);
    /// }
    /// finally
    /// {
    ///     ToolMemory.FreeIntPtr(ptr);
    /// }
    ///
    /// // 写入自定义协议头：长度（小端）+ 类型（大端）
    /// byte[] buffer = new byte[8];
    /// ToolMemory.WriteInt32LittleEndian(buffer, 0, payloadLength);
    /// ToolMemory.WriteInt32BigEndian(buffer, 4, messageType);
    /// </code>
    /// <para>字节序读写不依赖 <c>BitConverter</c>，因此在大小端机器上行为完全一致</para>
    /// </summary>
    /// <remarks>
    /// <para>关于内存所有权：</para>
    /// <list type="bullet">
    /// <item><see cref="StructToIntPtr{T}"/> 每次调用都在非托管堆上新分配一块内存，没有任何缓存或复用</item>
    /// <item>该内存不受 GC 管理，忘记调用 <see cref="FreeIntPtr"/> 会直接泄漏，且不会出现在 Unity 的托管内存统计里</item>
    /// <item>建议用 <c>try / finally</c> 包裹，避免中途抛异常导致释放被跳过</item>
    /// <item>同一个 <c>IntPtr</c> 释放两次会导致非托管堆损坏，<see cref="FreeIntPtr"/> 只在非零时释放，但不做重复释放检测</item>
    /// </list>
    /// <para>关于结构体约束：两个互转方法都要求 <c>T : struct</c>，且会按 <c>Marshal.SizeOf</c> 的布局方式处理，</para>
    /// <para>含引用类型字段（如 <c>string</c>、数组）的结构体需要自行加 <c>[MarshalAs]</c> 标注，否则行为不确定。</para>
    /// </remarks>
    public static class ToolMemory
    {
        #region 结构体与指针转换

        /// <summary>
        /// 把结构体复制到新分配的非托管内存，返回其地址
        /// <para>配合原生插件传递结构体参数时使用</para>
        /// <code>
        /// IntPtr ptr = ToolMemory.StructToIntPtr(config);
        /// try
        /// {
        ///     NativePlugin.ApplyConfig(ptr);
        /// }
        /// finally
        /// {
        ///     ToolMemory.FreeIntPtr(ptr);
        /// }
        /// </code>
        /// </summary>
        /// <typeparam name="T">要复制的结构体类型</typeparam>
        /// <param name="info">源结构体，按值传入后复制到非托管内存</param>
        /// <returns>非托管内存地址，使用完毕后必须传给 <see cref="FreeIntPtr"/> 释放</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr StructToIntPtr<T>(T info) where T : struct
        {
            int size = Marshal.SizeOf(info);
            IntPtr intPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(info, intPtr, false);
            return intPtr;
        }

        /// <summary>
        /// 释放由 <see cref="StructToIntPtr{T}"/> 申请的非托管内存
        /// <code>
        /// ToolMemory.FreeIntPtr(ptr);
        /// ptr = IntPtr.Zero; // 置零可避免后续被误用
        /// </code>
        /// </summary>
        /// <param name="ptr">要释放的地址；传 <c>IntPtr.Zero</c> 时不做任何操作</param>
        /// <remarks>
        /// <para>传零是安全的空操作，但传一个已被释放过的非零地址会破坏非托管堆，</para>
        /// <para>本方法无法检测这种情况，调用方需保证每个地址只释放一次。</para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FreeIntPtr(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// 从非托管内存读回结构体
        /// <para>通常用于读取原生插件回填的输出参数</para>
        /// </summary>
        /// <typeparam name="T">目标结构体类型，需与写入时的类型一致</typeparam>
        /// <param name="info">指向结构体数据的地址</param>
        /// <returns>读回的结构体副本</returns>
        /// <remarks>
        /// <para>本方法不校验地址有效性：传入 <c>IntPtr.Zero</c> 会访问非法地址并导致进程崩溃，</para>
        /// <para>调用前请自行判空。类型不匹配时不会报错，只会读到错位的数据。</para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T IntPtrToStruct<T>(IntPtr info) where T : struct
        {
            return Marshal.PtrToStructure<T>(info);
        }

        #endregion

        #region 字节序转换

        /// <summary>
        /// 以小端序（Little-Endian）把 int32 写入 byte 数组
        /// <para>Windows / Android / iOS 等常见平台的本地字节序就是小端，用于跨平台文件存储时首选</para>
        /// <code>
        /// byte[] buffer = new byte[4];
        /// ToolMemory.WriteInt32LittleEndian(buffer, 0, 305419896);
        /// // buffer = { 0x78, 0x56, 0x34, 0x12 }
        /// </code>
        /// </summary>
        /// <param name="buffer">目标缓冲区，不能为 null</param>
        /// <param name="startIndex">写入起始索引，从 0 开始</param>
        /// <param name="intValue">要写入的 int32 值（负数同样按补码写入）</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> 为 null 时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> 为负，或 <c>startIndex + 4</c> 超出缓冲区长度时抛出
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32LittleEndian(byte[] buffer, int startIndex, int intValue)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (startIndex < 0 || startIndex + 4 > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            buffer[startIndex] = (byte)(intValue & 0xFF);           // 最低字节
            buffer[startIndex + 1] = (byte)((intValue >> 8) & 0xFF);  // 第二字节
            buffer[startIndex + 2] = (byte)((intValue >> 16) & 0xFF); // 第三字节
            buffer[startIndex + 3] = (byte)((intValue >> 24) & 0xFF); // 最高字节
        }

        /// <summary>
        /// 以大端序（Big-Endian）把 int32 写入 byte 数组
        /// <para>网络协议（TCP/IP 规定的网络字节序）使用大端，自定义二进制协议头部通常遵循此约定</para>
        /// <code>
        /// byte[] buffer = new byte[4];
        /// ToolMemory.WriteInt32BigEndian(buffer, 0, 305419896);
        /// // buffer = { 0x12, 0x34, 0x56, 0x78 }
        /// </code>
        /// </summary>
        /// <param name="buffer">目标缓冲区，不能为 null</param>
        /// <param name="startIndex">写入起始索引，从 0 开始</param>
        /// <param name="intValue">要写入的 int32 值（负数同样按补码写入）</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> 为 null 时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> 为负，或 <c>startIndex + 4</c> 超出缓冲区长度时抛出
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32BigEndian(byte[] buffer, int startIndex, int intValue)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (startIndex < 0 || startIndex + 4 > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            buffer[startIndex] = (byte)((intValue >> 24) & 0xFF);     // 最高字节
            buffer[startIndex + 1] = (byte)((intValue >> 16) & 0xFF); // 第三字节
            buffer[startIndex + 2] = (byte)((intValue >> 8) & 0xFF);  // 第二字节
            buffer[startIndex + 3] = (byte)(intValue & 0xFF);         // 最低字节
        }

        /// <summary>
        /// 以小端序从 byte 数组读取 int32
        /// <para>必须与 <see cref="WriteInt32LittleEndian"/> 配对使用，写读字节序不一致会得到错乱的值</para>
        /// </summary>
        /// <param name="buffer">源缓冲区，不能为 null</param>
        /// <param name="startIndex">读取起始索引，从 0 开始</param>
        /// <returns>读出的 int32 值（最高位为 1 时按补码解释为负数）</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> 为 null 时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> 为负，或 <c>startIndex + 4</c> 超出缓冲区长度时抛出
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32LittleEndian(byte[] buffer, int startIndex)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (startIndex < 0 || startIndex + 4 > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            return buffer[startIndex]
                | (buffer[startIndex + 1] << 8)
                | (buffer[startIndex + 2] << 16)
                | (buffer[startIndex + 3] << 24);
        }

        /// <summary>
        /// 以大端序从 byte 数组读取 int32
        /// <para>必须与 <see cref="WriteInt32BigEndian"/> 配对使用</para>
        /// </summary>
        /// <param name="buffer">源缓冲区，不能为 null</param>
        /// <param name="startIndex">读取起始索引，从 0 开始</param>
        /// <returns>读出的 int32 值（最高位为 1 时按补码解释为负数）</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> 为 null 时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> 为负，或 <c>startIndex + 4</c> 超出缓冲区长度时抛出
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32BigEndian(byte[] buffer, int startIndex)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (startIndex < 0 || startIndex + 4 > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            return (buffer[startIndex] << 24)
                | (buffer[startIndex + 1] << 16)
                | (buffer[startIndex + 2] << 8)
                | buffer[startIndex + 3];
        }

        #endregion
    }
}
