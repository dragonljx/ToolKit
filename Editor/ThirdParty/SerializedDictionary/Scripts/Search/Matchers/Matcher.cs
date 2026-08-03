using UnityEditor;

namespace ToolKit.SerializedDictionary.Editor
{
    public abstract class Matcher
    {
        public string SearchString { get; private set; }

        public void Prepare(string searchString)
        {
            SearchString = ProcessSearchString(searchString);
        }

        public virtual string ProcessSearchString(string searchString)
        {
            return searchString;
        }
        public abstract bool IsMatch(SerializedProperty property);
    }
}