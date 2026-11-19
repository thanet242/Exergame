using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CookieRun
{
    /// <summary>
    /// ปุ่มสัมผัสบนหน้าจอ (Touch / UI) สำหรับรองรับการกดค้าง (เช่น สไลด์) และกดครั้งเดียว (เช่น กระโดด)
    /// </summary>
    public class CookieTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Events")]
        public UnityEvent onPointerDown;
        public UnityEvent onPointerUp;

        private bool isPressed = false;
        public bool IsPressed => isPressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            onPointerDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            onPointerUp?.Invoke();
        }

        private void OnDisable()
        {
            if (isPressed)
            {
                isPressed = false;
                onPointerUp?.Invoke();
            }
        }
    }
}
