using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nuitrack;
using Vector3 = UnityEngine.Vector3;
using System.Runtime.InteropServices;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine.Experimental.Rendering;

public class FrameDataCompute : MonoBehaviour
{
    [SerializeField] private int scale = 1000;
    [SerializeField] private int width = 640;
    [SerializeField] private int height = 480;
    [SerializeField] private RenderTexture colorMap;
    [SerializeField] private RenderTexture positionMap;
    //
    [SerializeField] private RenderTexture TestMap;
    //
    [SerializeField] private float threshold = 4;
    [SerializeField] private int scaleZ = 1000;

    [SerializeField] private ComputeShader _colorCompute;
    [SerializeField] private ComputeShader _positionCompute;

    //test
    [SerializeField]
    private Material testMaterial;
    //

    private int texWidth;
    private int texHeight;
    private Color32 data;

    private ComputeBuffer _colorBuffer;
    private ComputeBuffer _positionBuffer;
    private uint _threadGroupSizeX;
    private uint _threadGroupSizeY;

    private RenderTexture _tempPositionMap;
    private RenderTexture _tempColorMap;
    private RenderTexture _tempTestMap;


    Texture2D texColors;
    Texture2D texPositions;




    private List<Vector3> _positions = new List<Vector3>();

    private byte[] _colorBytes = new byte[640*480*3];
    private uint[] _colorsArrInt = new uint[640 * 480];
    private Color[] _positionsArray = new Color[640 * 480];

    public List<Vector3> Positions { get => _positions; private set => _positions = value; }
    
    private void Start()
    {
        NuitrackManager.onColorUpdate += GetColorFrame;
        NuitrackManager.onDepthUpdate += GetDepthFrame;

        texColors = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        texPositions = new Texture2D(width, height, TextureFormat.RGBAFloat, false);

        texWidth = texPositions.width;
        texHeight = texPositions.height;

        _colorBuffer = new ComputeBuffer(640 * 480, sizeof(int));
        //_positionBuffer = new ComputeBuffer(640 * 480*2, sizeof(int));
        _positionBuffer = new ComputeBuffer(640 * 480*2, sizeof(float));

        _tempColorMap = new RenderTexture(640, 480, 0, colorMap.format);
        _tempColorMap.enableRandomWrite = true;
        _tempColorMap.Create();

        _tempTestMap = new RenderTexture(640, 480, 0, TestMap.format);
        _tempTestMap.enableRandomWrite = true;
        _tempTestMap.Create();

        _tempPositionMap = new RenderTexture(640, 480, 0, positionMap.format);
        _tempPositionMap.enableRandomWrite = true;
        _tempPositionMap.Create();
    }

    static MethodInfo _method;
    static object[] _args5 = new object[5];
    public static void SetUnmanagedData
            (ComputeBuffer buffer, IntPtr pointer, int count, int stride)
    {
        if (_method == null)
        {
            _method = typeof(ComputeBuffer).GetMethod(
                "InternalSetNativeData",
                BindingFlags.InvokeMethod |
                BindingFlags.NonPublic |
                BindingFlags.Instance
            );
        }

        _args5[0] = pointer;
        _args5[1] = 0;      // source offset
        _args5[2] = 0;      // buffer offset
        _args5[3] = count;
        _args5[4] = stride;

        _method.Invoke(buffer, _args5);
    }

