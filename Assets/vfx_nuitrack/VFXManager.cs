using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFXManager : MonoBehaviour
{
    [SerializeField] VisualEffect vfx;
    
    void Update()
    {
        var x=Random.Range(0f, 3f);
        var y=Random.Range(0f, 3f);
        var z=Random.Range(0f, 3f);
        List<Vector3> positions = new List<Vector3>();

        positions.Add(new Vector3(x, y, z));
        x = Random.Range(0f, 3f);
        y = Random.Range(0f, 3f);
        z = Random.Range(0f, 3f);
        positions.Add(new Vector3(x, y, z));
        //vfx.SetList("ParticlePositions", positions);
    }
}
