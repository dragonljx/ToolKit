using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace ToolKit
{

    public static class ToolMemory
    {
        /// <summary>
        /// 结构体转IntPtr
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info"></param>
        /// <returns></returns>
        [MethodImpl(methodImplOptions:MethodImplOptions.AggressiveInlining)]
        public static IntPtr StructToIntPtr<T>(T info)
        {
            int size = Marshal.SizeOf(info);
            IntPtr intPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(info, intPtr, true);
            return intPtr;
        }

        /// <summary>
        /// 释放 StructToIntPtr 申请的非托管内存
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static void FreeIntPtr(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        /// <summary>
        /// IntPtr转结构体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info"></param>
        /// <returns></returns>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static T IntPtrToStruct<T>(IntPtr info)
        {
            return (T)Marshal.PtrToStructure(info, typeof(T));
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static void BytesAddInt(byte[] buffer, int startIndex, int intValue)
        {
            buffer[startIndex] = (byte)(intValue & 0xFF);           // 获取最低字节
            buffer[startIndex + 1] = (byte)((intValue >> 8) & 0xFF);  // 获取第二个字节
            buffer[startIndex + 2] = (byte)((intValue >> 16) & 0xFF); // 获取第三个字节
            buffer[startIndex + 3] = (byte)((intValue >> 24) & 0xFF); // 获取最高字节
        }
    }
}
