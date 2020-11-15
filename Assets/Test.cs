using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CalcRotation;
using static CalcMatrix;
using static System.Math;


public class Test : MonoBehaviour
{
    public GameObject cube;

    public double accx, accy, accz, magx, magy, magz;

    // Start is called before the first frame update
    void Start()
    {
        /*double[] sensorData = { accx, accy, accz,0, 0, 0, magx, magy, magz };
        rotate(sensorData, cube);*/
        Quaternion rot = Quaternion.Euler(90, 0, 90);
        cube.transform.rotation = rot;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
