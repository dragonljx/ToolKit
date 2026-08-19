using System;

namespace ToolKit
{
    public abstract class Singleton<T> where T : class
    {
        private static T instance;

        private static readonly object Lock = new object();


        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (Lock)
                    {
                        if (instance == null)
                        {
                            instance = Activator.CreateInstance(
                                typeof(T),
                                true
                            ) as T;
                        }
                    }
                }

                return instance;
            }
        }


        public virtual void Init()
        {
        }
    }
}
