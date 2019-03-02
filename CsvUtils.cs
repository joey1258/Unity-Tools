/**
 * Licensed under The MIT License
 * For full copyright and license information, please see the MIT-LICENSE.txt
 * Redistributions of files must retain the above copyright notice.
 *
 * @copyright Joey1258
 * @link https://github.com/joey1258/Unity-Tools
 * @license http://www.opensource.org/licenses/mit-license.php MIT License
 */

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Unity 不支持 DataTable，因此只能自己实现
    /// </summary>
    public static class CSV
    {
        public static CsvTable selectTable { get; private set; }

        public static Dictionary<string, CsvTable> tables
        {
            get
            {
                if (_tables == null) { _tables = new Dictionary<string, CsvTable>(); }
                return _tables;
            }
        }
        private static Dictionary<string, CsvTable> _tables;

        #region Csv IO

        /// <summary>
        ///  设置 UsedDir 的值，从而影响数据操作的目录（相当于选定数据库）
        /// </summary>
        public static void Use(string db)
        {
            Dir_Constants.UsedDir = db;
            // 如果路径不存在就创建，存在则不操作
            IOUtils.CreatePath(db, DataType.CSV);
        }

        /// <summary>
        /// 读取 cvs 文件为一整条的字符串
        /// </summary>
        public static string LoadCsvString(string fileName, string usedDir)
        {
            if (!string.IsNullOrEmpty(usedDir)) { Use(usedDir); }
            string filePath = Application.persistentDataPath + "/" + Dir_Constants.CSVDir + "/" + fileName + ".csv";
            // 必须使用 Encoding.Default，其他均为乱码，不用 Encoding 参数也为乱码
            string text = File.ReadAllText(filePath, Encoding.Default);
            return text;
        }

        /// <summary>
        /// 保存 csv 到指定路径
        /// </summary>
        public static void SaveCSVToFile(CsvTable cvs, string fileName, string usedDir)
        {
            if (!string.IsNullOrEmpty(usedDir)) { Use(usedDir); }
            string filePath = Application.persistentDataPath + "/" + Dir_Constants.CSVDir + "/" + fileName + ".csv";
            using (StreamWriter file = new StreamWriter(filePath, false))
            {
                file.Write(OutputCSVTable(cvs));
            }
        }

        #region Output Function

        public static string OutputCSVTable(CsvTable cvs)
        {
            StringBuilder data = new StringBuilder();

            List<string> columnsKeys = new List<string>(cvs.columns.Keys);
            List<string> rowsKeys = new List<string>(cvs.rows.Keys);
            for (int i = 0; i < cvs.rows.Count; i++)
            {
                for (int n = 0; n < cvs.rows.Count; n++)
                {
                    if (n == 0) { data.Append(cvs.Where(rowsKeys[i], columnsKeys[n])); }
                    else
                    {
                        data.Append(',');
                        data.Append(cvs.Where(rowsKeys[i], columnsKeys[n]));
                    }
                }
                data.Append("\r\n");
            }
            return data.ToString();
        }

        #endregion

        #endregion

        #region Load Function

        /// <summary>
        /// 读取 CSV 表格，并用路径作为 id
        /// </summary>
        public static CsvTable LoadTable(string fileName, string folder)
        {
            CsvTable table = new CsvTable(fileName);
            CsvNode node = null;
            tables[fileName] = table;

            string[] lines = LoadCsvString(fileName, folder).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            int columns = lines.Length;
            // 每一个 csv 文件的第一行为各列的列名
            string[] titles = lines[0].Split(',');
            // 制作表格时需要注意，csv 表格中实际行的单元格数超过列名数量的一律不予识别
            int rows = titles.Length;

            for (int i = 0; i < columns; i++)
            {
                // 当 id 为 0 时（处于列名行时）向 table 类中写入列名对应的 index
                if (i == 0)
                {
                    for (int n = 0; n < rows; n++)
                    {
                        // 向 table 类中写入各列列名对应的 index
                        table.columns[titles[n]] = n;
                        // 并将各单元格的内容加入 table 的 x 字典和 y 字典
                        node = new CsvNode(i, n, titles[n]);
                        if (!table.nodesX.ContainsKey(i))
                        {
                            table.nodesX[i] = new List<CsvNode>();
                        }
                        table.nodesX[i].Add(node);
                        // 即使是在标题行也要将首个单元格作为行名加入，否则行数不对
                        if (n == 0) { table.rows[titles[n]] = n; }
                        if (!table.nodesY.ContainsKey(n))
                        {
                            table.nodesY[n] = new List<CsvNode>();
                        }
                        table.nodesY[n].Add(node);
                    }
                }
                else
                {
                    string[] fields = lines[i].Split(',');
                    for (int d = 0; d < rows; d++)
                    {
                        // 将各单元格的内容加入 table
                        node = new CsvNode(i, d, fields[d]);
                        if (!table.nodesX.ContainsKey(i))
                        {
                            table.nodesX[i] = new List<CsvNode>();
                        }
                        table.nodesX[i].Add(node);
                        // 每行的第一个单元格为行名，为行名写入索引(直接使用上级循环索引 i 即可)
                        if (d == 0) { table.rows[fields[d]] = i; }
                        table.nodesY[d].Add(node);
                    }
                }
            }

            // 读取时的顺序就是正确的，无需再进行排序

            return table;
        }

        #endregion

        #region Select Function

        /// <summary>
        /// 返回指定 Table
        /// </summary>
        public static CsvTable Select(string id, string folder = "")
        {
            if (selectTable == null || selectTable.id != id)
            {
                if (tables.ContainsKey(id)) { selectTable = tables[id]; }
                else { selectTable = LoadTable(id, folder); }
            }
            
            return selectTable;
        }

        #endregion
    }

    /// <summary>
    /// 如果不用 int 来保存 x 和 y 而直接用 string 将导致 X 和 Y 字典的 List 中的元素无法索引，
    /// 所以虽然为了转换为 int 形式在读取表格时需要多做很多工作，还需要额外的字典，但这些都是不可或缺的
    /// </summary>
    public class CsvTable
    {
        public string id { get; private set; }
        public Dictionary<string, int> rows { get; private set; }
        public Dictionary<string, int> columns { get; private set; }
        public Dictionary<int, List<CsvNode>> nodesX { get; private set; }
        public Dictionary<int, List<CsvNode>> nodesY { get; private set; }

        public CsvTable(string id)
        {
            this.id = id;
            nodesX = new Dictionary<int, List<CsvNode>>();
            nodesY = new Dictionary<int, List<CsvNode>>();
            rows = new Dictionary<string, int>();
            columns = new Dictionary<string, int>();
        }

        #region Where Function

        /// <summary>
        /// 返回 Table 中的指定 node data，参数 rowFirest 为真时参数 i1 为 x，i2 为 y，为假时反之。
        /// </summary>
        public string Where(string id1, string id2, bool rowFirest = true)
        {
            if (rowFirest) { return GetNodeByX(rows[id1], columns[id2]).data; }
            else { return GetNodeByY(columns[id2], rows[id1]).data; }
        }

        /// <summary>
        /// 返回 Table 中的指定行的所有内容, action 参数用于对得到的结果进行操作（如过滤等）
        /// </summary>
        public List<string> WhereRow(string rowName, Action<List<string>> action = null)
        {
            List<string> nodes = new List<string>();
            List<string> columnKeys = new List<string>(columns.Keys);
            // 由于 WhereColumn 方法返回的第一个元素为表格的列名，所以不能从 0 开始
            for (int i = 1; i < columns.Count; i++)
            {
                nodes.Add(nodesX[rows[rowName]][columns[columnKeys[i]]].data);
            }

            if (action != null) { action(nodes); }

            return nodes;
        }

        /// <summary>
        /// 返回 Table 中的指定列的所有内容, action 参数用于对得到的结果进行操作（如过滤等）
        /// </summary>
        public List<string> WhereColumn(string columnName, Action<List<string>> action = null)
        {
            List<string> nodes = new List<string>();
            List<string> rowKeys = new List<string>(rows.Keys);
            // 由于 WhereColumn 方法返回的第一个元素为表格的列名，所以不能从 0 开始
            for (int i = 1; i < rows.Count; i++)
            {
                nodes.Add(nodesY[columns[columnName]][rows[rowKeys[i]]].data);
            }

            if (action != null) { action(nodes); }

            return nodes;
        }

        #endregion

        #region Index Function

        public int RowIndex(string rowName)
        {
            if (!rows.ContainsKey(rowName))
            {
                throw new Exception("Do not have this row : " + rowName);
            }

            return rows[rowName];
        }

        public int ColumnIndex(string columnsName)
        {
            if (!columns.ContainsKey(columnsName))
            {
                throw new Exception("Do not have this columns : " + columnsName);
            }

            return columns[columnsName];
        }

        /// <summary>
        /// 根据 x 和 y 返回具体 node
        /// </summary>
        public CsvNode GetNodeByX(int x, int y)
        {
            if (!nodesX.ContainsKey(x))
            {
                throw new Exception("Do not have this row : " + x);
            }
            if (y >= nodesX[x].Count)
            {
                throw new Exception("Do not have this columns : " + (y));
            }
            return nodesX[x][y];
        }

        /// <summary>
        /// 根据 x 和 y 返回具体 node
        /// </summary>
        public CsvNode GetNodeByY(int y, int x)
        {
            if (!nodesY.ContainsKey(y))
            {
                throw new Exception("Do not have this row : " + y);
            }
            if (x >= nodesY[y].Count)
            {
                throw new Exception("Do not have this columns : " + (x));
            }
            return nodesY[y][x];
        }

        /// <summary>
        /// 返回指定行
        /// </summary>
        public List<CsvNode> GetRow(int x)
        {
            if (!nodesX.ContainsKey(x))
            {
                throw new Exception("Do not have this row : " + x);
            }
            return nodesX[x];
        }

        /// <summary>
        /// 返回指定列
        /// </summary>
        public List<CsvNode> GetColumn(int y)
        {
            if (!nodesY.ContainsKey(y))
            {
                throw new Exception("Do not have this columns : " + y);
            }
            return nodesY[y];
        }

        #endregion
    }

    /// <summary>
    /// 表格永远只有1个深度，所以用二叉树算法的提升不大，采取读取时进行排序的方法根据 index
    /// 获取元素（排序只在读取 table 时进行，因此只要合理安排读取时间就不会造成不良影响）
    /// </summary>
    public class CsvNode
    {
        public int x { get; private set; }
        public int y { get; private set; }
        public string data { get; private set; }

        public CsvNode(int x, int y, string data)
        {
            this.x = x;
            this.y = y;
            this.data = data;
        }
    }

    /// <summary>
    /// 用于对 csv 查询结果 list 根据 x 的大小进行排序
    /// </summary>
    public class CsvNodeIndexXComparer : IComparer<CsvNode>
    {
        public int Compare(CsvNode A, CsvNode B)
        {
            if (A == null && B == null) { return 0; }
            if (A == null) { return -1; }
            if (B == null) { return 1; }

            return A.x.CompareTo(B.x);
        }
    }

    /// <summary>
    /// 用于对 csv 查询结果 list 根据 y 的大小进行排序
    /// </summary>
    public class CsvNodeIndexYComparer : IComparer<CsvNode>
    {
        public int Compare(CsvNode A, CsvNode B)
        {
            if (A == null && B == null) { return 0; }
            if (A == null) { return -1; }
            if (B == null) { return 1; }

            return A.y.CompareTo(B.y);
        }
    }
}
