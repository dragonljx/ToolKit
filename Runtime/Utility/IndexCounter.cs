using System;

namespace ToolKit
{
    /// <summary>
    /// 索引计数器。
    /// <para><b>使用示例：</b></para>
    /// <code>
    /// // 创建一个长度为 5 的循环计数器，起始索引为 0
    /// IndexCounter counter = new IndexCounter(5);
    /// 
    /// // 基本操作
    /// int next = counter.Next();        // 获取下一个索引（1）
    /// int prev = counter.Prev();        // 获取上一个索引（0）
    /// 
    /// // 重置操作
    /// counter.Reset();                  // 重置到索引 0
    /// counter.ResetIndex(2);            // 重置到索引 2
    /// 
    /// // 修改配置
    /// counter.SetLength(10, true);      // 修改长度为 10，保持当前索引
    /// counter.ResetAll(10, 0, Mode.Loop); // 完整重置：长度10，起始索引0，模式Loop
    /// 
    /// // 安全移动（适用于 Clamp 模式）
    /// if (counter.TryNext(out int newIndex))
    /// {
    ///     Console.WriteLine($"成功移动到: {newIndex}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("已到达边界，无法继续移动");
    /// }
    /// </code>
    /// 
    /// </summary>
    public sealed class IndexCounter
    {
        public enum Mode
        {
            Loop,

            /// <summary>
            /// 到达边界后停在边界。
            /// Next 停在最后一个，Prev 停在第一个。
            /// </summary>
            Clamp
        }

        public int CurrentIndex { get; private set; }

        public int Length { get; private set; }

        public Mode CounterMode { get; set; }

        public bool IsAtStart => CurrentIndex == 0;

        public bool IsAtEnd => CurrentIndex == Length - 1;

        public IndexCounter(
            int length,
            int startIndex = 0,
            Mode mode = Mode.Loop)
        {
            SetLength(length, false);
            CounterMode = mode;
            CurrentIndex = ClampIndex(startIndex);
        }

        /// <summary>
        /// 重置索引，不修改长度和模式。
        /// </summary>
        public void ResetIndex(int startIndex = 0)
        {
            CurrentIndex = ClampIndex(startIndex);
        }

        /// <summary>
        /// 重置长度，并可选择是否保留当前索引。
        /// </summary>
        public void SetLength(int length, bool keepCurrentIndex = true)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than 0.");

            Length = length;

            if (keepCurrentIndex)
            {
                CurrentIndex = ClampIndex(CurrentIndex);
            }
            else
            {
                CurrentIndex = 0;
            }
        }

        /// <summary>
        /// 重置长度、索引和模式。
        /// </summary>
        public void ResetAll(
            int length,
            int startIndex = 0,
            Mode mode = Mode.Loop)
        {
            SetLength(length, false);
            CounterMode = mode;
            CurrentIndex = ClampIndex(startIndex);
        }

        public int Next()
        {
            if (CurrentIndex + 1 >= Length)
            {
                CurrentIndex = CounterMode == Mode.Loop
                    ? 0
                    : Length - 1;
            }
            else
            {
                CurrentIndex++;
            }

            return CurrentIndex;
        }

        public int Prev()
        {
            if (CurrentIndex - 1 < 0)
            {
                CurrentIndex = CounterMode == Mode.Loop
                    ? Length - 1
                    : 0;
            }
            else
            {
                CurrentIndex--;
            }

            return CurrentIndex;
        }

        /// <summary>
        /// 尝试移动到下一个。
        /// Clamp 模式下，如果已经在末尾，则返回 false。
        /// Loop 模式下，通常都会返回 true。
        /// </summary>
        public bool TryNext(out int newIndex)
        {
            if (CounterMode == Mode.Clamp && IsAtEnd)
            {
                newIndex = CurrentIndex;
                return false;
            }

            newIndex = Next();
            return true;
        }

        /// <summary>
        /// 尝试移动到上一个。
        /// Clamp 模式下，如果已经在开头，则返回 false。
        /// Loop 模式下，通常都会返回 true。
        /// </summary>
        public bool TryPrev(out int newIndex)
        {
            if (CounterMode == Mode.Clamp && IsAtStart)
            {
                newIndex = CurrentIndex;
                return false;
            }

            newIndex = Prev();
            return true;
        }

        private int ClampIndex(int value)
        {
            if (value < 0)
                return 0;

            if (value >= Length)
                return Length - 1;

            return value;
        }
    }
}