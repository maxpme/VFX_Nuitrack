using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nuitrack;
using Vector3 = UnityEngine.Vector3;

public class FrameData : MonoBehaviour
{
    [SerializeField] private int scale = 1000;
    [SerializeField] private int width = 640;
    [SerializeField] private int height = 480;
    [SerializeField] private RenderTexture colorMap;
    [SerializeField] private RenderTexture positionMap;
    [SerializeField] private float threshold = 4;
    [SerializeField] private int scaleZ = 1000;

    [SerializeField] private ComputeShader _colorCompute;

    private int texWidth;
    private int texHeight;
    private Color data;

    Texture2D texColors;
    Texture2D texPositions;




    private List<Vector3> _positions = new List<Vector3>();
    private Color[] _colors = new Color[640 * 480];
    private Color[] _positionsArray = new Color[640 * 480];

    public List<Vector3> Positions { get => _positions; private set => _positions = value; }
    public Color[] Colors { get => _colors; private set => _colors = value; }


    private void Start()
    {
        NuitrackManager.onColorUpdate += GetColorFrame;
        NuitrackManager.onDepthUpdate += GetDepthFrame;

        texColors = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        texPositions = new Texture2D(width, height, TextureFormat.RGBAFloat, false);

        texWidth = texPositions.width;
        texHeight = texPositions.height;
    }

    private void GetColorFrame(ColorFrame cf)
    {
        Color currentColor = new Color();
        int index = 0;
        int height = cf.Rows;
        int width = cf.Cols;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                currentColor = new Color32(cf[y, x].Red, cf[y, x].Green, cf[y, x].Blue, 255);
                Colors[index] = currentColor;
                index++;
            }
        }
        texColors.SetPixels(Colors);
        texColors.Apply();

        RenderTexture.active = colorMap;
        Graphics.Blit(texColors, colorMap);
    }
    private void GetDepthFrame(DepthFrame df)
    {
        int height = df.Rows;
        int width = df.Cols;

        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (df[y, x] < 0.1f || df[y, x] > threshold * scale)
                {
                    data = new Color32(0, 0, 0, 0);
                }
                else
                {
                    data = new Color((float)x / scale, -(float)y / scale, (float)df[y, x] / scaleZ, 1);
                }
                _positionsArray[index] = data;
                index++;
            }
        }
        texPositions.SetPixels(_positionsArray);
        texPositions.Apply();

        RenderTexture.active = positionMap;
        Graphics.Blit(texPositions, positionMap);
    }
}
