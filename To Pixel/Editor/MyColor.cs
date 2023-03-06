using UnityEngine;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace To_Pixel.Editor
{
    public class MyColor
    {
        private Vector3 color;
        private float times;

        public MyColor(Color oriColor)
        {
            color = Color2Vector(oriColor);
            times = 1;
        }

        public MyColor()
        {
            color = new Vector3(-99, -99, -99);
        }

        public float Similarity(Color newColor)
        {
            return Vector3.Distance(Color2Vector(newColor), color);
        }

        public void Mix(Color newColor)
        {
            color = color * times + Color2Vector(newColor);
            times++;
            color /= times;
        }

        private static Vector3 Color2Vector(Color newColor)
        {
            return new Vector3(newColor.r, newColor.g, newColor.b);
        }

        public Color finalColor;

        public Color MakeColor()
        {
            finalColor = color.x == -99 ? Color.clear : new Color(color.x, color.y, color.z);
            return finalColor;
        }
    }
}