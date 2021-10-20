using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloudRenderer : MonoBehaviour
{
    [SerializeField] private float particleSize = 0.1f;
    [SerializeField] private int scale;
    [SerializeField] private int width=640;
    [SerializeField] private int height=480;
    [SerializeField] private RenderTexture positionMap;
    [SerializeField] private RenderTexture colorMap;
    [Space]
    [SerializeField] private int depthThreshold = 4000;
    [SerializeField] private float threshold;
    [SerializeField] private float maxLifetime;
    

    Texture2D texColor;
    Texture2D texPositions;
    VisualEffect vfx;
    uint resolution = 2048;

    
    bool toUpdate = false;
    uint particleCount = 0;
    private void Start()
    {
        //vfx = GetComponent<VisualEffect>();
        
    }

    private void Update()
    {
        //SetParticles(GetComponent<FrameData>().Positions, GetComponent<FrameData>().Colors);
        SetParticles(GetComponent<FrameData>().Positions);

        //if (toUpdate)
        //{
        //    toUpdate = false;

        //    vfx.Reinit();
        //    vfx.SetUInt(Shader.PropertyToID("ParticleCount"), particleCount);
        //    vfx.SetTexture(Shader.PropertyToID("TexColor"), texColor);
        //    vfx.SetTexture(Shader.PropertyToID("TexPosScale"), texPositions);
        //    vfx.SetUInt(Shader.PropertyToID("Resolution"), resolution);
        //}
    }
    private float GetLifetime(float z)
    {
        return z > threshold * scale ? 0f : Random.Range(0f, maxLifetime);
    }
    //public void SetParticles(List<Vector3> positions,Color[] colors)
    public void SetParticles(List<Vector3> positions)
    {
        texColor = new Texture2D(width,height, TextureFormat.ARGB32, false);
        texPositions = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        int texWidth = texColor.width;
        int texHeight = texColor.height;
        
        int index = 0;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                
                //texColor.SetPixel(x, y, colors[index]);
                var data = new Color(positions[index].x/ scale, positions[index].y/ scale, positions[index].z/ scale, GetLifetime(positions[index].z));
                //texColor.SetPixel(x, y, colors[index]);
                texPositions.SetPixel(x, y, data);
                index++;
            }
        }

        texColor.Apply();
        texPositions.Apply();
        //particleCount = (uint)positions.Length;
        toUpdate = true;
        RenderTexture.active = positionMap;
        Graphics.Blit(texPositions, positionMap);
        RenderTexture.active = colorMap;
        Graphics.Blit(texColor, colorMap);
    }
}