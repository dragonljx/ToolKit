using System;

namespace ToolKit
{
    internal sealed class SequenceTimer : AbstractTimer
    {
        private readonly Action[] actions;

        private int index;

        /// <param name="interval">间隔时间（秒），必须大于 0</param>
        /// <param name="actions">要顺序执行的 Action 数组，不可为 null 或空</param>
        /// <param name="loopCount">循环次数：1 = 只跑一遍就停，<=0 = 无限循环</param>
        /// <exception cref="ArgumentException">actions 为 null 或空时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">interval <= 0 时抛出</exception>
        public SequenceTimer(
            float interval,
            Action[] actions,
            int loopCount = 1)
            : base(interval, CalculateRepeatCount(actions, loopCount))
        {
            if (actions == null || actions.Length == 0)
                throw new ArgumentException(
                    "actions 不能为 null 或空数组", nameof(actions));

            // 拷贝一份，避免外部修改影响计时器
            this.actions = (Action[])actions.Clone();
        }

        /// <summary>计算 base 构造函数所需的 repeatCount</summary>
        private static int CalculateRepeatCount(Action[] actions, int loopCount)
        {
            if (actions == null || actions.Length == 0)
                throw new ArgumentException(
                    "actions 不能为 null 或空数组", nameof(actions));

            if (loopCount <= 0)
                return -1; // 无限循环

            checked
            {
                return loopCount * actions.Length;
            }
        }

        protected override void Execute()
        {
            actions[index]?.Invoke();

            index++;

            if (index >= actions.Length)
            {
                index = 0;
            }
        }

        public override void Reset()
        {
            base.Reset();

            index = 0;
        }
    }
}
