using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloudRenderer : MonoBehaviour
{
    [SerializeField] private float particleSize = 0.1f;
    [SerializeField] private int scale;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private RenderTexture positionMap;
    [SerializeField] private RenderTexture colorMap;

    Texture2D texColor;
    Texture2D texPositions;
    VisualEffect vfx;
    uint resolution = 2048;

    
    bool toUpdate = false;
    uint particleCount = 0;
    private void Start()
    {
        vfx = GetComponent<VisualEffect>();
    }

    private void Update()
    {
        if (toUpdate)
        {
            toUpdate = false;

            vfx.Reinit();
            vfx.SetUInt(Shader.PropertyToID("ParticleCount"), particleCount);
            vfx.SetTexture(Shader.PropertyToID("TexColor"), texColor);
            vfx.SetTexture(Shader.PropertyToID("TexPosScale"), texPositions);
            vfx.SetUInt(Shader.PropertyToID("Resolution"), resolution);
        }
    }

    public void SetParticles(Vector3[] positions, Color[] colors)
    {
        texColor = new Texture2D(width,height, TextureFormat.RGBAFloat, false);
        texPositions = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        int texWidth = texColor.width;
        int texHeight = texColor.height;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = x + y * texWidth;
                texColor.SetPixel(x, y, colors[index]);
                var data = new Color(positions[index].x/ scale, positions[index].y/ scale, positions[index].z/ scale, particleSize);
                texPositions.SetPixel(x, y, data);
            }
        }

        texColor.Apply();
        texPositions.Apply();
        particleCount = (uint)positions.Length;
        toUpdate = true;
        RenderTexture.active = positionMap;
        Graphics.Blit(texPositions, positionMap);
    }
}