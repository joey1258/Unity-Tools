using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 打包文件类型枚举，可自行根据需要定义
/// </summary>
public enum SuffixEnum
{
    Prefab,
    Png,
    Csv,
    Txt,
    Lua
}

/// <summary>
/// AssetBundle variant 属性枚举，可自行根据需要定义
/// </summary>
public enum VariantEnum
{
    None,
    _1280x720,
}

/// <summary>
/// AssetBundle variant 属性枚举，可自行根据需要定义
/// </summary>
public enum YN
{
    _true,
    _false,
}

public class AddBuildMapWindow : EditorWindow
{
    /// <summary>
    /// 总数
    /// </summary>
    int count = 0;

    /// <summary>
    /// bundle 名称 List
    /// </summary>
    List<string> bundleNameList = new List<string>();

    /// <summary>
    /// 文件类型 List
    /// </summary>
    List<SuffixEnum> suffixList = new List<SuffixEnum>();

    /// <summary>
    /// path List
    /// </summary>
    List<string> pathList = new List<string>();

    Vector2 scrollValue = Vector2.zero;
    VariantEnum variant = VariantEnum.None;
    YN deleteExists = YN._false;
    BuildTarget target = BuildTarget.StandaloneWindows;

    /// <summary>
    /// 
    /// </summary>
    [MenuItem("BuildUtility/AddBuildMapUtility")]
    static void SetAssetBundleNameExtension()
    {
        EditorWindow.GetWindow<AddBuildMapWindow>();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("添加一项")) { AddItem(); }

        if (GUILayout.Button("清除所有项")) { Clear(); }

        EditorGUILayout.LabelField("Variant(非贴图类资源请设none):", GUILayout.Width(175));
        variant = (VariantEnum)EditorGUILayout.EnumPopup(variant, GUILayout.Width(100));

