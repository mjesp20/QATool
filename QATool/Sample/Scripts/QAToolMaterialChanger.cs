using UnityEngine;

namespace QATool
{
    public class MaterialChanger : MonoBehaviour
    {
        public Material newMaterial;

        private Renderer objectRenderer;

        void Start()
        {
            objectRenderer = GetComponent<Renderer>();

            if (objectRenderer == null)
            {
                Debug.LogError("No Renderer found on this GameObject!");
                return;
            }

            ApplyMaterial();
        }

        public void ApplyMaterial()
        {
            if (newMaterial != null)
            {
                objectRenderer.material = newMaterial;
            }
            else
            {
                Debug.LogWarning("No material assigned!");
            }
        }
    }
}
