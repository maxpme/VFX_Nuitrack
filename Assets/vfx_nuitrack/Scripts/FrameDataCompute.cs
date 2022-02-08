using UnityEngine;
using nuitrack;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;

public class FrameDataCompute : MonoBehaviour
{
    [FormerlySerializedAs("width")]
    [Header("Input Resolution")]
    [SerializeField] private int _width = 640;
    [FormerlySerializedAs("height")] [SerializeField] private int _height = 480;

    [Header("OutputTextures")]
    [FormerlySerializedAs("colorMap")] [SerializeField] private RenderTexture _colorMap;
    [FormerlySerializedAs("positionMap")] [SerializeField] private RenderTexture _positionMap;
    
    [Header("Params")]
    [SerializeField] private float _threshold = 2000.0f;

    [Header("Compute Shaders Section")]
    [SerializeField] private ComputeShader _colorCompute;
    [SerializeField] private ComputeShader _positionCompute;

    private int _texWidth;
    private int _texHeight;

    private ComputeBuffer _colorBuffer;
    private ComputeBuffer _positionBuffer;
    
    private uint _threadGroupSizeX;
    private uint _threadGroupSizeY;

    private RenderTexture _tempPositionMap;
    private RenderTexture _tempColorMap;
    private RenderTexture _tempTestMap;
    
    private Texture2D _texColors;
    private Texture2D _texPositions;

    private Texture2D _rawColorTexture;
    
    
    /*utils*/
    private byte[] _depthData;
    
    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        CreateOutputTextures();
        CreateTempTextures();
        CreateComputerBuffers();
        
        _rawColorTexture=new Texture2D(640, 480, TextureFormat.RGB24, false);
        
        SubscribeToFrameData();
        
        _texWidth = _texPositions.width;
        _texHeight = _texPositions.height;
    }

    private void CreateComputerBuffers()
    {
        _colorBuffer = new ComputeBuffer(640 * 480, sizeof(int));
        _positionBuffer = new ComputeBuffer(640 * 480*2, sizeof(float));
    }

    private void CreateTempTextures()
    {
        _tempColorMap = new RenderTexture(640, 480, 0, _colorMap.format);
        _tempColorMap.enableRandomWrite = true;
        _tempColorMap.Create();
        
        _tempPositionMap = new RenderTexture(640, 480, 0, _positionMap.format);
        _tempPositionMap.enableRandomWrite = true;
        _tempPositionMap.Create();
    }

    private void CreateOutputTextures()
    {
        _texColors = new Texture2D(_width, _height, TextureFormat.RGBAFloat, false);
        _texPositions = new Texture2D(_width, _height, TextureFormat.RGBAFloat, false);
    }

    private void SubscribeToFrameData()
    {
        NuitrackManager.onColorUpdate += GetColorFrame;
        NuitrackManager.onDepthUpdate += GetDepthFrame;
    }
    
    private void GetColorFrame(ColorFrame cf)
    {
        // _rawColorTexture=new Texture2D(cf.Cols, cf.Rows, TextureFormat.RGB24, false);
        
        //TODO: encapsulate Compute to class
        ColorDataLoad(_rawColorTexture, cf);

        int computeKernel=GetColorComputeKernel();
        SetColorComputeVariables(computeKernel,_tempColorMap,_rawColorTexture);
        RunColorCompute(computeKernel);
    }

    private void ColorDataLoad(Texture2D texture, ColorFrame cf)
    {
        texture.LoadRawTextureData(cf.Data, cf.DataSize);
        texture.Apply();
    }

    private int GetColorComputeKernel()
    {
        int colorComputeKernel = _colorCompute.FindKernel("ColorMain");
        return colorComputeKernel;
    }

    private void SetColorComputeVariables(int kernel, RenderTexture tempTexture, Texture2D colorData)
    {
        _colorCompute.SetTexture(kernel, "ColorMap", tempTexture);
        _colorCompute.SetTexture(kernel, "RawTexture", colorData);
    }

    private void RunColorCompute(int kernel)
    {
        _colorCompute.GetKernelThreadGroupSizes(kernel, out _threadGroupSizeX,
            out _threadGroupSizeY, out _);
        
        var threadGroupsX = (int)(_texWidth / _threadGroupSizeX);
        var threadGroupsY = (int)(_texHeight / _threadGroupSizeY);
        _colorCompute.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        Graphics.CopyTexture(_tempColorMap, _colorMap);
    }

    private void GetDepthFrame(DepthFrame df)
    {
        _positionBuffer=new ComputeBuffer(df.DataSize / 2, sizeof(uint));
        
        LoadDepthData(df);

        int positionComputeKernel = _positionCompute.FindKernel("PositionMain");
        
        SetDepthComputeVariables(positionComputeKernel,_tempPositionMap, _positionBuffer, _threshold);

        SetDataToBuffer(_depthData, _positionBuffer);

        RunDepthCompute(positionComputeKernel);
    }

    private void LoadDepthData(DepthFrame df)
    {
        _depthData = new byte[df.DataSize];
        
        Marshal.Copy(df.Data, _depthData, 0, _depthData.Length);
    }

    private void SetDepthComputeVariables(int kernel, RenderTexture positionTexture, ComputeBuffer positionBuffer, float threshold)
    {
        _positionCompute.SetTexture(kernel, "PositionMap", positionTexture);
        _positionCompute.SetBuffer(kernel, "DepthBuffer", positionBuffer);
        _positionCompute.SetFloat("DepthThreshold", threshold);
    }

    private void SetDataToBuffer(byte[] data, ComputeBuffer buffer)
    {
        buffer.SetData(data);
    }

    private void RunDepthCompute(int kernel)
    {
        _positionCompute.GetKernelThreadGroupSizes(kernel, out _threadGroupSizeX,
            out _threadGroupSizeY, out _);
        
        var threadGroupsX = (int)(640 / _threadGroupSizeX);
        var threadGroupsY = (int)(480 / _threadGroupSizeY);
        _positionCompute.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
        /**/
        Graphics.CopyTexture(_tempPositionMap, _positionMap);
    }
}
