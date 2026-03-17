using UnityEngine;
using UnityEditor;

namespace JunzhenDuijue.Editor
{
    /// <summary>
    /// 菜单：Tools -> 军阵对决 -> 清除本地缓存（账号、牌组等 PlayerPrefs 数据）
    /// </summary>
    public static class ClearLocalCache
    {
        [MenuItem("Tools/军阵对决/清除本地缓存")]
        public static void Execute()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[军阵对决] 本地缓存已清除（账号、牌组等）。下次进入将重新弹出注册。");
        }
    }
}
