using System;
using System.Collections;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// Texture工具类
    /// 提供截图、纹理合并、缩放、裁剪、释放等功能
    /// </summary>
    public static class ToolTexture
    {
        #region 屏幕截图

#if SCREEN_CAPTURE
        /// <summary>
        /// 截取屏幕
        /// </summary>
        /// <param name="callback">截图完成回调，返回Texture2D</param>
        /// <param name="rect">裁剪区域，默认截取全屏</param>
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
        /// 合并两张纹理（直接拷贝覆盖）
        /// 将front的指定区域拷贝到back的指定位置
        /// </summary>
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
        /// 使用ComputeShader进行Alpha混合合并
        /// </summary>
        public static Texture2D MergeTextureAlpha(Texture2D back, Texture2D front, int offsetX, int offsetY)
        {
            ValidateTexture(back);
            ValidateTexture(front);
            RenderTextureDescriptor desc = new RenderTextureDescriptor(
                back.width, back.height, RenderTextureFormat.ARGB32);

            desc.enableRandomWrite = true;
            RenderTexture renderTexture = new RenderTexture(desc);
            renderTexture.Create();

            Texture2D result = new Texture2D(
                back.width, back.height, TextureFormat.RGBA32, false);
            try
            {
                ComputeShader shader = AddTextureShader;
                int kernel = shader.FindKernel("CSMain");
                shader.SetInt("width", back.width);
                shader.SetInt("height", back.height);
                shader.SetInt("frontWidth", front.width);
                shader.SetInt("frontHeight", front.height);
                shader.SetInt("offsetX", offsetX);
                shader.SetInt("offsetY", offsetY);
                shader.SetTexture(kernel, "Result", renderTexture);
                shader.SetTexture(kernel, "FrontBuffer", front);
                shader.SetTexture(kernel, "BackBuffer", back);
                shader.Dispatch(kernel, Mathf.CeilToInt(back.width / 8f), Mathf.CeilToInt(back.height / 8f), 1);
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
        /// 缩放纹理
        /// </summary>
        public static Texture2D ResizeTexture(Texture2D source, int width, int height)
        {
            ValidateTexture(source);

            RenderTexture rt = RenderTexture.GetTemporary(width, height);

            try
            {
                rt.filterMode = FilterMode.Point;
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
        /// </summary>
        public static Texture2D CropTexture(Texture2D source, float x, float y, float width, float height)
        {
            return CropTexture(source, new Rect(x, y, width, height));
        }

        /// <summary>
        /// 裁剪纹理
        /// </summary>
        public static Texture2D CropTexture(Texture2D source, Rect rect)
        {
            ValidateTexture(source);

            RenderTexture rt = RenderTexture.GetTemporary(
                source.width, source.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

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
        /// 释放纹理资源
        /// </summary>
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

        private static ComputeShader AddTextureShader
        {
            get
            {
                if (_addTextureShader == null)
                {
                    _addTextureShader = Resources.Load<ComputeShader>("ComputeShader/AddTestureCS");
                }
                return _addTextureShader;
            }
        }

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