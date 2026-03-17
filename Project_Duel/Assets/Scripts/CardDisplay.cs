using UnityEngine;
using UnityEngine.UI;

namespace JunzhenDuijue
{
    /// <summary>
    /// 卡牌势力：用于筛选。
    /// </summary>
    public enum CardFaction
    {
        蜀, 魏, 吴, 群, 汉, 晋
    }

    /// <summary>
    /// 卡牌花色：用于筛选。
    /// </summary>
    public enum CardSuit
    {
        红桃, 方片, 黑桃, 梅花, 无
    }

    /// <summary>
    /// 卡牌点数：用于筛选。
    /// </summary>
    public enum CardRank
    {
        A, R2, R3, R4, R5, R6, R7, R8, R9, R10, J, Q, K
    }

    /// <summary>
    /// 挂在卡牌预制体根节点上。卡牌图片比例 1016×1488 像素。
    /// 只需在 Inspector 里把卡面图片赋给 FaceImage，并设置势力/花色/点数/名字用于筛选。
    /// </summary>
    public class CardDisplay : MonoBehaviour
    {
        [Header("身份")]
        public string CardId = "NO001";

        [Header("筛选用（后续筛选功能）")]
        public CardFaction Faction = CardFaction.蜀;
        public CardSuit Suit = CardSuit.无;
        public CardRank Rank = CardRank.A;
        [Tooltip("显示名，用于名字搜索")]
        public string DisplayName = "";

        [Header("卡面")]
        [Tooltip("把卡牌图片拖到这里即可")]
        public Image FaceImage;

        /// <summary> 1016×1488 像素的宽高比（宽/高） </summary>
        public const float AspectRatio = 1016f / 1488f;

        /// <summary> 卡牌像素尺寸 1016×1488，用于与图鉴区域匹配时计算缩放 </summary>
        public static readonly Vector2 SizePixels = new Vector2(1016f, 1488f);

        public Sprite FaceSprite
        {
            get => FaceImage != null ? FaceImage.sprite : null;
            set { if (FaceImage != null) FaceImage.sprite = value; }
        }
    }
}
