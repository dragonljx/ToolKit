using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace ToolKit.Editor
{

    public class CreatePackage : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset uxml;

        [SerializeField]
        private StyleSheet uss;

        [SerializeField]
        private string PackagePathText = "";

        [SerializeField]
        private string PackageNameText = "Package Name";

        [SerializeField]
        private string CompanyNameText = "Dragon";

        [SerializeField]
        private string EmailText = "";

        [SerializeField]
        private string UrlText = "";

        [SerializeField]
        private string DescriptionText = "";
        HelpBox help;

        private const string FresetFilePath = "Packages/com.dragon.toolkit/Editor/Windows/CreateNewPackage";
        private const string FresetFileName = ".FresetFile";
        private const string NameTag = "[Name]";
        private const string LowerNameTag = "[LowerName]";
        private const string CompanyNameTag = "[CompanyName]";
        private const string LowerCompanyNameTag = "[LowerCompanyName]";
        private const string DescriptionTag = "[Description]";
        private const string EmailTag = "[Email]";
        private const string UrlTag = "[Url]";

        private const string RuntimeText = "Runtime";
        private const string EditorText = "Editor";
        private const string AsmdefSuffix = ".asmdef";

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            VisualElement labelFromUXML = uxml.Instantiate();
            labelFromUXML.Bind(new SerializedObject(this));

            root.Add(labelFromUXML);

            string path = Path.GetFullPath($"{Application.dataPath}//..//Packages");

            PackagePathText = path;

            Button openFileButton = root.Q<Button>("OpenFileButton");
            openFileButton.clicked += OpenFileCall;

            Button createButton = root.Q<Button>("CreateButton");
            createButton.clicked += CreateCall;
            Button testButton = root.Q<Button>("TestButton");
            testButton.clicked += TestCall;
            Button localPathButton = root.Q<Button>("LocalPathButton");

            localPathButton.clicked += () =>
            {
                PackagePathText = Path.GetFullPath($"{Application.dataPath}//..//Packages");
            };
            Button externalPathButton = root.Q<Button>("ExternalPathButton");
            externalPathButton.clicked += () =>
            {
                PackagePathText = Path.GetFullPath($"{Application.dataPath}//..//..//Packages");
            };

            help = new HelpBox("", HelpBoxMessageType.None);
            help.style.display = DisplayStyle.None;

            root.Add(help);
        }


        private void TestCall()
        {
            UnityEditor.PackageManager.Client.Resolve();

        }
        private void CreateCall()
        {
            if (PackageNameText.Trim() == "" || PackageNameText == "Package Name")
            {
                Log("请输入插件名", true);
                return;

            }
            if (PackagePathText.Trim() == "")
            {
                Log("请填写插件地址", true);
                return;

            }

            DirectoryInfo dir = ToolFile.GetFolder(PackageNameText);
            foreach (DirectoryInfo item in dir.GetDirectories())
            {
                if (item.Name == PackageNameText)
                {
                    Log("插件名重复，当前地址已包含同名文件夹", true);

                    return;
                }

            }
            string absolute = Path.GetFullPath(FresetFilePath);

            string packageName = "package.json";
            // 获取预设数据
            string json = File.ReadAllText($"{absolute}/{FresetFileName}/{packageName}");
            // 开始生成插件包

            //{
            //"name": "NewAssembly"
            //}
            string packageFolderPath = Path.Combine(PackagePathText, PackageNameText);

            ToolFile.CreateFolder(packageFolderPath);
            string runtimePath = Path.Combine(packageFolderPath, RuntimeText);
            ToolFile.CreateFolder(runtimePath);

            string editorPath = Path.Combine(packageFolderPath, EditorText);
            ToolFile.CreateFolder(editorPath);


            // package.json 生成
            json = json.Replace(LowerCompanyNameTag, CompressStrLower(CompanyNameText));
            json = json.Replace(LowerNameTag, CompressStrLower(PackageNameText));
            json = json.Replace(NameTag, PackageNameText);
            json = json.Replace(CompanyNameTag, CompanyNameText);
            json = json.Replace(DescriptionTag, DescriptionText);
            json = json.Replace(EmailTag, EmailText);
            json = json.Replace(UrlTag, UrlText);

            ToolFile.SaveJson(Path.Combine(packageFolderPath, packageName), json);

            // runtime asmdef
            string runtimeFolderName = $"{CompressStr(CompanyNameText)}.{CompressStr(PackageNameText)}.{RuntimeText}";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine($"\"name\": \"{runtimeFolderName}\",");
            builder.AppendLine($"\"rootNamespace\": \"{PackageNameText}\"");
            builder.AppendLine("}");

            ToolFile.SaveJson(Path.Combine(runtimePath, $"{runtimeFolderName}{AsmdefSuffix}"), builder.ToString());

            // editor asmdef
            string editorFolderName = $"{CompressStr(CompanyNameText)}.{CompressStr(PackageNameText)}.{EditorText}";
            builder.Clear();
            builder.AppendLine("{");
            builder.AppendLine($"\"name\": \"{editorFolderName}\",");
            builder.AppendLine("\"includePlatforms\": [\"Editor\"],");
            builder.AppendLine($"\"rootNamespace\": \"{PackageNameText}\"");
            builder.AppendLine("}");

            ToolFile.SaveJson(Path.Combine(editorPath, $"{editorFolderName}{AsmdefSuffix}"), builder.ToString());

            //EditorCoroutineUtility.StartCoroutine(AddPackage(packageFolderPath), this);
            AddPackage(packageFolderPath);

            HiddenLog();
            UnityEditor.PackageManager.Client.Resolve();
            this.Close();
            string CompressStrLower(string str)
            {
                return str.ToLower().Trim().Replace(" ", "");
            }
            string CompressStr(string str)
            {
                return str.Trim().Replace(" ", "");
            }

        }
        void AddPackage(string path)
        {

            string dPath = Path.GetFullPath($"{Application.dataPath}//..//Packages");
            if (path.Contains(dPath))
            {
                UnityEditor.PackageManager.Client.Resolve();
                Log("插件创建加载完成", false);
            }
            else
            {
                var result = UnityEditor.PackageManager.Client.Add($"file:{path}").Result;

                rootVisualElement.SetEnabled(false);
                Log($"插件创建加载完成{result.name}", false);
            }
            rootVisualElement.SetEnabled(true);

        }
        private void OpenFileCall()
        {


            string p = Path.GetFullPath($"{Application.dataPath}//..//..");
            string path = EditorUtility.OpenFolderPanel("选择插件包创建的地址", p, "");
            if (path != "")
            {
                PackagePathText = path;
            }
        }
        private void Log(string message, bool isError)
        {
            help.text = message;
            help.messageType = isError ? HelpBoxMessageType.Error : HelpBoxMessageType.Info;
            help.style.display = DisplayStyle.Flex;
            Debug.Log(message);

        }
        private void HiddenLog()
        {
            help.style.display = DisplayStyle.None;

        }
    }
}