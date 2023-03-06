using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;

namespace To_Pixel.Editor
{
    public partial class PixelArt : EditorWindow
    {
        public static PixelArt instance = null!;
        private static float whiteThreshold = 0.8f;
        private static int num = 6;
        private static bool replaceBool;
        private static string additive = " pix";
        public static PaletteType paletteType = PaletteType.Intelligent;
        private static Adjustment adjustment = Adjustment.BrightnessCurve;
        private static AnimationCurve curve;
        private static Texture2D preview;
        private bool advanced;
        private static float blackThreshold = 0.1f;
        private static float alphaThreshold = 0.7f;
        private static float connectivity = 0.1f;
        private static bool inclineConnectivity = true;

        public enum PaletteType
        {
            Existent,
            Intelligent
        }

        public enum Adjustment
        {
            Grayscale,
            BrightnessCurve
        }

        [MenuItem("Window/To Pixel")]
        private static void ShowWindow()
        {
            instance = GetWindow<PixelArt>();
            instance.titleContent = new GUIContent("To Pixel");
        }

        private void OnGUI()
        {
            if (additive == "")
            {
                additive = " pixel";
            }

            if (curve == null)
            {
                curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }

            num = EditorGUILayout.IntSlider(new GUIContent("Num", "Every Num pixels will be merged into one pixel")
                , num, 2, 20);
            EditorGUILayout.Space();

            advanced = EditorGUILayout.Foldout(advanced, "Advanced");

            if (advanced)
            {
                paletteType = (PaletteType)EditorGUILayout.EnumPopup(
                    new GUIContent("Palette Strategy", "Which type of palette to use."), paletteType);
                switch (paletteType)
                {
                    case PaletteType.Existent:
                        palette = (Palette)EditorGUILayout.EnumPopup(
                            new GUIContent("Palette Type",
                                "How many colors are in the existent palette. Palettes with fewer colors tend to be more like early video games"),
                            palette);
                        break;
                    case PaletteType.Intelligent:
                        difference = EditorGUILayout.Slider(
                            new GUIContent("Difference",
                                "How differences in two similar color can be accepted. Bigger value usually lead to less color and and vice versa."),
                            difference, 0.001f, 3f);
                        break;
                }

                EditorGUILayout.Space();
                connectivity = EditorGUILayout.Slider(
                    new GUIContent("Connectivity",
                        "In tradition pixel-art, adjacent pixel often have the same color. Connectivity is the impact of the effect. The lower it is, the slower To Pixel works. For realistic pictures it should be set very low or zero."),
                    connectivity, 0, 2f);
                inclineConnectivity =
                    EditorGUILayout.Toggle(
                        new GUIContent("Inclined Connectivity", "Support inclined connectivity influence or not."),
                        inclineConnectivity);
                EditorGUILayout.Space();

                adjustment = (Adjustment)EditorGUILayout.EnumPopup(
                    new GUIContent("Adjustment",
                        "Which way to make the image into pixel-art. Grayscale is based on human vision. Brightness is based on color curve. Realistic pictures should not use Grayscale."),
                    adjustment);
                switch (adjustment)
                {
                    case Adjustment.Grayscale:
                        whiteThreshold = EditorGUILayout.Slider(
                            new GUIContent("White Threshold",
                                "When the gray level of a pixel is higher than White Threshold, the pixel is displayed as white. In most cases( Num is larger than 5), this value should not be less than 0.75."),
                            whiteThreshold, 0, 1);
                        if (whiteThreshold < blackThreshold)
                        {
                            whiteThreshold = blackThreshold;
                        }

                        blackThreshold = EditorGUILayout.Slider(
                            new GUIContent("Black Threshold",
                                "When the gray level of a pixel is lower than Black Threshold, the pixel is displayed as black. In most cases( Num is larger than 5), this value should not be more than 0.15."),
                            blackThreshold, 0, 1);
                        if (blackThreshold > whiteThreshold)
                        {
                            blackThreshold = whiteThreshold;
                        }

                        break;
                    case Adjustment.BrightnessCurve:
                        curve = EditorGUILayout.CurveField(
                            new GUIContent("Brightness Curve",
                                "The horizontal axis represents the original brightness, and the vertical axis represents the processed brightness."),
                            curve);
                        break;
                }

                EditorGUILayout.Space();
                alphaThreshold = EditorGUILayout.Slider(
                    new GUIContent("Alpha Threshold",
                        "When the alpha of a pixel is higher than Alpha Threshold, the pixel is displayed as black. Reduce this can bring the outline effect."),
                    alphaThreshold, 0.001f, 1);
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();

            replaceBool = EditorGUILayout.Toggle(new GUIContent("Replace the original texture",
                    "If true, the output texture will replace the original texture."),
                replaceBool);

            additive = replaceBool
                ? ""
                : EditorGUILayout.TextField(
                    new GUIContent("Additive",
                        "Name additive for the new Texture. For example, the name of original texture is test.png, and Additive is _1, then the name of output texture is test_1.png."),
                    additive);


            EditorGUILayout.HelpBox(
                "The Import Settings will be adjusted appropriately according to the original texture import settings and inherent attributes.",
                MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview") || preview == null)
            {
                Work(false);
                preview.filterMode = FilterMode.Point;
                preview.alphaIsTransparency = true;
                preview.Apply();
            }

            if (GUILayout.Button("Generate"))
            {
                Work(true);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
            float width = EditorGUIUtility.currentViewWidth * 2 / 3;
            float height = preview.height * width / preview.width;
            Rect controlRect = EditorGUILayout.GetControlRect(false, preview.height);
            controlRect.x = width / 4;
            controlRect.width = width;
            controlRect.height = height;
            GUI.DrawTexture(controlRect, preview);
        }

        public static Color ToPixel(Color[] cs)
        {
            float rAv = 0;
            float gAv = 0;
            float bAv = 0;
            float aAv = 0;

            for (int i = 0; i < cs.Length; i++)
            {
                rAv += cs[i].r * cs[i].a;
                gAv += cs[i].g * cs[i].a;
                bAv += cs[i].b * cs[i].a;
                aAv += cs[i].a;
            }

            rAv /= cs.Length;
            gAv /= cs.Length;
            bAv /= cs.Length;
            aAv /= cs.Length;


            float r;
            float g;
            float b;
            if (adjustment == Adjustment.Grayscale)
            {
                float bright = 0.299f * rAv + 0.587f * gAv + 0.114f * bAv;
                if (bright >= whiteThreshold)
                {
                    r = 1;
                    g = 1;
                    b = 1;
                }
                else if (bright <= blackThreshold)
                {
                    r = 0;
                    g = 0;
                    b = 0;
                }
                else
                {
                    r = rAv;
                    g = gAv;
                    b = bAv;
                }
            }
            else
            {
                r = curve.Evaluate(rAv);
                g = curve.Evaluate(gAv);
                b = curve.Evaluate(bAv);
            }

            float a = aAv >= alphaThreshold ? 1 : 0;
            return new Color(r, g, b, a);
        }

        private static void Work(bool generate)
        {
            if (additive == "")
            {
                additive = " pix";
            }

            if (paletteType == PaletteType.Existent)
            {
                Method1_CreatePalette();
            }

            Object[] texture2Ds = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);

            if (texture2Ds.Length == 0 && !generate)
            {
                preview = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            }


            foreach (Object o in texture2Ds)
            {
                if (!generate && o != texture2Ds[0])
                {
                    return;
                }

                Texture2D texture2D = (Texture2D)o;
                string oldRelativePath = AssetDatabase.GetAssetPath(texture2D);
                string newRelativePath = replaceBool
                    ? oldRelativePath
                    : oldRelativePath.Replace(".jpg", additive + ".jpg").Replace(".png", additive + ".png");
                string newAbsolutePath = Application.dataPath + newRelativePath.Substring(6);

                SetReadable(oldRelativePath, true);
                Texture2D newTexture2D = new Texture2D(texture2D.width / num, texture2D.height / num);
                if (paletteType == PaletteType.Existent)
                {
                    PictureToPixel_Method1(newTexture2D, texture2D);
                }
                else
                {
                    PictureToPixel_Method2(newTexture2D, texture2D);
                }

                

                if (generate)
                {
                    Create(newAbsolutePath, newTexture2D);
                    SetNewTI(newRelativePath, oldRelativePath);
                    SetReadable(newRelativePath, false);
                }
                else
                {
                    preview = newTexture2D;
                }
            }
        }


        private static void SetReadable(string oldRelativePath, bool readable)
        {
            TextureImporter TI = AssetImporter.GetAtPath(oldRelativePath) as TextureImporter;
            if (TI == null) return;
            TI.isReadable = readable;
            AssetDatabase.ImportAsset(oldRelativePath);
        }

        private static void SetNewTI(string replacePath, string oldPath)
        {
            TextureImporter TI = AssetImporter.GetAtPath(oldPath) as TextureImporter;
            TextureImporter newTI = AssetImporter.GetAtPath(replacePath) as TextureImporter;
            if (TI == null || newTI == null)
                return;

            TextureImporterSettings tis = new TextureImporterSettings();
            TI.ReadTextureSettings(tis);
            newTI.SetTextureSettings(tis);
            newTI.filterMode = FilterMode.Point;
            newTI.spritePixelsPerUnit = TI.spritePixelsPerUnit / num;

            newTI.spritesheet = TI.spritesheet.Select(spriteMetaData => new SpriteMetaData
            {
                name = spriteMetaData.name,
                alignment = spriteMetaData.alignment,
                border = spriteMetaData.border / num,
                pivot = spriteMetaData.pivot,
                rect = new Rect(spriteMetaData.rect.x / num, spriteMetaData.rect.y / num,
                    spriteMetaData.rect.width / num,
                    spriteMetaData.rect.height / num)
            }).ToArray();

            AssetDatabase.ImportAsset(replacePath);
        }

        private static void Create(string newAbsolutePath, Texture2D newTexture2D)
        {
            byte[] dataBytes = newTexture2D.EncodeToPNG();
            FileStream fileStream = File.Open(newAbsolutePath, FileMode.OpenOrCreate);
            fileStream.Write(dataBytes, 0, dataBytes.Length);
            fileStream.Close();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}