using UnityEngine;
using UnityEngine.EventSystems;

namespace JunzhenDuijue
{
    /// <summary>
    /// 挂在介绍弹窗上：鼠标移出弹窗时关闭。
    /// </summary>
    public class IntroOverlayHideOnExit : MonoBehaviour, IPointerExitHandler
    {
        public void OnPointerExit(PointerEventData eventData)
        {
            CompendiumUI.HideIntroModal();
        }
    }
}
