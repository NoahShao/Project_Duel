using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 从 .xlsx 表格加载卡牌数据。表格放 StreamingAssets/Cards.xlsx。
    /// 图鉴展示上限 = 表格中最大 id（只展示 id 1 到 maxId）。
    /// </summary>
    public static class CardTableLoader
    {
        private const string XlsxFileName = "Cards.xlsx";

        private static List<CardData> _allCards;
        private static int _maxId;
        private static Dictionary<int, CardData> _byId;

        /// <summary> 表格中最大 id，图鉴只展示 1..MaxId </summary>
        public static int MaxId => _maxId;

        /// <summary> 所有解析出的卡牌（按 id） </summary>
        public static IReadOnlyList<CardData> AllCards => _allCards;

        /// <summary> 特殊形态卡牌在 Resources/Cards 下的资源名，命名方式 NO + 三位数字 + "_1"，如 NO080_1 </summary>
        public static string GetSpecialFormCardId(int baseId)
        {
            if (baseId <= 0) return "";
            return "NO" + baseId.ToString("D3") + "_1";
        }

        /// <summary> 卡牌 ID 字符串转数字，如 NO001 -> 1；无效返回 -1 </summary>
        public static int CardIdToNumber(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || cardId.Length < 5 || !cardId.StartsWith("NO")) return -1;
            return int.TryParse(cardId.Substring(2), out int n) ? n : -1;
        }

        /// <summary> 根据 id 取单张卡数据 </summary>
        public static CardData GetCard(int id)
        {
            if (_byId != null && _byId.TryGetValue(id, out var card))
                return card;
            return null;
        }

        /// <summary> 校验表结构：G～O 列必须为 技能名称一、技能一tag、技能描述一、技能名称二、技能二tag、技能描述二、技能名称三、技能三tag、技能描述三。不符合则返回 false 并写出错原因。 </summary>
        public static bool ValidateTableLayout(byte[] xlsxBytes, out string error)
        {
            error = null;
            if (xlsxBytes == null || xlsxBytes.Length == 0) { error = "文件为空"; return false; }
            if (!XlsxParser.TryGetHeaderRow(xlsxBytes, out List<string> header))
            {
                error = "无法读取表头（需有 id 列）";
                return false;
            }
            while (header.Count <= 14) header.Add("");
            var expected = new[] {
                (6, "技能名称一"), (7, "技能一tag"), (8, "技能描述一"),
                (9, "技能名称二"), (10, "技能二tag"), (11, "技能描述二"),
                (12, "技能名称三"), (13, "技能三tag"), (14, "技能描述三")
            };
            foreach (var (col, name) in expected)
            {
                string cell = header[col].Trim();
                if (cell.Length > 0 && cell[0] == '\uFEFF') cell = cell.Substring(1).Trim();
                if (cell != name)
                {
                    error = $"第 {col + 1} 列（{(char)('A' + col)}列）应为「{name}」，当前为「{(string.IsNullOrEmpty(cell) ? "(空)" : cell)}」";
                    return false;
                }
            }
            return true;
        }

        /// <summary> 加载表格，返回是否成功。展示用 id 列表为 NO001..NO{MaxId}。表格请放在 StreamingAssets/Cards.xlsx </summary>
        public static bool Load()
        {
            _allCards = new List<CardData>();
            _byId = new Dictionary<int, CardData>();
            _maxId = 0;

            string path = Path.Combine(Application.streamingAssetsPath, XlsxFileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning("未找到 Cards.xlsx，请将表格放到 StreamingAssets/Cards.xlsx。当前尝试路径: " + path);
                return false;
            }
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogWarning("Cards.xlsx 文件为空");
                return false;
            }

            if (!ValidateTableLayout(bytes, out string validateError))
            {
                Debug.LogWarning("Cards.xlsx 表结构不符合要求：" + validateError);
                return false;
            }

            if (!XlsxParser.Parse(bytes, _allCards))
            {
                Debug.LogWarning("解析 Cards.xlsx 失败，请检查表头是否包含 id 列及第一行是否为表头");
                return false;
            }

            foreach (var c in _allCards)
            {
                if (c.Id > _maxId) _maxId = c.Id;
                _byId[c.Id] = c;
            }

            Debug.Log($"Cards.xlsx 加载成功: {_allCards.Count} 条卡牌，最大 id={_maxId}");
            return true;
        }

        /// <summary> 获取图鉴要展示的卡牌 id 列表（1 到 MaxId，NO001 格式） </summary>
        public static List<string> GetCompendiumCardIds()
        {
            var list = new List<string>();
            for (int i = 1; i <= _maxId; i++)
                list.Add("NO" + i.ToString("D3"));
            return list;
        }

        /// <summary> 多条件 AND 筛选：名称包含、势力/花色/点数/所属扩展包在选中集合内（选中为空表示不筛该项）。
        /// 表内规则：不填花色视为无花色；点数为 All 时无论筛什么点数该牌都显示。 </summary>
        public static List<string> GetFilteredCardIds(string nameSubstring,
            HashSet<string> factions, HashSet<string> suits, HashSet<string> ranks, HashSet<string> expansionPacks)
        {
            var list = new List<string>();
            for (int i = 1; i <= _maxId; i++)
            {
                var card = GetCard(i);
                if (card == null) continue;
                if (!string.IsNullOrWhiteSpace(nameSubstring) && (string.IsNullOrEmpty(card.RoleName) || !card.RoleName.Contains(nameSubstring)))
                    continue;
                if (factions != null && factions.Count > 0 && !factions.Contains(card.Faction.Trim()))
                    continue;
                // 不填花色视为无花色
                var cardSuit = string.IsNullOrWhiteSpace(card.Suit) ? "无" : card.Suit.Trim();
                if (suits != null && suits.Count > 0 && !suits.Contains(cardSuit))
                    continue;
                // 点数为 All 时无论筛什么点数都显示
                var cardRank = card.Rank.Trim();
                if (ranks != null && ranks.Count > 0 &&
                    !cardRank.Equals("All", System.StringComparison.OrdinalIgnoreCase) &&
                    !ranks.Contains(cardRank))
                    continue;
                if (expansionPacks != null && expansionPacks.Count > 0 && !expansionPacks.Contains(card.ExpansionPack.Trim()))
                    continue;
                list.Add(card.CardId);
            }
            return list;
        }
    }

    /// <summary>
    /// 纯 C# 解析 .xlsx（ZIP + XML），无外部 DLL。
    /// </summary>
    internal static class XlsxParser
    {
        private const string NsSpreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        /// <summary> 仅读取表头行（含 id 的那一行），用于表结构校验。返回的表头会补齐到至少 15 列。 </summary>
        public static bool TryGetHeaderRow(byte[] xlsxBytes, out List<string> headerRow)
        {
            headerRow = null;
            try
            {
                using var stream = new MemoryStream(xlsxBytes);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                var sharedStrings = ReadSharedStrings(zip);
                var rows = ReadFirstSheet(zip, sharedStrings);
                if (rows == null || rows.Count < 1) return false;
                var headers = rows[0];
                var colIndex = BuildColumnIndex(headers);
                if (colIndex == null && rows.Count >= 2)
                {
                    headers = rows[1];
                    colIndex = BuildColumnIndex(headers);
                }
                if (colIndex == null) return false;
                headerRow = new List<string>(headers);
                while (headerRow.Count <= 14) headerRow.Add("");
                return true;
            }
            catch { return false; }
        }

        public static bool Parse(byte[] xlsxBytes, List<CardData> outCards)
        {
            try
            {
                using var stream = new MemoryStream(xlsxBytes);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

                var sharedStrings = ReadSharedStrings(zip);

                var rows = ReadFirstSheet(zip, sharedStrings);
                if (rows == null || rows.Count < 2)
                {
                    UnityEngine.Debug.LogWarning("XlsxParser: 未找到工作表或行数不足（需要表头+至少一行数据）");
                    return false;
                }

                var headers = rows[0];
                var colIndex = BuildColumnIndex(headers);
                int dataStartRow = 1;
                if (colIndex == null && rows.Count > 2)
                {
                    headers = rows[1];
                    colIndex = BuildColumnIndex(headers);
                    dataStartRow = 2;
                }
                if (colIndex == null)
                {
                    UnityEngine.Debug.LogWarning("XlsxParser: 表头中未找到 id 列，请确保第一行或第二行有 id");
                    return false;
                }

                for (int i = dataStartRow; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (!colIndex.TryGetValue("id", out int idCol) || !TryParseInt(GetCol(row, idCol), out int id))
                        continue;
                    var card = new CardData();
                    card.Id = id;
                    card.RoleName = GetColSafe(row, colIndex, "角色名称") ?? "";
                    card.Faction = GetColSafe(row, colIndex, "势力") ?? "";
                    card.Suit = GetColSafe(row, colIndex, "花色") ?? "";
                    card.Rank = GetColSafe(row, colIndex, "点数") ?? "";
                    card.ExpansionPack = GetColSafe(row, colIndex, "所属扩展") ?? GetColSafe(row, colIndex, "所属扩展包") ?? "";
                    // 普通形态：G=名1, H=技能一tag, I=描述1, J=名2, K=技能二tag, L=描述2, M=名3, N=技能三tag, O=描述3（仅按列索引读）
                    card.SkillName1 = GetCol(row, 6) ?? "";
                    card.SkillTags1 = ParseTagList(GetCol(row, 7) ?? "");
                    card.SkillDesc1 = GetCol(row, 8) ?? "";
                    card.SkillName2 = GetCol(row, 9) ?? "";
                    card.SkillTags2 = ParseTagList(GetCol(row, 10) ?? "");
                    card.SkillDesc2 = GetCol(row, 11) ?? "";
                    card.SkillName3 = GetCol(row, 12) ?? "";
                    card.SkillTags3 = ParseTagList(GetCol(row, 13) ?? "");
                    card.SkillDesc3 = GetCol(row, 14) ?? "";
                    card.RoleTag = (GetColSafe(row, colIndex, "角色tag") ?? GetColSafe(row, colIndex, "角色Tag") ?? "").Trim();
                    card.HasSpecialForm = ParseBool(GetColSafeFirst(row, colIndex, "是否有特殊形态", "是否有特殊形态id", "有特殊形态") ?? "");
                    var specialIdRaw = GetColSafeFirst(row, colIndex, "特殊形态id", "特殊形态ID", "特殊形态 id", "特殊形态") ?? "";
                    card.SpecialFormId = ParseSpecialFormId(specialIdRaw);
                    if (card.SpecialFormId > 0 && !card.HasSpecialForm)
                        card.HasSpecialForm = true;
                    // 特殊形态：S=名1, T=19 tag, U=20 描述1, V=名2, W=22 tag, X=23 描述2, Y=名3, Z=25 tag, AA=26 描述3
                    card.SpecialSkillName1 = GetCol(row, 18) ?? "";
                    card.SpecialSkillTags1 = ParseTagList(GetCol(row, 19) ?? "");
                    card.SpecialSkillDesc1 = GetCol(row, 20) ?? "";
                    card.SpecialSkillName2 = GetCol(row, 21) ?? "";
                    card.SpecialSkillTags2 = ParseTagList(GetCol(row, 22) ?? "");
                    card.SpecialSkillDesc2 = GetCol(row, 23) ?? "";
                    card.SpecialSkillName3 = GetCol(row, 24) ?? "";
                    card.SpecialSkillTags3 = ParseTagList(GetCol(row, 25) ?? "");
                    card.SpecialSkillDesc3 = GetCol(row, 26) ?? "";
                    outCards.Add(card);
                }

                return true;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
        }

        private static string GetColSafe(List<string> row, Dictionary<string, int> colIndex, string key)
        {
            if (colIndex == null || !colIndex.TryGetValue(key, out int col)) return null;
            return GetCol(row, col);
        }

        /// <summary> 按多个表头名依次尝试取列，用于兼容不同表格列名（如 特殊形态id / 特殊形态ID） </summary>
        private static string GetColSafeFirst(List<string> row, Dictionary<string, int> colIndex, params string[] keys)
        {
            if (colIndex == null) return null;
            foreach (var key in keys)
            {
                var v = GetColSafe(row, colIndex, key);
                if (v != null && v.Length > 0) return v;
            }
            return null;
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
            var names = new[] { "xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml", "xl/worksheets/sheet3.xml", "xl/worksheets/Sheet1.xml" };
            foreach (var name in names)
            {
                var entry = zip.GetEntry(name);
                if (entry == null) continue;
                var rows = ReadSheetFromEntry(entry, sharedStrings);
                if (rows != null && rows.Count >= 2)
                {
                    var headers = rows[0];
                    if (BuildColumnIndex(headers) != null)
                        return rows;
                }
            }
            foreach (var e in zip.Entries)
            {
                var fn = e.FullName.Replace('\\', '/');
                if (!fn.StartsWith("xl/worksheets/") || !fn.EndsWith(".xml")) continue;
                var rows = ReadSheetFromEntry(e, sharedStrings);
                if (rows != null && rows.Count >= 2 && BuildColumnIndex(rows[0]) != null)
                    return rows;
            }
            return null;
        }

        private static List<List<string>> ReadSheetFromEntry(ZipArchiveEntry entry, List<string> sharedStrings)
        {
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
                var cells = new List<string>();
                var cellNodes = rowNode.SelectNodes("x:c", ns);
                if (cellNodes != null)
                {
                    var maxCol = -1;
                    var cellMap = new Dictionary<int, string>();
                    foreach (XmlNode c in cellNodes)
                    {
                        var r = c.Attributes["r"]?.Value ?? "";
                        if (string.IsNullOrEmpty(r)) continue;
                        int col = ColLettersToIndex(r);
                        if (col < 0) continue;
                        var v = c.SelectSingleNode("x:v", ns)?.InnerText ?? "";
                        var t = c.Attributes["t"]?.Value;
                        if (t == "s" && int.TryParse(v, out int si))
                        {
                            if (si >= 0 && si < sharedStrings.Count)
                                v = sharedStrings[si];
                            else
                                v = "";
                        }
                        cellMap[col] = v;
                        if (col > maxCol) maxCol = col;
                    }
                    for (int i = 0; i <= maxCol; i++)
                        cells.Add(cellMap.TryGetValue(i, out var val) ? val : "");
                }
                while (cells.Count <= 26)
                    cells.Add("");
                rows.Add(cells);
            }
            return rows;
        }

        private static int ColLettersToIndex(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef)) return -1;
            int i = 0;
            while (i < cellRef.Length && char.IsLetter(cellRef[i])) i++;
            if (i == 0) return -1;
            var colPart = cellRef.Substring(0, i);
            int col = 0;
            foreach (char c in colPart)
                col = col * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
            return col - 1;
        }

        private static string NormalizeHeader(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            if (s.Length > 0 && s[0] == '\uFEFF') s = s.Substring(1).Trim();
            return s;
        }

        private static Dictionary<string, int> BuildColumnIndex(List<string> headers)
        {
            var index = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                var h = NormalizeHeader(headers[i] ?? "");
                if (!string.IsNullOrEmpty(h)) index[h] = i;
            }
            if (!index.ContainsKey("id")) return null;
            return index;
        }

        private static string GetCol(List<string> row, int col)
        {
            if (col < 0 || col >= row.Count) return null;
            var s = row[col];
            return string.IsNullOrEmpty(s) ? null : s.Trim();
        }

        private static bool TryParseInt(string s, out int v)
        {
            v = 0;
            if (string.IsNullOrEmpty(s)) return false;
            return int.TryParse(s.Trim(), out v);
        }

        /// <summary> 解析特殊形态 id：支持纯数字（80）或 "080_1" 格式，取数字部分 </summary>
        private static int ParseSpecialFormId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            raw = raw.Trim();
            int underscore = raw.IndexOf('_');
            string numPart = underscore >= 0 ? raw.Substring(0, underscore) : raw;
            return TryParseInt(numPart, out int id) ? id : 0;
        }

        private static bool ParseBool(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToLowerInvariant();
            return s == "1" || s == "true" || s == "是" || s == "yes" || s == "有" || s == "y";
        }

        /// <summary> 解析「多个 tag 用 | 或 ｜ 分隔」的字符串，如 强制技|持续技，读表后存成列表供挨个对比。 </summary>
        private static List<string> ParseTagList(string raw)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(raw)) return list;
            char[] sep = { '|', '\uFF5C' };
            foreach (var s in raw.Split(sep))
            {
                var t = s.Trim();
                if (!string.IsNullOrEmpty(t)) list.Add(t);
            }
            return list;
        }
    }
}
