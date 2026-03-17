using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 账号与按用户本地缓存。当前用户名存于 PlayerPrefs，其余数据按 "User_用户名_键" 存储。
    /// </summary>
    public static class AccountManager
    {
        private const string KeyCurrentUsername = "CurrentUsername";

        public static bool IsRegistered()
        {
            var name = PlayerPrefs.GetString(KeyCurrentUsername, "");
            return !string.IsNullOrWhiteSpace(name);
        }

        public static string GetCurrentUsername()
        {
            return PlayerPrefs.GetString(KeyCurrentUsername, "").Trim();
        }

        /// <summary> 注册/登录成功后调用，保存当前用户名（密码仅本地校验用可另存） </summary>
        public static void SetCurrentUser(string username)
        {
            PlayerPrefs.SetString(KeyCurrentUsername, (username ?? "").Trim());
            PlayerPrefs.Save();
        }

        private static string UserKey(string key)
        {
            var user = GetCurrentUsername();
            if (string.IsNullOrEmpty(user)) return "";
            return "User_" + user + "_" + key;
        }

        public static void SaveUserData(string key, string value)
        {
            var k = UserKey(key);
            if (string.IsNullOrEmpty(k)) return;
            PlayerPrefs.SetString(k, value ?? "");
            PlayerPrefs.Save();
        }

        public static string LoadUserData(string key)
        {
            var k = UserKey(key);
            return string.IsNullOrEmpty(k) ? "" : PlayerPrefs.GetString(k, "");
        }
    }
}
