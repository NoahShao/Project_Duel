using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 挂在介绍弹窗上：弹窗显示时每帧将位置设为鼠标位置 + 偏移，实现跟随鼠标。
    /// </summary>
    public class DetailIntroFollowMouse : MonoBehaviour
    {
        private const float OffsetX = 16f;
        private const float OffsetY = -16f;

        private RectTransform _rect;
        private RectTransform _parentRect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (transform.parent != null)
                _parentRect = transform.parent as RectTransform;
        }

        private void LateUpdate()
        {
            if (!enabled || _rect == null || _parentRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRect, Input.mousePosition, null, out Vector2 localFromCenter))
            {
                float pw = _parentRect.rect.width, ph = _parentRect.rect.height;
                Vector2 offsetFromTopLeft = new Vector2(
                    localFromCenter.x + pw * 0.5f,
                    localFromCenter.y - ph * 0.5f);
                _rect.anchoredPosition = new Vector2(offsetFromTopLeft.x + OffsetX, offsetFromTopLeft.y + OffsetY);
            }
        }
    }
}
