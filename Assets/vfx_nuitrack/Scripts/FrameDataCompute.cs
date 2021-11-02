using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nuitrack;
using Vector3 = UnityEngine.Vector3;
using System.Runtime.InteropServices;

public class FrameDataCompute : MonoBehaviour
{
    [SerializeField] private int scale = 1000;
    [SerializeField] private int width = 640;
    [SerializeField] private int height = 480;
    [SerializeField] private RenderTexture colorMap;
    [SerializeField] private RenderTexture positionMap;
    [SerializeField] private float threshold = 4;
    [SerializeField] private int scaleZ = 1000;

    [SerializeField] private ComputeShader _colorCompute;
    [SerializeField] private ComputeShader _positionCompute;

    private int texWidth;
    private int texHeight;
    private Color data;

    private ComputeBuffer _colorBuffer;
    private ComputeBuffer _positionBuffer;
    private uint _threadGroupSizeX;
    private uint _threadGroupSizeY;

    private RenderTexture _tempPositionMap;
    private RenderTexture _tempColorMap;


    Texture2D texColors;
    Texture2D texPositions;




    private List<Vector3> _positions = new List<Vector3>();
    private Color[] _colors = new Color[640 * 480];
    private Color[] _positionsArray = new Color[640 * 480];
    private byte[] _colorArr = new byte[640 * 480 * 3];

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

        //_colorBuffer = new ComputeBuffer(640 * 480, sizeof(float) * 4);
        _colorBuffer = new ComputeBuffer(640 * 480, sizeof(float)*4);
        _positionBuffer = new ComputeBuffer(640 * 480, sizeof(float)*4);

        _tempColorMap = new RenderTexture(640, 480, 0, colorMap.format);
        _tempColorMap.enableRandomWrite = true;
        _tempColorMap.Create();

        _tempPositionMap = new RenderTexture(640, 480, 0, positionMap.format);
        _tempPositionMap.enableRandomWrite = true;
        _tempPositionMap.Create();
    }

    private void GetColorFrame(ColorFrame cf)
    {
        
        Color currentColor = new Color32();
        int index = 0;
        int height = cf.Rows;
        int width = cf.Cols;

        /*for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //currentColor.(cf[y, x].Red, cf[y, x].Green, cf[y, x].Blue, 255);
                currentColor.r = cf[y, x].Red;
                currentColor.g = cf[y, x].Green;
                currentColor.b = cf[y, x].Blue;
                currentColor.a = 255;
                //Colors[index] = currentColor;
                
                index++;
            }
        }*/
        Marshal.Copy(cf.Data, _colorArr, 0, cf.DataSize);
        //UnsafeUtility.SetUnmanagedData(_colorBuffer, cf.Data, cf.Cols*cf.Rows, sizeof(float)*4);
        /*
        
        /*65ms - 15fps*/
        /*
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                currentColor = new Color32(cf[y, x].Red, cf[y, x].Green, cf[y, x].Blue, 255);
                Colors[index] = currentColor;
                index++;
            }
        }

        /**/
        /*1ms*/

        int colorComputeKernel = _colorCompute.FindKernel("ColorMain");
        _colorCompute.GetKernelThreadGroupSizes(colorComputeKernel, out _threadGroupSizeX,
            out _threadGroupSizeY, out _);


        _colorCompute.SetTexture(colorComputeKernel, "ColorMap", _tempColorMap);

        //_colorBuffer.SetData(Colors);
        _colorBuffer.SetData(_colorArr);
        

        _colorCompute.SetBuffer(colorComputeKernel, "ColorBuffer", _colorBuffer);

        var threadGroupsX = (int)(640 / _threadGroupSizeX);
        var threadGroupsY = (int)(480 / _threadGroupSizeY);
        _colorCompute.Dispatch(colorComputeKernel,threadGroupsX,threadGroupsY,1);
        /**/

        
       /*0ms*/
        
        Graphics.CopyTexture(_tempColorMap, colorMap);
        
        /**/
    }
    private void GetDepthFrame(DepthFrame df)
    {
        /*47ms*/
        //watch.Restart();
        int height = df.Rows;
        int width = df.Cols;

        int index = 0;
        /*Parallel.For(0, height, y =>
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
        });*/
        

        /*30ms - 22fps*/
        /*
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
        
        /**/

        //texPositions.SetPixels(_positionsArray);
        //texPositions.Apply();

        //RenderTexture.active = positionMap;
        //Graphics.Blit(texPositions, positionMap);
        /*
        int positionComputeKernel = _positionCompute.FindKernel("PositionMain");
        _colorCompute.GetKernelThreadGroupSizes(positionComputeKernel, out _threadGroupSizeX,
            out _threadGroupSizeY, out _);


        _colorCompute.SetTexture(positionComputeKernel, "PositionMap", _tempPositionMap);

        _positionBuffer.SetData(_positionsArray);

        _colorCompute.SetBuffer(positionComputeKernel, "PositionBuffer", _positionBuffer);

        var threadGroupsX = (int)(640 / _threadGroupSizeX);
        var threadGroupsY = (int)(480 / _threadGroupSizeY);
        _colorCompute.Dispatch(positionComputeKernel, threadGroupsX, threadGroupsY, 1);
        /**/
        Graphics.CopyTexture(_tempPositionMap, positionMap);

        //watch.Stop();
        //UnityEngine.Debug.Log("work color compute in {0}");
        //UnityEngine.Debug.Log(watch.ElapsedMilliseconds);
    }
}