    private void GetColorFrame(ColorFrame cf)
    {
        
        byte[] currentByte= new byte[4];
        int index = 0;

        //Marshal.Copy(cf.Data, _colorBytes, 0, cf.DataSize);
        Texture2D rawColorTexture = new Texture2D(cf.Cols, cf.Rows, TextureFormat.RGB24, false);
        rawColorTexture.LoadRawTextureData(cf.Data, cf.DataSize);
        rawColorTexture.Apply();
        testMaterial.mainTexture = rawColorTexture;

        /*for (int x = 0; x < _colorBytes.Length - 3; x += 3)
        {
            
            currentByte[0] = _colorBytes[x];    //r
            currentByte[1] = _colorBytes[x + 1];    //g
            currentByte[2] = _colorBytes[x + 2];    //b
            currentByte[3] = 255;   //a

            _colorsArrInt[index] = BitConverter.ToUInt32(currentByte, 0);
            index++;
        }*/
        /*index = 0;
        byte[] newColor = new byte[640 * 480 * 4];
        for(int i=0;i< _colorBytes.Length; i+=3)
        {
            newColor[index] = _colorBytes[i];
            newColor[index+1] = _colorBytes[i+1];
            newColor[index+2] = _colorBytes[i+2];
            newColor[index+3] = 255;
            index += 4;
        }

        IntPtr colorPtr;

        /*GCHandle handle = GCHandle.Alloc(newColor, GCHandleType.Pinned);
        try
        {
            colorPtr = handle.AddrOfPinnedObject();
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
        */
        //ComputeBuffer colorData = new ComputeBuffer(640 * 480, 4);

        /*uint[] Test = new uint[640 * 480];
        for(int i = 0; i < 640 * 480; i++)
        {
            Test[i] = 255;
        }*/

        //colorData.SetData(Test);

        /*
        GCHandle pinnedArray = GCHandle.Alloc(newColor, GCHandleType.Pinned);
        colorPtr = pinnedArray.AddrOfPinnedObject();

        */

        int colorComputeKernel = _colorCompute.FindKernel("ColorMain");
        _colorCompute.GetKernelThreadGroupSizes(colorComputeKernel, out _threadGroupSizeX,
            out _threadGroupSizeY, out _);


        _colorCompute.SetTexture(colorComputeKernel, "ColorMap", _tempColorMap);

        _colorCompute.SetTexture(colorComputeKernel, "TestMap", _tempTestMap);

        _colorCompute.SetTexture(colorComputeKernel, "RawTexture", rawColorTexture);

        //_colorBuffer.SetData(_colorsArrInt);
        //SetUnmanagedData(colorData, colorPtr, 640 * 480, 4);

        


        //_colorCompute.SetBuffer(colorComputeKernel, "ColorBuffer", _colorBuffer);
        //_colorCompute.SetBuffer(colorComputeKernel, "ColorBuffer", colorData);

        var threadGroupsX = (int)(texWidth / _threadGroupSizeX);
        var threadGroupsY = (int)(texHeight / _threadGroupSizeY);
        _colorCompute.Dispatch(colorComputeKernel, threadGroupsX, threadGroupsY, 1);

        Graphics.CopyTexture(_tempColorMap, colorMap);
        Graphics.CopyTexture(_tempTestMap, TestMap);

        //pinnedArray.Free();


    }

    private void GetDepthFrame(DepthFrame df)
    {
        /*int height = df.Rows;
        int width = df.Cols;
        int index = 0;

        byte[] _depthTest1 = new byte[2];
        byte[] _depthTest2 = new byte[2];

        int test = (int)df[0, 0];
        for (int i = 0; i < df.Rows; i++)
        {
            for (int j = 0; j < df.Cols; j++)
            {
                _depth[j + i * 640] = (float)(df[i, j]/16384f);
            }

        }
        //for (int y = 0; y < height; y++)
        //{
        //    for (int x = 0; x < width; x++)
        //    {
        //        //df[y,x]->ushort->16bit, x y->int->32bit
        //        //if (df[y, x] < 0.1f || df[y, x] > threshold * scale)
        //        //{
        //        //    data.r = 0;
        //        //    data.g = 0;
        //        //    data.b = 0;
        //        //    data.a = 0;
        //        //}
        //        //else
        //        //{
        //            //data = new Color((float)x / scale, -(float)y / scale, (float)df[y, x] / scaleZ, 1);
        //            data.r =(byte)((float)x/scale);
        //            data.g =(byte)(-(float)y/scale);
        //            data.b =(byte)(-(float)df[y,x]/scaleZ);
        //            data.a =1;
        //        //}
        //        _positionsArray[index] = data;
        //        index++;
        //    }
        //}
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //df[y,x]->ushort->16bit, x y->int->32bit
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
        /*
        texPositions.SetPixels(_positionsArray);
        texPositions.Apply();

        RenderTexture.active = positionMap;
        Graphics.Blit(texPositions, positionMap);*/
        ComputeBuffer depthBuffer = new ComputeBuffer(df.DataSize / 2, sizeof(uint));
        byte[] depthData = new byte[df.DataSize];
        
        int positionComputeKernel = _positionCompute.FindKernel("PositionMain");
        _positionCompute.GetKernelThreadGroupSizes(positionComputeKernel, out _threadGroupSizeX,
            out _threadGroupSizeY, out _);


        _positionCompute.SetTexture(positionComputeKernel, "PositionMap", _tempPositionMap);

        _positionCompute.SetBuffer(positionComputeKernel, "DepthBuffer", depthBuffer);

        _positionCompute.SetFloat("DepthThreshold", 2000.0f);

        Marshal.Copy(df.Data, depthData, 0, depthData.Length);

        depthBuffer.SetData(depthData);

        

        //_positionBuffer.SetData(_positionsArray);

        //_positionBuffer.SetData(_depthData);
        //_positionBuffer.SetData(_depth);

        //_positionCompute.SetBuffer(positionComputeKernel, "PositionBuffer", _positionBuffer);

        var threadGroupsX = (int)(640 / _threadGroupSizeX);
        var threadGroupsY = (int)(480 / _threadGroupSizeY);
        _positionCompute.Dispatch(positionComputeKernel, threadGroupsX, threadGroupsY, 1);
        /**/
        Graphics.CopyTexture(_tempPositionMap, positionMap);
    }
}
