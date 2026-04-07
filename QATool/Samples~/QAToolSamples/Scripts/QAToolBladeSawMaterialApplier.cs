using UnityEngine;

namespace QATool
{
    public class QAToolBladeSawMaterialApplier : MonoBehaviour
    {
        public enum ObjectColor
        {
            Red,
            Blue,
            Gold,
            Silver,
            Black
        }

        public ObjectColor selectedColor;

        private Material runtimeMaterial;

        void Start()
        {
            CreateAndApplyMaterial();
        }

        void OnValidate()
        {
            CreateAndApplyMaterial();
        }

        void CreateAndApplyMaterial()
        {
            if (runtimeMaterial == null)
            {
                runtimeMaterial = new Material(Shader.Find("Standard"));
            }


            runtimeMaterial.color = GetColor(selectedColor);


            runtimeMaterial.SetFloat("_Metallic", 1f);      
            runtimeMaterial.SetFloat("_Glossiness", 0.8f);    

            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer rend in renderers)
            {
                rend.material = runtimeMaterial;
            }
        }

        Color GetColor(ObjectColor color)
        {
            switch (color)
            {
                case ObjectColor.Red: return new Color(0.8f, 0.1f, 0.1f);
                case ObjectColor.Blue: return new Color(0.1f, 0.2f, 0.8f);
                case ObjectColor.Gold: return new Color(1.0f, 0.84f, 0.0f);
                case ObjectColor.Silver: return new Color(0.75f, 0.75f, 0.75f);
                case ObjectColor.Black: return new Color(0.1f, 0.1f, 0.1f);
                default: return Color.white;
            }
        }
    }
}
