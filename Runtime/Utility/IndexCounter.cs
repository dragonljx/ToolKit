using System;

namespace ToolKit
{
    /// <summary>
    /// 环形索引计数器，在 <c>0 ~ Length-1</c> 之间移动并自动处理边界
    /// <para>用法：适合轮播图、列表选中项、回合制队伍轮转等「下一个 / 上一个」的切换场景</para>
    /// <code>
    /// // 创建一个长度为 5 的循环计数器，起始索引为 0
    /// IndexCounter counter = new IndexCounter(5);
    ///
    /// // 基本操作
    /// int next = counter.Next();        // 获取下一个索引（1）
    /// int prev = counter.Prev();        // 获取上一个索引（0）
    ///
    /// // 重置操作
    /// counter.ResetIndex();             // 重置到索引 0
    /// counter.ResetIndex(2);            // 重置到索引 2
    ///
    /// // 修改配置
    /// counter.SetLength(10, true);      // 修改长度为 10，保持当前索引
    /// counter.ResetAll(10, 0, IndexCounter.Mode.Loop); // 完整重置：长度 10，起始索引 0，模式 Loop
    ///
    /// // 安全移动：Clamp 模式下到达边界会返回 false，不会被静默吞掉
    /// if (counter.TryNext(out int newIndex))
    /// {
    ///     DLogger.Log($"成功移动到: {newIndex}");
    /// }
    /// else
    /// {
    ///     DLogger.Log("已到达边界，无法继续移动");
    /// }
    /// </code>
    /// <para>索引永远保证落在 <c>[0, Length - 1]</c>：构造时传入越界的起始索引会被自动收敛到边界，</para>
    /// <para>缩短长度时当前索引同样会被收敛，不会出现悬空索引</para>
    /// </summary>
    /// <remarks>
    /// <para>关于两种模式：</para>
    /// <list type="bullet">
    /// <item><see cref="Mode.Loop"/>：越界后回绕，<c>Next</c> 从末尾回到 0，<c>Prev</c> 从 0 回到末尾</item>
    /// <item><see cref="Mode.Clamp"/>：越界后停在边界，<c>Next</c> 停在末尾，<c>Prev</c> 停在 0</item>
    /// <item>无论哪种模式，<c>Next</c> / <c>Prev</c> 都<strong>不会</strong>报错或抛出异常，区别只在返回值</item>
    /// <item>需要知道「是否真的移动了」时，用 <see cref="TryNext"/> / <see cref="TryPrev"/> 而不是比较前后值</item>
    /// </list>
    /// <para>关于线程：本类不含任何同步机制，也不是线程安全的，请勿跨线程共享同一个实例。</para>
    /// </remarks>
    public sealed class IndexCounter
    {
        /// <summary>
        /// 索引的移动模式
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// 越界后回绕。
            /// Next 从最后一个回到 0，Prev 从 0 回到最后一个。
            /// </summary>
            Loop,

            /// <summary>
            /// 到达边界后停在边界。
            /// Next 停在最后一个，Prev 停在第一个。
            /// </summary>
            Clamp
        }

        /// <summary>
        /// 当前索引，取值范围恒为 <c>[0, Length - 1]</c>
        /// <para>只能通过 <see cref="Next"/> / <see cref="Prev"/> / <see cref="ResetIndex"/> 等方法修改</para>
        /// </summary>
        public int CurrentIndex { get; private set; }

        /// <summary>
        /// 索引的总长度，即索引的上界为 <c>Length - 1</c>
        /// <para>只能通过 <see cref="SetLength"/> / <see cref="ResetAll"/> 修改，取值恒大于 0</para>
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// 当前的移动模式（Loop 或 Clamp）
        /// <para>可随时修改，修改后立即影响下一次 <see cref="Next"/> / <see cref="Prev"/> 的行为</para>
        /// </summary>
        public Mode CounterMode { get; set; }

        /// <summary>
        /// 是否位于第一个索引（<c>CurrentIndex == 0</c>）
        /// </summary>
        public bool IsAtStart => CurrentIndex == 0;

        /// <summary>
        /// 是否位于最后一个索引（<c>CurrentIndex == Length - 1</c>）
        /// </summary>
        public bool IsAtEnd => CurrentIndex == Length - 1;

        /// <summary>
        /// 创建一个索引计数器
        /// <code>
        /// // 默认：长度 5，从索引 0 开始，循环模式
        /// var loop = new IndexCounter(5);
        ///
        /// // 从中间的索引 2 开始，到达边界时停住
        /// var clamp = new IndexCounter(5, startIndex: 2, mode: IndexCounter.Mode.Clamp);
        /// </code>
        /// </summary>
        /// <param name="length">索引总数，必须大于 0</param>
        /// <param name="startIndex">起始索引，默认 0；越界时会被收敛到 <c>[0, length - 1]</c> 内</param>
        /// <param name="mode">移动模式，默认 <see cref="Mode.Loop"/></param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 不大于 0 时抛出</exception>
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
        /// 重置索引，不修改长度和模式
        /// <code>
        /// counter.ResetIndex();    // 回到索引 0
        /// counter.ResetIndex(3);   // 跳到索引 3
        /// </code>
        /// </summary>
        /// <param name="startIndex">目标索引，默认 0；越界时会被收敛到 <c>[0, Length - 1]</c> 内</param>
        public void ResetIndex(int startIndex = 0)
        {
            CurrentIndex = ClampIndex(startIndex);
        }

