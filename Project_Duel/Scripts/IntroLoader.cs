using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 从 StreamingAssets/intro.xlsx 加载 A列id -> B列介绍内容 的对照表，用于技能 tag 悬停提示。tag 与 A 列 id 对应（如强制技对强制技）。
    /// </summary>
    public static class IntroLoader
    {
        private const string XlsxFileName = "intro.xlsx";
        private const string NsSpreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static Dictionary<string, string> _idToIntro;

        public static bool Load()
        {
            _idToIntro = new Dictionary<string, string>();
            string path = Path.Combine(Application.streamingAssetsPath, XlsxFileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[IntroLoader] 未找到 intro.xlsx，路径: " + path);
                return false;
            }
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0) return false;
            try
            {
                using var stream = new MemoryStream(bytes);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                var sharedStrings = ReadSharedStrings(zip);
                var rows = ReadFirstSheet(zip, sharedStrings);
                if (rows == null || rows.Count < 2) return false;
                var headers = rows[0];
                int idCol = -1, contentCol = -1;
                for (int i = 0; i < headers.Count; i++)
                {
                    var h = (headers[i] ?? "").Trim();
                    if (h.Length > 0 && h[0] == '\uFEFF') h = h.Substring(1).Trim();
                    if (string.Equals(h, "id", System.StringComparison.OrdinalIgnoreCase)) idCol = i;
                    if (contentCol < 0 && (string.Equals(h, "介绍内容", System.StringComparison.Ordinal) || string.Equals(h, "内容", System.StringComparison.Ordinal)
                        || string.Equals(h, "介绍", System.StringComparison.Ordinal) || string.Equals(h, "说明", System.StringComparison.Ordinal))) contentCol = i;
                }
                if (idCol < 0) idCol = 0;
                if (contentCol < 0) contentCol = 1;
                if (idCol >= headers.Count || contentCol >= headers.Count) return false;
                for (int r = 1; r < rows.Count; r++)
                {
                    var row = rows[r];
                    string id = GetCell(row, idCol);
                    string content = GetCell(row, contentCol);
                    if (!string.IsNullOrEmpty(id))
                        _idToIntro[id.Trim()] = content?.Trim() ?? "";
                }
                Debug.Log("[IntroLoader] 已加载 " + _idToIntro.Count + " 条介绍");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[IntroLoader] 解析失败: " + e.Message);
                return false;
            }
        }

        private static HashSet<string> _loggedMissingIds;

        public static string GetIntro(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            if (_idToIntro == null)
                Load();
            if (_idToIntro == null || _idToIntro.Count == 0) return "";
            var key = id.Trim();
            if (_idToIntro.TryGetValue(key, out var s)) return s;
            if (_loggedMissingIds == null) _loggedMissingIds = new HashSet<string>();
            if (_loggedMissingIds.Add(key))
                Debug.LogWarning("[IntroLoader] 未找到 id=\"" + key + "\" 的介绍，请检查 intro.xlsx 的 A 列是否包含该 id");
            return "";
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return new List<string>();
            using var stream = entry.Open();
            var doc = new XmlDocument();
            doc.Load(stream);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("x", NsSpreadsheet);
            var list = new List<string>();
            var nodes = doc.SelectNodes("//x:si", ns);
            if (nodes == null) return list;
            foreach (XmlNode si in nodes)
            {
                var allT = si.SelectNodes(".//x:t", ns);
                if (allT != null && allT.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (XmlNode t in allT) sb.Append(t.InnerText);
                    list.Add(sb.ToString());
                }
                else
                {
                    var t = si.SelectSingleNode("x:t", ns);
                    list.Add(t != null ? t.InnerText : "");
                }
            }
            return list;
        }

        private static List<List<string>> ReadFirstSheet(ZipArchive zip, List<string> sharedStrings)
        {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml") ?? zip.GetEntry("xl/worksheets/Sheet1.xml");
            if (entry == null) return null;
            using var stream = entry.Open();
            var doc = new XmlDocument();
            doc.Load(stream);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("x", NsSpreadsheet);
            var rowNodes = doc.SelectNodes("//x:row", ns);
            if (rowNodes == null || rowNodes.Count == 0) return null;
            var rows = new List<List<string>>();
            foreach (XmlNode rowNode in rowNodes)
            {
                var cellMap = new Dictionary<int, string>();
                var cellNodes = rowNode.SelectNodes("x:c", ns);
                if (cellNodes != null)
                    foreach (XmlNode c in cellNodes)
                    {
                        var r = c.Attributes["r"]?.Value ?? "";
                        if (string.IsNullOrEmpty(r)) continue;
                        int col = ColLettersToIndex(r);
                        if (col < 0) continue;
                        string v = c.SelectSingleNode("x:v", ns)?.InnerText ?? "";
                        var t = c.Attributes["t"]?.Value;
                        if (t == "s" && int.TryParse(v, out int si) && si >= 0 && si < sharedStrings.Count)
                            v = sharedStrings[si];
                        else if (t == "inlineStr")
                        {
                            var isNode = c.SelectSingleNode("x:is", ns);
                            if (isNode != null)
                            {
                                var tNodes = isNode.SelectNodes(".//x:t", ns);
                                var sb = new System.Text.StringBuilder();
                                if (tNodes != null) foreach (XmlNode tn in tNodes) sb.Append(tn.InnerText);
                                v = sb.ToString();
                            }
                        }
                        cellMap[col] = v;
                    }
                int maxCol = -1;
                foreach (var k in cellMap.Keys) if (k > maxCol) maxCol = k;
                var row = new List<string>();
                for (int i = 0; i <= maxCol; i++)
                    row.Add(cellMap.TryGetValue(i, out var val) ? val : "");
                rows.Add(row);
            }
            return rows;
        }

        private static int ColLettersToIndex(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef)) return -1;
            int i = 0;
            while (i < cellRef.Length && char.IsLetter(cellRef[i])) i++;
            if (i == 0) return -1;
            int col = 0;
            foreach (char c in cellRef.Substring(0, i))
                col = col * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
            return col - 1;
        }

        private static string GetCell(List<string> row, int col)
        {
            if (col < 0 || col >= row.Count) return "";
            var s = row[col];
            return s?.Trim() ?? "";
        }
    }
}
