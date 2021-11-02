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

    private int texWidth;
    private int texHeight;
    private Color data;

    Texture2D texColors;
    Texture2D texPositions;




    private List<Vector3> _positions = new List<Vector3>();
    private Color[] _colors = new Color[640 * 480];

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
        //Colors = null;
        Color currentColor = new Color();
        int index = 0;
        int height = cf.Rows;
        int width = cf.Cols;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                currentColor = new Color32(cf[y, x].Red, cf[y, x].Green, cf[y, x].Blue, 255);
                texColors.SetPixel(x, y, currentColor);
                index++;
            }
        }
        texColors.Apply();

        RenderTexture.active = colorMap;
        Graphics.Blit(texColors, colorMap);
    }
    private void GetDepthFrame(DepthFrame df)
    {
        //Positions.Clear();

        int height = df.Rows;
        int width = df.Cols;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (df[y, x] < 0.1f || df[y, x] > threshold * scale)
                {
                    data = new Color(0.0f, 0, 0, 0.0f);
                }
                else
                {
                    //_projCoords = new Vector3(x, y, df[y, x]);
                    //_positionCoords = NuitrackManager.DepthSensor.ConvertProjToRealCoords(x, y, df[y, x]).ToVector3();

                    //Positions.Add(_positionCoords);
                    data = new Color((float)x / scale, -(float)y / scale, (float)df[y, x] / scaleZ, 1.0f);
                }
                //texColor.SetPixel(x, y, colors[index]);
                texPositions.SetPixel(x, y, data);
            }
        }

        texPositions.Apply();

        RenderTexture.active = positionMap;
        Graphics.Blit(texPositions, positionMap);
    }
}
