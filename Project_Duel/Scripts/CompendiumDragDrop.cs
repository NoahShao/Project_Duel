using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace JunzhenDuijue
{
    /// <summary>
    /// 挂在图鉴卡牌或牌组内卡牌上，用于拖拽。图鉴卡拖到牌组区域则加入牌组；牌组卡拖到图鉴区域则移回图鉴。
    /// </summary>
    public class CompendiumDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string CardId;
        public bool IsDeckCard;

        private RectTransform _rt;
        private GameObject _ghost;
        private Canvas _canvas;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_rt == null || _canvas == null) return;
            _ghost = new GameObject("DragGhost");
            _ghost.transform.SetParent(_canvas.transform, false);
            var ghostRt = _ghost.AddComponent<RectTransform>();
            ghostRt.sizeDelta = _rt.sizeDelta;
            var ghostImg = _ghost.AddComponent<Image>();
            var cv = GetComponent<CardView>();
            if (cv != null && cv.FaceImage != null)
            {
                ghostImg.sprite = cv.FaceSprite;
                ghostImg.color = cv.FaceImage.color;
                ghostImg.preserveAspect = true;
            }
            else
                ghostImg.color = new Color(0.3f, 0.3f, 0.35f, 0.9f);
            var cg = _ghost.AddComponent<CanvasGroup>();
            cg.alpha = 0.85f;
            cg.blocksRaycasts = false;
            _ghost.GetComponent<RectTransform>().position = _rt.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost != null && eventData.dragging)
                _ghost.GetComponent<RectTransform>().position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
            }

            var dropZone = FindDropZone(eventData.position);
            if (dropZone == null) return;

            if (dropZone.IsDeckZone && !IsDeckCard)
                CompendiumUI.TryAddCardToEditingDeck(CardId);
            else if (dropZone.IsCompendiumZone && IsDeckCard)
                CompendiumUI.TryRemoveCardFromEditingDeck(CardId);
        }

        private static DropZone FindDropZone(Vector2 screenPos)
        {
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPos }, results);
            foreach (var r in results)
            {
                var dz = r.gameObject.GetComponent<DropZone>();
                if (dz != null) return dz;
            }
            return null;
        }
    }

    /// <summary>
    /// 挂在可接受拖放的区域上，标记为牌组区域或图鉴区域。
    /// </summary>
    public class DropZone : MonoBehaviour
    {
        public bool IsDeckZone;
        public bool IsCompendiumZone;
    }
}
