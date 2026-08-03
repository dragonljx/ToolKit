using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ToolKit.Test
{
    public class DLoggerTest : MonoBehaviour
    {
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


            //»Ö¸´Ä¬ÈÏ
            DLogger.MaxDepth = 3;
        }


        #endregion



        #region Unity Object


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



        [Serializable]
        public class TestClass
        {
            public int Id;

            public string Name;

            public float Value;


            public override string ToString()
            {
                return "TestClass ToString";
            }
        }



        public class Node
        {
            public string Name;

            public Node Next;
        }



        public class DeepObject
        {
            public int Level;

            public DeepObject Child;
        }
    }
}