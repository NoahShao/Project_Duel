using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using UnityEngine;

namespace JunzhenDuijue
{
    public static class IntroLoader
    {
        private const string XlsxFileName = "Intro.xlsx";
        private static Dictionary<string, string> _idToIntro;
        private static HashSet<string> _loggedMissingIds;

        public static bool Load()
        {
            _idToIntro = new Dictionary<string, string>();

            if (TryLoadCompiledConfig())
                return true;

            return TryLoadFromXlsx();
        }

        public static string GetIntro(string id)
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            if (_idToIntro == null)
                Load();
            if (_idToIntro == null || _idToIntro.Count == 0)
                return string.Empty;

            string key = id.Trim();
            if (_idToIntro.TryGetValue(key, out string value))
                return value;

            if (_loggedMissingIds == null)
                _loggedMissingIds = new HashSet<string>();
            if (_loggedMissingIds.Add(key))
                Debug.LogWarning("[IntroLoader] Missing intro id: " + key);
            return string.Empty;
        }

        private static bool TryLoadCompiledConfig()
        {
            var textAsset = Resources.Load<TextAsset>(CompiledConfigNames.IntroResourcePath);
            if (textAsset == null || textAsset.bytes == null || textAsset.bytes.Length == 0)
                return false;

            try
            {
                string json = System.Text.Encoding.UTF8.GetString(textAsset.bytes);
                var table = JsonUtility.FromJson<IntroTableBinary>(json);
                if (table == null || table.Entries == null || table.Entries.Count == 0)
                    return false;

                ApplyEntries(table.Entries);
                Debug.Log("[IntroLoader] Loaded compiled intro config: " + _idToIntro.Count);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[IntroLoader] Failed to load compiled intro config, fallback to xlsx. " + e.Message);
                _idToIntro = new Dictionary<string, string>();
                return false;
            }
        }

        private static bool TryLoadFromXlsx()
        {
            string path = Path.Combine(Application.streamingAssetsPath, XlsxFileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[IntroLoader] Intro.xlsx not found: " + path);
                return false;
            }

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0)
                return false;

            var entries = new List<IntroConfigEntry>();
            if (!IntroXlsxParser.Parse(bytes, entries))
            {
                Debug.LogWarning("[IntroLoader] Failed to parse Intro.xlsx");
                return false;
            }

            ApplyEntries(entries);
            Debug.Log("[IntroLoader] Loaded xlsx intro config: " + _idToIntro.Count);
            return true;
        }

        private static void ApplyEntries(List<IntroConfigEntry> entries)
        {
            _idToIntro = new Dictionary<string, string>();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                    continue;
                _idToIntro[entry.Id.Trim()] = entry.Content?.Trim() ?? string.Empty;
            }
        }
    }

    public static class IntroXlsxParser
    {
        private const string NsSpreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        public static bool Parse(byte[] xlsxBytes, List<IntroConfigEntry> outEntries)
        {
            if (xlsxBytes == null || xlsxBytes.Length == 0 || outEntries == null)
                return false;

            try
            {
                using var stream = new MemoryStream(xlsxBytes);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                var sharedStrings = ReadSharedStrings(zip);
                var rows = ReadFirstSheet(zip, sharedStrings);
                if (rows == null || rows.Count < 2)
                    return false;

                var headers = rows[0];
                int idCol = -1;
                int contentCol = -1;
                for (int i = 0; i < headers.Count; i++)
                {
                    string header = (headers[i] ?? string.Empty).Trim();
                    if (header.Length > 0 && header[0] == '﻿')
                        header = header.Substring(1).Trim();
                    if (string.Equals(header, "id", System.StringComparison.OrdinalIgnoreCase))
                        idCol = i;
                    if (contentCol < 0 && (header == "????" || header == "??" || header == "??" || header == "??"))
                        contentCol = i;
                }

                if (idCol < 0) idCol = 0;
                if (contentCol < 0) contentCol = 1;

                for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    string id = GetCell(row, idCol);
                    string content = GetCell(row, contentCol);
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    outEntries.Add(new IntroConfigEntry
                    {
                        Id = id.Trim(),
                        Content = content?.Trim() ?? string.Empty
                    });
                }

                return outEntries.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return new List<string>();

            using var stream = entry.Open();
            var doc = new XmlDocument();
            doc.Load(stream);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("x", NsSpreadsheet);
            var list = new List<string>();
            var nodes = doc.SelectNodes("//x:si", ns);
            if (nodes == null)
                return list;

            foreach (XmlNode si in nodes)
            {
                var allT = si.SelectNodes(".//x:t", ns);
                if (allT != null && allT.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (XmlNode t in allT)
                        sb.Append(t.InnerText);
                    list.Add(sb.ToString());
                }
                else
                {
                    var t = si.SelectSingleNode("x:t", ns);
                    list.Add(t != null ? t.InnerText : string.Empty);
                }
            }
            return list;
        }

        private static List<List<string>> ReadFirstSheet(ZipArchive zip, List<string> sharedStrings)
        {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml") ?? zip.GetEntry("xl/worksheets/Sheet1.xml");
            if (entry == null)
                return null;

            using var stream = entry.Open();
            var doc = new XmlDocument();
            doc.Load(stream);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("x", NsSpreadsheet);
            var rowNodes = doc.SelectNodes("//x:row", ns);
            if (rowNodes == null || rowNodes.Count == 0)
                return null;

            var rows = new List<List<string>>();
            foreach (XmlNode rowNode in rowNodes)
            {
                var cellMap = new Dictionary<int, string>();
                var cellNodes = rowNode.SelectNodes("x:c", ns);
                if (cellNodes != null)
                {
                    foreach (XmlNode cell in cellNodes)
                    {
                        string cellRef = cell.Attributes?["r"]?.Value ?? string.Empty;
                        if (string.IsNullOrEmpty(cellRef))
                            continue;
                        int col = ColLettersToIndex(cellRef);
                        if (col < 0)
                            continue;

                        string value = cell.SelectSingleNode("x:v", ns)?.InnerText ?? string.Empty;
                        string type = cell.Attributes?["t"]?.Value;
                        if (type == "s" && int.TryParse(value, out int sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                            value = sharedStrings[sharedIndex];
                        else if (type == "inlineStr")
                        {
                            var isNode = cell.SelectSingleNode("x:is", ns);
                            if (isNode != null)
                            {
                                var textNodes = isNode.SelectNodes(".//x:t", ns);
                                var sb = new System.Text.StringBuilder();
                                if (textNodes != null)
                                    foreach (XmlNode t in textNodes)
                                        sb.Append(t.InnerText);
                                value = sb.ToString();
                            }
                        }
                        cellMap[col] = value;
                    }
                }

                int maxCol = -1;
                foreach (int col in cellMap.Keys)
                    if (col > maxCol)
                        maxCol = col;
                var row = new List<string>();
                for (int i = 0; i <= maxCol; i++)
                    row.Add(cellMap.TryGetValue(i, out string value) ? value : string.Empty);
                rows.Add(row);
            }

            return rows;
        }

        private static int ColLettersToIndex(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef))
                return -1;
            int i = 0;
            while (i < cellRef.Length && char.IsLetter(cellRef[i]))
                i++;
            if (i == 0)
                return -1;
            int col = 0;
            foreach (char c in cellRef.Substring(0, i))
                col = col * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
            return col - 1;
        }

        private static string GetCell(List<string> row, int col)
        {
            if (row == null || col < 0 || col >= row.Count)
                return string.Empty;
            return row[col]?.Trim() ?? string.Empty;
        }
    }
}
