using System;
using System.Collections;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 纹理的截取、合并、缩放与裁剪工具
    /// <para>用法：除 <see cref="Capture"/> 是协程外都是同步方法；除 <see cref="Capture"/> 外都需要显式传入源纹理</para>
    /// <code>
    /// // 截图（需要 com.unity.modules.screencapture 模块）
    /// yield return ToolTexture.Capture(screenshot =>
    /// {
    ///     // 拿到的纹理由调用方负责释放
    ///     SaveScreenshot(screenshot);
    ///     ToolTexture.Release(screenshot);
    /// });
    ///
    /// // 缩放与裁剪，同样返回新纹理
    /// Texture2D small = ToolTexture.ResizeTexture(source, 256, 256);
    /// Texture2D icon = ToolTexture.CropTexture(source, new Rect(0, 0, 128, 128));
    /// </code>
    /// <para>所有生成纹理的方法都会新建一块 <c>RGBA32</c> 的 <c>Texture2D</c>，不受 GC 管理，</para>
    /// <para>使用完毕后必须自行调用 <see cref="Release"/>，否则会持续占用显存</para>
    /// </summary>
    /// <remarks>
    /// <para>关于坐标系：</para>
    /// <list type="bullet">
    /// <item>所有区域参数的坐标原点都在<strong>左下角</strong>，与 Unity 的纹理坐标一致，而不是屏幕左上角</item>
    /// <item>传入 UI 或屏幕坐标系（左上角原点）的矩形时，需要先把 y 翻转：<c>y = texture.height - rect.y - rect.height</c></item>
    /// </list>
    /// <para>关于性能：</para>
    /// <para>缩放与裁剪内部走 <c>RenderTexture</c> + <c>ReadPixels</c>，是 GPU 到 CPU 的同步回读，</para>
    /// <para>在移动端可能造成明显卡顿；批量处理多张纹理时建议分散到多帧执行。</para>
    /// </remarks>
    public static class ToolTexture
    {
        #region 屏幕截图

#if SCREEN_CAPTURE
        /// <summary>
        /// 截取屏幕生成纹理（协程）
        /// <para>内部先等待本帧渲染结束再截图，因此不会截到未渲染完的画面</para>
        /// <code>
        /// // 截取全屏
        /// yield return ToolTexture.Capture(full => { /* ... */ });
        ///
        /// // 只截中间一块区域（坐标原点在左下角）
        /// yield return ToolTexture.Capture(part => { /* ... */ }, new Rect(100, 100, 200, 200));
        /// </code>
        /// </summary>
        /// <param name="callback">截图完成回调，参数为截取到的纹理；为 null 时只执行截图不回调</param>
        /// <param name="rect">要裁剪的区域，宽高都为 0（默认值）时截取整屏</param>
        /// <remarks>
        /// <para>关于返回值与资源归属：</para>
        /// <list type="bullet">
        /// <item>指定了 <paramref name="rect"/>（宽高都大于 0）时，回调收到的是裁剪后的新纹理，
        /// 内部会把整屏纹理释放掉，不必也不应再释放整屏那张</item>
        /// <item>未指定 <paramref name="rect"/> 时，回调收到的就是整屏纹理，由调用方负责 <see cref="Release"/></item>
        /// <item>必须用 <c>yield return StartCoroutine(...)</c> 或直接在协程里 <c>yield return</c> 调用</item>
        /// </list>
        /// <para>本方法仅在项目包含 <c>com.unity.modules.screencapture</c> 时编译（<c>SCREEN_CAPTURE</c> 宏）。</para>
        /// </remarks>
        public static IEnumerator Capture(Action<Texture2D> callback, Rect rect = default)
        {
            yield return new WaitForEndOfFrame();

            Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();

            if (rect.width > 0 && rect.height > 0)
            {
                Texture2D cropped = CropTexture(screenTexture, rect);
                Release(screenTexture);
                callback?.Invoke(cropped);
            }
            else
            {
                callback?.Invoke(screenTexture);
            }
        }
#endif

        #endregion

        #region 纹理合并

        /// <summary>
        /// 把一张纹理的指定区域直接拷贝到另一张纹理上（覆盖式，不做 Alpha 混合）
        /// <para>适用于图集拼合：先把各小图拷到大图的空白区域，再上传为一张图集</para>
        /// <code>
        /// // 把 icon 的 (0, 0) 起 64x64 区域贴到 atlas 的 (128, 256) 处
        /// Texture2D atlas = ToolTexture.MergeTexture(background, 128, 256, icon, 0, 0, 64, 64);
        /// </code>
        /// </summary>
        /// <param name="back">底图，决定结果尺寸</param>
        /// <param name="backOffsetX">在底图上贴入位置的水平坐标（左下角原点）</param>
        /// <param name="backOffsetY">在底图上贴入位置的垂直坐标（左下角原点）</param>
        /// <param name="front">要贴入的纹理</param>
        /// <param name="frontSourceX">源纹理被拷贝区域的水平起点（左下角原点）</param>
        /// <param name="frontSourceY">源纹理被拷贝区域的垂直起点（左下角原点）</param>
        /// <param name="frontWidth">被拷贝区域的宽度</param>
        /// <param name="frontHeight">被拷贝区域的高度</param>
        /// <returns>合并后的新纹理（尺寸与 <paramref name="back"/> 相同），使用完毕后需 <see cref="Release"/></returns>
        /// <exception cref="ArgumentNullException">任一纹理为 null 时抛出</exception>
        /// <remarks>
        /// <para>关于覆盖而非混合：本方法按像素直接替换，源纹理的 Alpha 通道<strong>不会</strong>参与计算。</para>
        /// <para>需要按 Alpha 融合时请改用 <see cref="MergeTextureAlpha"/>。</para>
        /// <para>关于参数校验：方法只检查纹理是否为 null，<strong>不检查区域是否越界</strong>。</para>
        /// <para><c>frontSourceX + frontWidth</c> 超出源纹理，或 <c>backOffsetX + frontWidth</c> 超出底图时，</para>
        /// <para><c>Graphics.CopyTexture</c> 会报错并导致结果不符合预期，调用方需自行保证区域合法。</para>
        /// <para>关于 CPU 可读性：底层是 GPU 侧的拷贝，脚本创建的纹理虽然标记为可读，</para>
        /// <para>但拷贝后 CPU 侧数据不会自动同步，直接 <c>GetPixels</c> 可能拿到旧数据；</para>
        /// <para>需要回读像素时请走 <c>RenderTexture</c> + <c>ReadPixels</c>（参考 <see cref="CropTexture"/> 的实现）。</para>
        /// </remarks>
        public static Texture2D MergeTexture(
            Texture2D back,
            int backOffsetX,
            int backOffsetY,
            Texture2D front,
            int frontSourceX,
            int frontSourceY,
            int frontWidth,
            int frontHeight)
        {
            ValidateTexture(back);
            ValidateTexture(front);

            Texture2D result = new Texture2D(back.width, back.height, TextureFormat.RGBA32, false);
            Graphics.CopyTexture(back, result);

            Graphics.CopyTexture(
                front, 0, 0, frontSourceX, frontSourceY, frontWidth, frontHeight,
                result, 0, 0, backOffsetX, backOffsetY);

            return result;
        }

        /// <summary>
        /// 用 Compute Shader 把一张纹理按 Alpha 混合叠加到另一张纹理上
        /// <para>与 <see cref="MergeTexture"/> 的区别是会计算 Alpha，可以叠加半透明的贴花、光效、签名等</para>
        /// <code>
        /// // 把半透明的签名叠到截图右下角
        /// Texture2D signed = ToolTexture.MergeTextureAlpha(screenshot, signature, x: 800, y: 40);
        /// if (signed == null)
        /// {
        ///     DLogger.LogWarning("Capture", "ComputeShader 缺失，已跳过叠加");
        /// }
        /// </code>
        /// </summary>
        /// <param name="back">底图，决定结果尺寸</param>
        /// <param name="front">要叠加的纹理，尺寸可以小于底图</param>
        /// <param name="offsetX">叠加位置的水平坐标（左下角原点）</param>
        /// <param name="offsetY">叠加位置的垂直坐标（左下角原点）</param>
        /// <param name="shader">执行混合的 ComputeShader；传 null 时从 <c>Resources/ComputeShader/AddTextureCS</c> 加载</param>
        /// <returns>混合后的新纹理（尺寸与 <paramref name="back"/> 相同），使用完毕后需 <see cref="Release"/></returns>
        /// <exception cref="ArgumentNullException">任一纹理为 null 时抛出</exception>
        /// <remarks>
        /// <para>关于 ComputeShader 的依赖（最容易踩的坑）：</para>
        /// <list type="bullet">
        /// <item>需要一个名为 <c>AddTextureCS</c> 的 ComputeShader，放在 <c>Resources/ComputeShader/</c> 下</item>
        /// <item>Shader 内必须有一个名为 <c>CSMain</c> 的 kernel</item>
        /// <item>需要三个纹理参数：<c>Result</c>（可随机写入）、<c>FrontBuffer</c>、<c>BackBuffer</c>，
        /// 以及 <c>width</c>、<c>height</c>、<c>frontWidth</c>、<c>frontHeight</c>、<c>offsetX</c>、<c>offsetY</c> 六个 int 参数</item>
        /// <item>凑齐这些条件很麻烦，若只是需要简单的叠加效果，用普通 Shader + <c>Graphics.Blit</c> 会更直接</item>
        /// <item>Shader 缺失时记录错误日志并<strong>返回 null</strong>，调用方必须判空</item>
        /// </list>
        /// <para>内部按 8x8 的线程组派发，因此宽高不是 8 的倍数时会有多余线程，
        /// 由 Shader 自身的边界判断保证不越界写入。</para>
        /// </remarks>
        public static Texture2D MergeTextureAlpha(
            Texture2D back,
            Texture2D front,
            int offsetX,
            int offsetY,
            ComputeShader shader = null)
        {
            ValidateTexture(back);
            ValidateTexture(front);

            // 获取 ComputeShader
            ComputeShader computeShader = shader ?? GetAddTextureShader();
            if (computeShader == null)
            {
                DLogger.LogError("ComputeShader 未找到，无法进行 Alpha 混合");
                return null;
            }

            RenderTextureDescriptor desc = new RenderTextureDescriptor(
                back.width, back.height, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true
            };

            RenderTexture renderTexture = new RenderTexture(desc);
            renderTexture.Create();

            Texture2D result = new Texture2D(back.width, back.height, TextureFormat.RGBA32, false);

            try
            {
                int kernel = computeShader.FindKernel("CSMain");
                computeShader.SetInt("width", back.width);
                computeShader.SetInt("height", back.height);
                computeShader.SetInt("frontWidth", front.width);
                computeShader.SetInt("frontHeight", front.height);
                computeShader.SetInt("offsetX", offsetX);
                computeShader.SetInt("offsetY", offsetY);
                computeShader.SetTexture(kernel, "Result", renderTexture);
                computeShader.SetTexture(kernel, "FrontBuffer", front);
                computeShader.SetTexture(kernel, "BackBuffer", back);
                computeShader.Dispatch(kernel, Mathf.CeilToInt(back.width / 8f), Mathf.CeilToInt(back.height / 8f), 1);

                RenderTexture old = RenderTexture.active;
                try
                {
                    RenderTexture.active = renderTexture;
                    result.ReadPixels(new Rect(0, 0, back.width, back.height), 0, 0);
                    result.Apply();
                }
                finally
                {
                    RenderTexture.active = old;
                }
            }
            finally
            {
                renderTexture.Release();
                UnityEngine.Object.Destroy(renderTexture);
            }

            return result;
        }

        #endregion

        #region 纹理变换

        /// <summary>
        /// 等比或非等比缩放到指定尺寸
        /// <para>使用双线性过滤采样，缩小细节丰富的图会有一点糊，需要更锐利的缩放请自行改用其他过滤模式</para>
        /// <code>
        /// Texture2D thumbnail = ToolTexture.ResizeTexture(original, 256, 256);
        /// </code>
        /// </summary>
        /// <param name="source">源纹理</param>
        /// <param name="width">目标宽度（像素），与原始宽高比不同会被拉伸</param>
        /// <param name="height">目标高度（像素）</param>
        /// <returns>缩放后的新纹理，使用完毕后需 <see cref="Release"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 null 时抛出</exception>
        /// <remarks>
        /// <para>若要保持宽高比，先用 <c>ToolMath.CalculateAspectRatioScale</c> 算目标尺寸。</para>
        /// <para>内部通过 <c>RenderTexture.GetTemporary</c> + <c>Graphics.Blit</c> + <c>ReadPixels</c> 实现，</para>
        /// <para>是一次 GPU 到 CPU 的同步回读，并且依赖渲染管线，只能在主线程调用。</para>
        /// </remarks>
        public static Texture2D ResizeTexture(Texture2D source, int width, int height)
        {
            ValidateTexture(source);

            RenderTexture rt = RenderTexture.GetTemporary(width, height);

            try
            {
                rt.filterMode = FilterMode.Bilinear;
                Graphics.Blit(source, rt);

                Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;

                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply();

                RenderTexture.active = previous;

                return result;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// 裁剪纹理（坐标参数重载）
        /// <para>把四个分量拆开传时更易读，行为与 <see cref="CropTexture(Texture2D, Rect)"/> 完全一致</para>
        /// </summary>
        /// <param name="source">源纹理</param>
        /// <param name="x">裁剪区域左下角的水平坐标</param>
        /// <param name="y">裁剪区域左下角的垂直坐标</param>
        /// <param name="width">裁剪区域宽度</param>
        /// <param name="height">裁剪区域高度</param>
        /// <returns>裁剪后的新纹理，使用完毕后需 <see cref="Release"/></returns>
        public static Texture2D CropTexture(Texture2D source, float x, float y, float width, float height)
        {
            return CropTexture(source, new Rect(x, y, width, height));
        }

        /// <summary>
        /// 裁剪纹理的指定区域
        /// <code>
        /// // 取左下角 128x128（注意原点在左下角）
        /// Texture2D icon = ToolTexture.CropTexture(source, new Rect(0, 0, 128, 128));
        ///
        /// // 取 UI 坐标系下的 (0, 0, 128, 128)，需要先翻转 y
        /// var uiRect = new Rect(0, 0, 128, 128);
        /// var rect = new Rect(uiRect.x, source.height - uiRect.y - uiRect.height, uiRect.width, uiRect.height);
        /// Texture2D same = ToolTexture.CropTexture(source, rect);
        /// </code>
        /// </summary>
        /// <param name="source">源纹理</param>
        /// <param name="rect">裁剪区域，坐标原点在纹理左下角，单位为像素</param>
        /// <returns>裁剪后的新纹理，尺寸为 <c>(int)rect.width</c> × <c>(int)rect.height</c>，使用完毕后需 <see cref="Release"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 null 时抛出</exception>
        /// <remarks>
        /// <para>关于参数校验：方法只检查纹理是否为 null，<strong>不校验区域是否越界</strong>。</para>
        /// <para>区域超出源纹理范围时 <c>ReadPixels</c> 会读到未定义内容（通常是黑色或残留像素）；</para>
        /// <para>宽高为负值或 0 时，创建目标纹理会抛异常。</para>
        /// <para>内部用 <c>RenderTextureReadWrite.Linear</c> 中转，因此不参与 sRGB 转换，
        /// 得到的像素值与源纹理的原始数据一致。</para>
        /// </remarks>
        public static Texture2D CropTexture(Texture2D source, Rect rect)
        {
            ValidateTexture(source);

            RenderTexture rt = RenderTexture.GetTemporary(
                source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            try
            {
                Graphics.Blit(source, rt);

                Texture2D result = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;

                result.ReadPixels(rect, 0, 0);
                result.Apply();

                RenderTexture.active = previous;

                return result;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        #endregion

        #region 资源管理

        /// <summary>
        /// 销毁纹理，释放其占用的显存
        /// <para>本类所有生成纹理的方法都需要配合本方法使用，否则纹理不会被 GC 回收</para>
        /// <code>
        /// Texture2D temp = ToolTexture.ResizeTexture(source, 64, 64);
        /// // ... 使用 temp ...
        /// ToolTexture.Release(temp);
        /// temp = null; // 避免后续误用已销毁的对象
        /// </code>
        /// </summary>
        /// <param name="texture">要销毁的纹理；传 null 时是安全的空操作</param>
        /// <remarks>
        /// <para>关于编辑模式：内部使用 <c>Object.Destroy</c>，它在编辑模式（未进入播放）下不可用，</para>
        /// <para>会输出「Destroy may not be called from edit mode」错误且纹理不会被释放。</para>
        /// <para>在编辑器工具中调用本类方法时，请改用 <c>Object.DestroyImmediate</c> 自行释放。</para>
        /// <para>已销毁的纹理引用会变成「假 null」，判空仍能拦住，但重复调用本方法会再触发一次销毁错误。</para>
        /// </remarks>
        public static void Release(Texture texture)
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        #endregion

        #region 内部工具

        private static ComputeShader _addTextureShader;

        /// <summary>
        /// 获取用于 Alpha 混合的 ComputeShader，首次调用时从 Resources 加载并缓存
        /// </summary>
        /// <returns>加载到的 ComputeShader；资源不存在时返回 null 并记录错误日志</returns>
        private static ComputeShader GetAddTextureShader()
        {
            if (_addTextureShader == null)
            {
                // 修正拼写错误：AddTestureCS -> AddTextureCS
                _addTextureShader = Resources.Load<ComputeShader>("ComputeShader/AddTextureCS");

                if (_addTextureShader == null)
                {
                    DLogger.LogError("未找到 ComputeShader: Resources/ComputeShader/AddTextureCS.compute");
                }
            }

            return _addTextureShader;
        }

        /// <summary>
        /// 校验纹理参数不为 null
        /// </summary>
        /// <param name="texture">待校验的纹理</param>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> 为 null 时抛出</exception>
        private static void ValidateTexture(Texture2D texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture), "纹理不能为空");
            }
        }

        #endregion
    }
}
