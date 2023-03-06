using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace To_Pixel.Editor
{
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public partial class PixelArt
    {
        private static Color[] colorPalette = new Color[1];
        private static Palette palette;
        
        public enum Palette
        {
            Color256,
            Color128,
            Color64,
            Color48,
            Color32
        }

        private static void PictureToPixel_Method1(Texture2D newTexture2D, Texture2D texture2D)
        {
            Color[][] results = new Color[newTexture2D.width][];
            for (int x = 0; x < newTexture2D.width; x++)
            {
                results[x] = new Color[newTexture2D.height];
                for (int y = 0; y < newTexture2D.height; y++)
                {
                    Color[] block = texture2D.GetPixels(x * num, y * num, num, num);

                    if (block.Length > 0)
                    {
                        Color average = ToPixel(block);
                        results[x][y] = Method1(newTexture2D, average, x, y, results);
                    }
                }
            }

            newTexture2D.Apply();
        }

        private static Color FindColor(Color average, Color[][] results, int x, int y)
        {
            float min;
            float y_1 = y > 0 ? Similarity(average, results[x][y - 1]) : 99;
            float x_1 = x > 0 ? Similarity(average, results[x - 1][y]) : 99;
            float xy_11 = 99;
            float xy_12 = 99;
            if (inclineConnectivity)
            {
                xy_11 = x > 0 && y > 0 ? Similarity(average, results[x - 1][y - 1]) : 99;
                xy_12 = x > 0 && y < results[x - 1].Length - 1
                    ? Similarity(average, results[x - 1][y + 1])
                    : 99;
                min = Mathf.Min(y_1, x_1, xy_11, xy_12);
            }
            else
            {
                min = Mathf.Min(y_1, x_1);
            }

            if (min <= difference * connectivity)
            {
                Color findLast;
                if (min == y_1)
                {
                    findLast = results[x][y - 1];
                }
                else if (min == x_1)
                {
                    findLast = results[x - 1][y];
                }
                else if (min == xy_11)
                {
                    findLast = results[x - 1][y - 1];
                }
                else
                {
                    findLast = results[x - 1][y + 1];
                }

                return new Color(findLast.r, findLast.g, findLast.b, average.a);
            }
            else
            {
                float near = 1000;
                int index = 0;

                for (int cl = 0; cl < colorPalette.Length; cl++)
                {
                    Color paletteColor = colorPalette[cl];
                    float similarity = Similarity(paletteColor, average);

                    if (similarity < near)
                    {
                        index = cl;
                        near = similarity;
                    }
                }

                Color c = colorPalette[index];
                return new Color(c.r, c.g, c.b, average.a);
            }
        }

        private static float Similarity(Color color1, Color color2)
        {
            float difR = color1.r - color2.r;
            float difG = color1.g - color2.g;
            float difB = color1.b - color2.b;
            return difR * difR + difG * difG + difB * difB;
        }

        private static void Method1_CreatePalette()
        {
            string path;
            switch (palette)
            {
                case Palette.Color256:
                    path = "256Color";
                    break;
                case Palette.Color128:
                    path = "128Color";
                    break;
                case Palette.Color64:
                    path = "64Color";
                    break;
                case Palette.Color48:
                    path = "48Color";
                    break;
                case Palette.Color32:
                    path = "32Color";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            TextAsset colorAsset = Resources.Load<TextAsset>(path);
            string[] colorStrings = colorAsset.text.Split('\n');
            colorPalette = new Color[colorStrings.Length];
            for (int i = 0; i < colorStrings.Length; i++)
            {
                string readLine = colorStrings[i].Trim();
                if (ColorUtility.TryParseHtmlString(readLine, out Color fromHtml))
                {
                    colorPalette[i] = fromHtml;
                }
                else
                {
                    Debug.LogWarning("can not parse this color" + readLine);
                }
            }
        }


        private static Color Method1(Texture2D newTexture2D, Color average, int x, int y, Color[][] results)
        {
            average = FindColor(average, results, x, y);
            newTexture2D.SetPixel(x, y, average);
            return average;
        }
    }
}