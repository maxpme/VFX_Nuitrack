using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nuitrack;
using Vector3 = UnityEngine.Vector3;

public class CreateCloud : MonoBehaviour
{
    
    private void Start()
    {
        NuitrackManager.onDepthUpdate += getDepthFrame;
    }


    private void Update()
    {
        
    }

    private void getDepthFrame(DepthFrame df)
    {
        Vector3 _positionCoords = new Vector3();

        List<Vector3> _positions = new List<Vector3>();
        int height = df.Rows;
        int width = df.Cols;

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                _positionCoords = NuitrackManager.DepthSensor.ConvertProjToRealCoords(x, y, df[x,y]).ToVector3();
                _positions.Add(_positionCoords);
            }
        }
    }
}
