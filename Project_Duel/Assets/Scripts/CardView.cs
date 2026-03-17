using UnityEngine;
using UnityEngine.UI;

namespace JunzhenDuijue
{
    /// <summary>
    /// 动态生成的卡牌视图，绑定表格中的 CardData；卡面图从 Resources/Cards/NO001 等加载。
    /// 图鉴与局内共用预制体 Resources/CardPrefabs/CardSlot.prefab，用 InstantiateCardSlot 生成即可。
    /// </summary>
    public class CardView : MonoBehaviour
    {
        /// <summary> 单张卡牌预制体路径（Resources 下），菜单 Tools→军阵对决→创建单张卡牌预制体 生成。 </summary>
        public const string CardSlotPrefabPath = "CardPrefabs/CardSlot";

        public CardData Data;
        public Image FaceImage;

        public Sprite FaceSprite
        {
            get => FaceImage != null ? FaceImage.sprite : null;
            set { if (FaceImage != null) { FaceImage.sprite = value; FaceImage.enabled = value != null; } }
        }

        /// <summary> 从 Resources 路径加载卡牌图；若贴图为 1024×1024 等正方形，会按 1016:1488 比例裁切，供图鉴详情/特殊形态使用 </summary>
        public static Sprite LoadCardSprite(string resourcesPathNoExtension)
        {
            return LoadSpriteFromResources(resourcesPathNoExtension);
        }

        /// <summary> 根据 CardData 的 CardId 从 Resources/Cards/ 加载卡面；支持 Sprite 或 Texture2D，可带扩展名 .png/.jpg/.jpeg </summary>
        public void LoadFaceSprite()
        {
            if (Data == null || FaceImage == null) return;
            string basePath = "Cards/" + Data.CardId;
            Sprite sprite = LoadSpriteFromResources(basePath);
            ApplyFaceSprite(sprite, basePath);
        }

        private void ApplyFaceSprite(Sprite sprite, string pathForLog)
        {
            if (FaceImage == null) return;
            FaceImage.preserveAspect = true;
            FaceImage.enabled = true;
            if (sprite == null)
            {
                FaceImage.sprite = GameUI.GetWhiteSprite();
                FaceImage.color = new Color(0.25f, 0.25f, 0.3f, 1f);
                Debug.LogWarning("[CardView] Source Image 未加载: " + pathForLog + "，请确认文件在 Resources/Cards 下且导入为 Sprite 或 Texture。");
            }
            else
            {
                FaceImage.sprite = sprite;
                FaceImage.color = Color.white;
            }
        }

        private const float CardAspectW = 1016f;
        private const float CardAspectH = 1488f;

        /// <summary> 若贴图为正方形（如 1024×1024），按 1016:1488 比例从中心裁切生成 Sprite，避免显示被拉伸 </summary>
        private static Sprite CreateCardSpriteFromTexture(Texture2D tex)
        {
            if (tex == null) return null;
            int w = tex.width;
            int h = tex.height;
            Rect rect;
            if (w == h)
            {
                float ratio = CardAspectW / CardAspectH;
                float rectW, rectH;
                if (ratio * h <= w) { rectH = h; rectW = (float)h * ratio; }
                else { rectW = w; rectH = (float)w / ratio; }
                float x = (w - rectW) * 0.5f;
                float y = (h - rectH) * 0.5f;
                rect = new Rect(x, y, rectW, rectH);
            }
            else
                rect = new Rect(0, 0, w, h);
            return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f));
        }

        /// <summary> 从 Resources 加载卡图。兼容：主资源为 Sprite、主资源为 Texture2D、以及带扩展名路径；若导入为 Sprite(2D and UI) 时主资源常为 Texture2D，需用 Object/Texture2D 或 LoadAll 取图。 </summary>
        private static Sprite LoadSpriteFromResources(string basePath)
        {
            Sprite sprite = null;
            Texture2D tex = null;
            var obj = Resources.Load(basePath);
            if (obj is Sprite s)
                sprite = s;
            else if (obj is Texture2D t)
                tex = t;
            if (sprite == null && tex == null)
            {
                sprite = Resources.Load<Sprite>(basePath);
                if (sprite == null)
                    tex = Resources.Load<Texture2D>(basePath);
            }
            if (sprite == null && tex == null)
            {
                var sprites = Resources.LoadAll<Sprite>(basePath);
                if (sprites != null && sprites.Length > 0)
                    sprite = sprites[0];
            }
            if (sprite == null && tex == null)
            {
                foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    obj = Resources.Load(basePath + ext);
                    if (obj is Sprite s2)
                    {
                        sprite = s2;
                        break;
                    }
                    if (obj is Texture2D t2)
                    {
                        tex = t2;
                        break;
                    }
                    sprite = Resources.Load<Sprite>(basePath + ext);
                    if (sprite != null) break;
                    tex = Resources.Load<Texture2D>(basePath + ext);
                    if (tex != null) break;
                }
            }
            if (tex != null)
                sprite = CreateCardSpriteFromTexture(tex);
            if (sprite != null && sprite.texture != null && sprite.texture.width == sprite.texture.height)
                sprite = CreateCardSpriteFromTexture(sprite.texture);
            return sprite;
        }

        private void Start()
        {
            if (FaceImage != null && FaceImage.sprite == null && Data != null)
                LoadFaceSprite();
        }

        /// <summary>
        /// 从预制体实例化一张卡牌并绑定数据，局内/图鉴通用。若无预制体返回 null，调用方可用代码创建。
        /// 用法：var go = CardView.InstantiateCardSlot(data); 若 go!=null 则设 parent/位置后即显示卡面。
        /// </summary>
        public static GameObject InstantiateCardSlot(CardData data)
        {
            var prefab = Resources.Load<GameObject>(CardSlotPrefabPath);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab);
            var view = go.GetComponent<CardView>();
            if (view != null && data != null)
            {
                view.Data = data;
                view.LoadFaceSprite();
            }
            return go;
        }

        /// <summary> 仅实例化预制体，不绑数据。局内可再设 view.FaceSprite = CardView.LoadCardSprite("Cards/NO001") 等。 </summary>
        public static GameObject InstantiateCardSlot()
        {
            var prefab = Resources.Load<GameObject>(CardSlotPrefabPath);
            return prefab != null ? Object.Instantiate(prefab) : null;
        }
    }
}
