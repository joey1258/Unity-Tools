using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Utils
{
    public enum DataType
    {
        CSV,
        JSON,
        STRING,
    }

    public static class Dir_Constants
    {
        /// <summary>
        /// 用户默认子目录
        /// </summary>
        public static string UsedDir = "default";

        /// <summary>
        /// CSV 子目录
        /// </summary>
        public static string CSVDir = "csv";

        /// <summary>
        /// JSON 子目录
        /// </summary>
        public static string JSONDir = "json";
    }

    public static class IOUtils
    {
        #region common

        /// <summary>
        /// 从指定路径删除一个文件（不可恢复）
        /// </summary>
        public static void DeleteFile(string fileName)
        {
            string destinationFile = Application.persistentDataPath + "/" + fileName;
            //如果文件存在，删除文件
            if (File.Exists(destinationFile))
            {
                FileInfo file = new FileInfo(destinationFile);
                if (file.Attributes.ToString().IndexOf("ReadOnly") != -1)
                    file.Attributes = System.IO.FileAttributes.Normal;

                File.Delete(destinationFile);
            }
        }

        /// <summary>
        /// 从指定路径文件中读取一个字符串
        /// </summary>
        public static string LoadStringFormFile(string fileName)
        {
            try
            {
                using (StreamReader sr = new StreamReader(Application.persistentDataPath + "/" + fileName))
                {
                    return sr.ReadLine();
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 在指定子文件夹不存在的情况下在 Application.persistentDataPath + "/" + jsonDataBase 
        /// 路径下创建一个子文件夹
        /// </summary>
        public static void CreatePath(string folder, DataType type = DataType.JSON)
        {
            string path = "";
            switch (type)
            {
                case DataType.CSV:
                    path = Application.persistentDataPath + "/" + Dir_Constants.CSVDir + "/" + folder;
                    break;
                case DataType.JSON:
                    path = Application.persistentDataPath + "/" + Dir_Constants.JSONDir + "/" + folder;
                    break;
                case DataType.STRING:
                    path = Application.persistentDataPath + "/" + Dir_Constants.UsedDir + "/" + folder;
                    break;
            }
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 删除指定文件夹及其下所有文件
        /// </summary>
        public static void DeletePath(string folder)
        {
            string path = Application.persistentDataPath + "/" + Dir_Constants.JSONDir + "/" + folder;
            DirectoryInfo di = new DirectoryInfo(path);
            di.Delete(true);
        }

        /// <summary>
        /// 返回指定路径下的所有文件名
        /// </summary>
        public static string[] GetFiles(string path)
        {
            System.IO.DirectoryInfo dir = new DirectoryInfo(Application.persistentDataPath + "/" + path);
            if (!dir.Exists) { return null; }

            FileInfo[] fiList = dir.GetFiles();
            int length = fiList.Length;
            string[] files = new string[length];
            for (int i = 0; i < length; i++)
            {
                string[] name = fiList[i].Name.Split('.');
                files[i] = name[0];
            }
            return files;
        }

        public static void WriteFiles(string fileName, string text, DataType type = DataType.JSON)
        {
            string filePath = Application.persistentDataPath + "/" + fileName;
            switch (type)
            {
                case DataType.CSV:
                    filePath += ".csv";
                    break;
                default:
                    filePath += ".txt";
                    break;
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, false))
            {
                file.Write(text);
            }
        }

        #endregion
    }
}
