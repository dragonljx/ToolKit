using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 挂载即生效的 FPS 统计组件，按秒采样并记录平均、最低、最高帧率
    /// <para>用法：把组件挂到场景中任意常驻物体上即可，四项数值都会显示在 Inspector 里实时刷新</para>
    /// <code>
    /// // 代码里读取
    /// FPS fps = GetComponent&lt;FPS&gt;();
    /// DLogger.Log("Perf", $"当前 {fps.Fps:0.0} / 平均 {fps.FpsAvg:0.0} / 最低 {fps.FpsMin:0.0}");
    ///
    /// // 换了场景或做完一段性能测试后重新开始统计
    /// fps.Reset();
    /// </code>
    /// <para>统计结果每累计满 1 秒更新一次，因此刚挂上组件的前 1 秒内四项数值都还是 0</para>
    /// </summary>
    /// <remarks>
    /// <para>关于 <c>FpsAvg</c> 的口径：</para>
    /// <list type="bullet">
    /// <item>它不是「开测至今的累计平均值」，而是<strong>相邻两次采样值的两点滑动平均</strong>：
    /// <c>fpsAvg = (fpsAvg + fps) / 2</c></item>
    /// <item>因此它对最近的变化更敏感，但受早期采样影响会长期残留，不能当作精确的长期均值使用</item>
    /// <item><c>FpsMin</c> / <c>FpsMax</c> 则是在 <see cref="Reset"/> 之后真正的历史极值，只增不减（极值）</item>
    /// </list>
    /// <para>关于 <see cref="Reset"/>：方法名与 Unity 的 <c>MonoBehaviour.Reset</c> 消息一致，</para>
    /// <para>因此在 Inspector 中对组件执行 Reset 时也会触发，属于预期行为。</para>
    /// </remarks>
    public class FPS : MonoBehaviour
    {
        protected float frameCount;
        protected float elapsedTime;

        [SerializeField]
        private float fps;

        [SerializeField]
        private float fpsAvg;

        [SerializeField]
        private float fpsMin;

        [SerializeField]
        private float fpsMax;

        private bool isInitialized;


        /// <summary>
        /// 最近一秒的帧率
        /// <para>每满 1 秒刷新一次，挂载后的前 1 秒内为 0</para>
        /// </summary>
        public float Fps { get { return this.fps; } }

        /// <summary>
        /// 帧率的滑动平均值
        /// <para>算法为 <c>(上次平均值 + 本次采样) / 2</c>，偏向反映近期表现，不等同于累计平均</para>
        /// </summary>
        public float FpsAvg { get { return this.fpsAvg; } }

        /// <summary>
        /// 自上次 <see cref="Reset"/> 以来的最低帧率
        /// <para>用于发现偶发卡顿，注意它会被单次极端值长期拉低</para>
        /// </summary>
        public float FpsMin { get { return this.fpsMin; } }

        /// <summary>
        /// 自上次 <see cref="Reset"/> 以来的最高帧率
        /// <para>通常受加载过程或垂直同步设置影响，参考价值低于 <see cref="FpsMin"/></para>
        /// </summary>
        public float FpsMax { get { return this.fpsMax; } }

        /// <summary>
        /// 累计本秒的帧数与耗时，满 1 秒时结算一次帧率并更新统计值
        /// </summary>
        private void Update()
        {
            // NOTE:
            // Maybe this is not strict.
            // this.fps = 1 / Time.deltaTime;

            this.frameCount += 1;
            this.elapsedTime += Time.deltaTime;

            if (this.elapsedTime > 1f)
            {
                this.fps = this.frameCount / this.elapsedTime;
                this.frameCount = 0;
                this.elapsedTime = 0;

                if (this.isInitialized)
                {
                    this.fpsAvg = (this.fpsAvg + this.fps) / 2;
                    this.fpsMin = Mathf.Min(this.fpsMin, this.fps);
                    this.fpsMax = Mathf.Max(this.fpsMax, this.fps);
                }
                else
                {
                    this.isInitialized = true;

                    this.fpsAvg = this.fps;
                    this.fpsMin = this.fps;
                    this.fpsMax = this.fps;
                }
            }
        }
        // private void OnGUI()
        // {
        //     GUI.Label(new Rect(Screen.width / 2f, 10, 100, 50), $"FPS: {fps:0.00}");
        //     GUI.Label(new Rect(Screen.width / 2f, 25, 100, 50), $"AVG: {fpsAvg:0.00}");
        //     GUI.Label(new Rect(Screen.width / 2f, 40, 100, 50), $"Min: {fpsMin:0.00}");
        //     GUI.Label(new Rect(Screen.width / 2f, 55, 100, 50), $"Max: {fpsAvg:0.00}");
        // }

        /// <summary>
        /// 清空累计数据与全部统计值，重新开始统计
        /// <para>四项帧率数值都会被置为 0，直到下一个满 1 秒的采样点才会重新写入</para>
        /// </summary>
        /// <remarks>
        /// <para>方法名与 Unity 的 <c>MonoBehaviour.Reset</c> 消息重名，在 Inspector 里对组件执行 Reset 时也会调用到这里，</para>
        /// <para>这正是期望行为：编辑器里重置组件即重置统计。子类重写时请保留 <c>virtual</c> 并调用 <c>base.Reset()</c>。</para>
        /// </remarks>
        public virtual void Reset()
        {
            this.frameCount = 0;
            this.elapsedTime = 0;

            this.isInitialized = false;

            this.fps = 0;
            this.fpsAvg = 0;
            this.fpsMin = 0;
            this.fpsMax = 0;
        }
    }
}
