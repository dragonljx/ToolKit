// 自由飞行控制工具
using UnityEngine;
namespace ToolKit
{
    /// <summary>
    /// 编辑器风格的自由飞行摄像机控制器，用于在场景里快速巡览
    /// <para>用法：把组件挂到 <c>Camera</c> 上直接运行，用 WASD + 右键拖拽操作</para>
    /// <code>
    /// // 挂到相机所在的 GameObject 上即可，不需要额外代码
    /// // 运行后：
    /// //   W / S        前进 / 后退
    /// //   A / D        左移 / 右移
    /// //   Q / E        下降 / 上升
    /// //   Left Shift   加速 10 倍
    /// //   鼠标右键按住  拖拽转视角
    /// //   鼠标滚轮      调整整体速度倍率
    /// //   Escape       退出应用（编辑器里是停止播放）
    /// </code>
    /// <para>移动与转向都做了与帧率无关的插值平滑，低帧率下也不会出现一顿一顿的手感</para>
    /// </summary>
    /// <remarks>
    /// <para>关于输入系统（重要）：本类直接使用旧版 <c>UnityEngine.Input</c>，
    /// <strong>没有做新 Input System 的兼容</strong>。</para>
    /// <para>如果项目在 Player Settings 里把 <c>Active Input Handling</c> 设成了
    /// 「Input System Package (New)」，本组件会完全没有响应或直接报错，
    /// 需要改成 <c>Both</c> 才能使用。</para>
    /// <para>包里的 <c>InputManager</c> 是两套输入系统都支持的，本类是为了保持示例简洁而没做适配。</para>
    /// <para>关于 <c>Escape</c> 退出：按下会直接调用 <c>Application.Quit()</c>，在编辑器里则停止播放模式。
    /// 这是 Unity 官方示例的原始行为，用于接入自己的项目时建议去掉这段，否则会与游戏内的
    /// 「按 Esc 呼出菜单」冲突。</para>
    /// <para>关于 <c>boost</c>：鼠标滚轮会持续累加到该值上，<strong>没有上下限钳制</strong>，
    /// 长时间滚动后移动速度会变得极大或极小，需要时可自行加 <c>Mathf.Clamp</c>。</para>
    /// <para>关于 <c>OnEnable</c>：启用时会用当前 Transform 的位姿重置内部状态，
    /// 因此禁用再启用不会保留之前的移动速度累积。</para>
    /// </remarks>
    public class SimpleCameraController : MonoBehaviour
    {
        /// <summary>
        /// 摄像机的内部状态：欧拉角与位置分离保存
        /// <para>分开存是因为插值要分别对旋转和位移使用不同的平滑系数，
        /// 直接对 <c>Quaternion</c> 做线性插值会有角度插值上的问题</para>
        /// </summary>
        class CameraState
        {
            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            /// <summary>
            /// 从 Transform 读取当前位姿，转换为内部状态
            /// </summary>
            /// <param name="t">数据来源的 Transform</param>
            public void SetFromTransform(Transform t)
            {
                pitch = t.eulerAngles.x;
                yaw = t.eulerAngles.y;
                roll = t.eulerAngles.z;
                x = t.position.x;
                y = t.position.y;
                z = t.position.z;
            }

            /// <summary>
            /// 按当前朝向平移状态中的位置
            /// </summary>
            /// <param name="translation">本地空间下的位移量（右、上、前方向）</param>
            /// <remarks>
            /// <para>先用当前朝向把本地位移旋转到世界空间再累加，因此「前进」永远沿着视线方向，
            /// 而不是世界坐标的 Z 轴。</para>
            /// </remarks>
            public void Translate(Vector3 translation)
            {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;

                x += rotatedTranslation.x;
                y += rotatedTranslation.y;
                z += rotatedTranslation.z;
            }

            /// <summary>
            /// 向目标状态插值逼近，产生平滑的跟随效果
            /// </summary>
            /// <param name="target">目标状态</param>
            /// <param name="positionLerpPct">本帧位置插值比例，由调用方按帧率无关的方式算好</param>
            /// <param name="rotationLerpPct">本帧旋转插值比例</param>
            /// <remarks>
            /// <para>角度是用 <c>Mathf.Lerp</c> 直接插值的，没有做 360° 环绕处理，
            /// 因此 yaw 累积超过一圈后可能出现绕远路的现象，对自由飞行相机影响不大。</para>
            /// </remarks>
            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);

