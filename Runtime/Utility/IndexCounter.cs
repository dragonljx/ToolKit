namespace ToolKits
{

    public class IndexCounter
    {
        public enum Mode { Loop, StopAtEnd }
        public int CurrentIndex { get => index;}
        private Mode mode;

        int index = 0;
        int length = 0;

        public IndexCounter(int length, int startIndex = 0, Mode mode = Mode.Loop)
        {
            this.length = length;
            this.index = startIndex;
            this.mode = mode;
        }
        public void Reset(int length, int startIndex = 0)
        {
            this.index = startIndex;
            this.length = length;

        }
        public void Reset(int startIndex = 0)
        {
            this.index = startIndex;

        }
        public int Next()
        {
            index++;
            if (index >= length)
            {
                if (mode == Mode.Loop)
                    index = 0;
                else
                    index = length - 1; // 停在最后一个
            }

            return index;
        }
        public int Prev()
        {
            if (index - 1 < 0)
            {
                if (mode == Mode.Loop)
                    index = length - 1;
                else
                    index = 0; // 停在第一个
            }
            else
            {
                index--;
            }
 
            return index;
        }
    }
}