        if (GUILayout.Button("读取(csv)"))
        {
            Clear();

            // 如果 streaming 目录存在，就删除并重新创建，并刷新 AssetDatabase
            if (Directory.Exists(Application.dataPath + "/Files/BuildMap_cvs/"))
            {
                Directory.CreateDirectory(Application.dataPath + "/Files/BuildMap_cvs/");
            }

            AssetDatabase.Refresh();

            string path = Application.dataPath + "/Files/BuildMap_cvs/AssetBundleInfo.csv";
            string content = File.ReadAllText(path);
            string[] contents = content.Split(new string[] { "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < contents.Length; i++)
            {
                string[] a = contents[i].Split(',');
                AddItem(a[0], StringToSuffixEnum(a[1]), a[2]);
            }

            variant = StringToVariantEnum(contents[0].Split(',')[3]);
        }

        if (GUILayout.Button("保存"))
        {
            Save();
        }

        if (GUILayout.Button("自动填写(所有选中的)"))
        {
            int startIndex = count;
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                AddItem();
                AutoFill(startIndex, Selection.objects[i]);
                startIndex++;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("注意：lua文件请以文件夹为单位进行选择！打包后文件夹名即为包名！lua文件请不要混同其他类型文件存放！打包lua文件请点击Build Lua按钮，Build Lua Selected打包选中的文件夹下的lua文件;");
        EditorGUILayout.LabelField("Build Lua All打包所有同级目录中的lua文件，打包其他文件请点击Build Asset按钮。按分辨率打包纹理时遇到报“Variant folder path cannot be empty”错误时请忽略，此为 U3D 5.3.X 版本的 bug，经实测不影响使用.");

        scrollValue = EditorGUILayout.BeginScrollView(scrollValue);

        for (int i = 0; i < count; i++)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(i.ToString() + ".AB包名:", GUILayout.Width(70));
            bundleNameList[i] = EditorGUILayout.TextField("", bundleNameList[i], GUILayout.Width(100));
            EditorGUILayout.LabelField(" 类型:", GUILayout.Width(40));
            suffixList[i] = (SuffixEnum)EditorGUILayout.EnumPopup(suffixList[i]);
            EditorGUILayout.LabelField(" 路径:", GUILayout.Width(40));
            pathList[i] = EditorGUILayout.TextField(pathList[i]);

            if (GUILayout.Button("自动填写(单个)"))
            {
                AutoFill(i, Selection.objects[0]);
            }
            if (GUILayout.Button("log路径"))
            {
                Debug.Log(pathList[i]);
            }
            if (GUILayout.Button("删除该项"))
            {
                RemoveItem(i);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(" 是否删除已存在的其他文件:", GUILayout.Width(140));
        deleteExists = (YN)EditorGUILayout.EnumPopup(deleteExists, GUILayout.Width(50));
        EditorGUILayout.LabelField(" 平台:", GUILayout.Width(40));
        target = (BuildTarget)EditorGUILayout.EnumPopup(target, GUILayout.Width(130));
        if (GUILayout.Button("Build Lua Selected"))
        {
            Save();
            if (deleteExists == YN._false) PackagerUtils.BuildLuaResource(false, false, target);
            else PackagerUtils.BuildLuaResource(false, true, target);
        }
        if (GUILayout.Button("Build Lua All"))
        {
            Save();
            if (deleteExists == YN._false) PackagerUtils.BuildLuaResource(true, false, target);
            else PackagerUtils.BuildLuaResource(true, true, target);
        }
        if (GUILayout.Button("Build Asset"))
        {
            Save();
            if (deleteExists == YN._false) PackagerUtils.BuildAssetResource(false, target);
            else PackagerUtils.BuildAssetResource(true, target);
        }
        EditorGUILayout.EndHorizontal();
    }

    void Clear()
    {
        count = 0;
        bundleNameList = new List<string>();
        suffixList = new List<SuffixEnum>();
        pathList = new List<string>();
    }

    /// <summary>
    /// 添加一个 Item
    /// </summary>
    void AddItem(string bundleName = "", SuffixEnum suffix = SuffixEnum.Prefab, string path = "")
    {
        count++;
        bundleNameList.Add(bundleName);
        suffixList.Add(suffix);
        pathList.Add(path);
    }

    /// <summary>
    /// 
    /// </summary>
    void RemoveItem(int index)
    {
        count--;
        bundleNameList.Remove(bundleNameList[index]);
        suffixList.Remove(suffixList[index]);
        pathList.Remove(pathList[index]);
    }

    /// <summary>
    /// 为在 U3D 中选中的资源自动填写
    /// </summary>
    void AutoFill(int index, Object selectedObject)
    {
        string path = AssetDatabase.GetAssetPath(selectedObject);
        bundleNameList[index] = path.Remove(0, path.LastIndexOf("/") + 1).ToLower() + ".unity3d";

        string[] files = Directory.GetFiles(path);
        string[] temp = files[0].Split('.');
        suffixList[index] = StringToSuffixEnum("*." + temp[1]);

        pathList[index] = path;
    }

    public static string SuffixEnumToString(SuffixEnum se)
    {
        switch (se)
        {
            case SuffixEnum.Prefab:
                return "*.prefab";
            case SuffixEnum.Png:
                return "*.png";
            case SuffixEnum.Csv:
                return "*.csv";
            case SuffixEnum.Txt:
                return "*.txt";
            case SuffixEnum.Lua:
                return "*.lua";
            default:
                return "null";
        }
    }

    public static SuffixEnum StringToSuffixEnum(string s)
    {
        switch (s)
        {
            case "*.prefab":
                return SuffixEnum.Prefab;
            case "*.png":
                return SuffixEnum.Png;
            case "*.csv":
                return SuffixEnum.Csv;
            case "*.txt":
                return SuffixEnum.Txt;
            case "*.lua":
                return SuffixEnum.Lua;
            default:
                return SuffixEnum.Prefab;
        }
    }

    public static string VariantEnumToString(VariantEnum se)
    {
        switch (se)
        {
            case VariantEnum.None:
                return "none";
            case VariantEnum._1280x720:
                return "1280x720";
            default:
                return "none";
        }
    }

    public static VariantEnum StringToVariantEnum(string s)
    {
        switch (s)
        {
            case "none":
                return VariantEnum.None;
            case "1280x720":
                return VariantEnum._1280x720;
            default:
                return VariantEnum.None;
        }
    }

    void Save()
    {
        // 如果 streaming 目录存在，就删除并重新创建，并刷新 AssetDatabase
        if (!Directory.Exists(Application.dataPath + "/Files/BuildMap_cvs/"))
        {
            Directory.CreateDirectory(Application.dataPath + "/Files/BuildMap_cvs/");
        }

        AssetDatabase.Refresh();

        string path = Application.dataPath + "/Files/BuildMap_cvs/AssetBundleInfo.csv";

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            if (string.IsNullOrEmpty(bundleNameList[i])) break;
            sb.Append(bundleNameList[i] + ",");
            sb.Append(SuffixEnumToString(suffixList[i]) + ",");
            sb.Append(pathList[i] + ",");
            sb.Append(VariantEnumToString(variant) + "\r\n");
        }
        File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();
        Clear();
    }
}