                x = Mathf.Lerp(x, target.x, positionLerpPct);
                y = Mathf.Lerp(y, target.y, positionLerpPct);
                z = Mathf.Lerp(z, target.z, positionLerpPct);
            }

            /// <summary>
            /// 把内部状态写回 Transform
            /// </summary>
            /// <param name="t">要更新的 Transform</param>
            public void UpdateTransform(Transform t)
            {
                t.eulerAngles = new Vector3(pitch, yaw, roll);
                t.position = new Vector3(x, y, z);
            }
        }

        /// <summary>输入直接驱动的目标状态，代表「理想中应该到达的位置」</summary>
        CameraState m_TargetCameraState = new CameraState();

        /// <summary>实际渲染用的状态，逐帧向目标状态插值逼近，让移动有惯性感</summary>
        CameraState m_InterpolatingCameraState = new CameraState();

        /// <summary>
        /// 移动速度的指数倍率，实际速度是 <c>2^boost</c>
        /// <para>鼠标滚轮会在运行中修改它（每格 ±0.2），因此不需要在 Inspector 里精调</para>
        /// <para>注意该值没有上下限，长时间滚动会累积到很大或很小</para>
        /// </summary>
        [Header("Movement Settings")]
        [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float boost = 3.5f;

        /// <summary>
        /// 位置插值到目标值 99% 所需的时间（秒）
        /// <para>数值越大移动越「拖沓」，越小越跟手；默认 0.2</para>
        /// </summary>
        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float positionLerpTime = 0.2f;

        /// <summary>
        /// 鼠标灵敏度曲线：横轴是本帧鼠标移动的距离，纵轴是灵敏度倍率
        /// <para>默认曲线在快速移动时灵敏度更高，慢速微调时更精细</para>
        /// </summary>
        [Header("Rotation Settings")]
        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        /// <summary>
        /// 旋转插值到目标值 99% 所需的时间（秒）
        /// <para>默认 0.01，明显小于位置插值时间，保证转视角跟手</para>
        /// </summary>
        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float rotationLerpTime = 0.01f;

        /// <summary>
        /// 是否反转鼠标 Y 轴的转向方向
        /// <para>默认 false（鼠标上推视角上抬），习惯反向操作的玩家可勾选</para>
        /// </summary>
        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool invertY = false;

        /// <summary>
        /// 启用时用当前 Transform 的位姿重置内部状态，保证从任意位置接手都不会跳变
        /// </summary>
        void OnEnable()
        {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
        }

        /// <summary>
        /// 汇总本帧的移动方向
        /// </summary>
        /// <returns>未归一化的方向向量，同时按下多个键时会叠加（斜向移动更快）</returns>
        /// <remarks>
        /// <para>返回值没有 <c>Normalize</c>，所以同时按 W 和 D 的移动速度约为单键的 1.41 倍。
        /// 对自由相机来说手感更自然，但需要匀速移动时要自行归一化。</para>
        /// </remarks>
        Vector3 GetInputTranslationDirection()
        {
            Vector3 direction = new Vector3();
            if (Input.GetKey(KeyCode.W))
            {
                direction += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                direction += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A))
            {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                direction += Vector3.right;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                direction += Vector3.down;
            }
            if (Input.GetKey(KeyCode.E))
            {
                direction += Vector3.up;
            }
            return direction;
        }

        /// <summary>
        /// 每帧读取输入，更新目标状态并向其插值逼近
        /// </summary>
        /// <remarks>
        /// <para>插值比例用 <c>1 - exp(log(0.01) / t * deltaTime)</c> 计算，
        /// 保证在任何帧率下「经过 <c>lerpTime</c> 秒后都到达 99%」，而不是简单的固定比例。</para>
        /// </remarks>
        void Update()
        {
            // Exit Sample

            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
            // Hide and lock cursor when right mouse button pressed
            if (Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // Unlock and show cursor when right mouse button released
            if (Input.GetMouseButtonUp(1))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // Rotation
            if (Input.GetMouseButton(1))
            {
                var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));

                var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
                m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
            }

            // Translation
            var translation = GetInputTranslationDirection() * Time.deltaTime;

            // Speed up movement when shift key held
            if (Input.GetKey(KeyCode.LeftShift))
            {
                translation *= 10.0f;
            }

            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            boost += Input.mouseScrollDelta.y * 0.2f;
            translation *= Mathf.Pow(2.0f, boost);

            m_TargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

            m_InterpolatingCameraState.UpdateTransform(transform);
        }
    }

}
