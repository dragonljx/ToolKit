using UnityEngine;

namespace ToolKits
{
    public enum CurveLoopMode
    {
        Loop,
        PingPong,
        Clamp
    }

    public class CurveLooper
    {
        private readonly AnimationCurve curve;
        private readonly float duration;
        private readonly CurveLoopMode mode;
        public float CurrentTime { get; set; }

        public CurveLooper(AnimationCurve curve, float duration, CurveLoopMode mode = CurveLoopMode.Loop)
        {
            this.curve = curve;
            this.duration = duration;
            this.mode = mode;
        }

        /// <summary>
        /// 获取当前曲线值，自动处理循环逻辑。
        /// </summary>
        /// <param name="time">全局时间</param>
        /// <returns>曲线输出值</returns>
        public float Evaluate(float time)
        {
            switch (mode)
            {
                case CurveLoopMode.Loop:
                    CurrentTime = Mathf.Repeat(time, duration);
                    break;
                case CurveLoopMode.PingPong:
                    CurrentTime = Mathf.PingPong(time, duration);
                    break;
                case CurveLoopMode.Clamp:
                default:
                    CurrentTime = Mathf.Clamp(time, 0f, duration);
                    break;
            }

            return curve.Evaluate(CurrentTime);
        }
    }
}