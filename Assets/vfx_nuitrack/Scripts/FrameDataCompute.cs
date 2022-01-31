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
    [SerializeField] private int width = 640;
    [SerializeField] private int height = 480;
    
    [SerializeField] private RenderTexture colorMap;
    [SerializeField] private RenderTexture positionMap;
    //
    [SerializeField] private RenderTexture TestMap;
    //
    [Header("Compute Shaders Section")]
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
    
    private Texture2D _texColors;
    private Texture2D _texPositions;


    private List<Vector3> _positions = new List<Vector3>();

    private byte[] _colorBytes = new byte[640*480*3];
    private uint[] _colorsArrInt = new uint[640 * 480];
    private Color[] _positionsArray = new Color[640 * 480];

    public List<Vector3> Positions { get => _positions; private set => _positions = value; }
    
    private void Start()
    {
        NuitrackManager.onColorUpdate += GetColorFrame;
        NuitrackManager.onDepthUpdate += GetDepthFrame;

        _texColors = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        _texPositions = new Texture2D(width, height, TextureFormat.RGBAFloat, false);

        texWidth = _texPositions.width;
        texHeight = _texPositions.height;

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
        
        Texture2D rawColorTexture = new Texture2D(cf.Cols, cf.Rows, TextureFormat.RGB24, false);
        rawColorTexture.LoadRawTextureData(cf.Data, cf.DataSize);
        rawColorTexture.Apply();
        testMaterial.mainTexture = rawColorTexture;

        int colorComputeKernel = _colorCompute.FindKernel("ColorMain");
        _colorCompute.GetKernelThreadGroupSizes(colorComputeKernel, out _threadGroupSizeX,
            out _threadGroupSizeY, out _);
        
        _colorCompute.SetTexture(colorComputeKernel, "ColorMap", _tempColorMap);

        _colorCompute.SetTexture(colorComputeKernel, "TestMap", _tempTestMap);

        _colorCompute.SetTexture(colorComputeKernel, "RawTexture", rawColorTexture);

        var threadGroupsX = (int)(texWidth / _threadGroupSizeX);
        var threadGroupsY = (int)(texHeight / _threadGroupSizeY);
        _colorCompute.Dispatch(colorComputeKernel, threadGroupsX, threadGroupsY, 1);

        Graphics.CopyTexture(_tempColorMap, colorMap);
        Graphics.CopyTexture(_tempTestMap, TestMap);

    }

    private void GetDepthFrame(DepthFrame df)
    {
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
        
        var threadGroupsX = (int)(640 / _threadGroupSizeX);
        var threadGroupsY = (int)(480 / _threadGroupSizeY);
        _positionCompute.Dispatch(positionComputeKernel, threadGroupsX, threadGroupsY, 1);
        /**/
        Graphics.CopyTexture(_tempPositionMap, positionMap);
    }
}