        /// <summary>
        /// 重置长度，并可选择是否保留当前索引
        /// <para>用于列表增删后同步更新计数器的容量，不会丢失当前位置</para>
        /// <code>
        /// // 列表新增一项，长度 +1 并保持当前选中项
        /// counter.SetLength(counter.Length + 1, keepCurrentIndex: true);
        ///
        /// // 换了整套数据，长度改为 8 并回到索引 0
        /// counter.SetLength(8, keepCurrentIndex: false);
        /// </code>
        /// </summary>
        /// <param name="length">新的索引总数，必须大于 0</param>
        /// <param name="keepCurrentIndex">是否保持当前索引，默认 true</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 不大于 0 时抛出</exception>
        /// <remarks>
        /// <para>关于 <paramref name="keepCurrentIndex"/> 为 true 时的收敛行为：</para>
        /// <para>新长度小于当前索引时，当前索引会被收敛到 <c>length - 1</c>，</para>
        /// <para>而不是取模回绕，因此缩短长度不会让你「意外跳到」列表中间。</para>
        /// </remarks>
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
        /// 一次性重置长度、起始索引和模式
        /// <code>
        /// counter.ResetAll(10, 0, IndexCounter.Mode.Loop);
        /// </code>
        /// </summary>
        /// <param name="length">新的索引总数，必须大于 0</param>
        /// <param name="startIndex">新的起始索引，默认 0；越界时会被收敛</param>
        /// <param name="mode">新的移动模式，默认 <see cref="Mode.Loop"/></param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 不大于 0 时抛出</exception>
        /// <remarks>
        /// <para>等价于依次调用 <c>SetLength(length, false)</c> + 设置模式 + <c>ResetIndex(startIndex)</c>，</para>
        /// <para>需要改多项配置时用它比逐个调用更清晰。</para>
        /// </remarks>
        public void ResetAll(
            int length,
            int startIndex = 0,
            Mode mode = Mode.Loop)
        {
            SetLength(length, false);
            CounterMode = mode;
            CurrentIndex = ClampIndex(startIndex);
        }

        /// <summary>
        /// 移动到下一个索引并返回
        /// <para>Loop 模式下从末尾回绕到 0；Clamp 模式下停在末尾，重复调用不会报错</para>
        /// <code>
        /// int index = counter.Next();
        /// ShowItem(items[index]);
        /// </code>
        /// </summary>
        /// <returns>移动后的当前索引；已在边界且模式为 Clamp 时返回边界值本身</returns>
        /// <remarks>
        /// <para>本方法总是「假装移动成功」，无法区分「真的移动了」和「停在边界」。</para>
        /// <para>需要区分时请改用 <see cref="TryNext"/>。</para>
        /// </remarks>
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

        /// <summary>
        /// 移动到上一个索引并返回
        /// <para>Loop 模式下从 0 回绕到末尾；Clamp 模式下停在 0</para>
        /// <code>
        /// int index = counter.Prev();
        /// ShowItem(items[index]);
        /// </code>
        /// </summary>
        /// <returns>移动后的当前索引；已在边界且模式为 Clamp 时返回边界值本身</returns>
        /// <remarks>
        /// <para>与 <see cref="Next"/> 同样无法区分「真的移动了」和「停在边界」，需要区分时用 <see cref="TryPrev"/>。</para>
        /// </remarks>
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
        /// 尝试移动到下一个索引，并告知是否真的发生了移动
        /// <para>Clamp 模式下已经在末尾时返回 false，且<strong>不会修改当前索引</strong></para>
        /// <code>
        /// // 列表翻页：到底了就禁用按钮
        /// if (counter.TryNext(out int index))
        /// {
        ///     ShowPage(index);
        /// }
        /// else
        /// {
        ///     nextButton.interactable = false;
        /// }
        /// </code>
        /// </summary>
        /// <param name="newIndex">移动后的索引；返回 false 时输出的是未改变的当前索引</param>
        /// <returns>发生了移动返回 true；Clamp 模式下已在末尾返回 false</returns>
        /// <remarks>
        /// <para>关于 Loop 模式：由于总是能回绕，该模式下几乎总是返回 true。</para>
        /// <para>唯一例外是长度为 1 的情况，此时 <c>Next</c> 把索引从 0 绕回 0，</para>
        /// <para>返回值仍是 true，但索引值没有变化，调用方不应把返回值当作「值变了」的依据。</para>
        /// </remarks>
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
        /// 尝试移动到上一个索引，并告知是否真的发生了移动
        /// <para>Clamp 模式下已经在开头时返回 false，且<strong>不会修改当前索引</strong></para>
        /// <code>
        /// if (counter.TryPrev(out int index))
        /// {
        ///     ShowPage(index);
        /// }
        /// </code>
        /// </summary>
        /// <param name="newIndex">移动后的索引；返回 false 时输出的是未改变的当前索引</param>
        /// <returns>发生了移动返回 true；Clamp 模式下已在开头返回 false</returns>
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

        /// <summary>
        /// 把任意索引收敛到 <c>[0, Length - 1]</c> 区间内
        /// </summary>
        /// <param name="value">待收敛的索引，允许为负数或超过长度</param>
        /// <returns>收敛后的合法索引</returns>
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
