using System;
using System.Collections;
using UnityEngine;

namespace ToolKit
{

    public class ToolTexture
    {
#if SCREEN_CAPTURE
        public static IEnumerator Capture(Action<Texture2D> callback, Rect rect = new Rect())
        {
            yield return new WaitForEndOfFrame();
            Texture2D view = ScreenCapture.CaptureScreenshotAsTexture();
            if (rect.width != 0 || rect.height != 0)
            {
                Texture2D result = ToolTexture.CropTexture(view, rect);
                callback(result);
                GameObject.Destroy(result);
            }
            else
            {
                callback(view);
            }
            GameObject.Destroy(view);
        }
#endif

        /// <summary>
        /// 两张图合并，同时裁剪前图
        /// </summary>
        /// <param name="back"></param>
        /// <param name="backOffsetX">前图在后图上的偏移X</param>
        /// <param name="backOffsetY">前图在后图上的偏移Y</param>
        /// <param name="front"></param>
        /// <param name="frontOffsetX">前图使用内容的X偏移</param>
        /// <param name="frontOffsetY">前图使用内容的Y偏移</param>
        /// <param name="frontWidth">前图使用的宽</param>
        /// <param name="frontHeight">前图使用的高</param>
        /// <returns></returns>
        public static Texture2D MergeTexture(Texture2D back, int backOffsetX, int backOffsetY, Texture2D front, int frontOffsetX, int frontOffsetY, int frontWidth, int frontHeight)
        {
            Texture2D texture = new Texture2D(back.width, back.height, TextureFormat.RGBA32, false);

            Graphics.CopyTexture(back, 0, texture, 0);
            Graphics.CopyTexture(front, 0, 0, frontOffsetX, frontOffsetY, frontWidth, frontHeight,
                texture, 0, 0, backOffsetX, backOffsetY);

            return texture;
        }

        /// <summary>
        /// 两张图合并，同时裁剪前图
        /// </summary>
        /// <param name="back"></param>
        /// <param name="backOffsetX">前图在后图上的偏移X</param>
        /// <param name="backOffsetY">前图在后图上的偏移Y</param>
        /// <param name="front"></param>
        /// <param name="frontOffsetX">前图使用内容的X偏移</param>
        /// <param name="frontOffsetY">前图使用内容的Y偏移</param>
        /// <param name="frontWidth">前图使用的宽</param>
        /// <param name="frontHeight">前图使用的高</param>
        /// <returns></returns>
        public static Texture2D MergeTextureAlpha(Texture2D back, Texture2D front, int backOffsetX, int backOffsetY)
        {
            ComputeShader shader = Resources.Load<ComputeShader>("ComputeShader/AddTestureCS");
            //ComputeShader shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/dragon.tools/Runtime/ComputeShader/AddTestureCS.compute");
            RenderTexture renderTex = new RenderTexture(back.width, back.height, 32);
            Texture2D texture = new Texture2D(back.width, back.height, TextureFormat.RGBA32, false);

            int kernelHandle = shader.FindKernel("CSMain");
            renderTex.enableRandomWrite = true;
            renderTex.Create();

            // [0] width [1] height [2] offsetX [3] offsetY
            shader.SetInt("heightB", back.height);
            shader.SetInt("heightF", front.height);
            shader.SetInt("offsetX", backOffsetX);
            shader.SetInt("offsetY", backOffsetY);

            shader.SetTexture(kernelHandle, "Result", renderTex);
            shader.SetTexture(kernelHandle, "FrontBuffer", front);
            shader.SetTexture(kernelHandle, "BackBuffer", back);
            shader.Dispatch(kernelHandle, back.width / 8, back.height / 8, 1);


            RenderTexture.active = renderTex;
            texture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            return texture;
        }
        /// <summary>
        /// 缩放Textur2D
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            source.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            var nTex = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            nTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            nTex.Apply();
            RenderTexture.active = null;
            return nTex;

        }
        /// <summary>
        /// 裁剪 Texture2D
        /// </summary>
        /// <param name="source"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public static Texture2D CropTexture(Texture2D source, float offsetX, float offsetY, float newWidth, float newHeight)
        {
            source.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            var nTex = new Texture2D((int)newWidth, (int)newHeight, TextureFormat.RGB24, false);
            nTex.ReadPixels(new Rect(offsetX, offsetY, newWidth, newHeight), 0, 0);
            nTex.Apply();
            RenderTexture.active = null;
            return nTex;
        }
        /// <summary>
        /// 裁剪 Texture2D
        /// </summary>
        /// <param name="source"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public static Texture2D CropTexture(Texture2D source, Rect rect)
        {
            source.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 24,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            rt.filterMode = FilterMode.Point;
            Graphics.Blit(source, rt);

            var nTex = new Texture2D((int)rect.width, (int)rect.height, source.format, false);
            RenderTexture.active = rt;
            nTex.ReadPixels(rect, 0, 0);
            nTex.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return nTex;
        }

    }
}
