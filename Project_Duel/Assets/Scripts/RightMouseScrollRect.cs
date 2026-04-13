using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 仅响应鼠标右键拖动的简易横向拖拽器，不再依赖 Unity ScrollRect 的自动回弹和布局修正。
    /// </summary>
    public class RightMouseScrollRect : MonoBehaviour
    {
        public RectTransform content;
        public RectTransform viewport;
        public bool horizontal = true;
        public bool vertical;

        private Vector2 _rightPrev;
        private bool _rightDragging;

        public void ClampToBounds(bool alignToStart = false)
        {
            if (content == null || viewport == null)
                return;

            Vector2 anchored = content.anchoredPosition;
            if (horizontal)
            {
                float minX = Mathf.Min(0f, viewport.rect.width - content.rect.width);
                anchored.x = alignToStart ? 0f : Mathf.Clamp(anchored.x, minX, 0f);
            }

            if (vertical)
            {
                float maxY = Mathf.Max(0f, content.rect.height - viewport.rect.height);
                anchored.y = alignToStart ? 0f : Mathf.Clamp(anchored.y, 0f, maxY);
            }

            content.anchoredPosition = anchored;
        }

        private void Update()
        {
            ClampToBounds();

            if (Input.GetMouseButtonDown(1))
            {
                if (viewport != null && !RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, null))
                    return;
                _rightDragging = true;
                _rightPrev = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1))
                _rightDragging = false;

            if (!_rightDragging || !Input.GetMouseButton(1) || content == null)
                return;

            Vector2 now = Input.mousePosition;
            Vector2 delta = now - _rightPrev;
            _rightPrev = now;

            Vector2 anchored = content.anchoredPosition;
            if (horizontal)
                anchored.x -= delta.x;
            if (vertical)
                anchored.y -= delta.y;

            content.anchoredPosition = anchored;
            ClampToBounds();
        }
    }
}
