using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class PackagerUtils
{
    /// <summary>
    /// 数据目录
    /// </summary>
    static string AppDataPath
    {
        get { return Application.dataPath.ToLower(); }
    }

    /// <summary>
    /// AssetBundleBuild list
    /// </summary>
    static List<AssetBundleBuild> maps = new List<AssetBundleBuild>();

    /// <summary>
    /// 路径字符串 list
    /// </summary>
    static List<string> paths = new List<string>();

    /// <summary>
    /// 文件名字符串 list
    /// </summary>
    static List<string> files = new List<string>();

    /// <summary>
    /// 打包非 Lua AssetBundle 资源
    /// </summary>
    public static void BuildAssetResource(bool deleteExists, BuildTarget target)
    {
        // 如果数据存放目录已经存在则删除它 (pc为"c:/" + 项目名 + "/")
        if (Directory.Exists(PathUtils.DataPath))
        {
            Directory.Delete(PathUtils.DataPath, true);
        }

        // 获取 streaming 目录(项目目录/Assets/StreamingAssets)，如果已经存在则删除它
        string streamPath = Application.streamingAssetsPath;
        if (deleteExists)
        {
            // 如果 streaming 目录存在，就删除并重新创建，并刷新 AssetDatabase
            if (Directory.Exists(streamPath))
            {
                Directory.Delete(streamPath, true);
            }

            Directory.CreateDirectory(streamPath);
            AssetDatabase.Refresh();
        }

        // 重新读取 maps
        maps.Clear();
        HandleBundle();

        /* ------------------------------------------------------*/
        /* Variant 问题有可能是 5.3.x 的 bug 不影响使用，经验证确实不影响使用 */
        /* ------------------------------------------------------*/

        // 获取资源地址（/Assets/StreamingAssets）
        string resPath = "Assets/" + PathUtils.AssetDir;
        // BuildAssetBundleOptions ： 在创建时不编译 | 哈希 id
        BuildAssetBundleOptions options =
            BuildAssetBundleOptions.DeterministicAssetBundle |
            BuildAssetBundleOptions.UncompressedAssetBundle;
        // 创建所有 asset
        BuildPipeline.BuildAssetBundles(resPath, maps.ToArray(), options, target);

        // 新建文件，写入路径及 md5 相关信息
        BuildFileIndex();

        // 刷新
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 处理非Lua包
    /// </summary>
    static void HandleBundle()
    {
        string resPath = AppDataPath + "/" + PathUtils.AssetDir + "/";
        if (!Directory.Exists(resPath)) Directory.CreateDirectory(resPath);

        string content = File.ReadAllText(Application.dataPath + "/Files/BuildMap_cvs/AssetBundleInfo.csv");

        string[] contents = content.Split(
            new string[] { "\r\n" },
            System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < contents.Length; i++)
        {
            string[] a = contents[i].Split(',');

            AddBuildMap(a[0], a[1], a[2], a[3]);
        }
    }

    /// <summary>
    /// 打包 Lua AssetBundle 资源
    /// </summary>
    public static void BuildLuaResource(bool all, bool deleteExists, BuildTarget target)
    {
        // 如果数据存放目录已经存在则删除它 (pc为"c:/" + 项目名 + "/")
        if (Directory.Exists(PathUtils.DataPath))
        {
            Directory.Delete(PathUtils.DataPath, true);
        }

        // 获取 streaming 目录(项目目录/Assets/StreamingAssets)，如果已经存在则删除它
        string streamPath = Application.streamingAssetsPath;
        if (deleteExists)
        {
            // 如果 streaming 目录存在，就删除并重新创建，并刷新 AssetDatabase
            if (Directory.Exists(streamPath))
            {
                Directory.Delete(streamPath, true);
            }

            Directory.CreateDirectory(streamPath);
            AssetDatabase.Refresh();
        }

        // 重新读取 maps
        maps.Clear();
        HandleLuaBundle(all);

        // 获取资源地址（/Assets/StreamingAssets）
        string resPath = "Assets/" + PathUtils.AssetDir;
        // BuildAssetBundleOptions ： 在创建时不编译 | 哈希 id
        BuildAssetBundleOptions options =
            BuildAssetBundleOptions.DeterministicAssetBundle |
            BuildAssetBundleOptions.UncompressedAssetBundle;
        // 创建所有 asset
        BuildPipeline.BuildAssetBundles(resPath, maps.ToArray(), options, target);

        // 新建文件，写入路径及 md5 相关信息
        BuildFileIndex();

        // 拼接 lua 临时目录
        string luaTempDir = Application.dataPath + PathUtils.LuaTempDir;
        // 删除 lua 临时目录
        if (Directory.Exists(luaTempDir)) Directory.Delete(luaTempDir, true);
        // 刷新
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 处理Lua包
    /// </summary>
    static void HandleLuaBundle(bool all)
    {
        string luaTempDir = Application.dataPath + PathUtils.LuaTempDir;
        if (!Directory.Exists(luaTempDir)) Directory.CreateDirectory(luaTempDir);

        string content = File.ReadAllText(Application.dataPath + "/Files/BuildMap_cvs/AssetBundleInfo.csv");

        string[] contents = content.Split(
            new string[] { "\r\n" },
            System.StringSplitOptions.RemoveEmptyEntries);

        string dir = Path.GetDirectoryName(contents[0].Split(',')[2]);
        string _name = contents[0].Split(',')[0];

        CopyLuaBytesFiles(dir, luaTempDir);

        if (all)
        {
            string[] dirs = Directory.GetDirectories(luaTempDir, "*", SearchOption.AllDirectories);
            for (int i = 0; i < dirs.Length; i++)
            {
                string name = dirs[i].Replace(luaTempDir, string.Empty);
                name = name.Replace('\\', '_').Replace('/', '_');
                name = "lua/lua_" + name.ToLower() + ".unity3d";
                string path = "Assets" + dirs[i].Replace(Application.dataPath, "");
                AddBuildMap(name, "*.bytes", path);
            }
        }
        else
        {
            string[] dirs = Directory.GetDirectories(luaTempDir, "*", SearchOption.AllDirectories);
            string newPath = "";
            for (int i = 0; i < dirs.Length; i++)
            {
                string[] tempDir = dirs[i].Split('/');
                string[] tempName = _name.Split(
                new string[] { "." },
                System.StringSplitOptions.RemoveEmptyEntries);
                //Debug.Log(tempDir[tempDir.Length - 1]);
                //Debug.Log(tempName[0]);
                if (tempDir[tempDir.Length - 1].ToLower() == tempName[0])
                {
                    newPath = "Assets" + dirs[i].Replace(Application.dataPath, "");
                }
            }
            AddBuildMap("lua/lua_" + _name + ".unity3d", "*.bytes", newPath);
        }

        AddBuildMap("lua/lua" + ".unity3d", "*.bytes", "Assets" + PathUtils.LuaTempDir);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 将参数赋值给新建的 AssetBundleBuild 实例并添加到 list “maps"中去
    ///  AssetBundleBuild 类型可作为 BuildPipeline.BuildAssetBundles 方法的参数
    ///  （有多个需要传递可以新建为数组）将需要打包的文件信息传递进方法进行打包
    /// </summary>
    static void AddBuildMap(string bundleName, string pattern, string path, string bundleVariant = null)
    {
        // 获取符合条件的文件的文件名，如果没有获取到就退出
        string[] files = Directory.GetFiles(path, pattern);
        if (files.Length == 0) return;

        for (int i = 0; i < files.Length; i++)
        {
            files[i] = files[i].Replace('\\', '/');
        }

        AssetBundleBuild build = new AssetBundleBuild();
        build.assetBundleName = bundleName;
        if (bundleVariant != "none")
        {
            build.assetBundleVariant = bundleVariant;
        }
        build.assetNames = files;
        maps.Add(build);
    }

    /// <summary>
    /// 创建与服务器对比用的 flies.txt，写入路径和 md5
    /// </summary>
    static void BuildFileIndex()
    {
        // 获取 StreamingAssets 路径
        string resPath = AppDataPath + "/StreamingAssets/";
        // 拼接完整文件路径字符串
        string newFilePath = resPath + "/files.txt";
        // 如果已经存在就先删除
        if (File.Exists(newFilePath)) File.Delete(newFilePath);

        // 清空 List<string> paths，List<string> files
        paths.Clear();
        files.Clear();
        // 遍历目录及其子目录,并格式化 string 为正确格式，更新到 paths list
        Recursive(resPath);

        // 在 newFilePath 路径创建一个新文件
        FileStream fs = new FileStream(newFilePath, FileMode.CreateNew);
        StreamWriter sw = new StreamWriter(fs);
        for (int i = 0; i < files.Count; i++)
        {
            // 获取当前字符串
            string file = files[i];
            // 获取当前字符串路径文件的扩展名
            //string ext = Path.GetExtension(file);
            // 如果是 .meta 或 .DS_Store 文件，跳过
            if (file.EndsWith(".meta") || file.Contains(".DS_Store")) continue;

            // 计算当前字符串的 md5 值
            string md5 = Md5Utils.md5file(file);
            // 获取当前字符串删除与 resPath 相同的字符后的结果
            string value = file.Replace(resPath, string.Empty);

            // 将 value 和 "|" 拼接 md5 写入到新文件中
            sw.WriteLine(value + "|" + md5);
        }
        sw.Close();
        fs.Close();
    }

    /// <summary>
    /// 遍历目录及其子目录,并格式化 string 为正确格式，更新到 paths list
    /// </summary>
    static void Recursive(string path)
    {
        string[] names = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);
        foreach (string filename in names)
        {
            string ext = Path.GetExtension(filename);
            if (ext.Equals(".meta")) continue;
            files.Add(filename.Replace('\\', '/'));
        }
        foreach (string dir in dirs)
        {
            paths.Add(dir.Replace('\\', '/'));
            Recursive(dir);
        }
    }

    public static void CopyLuaBytesFiles(string sourceDir, string destDir, bool appendext = true)
    {
        if (!Directory.Exists(sourceDir))
        {
            return;
        }

        // 返回目录及其子目录下所有lua文件名
        string[] files = Directory.GetFiles(sourceDir, "*.lua", SearchOption.AllDirectories);

        int len = sourceDir.Length;
        if (sourceDir[len - 1] == '/' || sourceDir[len - 1] == '\\')
        {
            --len;
        }

        for (int i = 0; i < files.Length; i++)
        {
            string str = files[i].Remove(0, len);
            string dest = destDir + str;
            if (appendext) dest += ".bytes";
            string dir = Path.GetDirectoryName(dest);
            Directory.CreateDirectory(dir);
            File.Copy(files[i], dest, true);
        }
    }
}

public class PathUtils
{
    /// <summary>
    /// 取得数据存放目录
    /// </summary>
    public const string AppName = "Name";

    /// <summary>
    /// 临时目录
    /// </summary>
    public const string LuaTempDir = "/TempLuaFiles/";

    /// <summary>
    /// 素材目录 
    /// </summary>
    public const string AssetDir = "StreamingAssets";

    /// <summary>
    /// 取得数据存放目录
    /// </summary>
    public static string DataPath
    {
        get
        {
            string game = AppName.ToLower();
            if (Application.isMobilePlatform)
            {
                return Application.persistentDataPath + "/" + game + "/";
            }
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                int i = Application.dataPath.LastIndexOf('/');
                return Application.dataPath.Substring(0, i + 1) + game + "/";
            }
            return "c:/" + game + "/";
        }
    }

    /// <summary>
    /// 框架根目录
    /// </summary>
    public static string FrameworkRoot
    {
        get
        {
            return Application.dataPath + "/" + AppName;
        }
    }

    /// <summary>
    /// 本地目录(更多对应平台请自行添加)
    /// </summary>
    public static string LocalFilePath
    {
        get
        {
#if UNITY_ANDROID
		return "jar:file://" + Application.dataPath + "!/assets/";
#elif UNITY_IPHONE
		return Application.dataPath + "/Raw/";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
            return "file://" + Application.dataPath + "/StreamingAssets/";
#else
        return string.Empty;
#endif
        }
    }
}

public class Md5Utils
{
    /// <summary>
    /// 计算文件的MD5值
    /// </summary>
    public static string md5file(string file)
    {
        try
        {
            FileStream fs = new FileStream(file, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(fs);
            fs.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception("md5file() fail, error:" + ex.Message);
        }
    }
}
