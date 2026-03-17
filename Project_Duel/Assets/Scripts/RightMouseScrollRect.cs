using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JunzhenDuijue
{
    /// <summary>
    /// 仅响应鼠标右键拖动的 ScrollRect，左键保留给选牌等操作。
    /// </summary>
    public class RightMouseScrollRect : ScrollRect
    {
        private Vector2 _rightPrev;
        private bool _rightDragging;

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;
            _rightDragging = true;
            _rightPrev = eventData.position;
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;
            base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;
            _rightDragging = false;
            base.OnEndDrag(eventData);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
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

            if (horizontal)
            {
                Vector2 p = content.anchoredPosition;
                p.x -= delta.x;
                content.anchoredPosition = p;
            }
        }
    }
}
