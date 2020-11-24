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
        Vector3 accData = new Vector3(1, 0, 0).normalized;
        Vector3 rotatedAcc = accData;
        Vector3 g = new Vector3(0, 0, 1);
        Vector3 cross = Vector3.Cross(rotatedAcc, g);
        Vector3 rotAxis = cross / cross.magnitude;
        double rotDegree = Acos((double)Vector3.Dot(rotatedAcc, g));
        float mult = (float)Sin(rotDegree / 2);
        Vector3 up = new Vector3(1, 0, 0);
        Quaternion qa = Quaternion.Euler(0, 0, 90);
        Quaternion qb = Quaternion.Euler(90, 0, 0);
        Quaternion qc = Quaternion.Euler(0, 90, 0);
        //Debug.Log($"{up.x}, {up.y}, {up.z}");
        cube.transform.rotation = cube.transform.rotation * qa * qb;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
