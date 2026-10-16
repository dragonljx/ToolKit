using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ToolKit.Test
{
    /// <summary>
    /// <c>DLogger</c> 的手动测试组件，运行时把各类日志输出到 Console 供人眼比对
    /// <para>用法：把组件挂到场景中任意 GameObject 上，进入播放模式，然后逐条检查 Console 输出</para>
    /// <code>
    /// // 1. 新建一个空场景，创建空物体
    /// // 2. 挂上 DLoggerTest 组件
    /// // 3. 进入播放模式，Console 会依次输出八组日志
    /// // 4. 对照每组的标题检查格式是否符合预期
    /// </code>
    /// <para>覆盖的场景包括：基础日志、对象展开、集合展开、循环引用、深度截断、Unity 对象、异常、跨线程</para>
    /// </summary>
    /// <remarks>
    /// <para>关于定位：这是<strong>手动测试组件，不是自动化测试</strong>。</para>
    /// <list type="bullet">
    /// <item>它没有断言，也不会在输出不符合预期时失败，是否合格完全靠人看</item>
    /// <item>它不在任何测试框架下运行，只是因为放在 <c>ToolKit.Tests</c> 程序集里才被归类到 Test 目录</item>
    /// <item>修改 <c>DLogger</c> 的格式化逻辑后，可以用它快速做一次回归目检</item>
    /// </list>
    /// <para>关于全局副作用（重要）：<see cref="Start"/> 会直接修改 <c>DLogger</c> 的公开配置
    /// （开启帧号与时间戳、把集合上限压到 3、把深度临时改为 2），</para>
    /// <para>这些设置是<strong>进程级全局状态</strong>，会影响到场景里其他所有使用 <c>DLogger</c> 的代码。</para>
    /// <para>深度配置在 <c>TestMaxDepth</c> 结束时会还原，但帧号、时间戳和集合上限不会还原，
    /// 因此不要把它和业务代码放在同一个场景里跑。</para>
    /// <para>关于线程测试：<c>TestThreadSafe</c> 会启动 5 个后台线程同时打日志。
    /// 这些线程是<strong>非后台线程（默认 IsBackground 为 false）</strong>，不会随播放模式停止而自动结束，
    /// 若在播放模式中途退出，可能看到线程仍存活的现象。</para>
    /// </remarks>
    public class DLoggerTest : MonoBehaviour
    {
        /// <summary>
        /// 入口：按固定顺序依次执行全部测试用例
        /// <para>顺序是先基础后进阶，每一组内部以一条带 <c>==========</c> 的标题日志开始，便于在 Console 中分段</para>
        /// </summary>
        /// <remarks>
        /// <para>开头会先把 <c>ShowFrameInTag</c>、<c>ShowTimeInTag</c> 打开并把 <c>MaxCollectionItems</c> 压到 3，
        /// 目的是让这些特性的效果在输出里一眼可见。</para>
        /// </remarks>
        private void Start()
        {
            Debug.Log("========== DLogger Test ==========");
            Debug.LogWarning("========== DLogger Test ==========");
            Debug.LogError("========== DLogger Test ==========");
            DLogger.ShowFrameInTag = true;
            DLogger.ShowTimeInTag = true;
            DLogger.MaxCollectionItems = 3;
            DLogger.Log($"{nameof(DLoggerTest)}.{nameof(Start)}", "fsdf");
            TestBasicLog();

            TestObjectLog();

            TestCollectionLog();

            TestCircularReference();

            TestMaxDepth();

            TestUnityObject();

            TestException();

            TestThreadSafe();
        }


        #region Basic


        /// <summary>
        /// 测试四种基础输出：无标签文本、带标签文本、警告、错误
        /// <para>检查点：标签是否渲染为橙色 <c>[CustomTag]</c>，警告与错误是否带对应的 Console 图标</para>
        /// </summary>
        private void TestBasicLog()
        {
            DLogger.Log("========== Basic Log ==========");

            DLogger.Log("Hello DLogger");

            DLogger.Log(
                "CustomTag",
                "Custom Tag Message"
            );


            DLogger.LogWarning(
                "This is warning"
            );


            DLogger.LogError(
                "This is error"
            );
        }


        #endregion



        #region Object


        /// <summary>
        /// 测试对象展开：传入自定义类时应展开字段，而不是只打印 <c>ToString()</c>
        /// <para>检查点：输出里应能看到 <c>Id</c>、<c>Name</c>、<c>Value</c> 三个字段及各自的值</para>
        /// </summary>
        /// <remarks>
        /// <para><see cref="TestClass"/> 特意重写了 <c>ToString()</c>，如果输出里只出现
        /// <c>TestClass ToString</c> 而看不到字段，说明对象展开没有生效。</para>
        /// </remarks>
        private void TestObjectLog()
        {
            DLogger.Log(
                "========== Object Log =========="
            );


            TestClass data = new TestClass()
            {
                Id = 100,
                Name = "TestObject",
                Value = 99.9f
            };


            DLogger.Log(
                "Object",
                data
            );
        }


        #endregion



        #region Collection


        /// <summary>
        /// 测试集合展开：列表、字典、哈希集合都应逐项展开
        /// <para>检查点：由于 <c>Start</c> 把 <c>MaxCollectionItems</c> 设为 3，
        /// 长度为 5 的 <c>list</c> 应只显示前三项并给出截断提示</para>
        /// </summary>
        private void TestCollectionLog()
        {
            DLogger.Log(
                "========== Collection Log =========="
            );


            List<int> list = new List<int>()
            {
                1,2,3,4,5
            };


            DLogger.Log(
                "List",
                list
            );


            Dictionary<string, object> dic =
                new Dictionary<string, object>()
                {
                    {
                        "Name",
                        "Unity"
                    },
                    {
                        "Version",
                        Application.unityVersion
                    },
                    {
                        "Numbers",
                        list
                    }
                };


            DLogger.Log(
                "Dictionary",
                dic
            );


            HashSet<string> set =
                new HashSet<string>()
                {
                    "A",
                    "B",
                    "C"
                };


            DLogger.Log(
                "HashSet",
                set
            );
        }


        #endregion



        #region Circular


        /// <summary>
        /// 测试循环引用检测：A 指向 B、B 指回 A 时不应无限递归
        /// <para>检查点：输出应正常结束，并在遇到重复对象处给出循环引用提示</para>
        /// </summary>
        /// <remarks>
        /// <para>这是 <c>DLogger</c> 里 <c>VisitedObjects</c> 机制的关键回归用例：
        /// 一旦检测失效，这里会直接栈溢出导致 Unity 崩溃，是最严重的一类回归。</para>
        /// </remarks>
        private void TestCircularReference()
        {
            DLogger.Log(
                "========== Circular Reference =========="
            );


            Node a = new Node()
            {
                Name = "A"
            };


            Node b = new Node()
            {
                Name = "B"
            };


            a.Next = b;
            b.Next = a;


            DLogger.Log(
                "Circular",
                a
            );
        }


        #endregion



        #region Depth


        /// <summary>
        /// 测试深度截断：临时把 <c>MaxDepth</c> 降为 2，构造四层嵌套对象
        /// <para>检查点：应只展开到第二层，第三层起降级为 <c>ToString()</c></para>
        /// </summary>
        /// <remarks>
        /// <para>测试结束会把 <c>MaxDepth</c> 还原为默认的 3，这是本组件里唯一被还原的全局配置。</para>
        /// <para><c>Child</c> 字段声明为 <see cref="DeepObject"/> 类型，
        /// 因此在深度耗尽时调用的是 <c>object.ToString()</c>，输出会是类型名。</para>
        /// </remarks>
        private void TestMaxDepth()
        {
            DLogger.Log(
                "========== Max Depth =========="
            );


            DLogger.MaxDepth = 2;


            DeepObject obj =
                new DeepObject()
                {
                    Level = 0,
                    Child = new DeepObject()
                    {
                        Level = 1,
                        Child = new DeepObject()
                        {
                            Level = 2,
                            Child = new DeepObject()
                            {
                                Level = 3
                            }
                        }
                    }
                };


            DLogger.Log(
                "Depth",
                obj
            );


            //恢复默认
            DLogger.MaxDepth = 3;
        }


        #endregion



        #region Unity Object


        /// <summary>
        /// 测试 Unity 对象的展开：GameObject、Transform、Camera
        /// <para>检查点：Unity 内置类型字段很多，应只展开到 <c>MaxDepth</c> 限制的层数就停下，不出现刷屏</para>
        /// </summary>
        /// <remarks>
        /// <para>Camera 用 <c>Camera.main</c> 获取，场景里没有带 <c>MainCamera</c> 标签的相机会跳过该条输出，
        /// 这是正常的，不是测试失败。</para>
        /// </remarks>
        private void TestUnityObject()
        {
            DLogger.Log(
                "========== Unity Object =========="
            );


            DLogger.Log(
                "GameObject",
                gameObject
            );


            DLogger.Log(
                "Transform",
                transform
            );


            Camera cam =
                Camera.main;


            if (cam != null)
            {
                DLogger.Log(
                    "Camera",
                    cam
                );
            }
        }


        #endregion



        #region Exception


        /// <summary>
        /// 测试异常日志：无标签与带标签两个重载
        /// <para>检查点：应输出红色的异常类型名，后接异常消息与完整堆栈</para>
        /// </summary>
        /// <remarks>
        /// <para>这里主动抛出再捕获，因此 Console 里不应出现 Unity 自己的未捕获异常条目。</para>
        /// </remarks>
        private void TestException()
        {
            DLogger.Log(
                "========== Exception =========="
            );


            try
            {
                throw new InvalidOperationException(
                    "Test Exception"
                );
            }
            catch (Exception e)
            {
                DLogger.LogException(e);


                DLogger.LogException(
                    "CustomException",
                    e
                );
            }
        }


        #endregion



        #region Trace


        #endregion



        #region Thread


        /// <summary>
        /// 测试跨线程输出：启动 5 个线程同时打日志
        /// <para>检查点：5 条日志内容完整、互不串行错乱，且不抛异常</para>
        /// </summary>
        /// <remarks>
        /// <para>线程里只传了 <c>"Thread-" + index</c> 这一个字符串参数，走的是 <c>string</c> 重载，
        /// <strong>不会</strong>做对象展开，因此这条用例验证的是「共享格式化缓冲区在多线程下是否安全」，
        /// 而不是对象展开的线程安全性。</para>
        /// <para>另外，这些日志来自后台线程，不会输出帧号（帧号只在主线程有效）。</para>
        /// <para>线程对象是<strong>前台线程</strong>，不会随播放模式停止而自动结束；
        /// 若要改成更安全的形式，可在创建时设置 <c>IsBackground = true</c>。</para>
        /// </remarks>
        private void TestThreadSafe()
        {
            DLogger.Log(
                "========== Thread Test =========="
            );


            for (int i = 0; i < 5; i++)
            {
                int index = i;


                Thread thread =
                    new Thread(() =>
                    {
                        DLogger.Log(
                            "Thread-" + index
                        );
                    });


                thread.Start();
            }
        }


        #endregion



        /// <summary>
        /// 用于对象展开测试的示例类型，故意重写了 <c>ToString()</c>
        /// <para>如果 <c>DLogger</c> 正确展开了字段，输出里就应该看得到字段值而不是 <c>TestClass ToString</c></para>
        /// </summary>
        [Serializable]
        public class TestClass
        {
            public int Id;

            public string Name;

            public float Value;


            /// <summary>
            /// 返回固定文本，用来验证 <c>DLogger</c> 优先展开字段而非调用本方法
            /// </summary>
            /// <returns>固定字符串 <c>TestClass ToString</c></returns>
            public override string ToString()
            {
                return "TestClass ToString";
            }
        }



        /// <summary>
        /// 用于循环引用测试的链表节点，两个节点互指形成环
        /// </summary>
        public class Node
        {
            public string Name;

            public Node Next;
        }



        /// <summary>
        /// 用于深度截断测试的嵌套类型，通过 <c>Child</c> 自引用构造多层结构
        /// </summary>
        public class DeepObject
        {
            public int Level;

            public DeepObject Child;
        }
    }
}
