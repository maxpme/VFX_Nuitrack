using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nuitrack;
using Vector3 = UnityEngine.Vector3;

public class FrameData : MonoBehaviour
{
    private List<Vector3> _positions = new List<Vector3>();
    private Color[] _colors = new Color[640*480];

    public List<Vector3> Positions { get => _positions; private set => _positions = value; }
    public Color[] Colors { get => _colors; private set => _colors = value; }


    private void Start()
    {
        NuitrackManager.onDepthUpdate += getDepthFrame;
        //NuitrackManager.onColorUpdate += getColorFrame;
    }

    private void getColorFrame(ColorFrame cf)
    {
        Colors = null;
        Color currentColor = new Color();
        int index = 0;
        int height = cf.Rows;
        int width = cf.Cols;
        for(int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                currentColor = new Color32(cf[x, y].Red, cf[x, y].Green, cf[x, y].Blue, 255);
                Colors[index] = currentColor;
                index++;
            }
        }
    }
    private void getDepthFrame(DepthFrame df)
    {
        Positions.Clear();
        Vector3 _positionCoords = new Vector3();

        
        int height = df.Rows;
        int width = df.Cols;

        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                _positionCoords = NuitrackManager.DepthSensor.ConvertProjToRealCoords(x, y, df[y,x]).ToVector3();
                Positions.Add(_positionCoords);
            }
        }
    }
}
