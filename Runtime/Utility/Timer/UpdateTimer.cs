using System;

namespace ToolKit
{
    internal sealed class UpdateTimer : AbstractTimer
    {
        private readonly Action callback;


        public UpdateTimer(
            float interval,
            int repeatCount,
            Action callback)
            : base(interval, repeatCount)
        {
            this.callback = callback;
        }


        protected override void Execute()
        {
            callback?.Invoke();
        }
    }

}
