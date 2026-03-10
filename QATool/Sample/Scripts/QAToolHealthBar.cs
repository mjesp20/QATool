using UnityEngine;
using UnityEngine.UI;

namespace QATool
{
    public class QAToolHealthBar : MonoBehaviour
    {
        public QAToolPlayerHealth playerHealth;
        public Image fillImage;
        
        public Camera mainCamera;

        void Update()
        {
            if (playerHealth != null && fillImage != null)
            {
                fillImage.fillAmount = playerHealth.currentHealth / playerHealth.maxHealth;
            }
        }
        
        void LateUpdate()
        {
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }
    }
}