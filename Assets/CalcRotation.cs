using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Math;
using static CalcMatrix;
using System.IO;

public class CalcRotation : MonoBehaviour
{
    // sensorData = [ax, ay , az, gx, gy, gz, mx, my, mz]
    public static double preR, preP, preY; // 前回のロール回転角, ピッチ回転角, ヨー回転角

    static double gOffsX = 0.21, gOffsY = 0.51, gOffsZ = -0.71; // ジャイロセンサのオフセット
    public const int limOfData = 100; // データの組の個数
    static int numOfData; // 現在登録されたデータの組の個数
    static double[,] gyroSensorDataList = new double[limOfData, 3]; // ジャイロセンサオフセット補正に使うセンサデータのリスト(x, y, z)順

    static int cnt = 0; // addSensorDataメソッドを呼び出した回数のカウント用変数
    public const int interval = 4; // addSensorDataメソッド内でリストにセンサデータを追加する間隔
    private static int smplCnt; // センサ(Arduino)からデータが送られてきた回数のカウント用変数

    public static void addSensorData(double[] sensorData)
    {
        if (numOfData == limOfData) return;

        // {interval}回中1回だけリストに追加する
        if (++cnt >= interval)
        {

            double[] gyroData = { sensorData[3], sensorData[4], sensorData[5] };
            for (int i = 0; i < gyroData.Length; i++)
            {
                gyroSensorDataList[numOfData, i] = gyroData[i];
            }
            numOfData++;

            cnt = 0;
        }
    }

    public static bool adjustGyroOffs(double[] sensorData) // 補正完了時にtrueを返す
    {
        // データを追加して終了
        if (smplCnt < limOfData * interval)
        {
            addSensorData(sensorData);
            smplCnt++;
            Debug.Log($"adjusting, {smplCnt}");
            return false;
        }
        smplCnt = 0;
        numOfData = 0; // この関数を実行したのちにaddSensorDataが再び使えるようにする

        double sum = 0;
        for (int i = 0; i < limOfData; i++) sum += gyroSensorDataList[i, 0];
        gOffsX = sum / limOfData;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += gyroSensorDataList[i, 1];
        gOffsY = sum / limOfData;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += gyroSensorDataList[i, 2];
        gOffsZ = sum / limOfData;

        Debug.Log($"gyro offs adjusted [{gOffsX}, {gOffsY}, {gOffsZ}]");
        return true;
    }

    public static void rotate(double[] sensorData, GameObject gameObject, long i2cInterval, double alpha)
    {
        Quaternion nowRot = gameObject.transform.rotation;
        Quaternion rotByGyro = nowRot * calcGyro(sensorData, i2cInterval);
        gameObject.transform.rotation = calcAdjRot(sensorData, rotByGyro, alpha);
    }

    static Quaternion calcGyro(double[] sensorData, long deltaT)
    {
        double multiplier = deltaT / 1000.0 * PI / 180.0;
        double gx = (sensorData[3] - gOffsX) * multiplier;
        double gy = (sensorData[4] - gOffsY) * multiplier;
        double gz = (sensorData[5] - gOffsZ) * multiplier;

        double r = gx + Sin(preR) * Sin(preP) / Cos(preP) * gy + Cos(preR) * Sin(preP) / Cos(preP) * gz;
        double p = Cos(preR) * gy - Sin(preR) * gz;
        double y = Sin(preR) / Cos(preP) * gy + Cos(preR) / Cos(preP) * gz;

        Quaternion xRot = Quaternion.AngleAxis((float)(r * 180.0 / PI), Vector3.right);
        Quaternion yRot = Quaternion.AngleAxis((float)(p * 180.0 / PI), Vector3.up);
        Quaternion zRot = Quaternion.AngleAxis((float)(y * 180.0 / PI), Vector3.forward);
        Quaternion gyroRot = xRot * yRot * zRot;
        return gyroRot;
    }

    static Quaternion calcAdjRot(double[] sensorData, Quaternion rotByGyro, double alpha)
    {
        Vector3 accData = new Vector3((float)sensorData[0], (float)sensorData[1], (float)sensorData[2]).normalized;
        Vector3 rotatedAcc = rotByGyro * accData;
        Vector3 g = new Vector3(0, 0, 1);
        Vector3 cross = Vector3.Cross(rotatedAcc, g);
        Vector3 rotAxis = cross / cross.magnitude;
        double rotDegree = (double)Acos((double)Vector3.Dot(rotatedAcc, g));
        float mult = (float)Sin(alpha * rotDegree / 2);
        float w = (float)Cos(alpha * rotDegree / 2);
        Quaternion qa = new Quaternion(rotAxis.x * mult, rotAxis.y * mult, rotAxis.z * mult, w);
        return qa * rotByGyro;
    }
}