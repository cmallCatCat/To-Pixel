using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace To_Pixel.Editor
{
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public partial class PixelArt
    {
        private static float difference = 0.25f;


        private static void PictureToPixel_Method2(Texture2D newTexture2D, Texture2D texture2D)
        {
            MyColor transparent = new MyColor();
            List<MyColor> existColor = new List<MyColor>(64) { transparent };
            MyColor[][] colorsTable = new MyColor[newTexture2D.width][];

            for (int x = 0; x < newTexture2D.width; x++)
            {
                colorsTable[x] = new MyColor[newTexture2D.height];
                for (int y = 0; y < newTexture2D.height; y++)
                {
                    Color[] block = texture2D.GetPixels(x * num, y * num, num, num);

                    if (block.Length > 0)
                    {
                        Color average = ToPixel(block);
                        Method2_CreateTargetColor(average, colorsTable, x, y, existColor, transparent);
                    }
                }
            }

            Method2_Draw(newTexture2D, existColor, colorsTable, texture2D);
            newTexture2D.Apply();
        }

        private static void Method2_Draw(Texture2D newTexture2D, List<MyColor> existColor, MyColor[][] colorsTable,
            Texture2D texture2D)
        {
            colorPalette = new Color[existColor.Count];
            for (int index = 0; index < existColor.Count; index++)
            {
                MyColor myColor = existColor[index];
                colorPalette[index] = myColor.MakeColor();
            }

            PictureToPixel_Method1(newTexture2D, texture2D);
        }

        private static void Method2_CreateTargetColor(Color average, MyColor[][] colorsTable, int x, int y,
            List<MyColor> existColor, MyColor transparent)
        {
            if (average.a == 0)
            {
                colorsTable[x][y] = transparent;
                return;
            }

            MyColor findLast;
            float min;
            float y_1 = y > 0 && colorsTable[x][y - 1] != transparent ? colorsTable[x][y - 1].Similarity(average) : 99;
            float x_1 = x > 0 && colorsTable[x - 1][y] != transparent ? colorsTable[x - 1][y].Similarity(average) : 99;
            float xy_11 = 99;
            float xy_12 = 99;
            if (inclineConnectivity)
            {
                xy_11 = x > 0 && y > 0 && colorsTable[x - 1][y - 1] != transparent
                    ? colorsTable[x - 1][y - 1].Similarity(average)
                    : 99;
                xy_12 = x > 0 && y < colorsTable[x - 1].Length - 1 && colorsTable[x - 1][y + 1] != transparent
                    ? colorsTable[x - 1][y + 1].Similarity(average)
                    : 99;
                min = Mathf.Min(y_1, x_1, xy_11, xy_12);
            }
            else
            {
                min = Mathf.Min(y_1, x_1);
            }

            if (min <= difference * connectivity)
            {
                if (min == y_1)
                {
                    findLast = colorsTable[x][y - 1];
                }
                else if (min == x_1)
                {
                    findLast = colorsTable[x - 1][y];
                }
                else if (min == xy_11)
                {
                    findLast = colorsTable[x - 1][y - 1];
                }
                else
                {
                    findLast = colorsTable[x - 1][y + 1];
                }
            }
            else
            {
                int index = -1;
                float minDiff = 99;
                float similarity = 99;
                for (int i = 0; i < existColor.Count; i++)
                {
                    similarity = existColor[i].Similarity(average);
                    if (similarity < minDiff)
                    {
                        index = i;
                        minDiff = similarity;
                    }
                }

                findLast = similarity < difference ? existColor[index] : null;
            }

            if (findLast != null)
            {
                findLast.Mix(average);
                colorsTable[x][y] = findLast;
            }
            else
            {
                MyColor newColor = new MyColor(average);
                existColor.Add(newColor);
                colorsTable[x][y] = newColor;
            }
        }
    }
}