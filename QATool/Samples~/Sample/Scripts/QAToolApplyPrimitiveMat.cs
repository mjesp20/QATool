using UnityEngine;

namespace QATool
{
    public class QAToolApplyPrimitiveMat : MonoBehaviour
    {
      
        public enum ObjectColor
        {
            Red,
            Green,
            Blue,
            gray,
            gray2,
            gray7,
            White,
            Black
        }

        public ObjectColor selectedColor;

        private Renderer objectRenderer;

        void Start()
        {
            objectRenderer = GetComponent<Renderer>();
            ApplyColor();
        }

        public void ApplyColor()
        {
            if (objectRenderer == null) return;

            objectRenderer.material.color = GetColor(selectedColor);
        }

        private Color GetColor(ObjectColor color)
        {
            switch (color)
            {
                case ObjectColor.Red: return Color.red;
                case ObjectColor.Green: return Color.green;
                case ObjectColor.Blue: return Color.blue;
                case ObjectColor.gray: return Color.gray;
                case ObjectColor.gray2: return new Color(0.2f, 0.2f, 0.2f, 1f);
                case ObjectColor.gray7: return new Color(0.7f, 0.7f, 0.7f, 1f);
                case ObjectColor.White: return Color.white;
                case ObjectColor.Black: return Color.black;
                default: return Color.white;
            }
        }
    }
}
