using UnityEngine;

public class PortalVFXTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // ค้นหา PlayerController ในฉากเพื่อสั่งเปิด Summary Popup
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.ShowSummaryPopup();
            }
            else
            {
                // กรณีหาที่ตัว Player ไม่เจอ ให้ลองค้นหาผ่าน GameObject
                PlayerController foundPlayer = FindObjectOfType<PlayerController>();
                if (foundPlayer != null)
                {
                    foundPlayer.ShowSummaryPopup();
                }
            }
        }
    }
}