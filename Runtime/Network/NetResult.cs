namespace ToolKit
{
    public class NetResult<T>
    {
        public bool Success;
        public string Error;
        public T Data;
    }
}