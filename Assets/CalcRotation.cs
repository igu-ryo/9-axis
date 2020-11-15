#define ONLY_ACC
//#undef ONLY_ACC

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

    static double aOffsX, aOffsY, aOffsZ; // 加速度センサのオフセット
    static double gOffsX = 0.21, gOffsY = 0.51, gOffsZ = -0.71; // ジャイロセンサのオフセット
    static double mOffsX = 2.54, mOffsY = -11.47, mOffsZ = 44.84; // 地磁気センサのオフセット
    static double mBaseX, mBaseY = 1; // 地磁気センサの基準ベクトル(これをx=0つまり正面だとする)
    public const int limOfData = 100; // データの組の個数
    static int numOfData; // 現在登録されたデータの組の個数
    static double[,] accSensorDataList = new double[limOfData, 3]; // 加速度センサオフセット補正に使うセンサデータのリスト(x, y, z)順
    static double[,] gyroSensorDataList = new double[limOfData, 3]; // ジャイロセンサオフセット補正に使うセンサデータのリスト(x, y, z)順
    static double[,] magSensorDataList = new double[limOfData, 3]; // 地磁気センサオフセット補正に使うセンサデータのリスト(x, y, z)順

    static int cnt = 0; // addSensorDataメソッドを呼び出した回数のカウント用変数
    public const int interval = 4; // addSensorDataメソッド内でリストにセンサデータを追加する間隔
    private static int smplCnt; // センサ(Arduino)からデータが送られてきた回数のカウント用変数

    public static double alpha = 0.95; // 加速度・地磁気センサから計算した回転角とジャイロセンサから計算した回転角を混ぜる割合(0 < alpha < 1)

    public static void addSensorData(double[] sensorData)
    {
        if (numOfData == limOfData) return;

        // {interval}回中1回だけリストに追加する
        if (++cnt >= interval)
        {
            double[] magData = { sensorData[6], sensorData[7], sensorData[8] };
            for (int i = 0; i < magData.Length; i++)
            {
                magSensorDataList[numOfData, i] = magData[i];
            }

            double[] gyroData = { sensorData[3], sensorData[4], sensorData[5] };
            for (int i = 0; i < gyroData.Length; i++)
            {
                gyroSensorDataList[numOfData, i] = gyroData[i];
            }
            numOfData++;

            cnt = 0;
        }
    }

    public static bool adjustMagOffs(double[] sensorData) // 補正完了時にtrueを返す
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

        // 近似した球面の中心の座標を求める
        double[,] A = new double[4, 4];
        double[] b = new double[4];

        double sum = 0;
        for (int i = 0; i < limOfData; i++) sum += magSensorDataList[i, 0];
        A[0, 0] = A[3, 1] = sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += Pow(magSensorDataList[i, 0], 2);
        A[0, 1] = sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += magSensorDataList[i, 0] * magSensorDataList[i, 1];
        A[0, 2] = A[1, 1] = sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += magSensorDataList[i, 0] * magSensorDataList[i, 2];
        A[0, 3] = A[2, 1] = sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += magSensorDataList[i, 1];
        A[1, 0] = A[3, 2] = sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += Pow(magSensorDataList[i, 1], 2);
        A[1, 2] = sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += magSensorDataList[i, 1] * magSensorDataList[i, 2];
        A[1, 3] = A[2, 2] = sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += magSensorDataList[i, 2];
        A[2, 0] = A[3, 3] = sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += Pow(magSensorDataList[i, 2], 2);
        A[2, 3] = sum;

        A[3, 0] = limOfData;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += magSensorDataList[i, 0] * (Pow(magSensorDataList[i, 0], 2) + Pow(magSensorDataList[i, 1], 2) + Pow(magSensorDataList[i, 2], 2));
        b[0] = -sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += magSensorDataList[i, 1] * (Pow(magSensorDataList[i, 0], 2) + Pow(magSensorDataList[i, 1], 2) + Pow(magSensorDataList[i, 2], 2));
        b[1] = -sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += magSensorDataList[i, 2] * (Pow(magSensorDataList[i, 0], 2) + Pow(magSensorDataList[i, 1], 2) + Pow(magSensorDataList[i, 2], 2));
        b[2] = -sum;

        sum = 0;
        for (int i = 0; i < limOfData; i++) sum += Pow(magSensorDataList[i, 0], 2) + Pow(magSensorDataList[i, 1], 2) + Pow(magSensorDataList[i, 2], 2);
        b[3] = -sum;

        double[] x = GaussianElimination(A, b);

        mOffsX = x[1] / -2.0;
        mOffsY = x[2] / -2.0;
        mOffsZ = x[3] / -2.0;

        Debug.Log($"mag offs adjusted [{mOffsX}, {mOffsY}, {mOffsZ}]");
        return true;
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

    public static void rotate(double[] sensorData, GameObject gameObject, long i2cInterval)
    {
        
        double[] rotation = new double[3];
        double[] gyro = calcGyro(sensorData, i2cInterval);
        double[] accMag = calcAccMag(sensorData);
        Debug.Log($"gyro: {gyro[0] * 180.0 / PI} {gyro[1] * 180.0 / PI} {gyro[2] * 180.0 / PI} ");
        Debug.Log($"acc: {accMag[0] * 180.0 / PI} {accMag[1] * 180.0 / PI} {accMag[2] * 180.0 / PI} ");
#if ONLY_ACC

        for (int i = 0; i < 2; i++)
        {
            rotation[i] = alpha * gyro[i] + (1 - alpha) * accMag[i];
        }
        rotation[2] = gyro[2];

#else

        for (int i = 0; i < 3; i++)
        {
            rotation[i] = alpha * gyro[i] + (1 - alpha) * accMag[i];
        }

#endif
        preR = rotation[0];
        preP = rotation[1];
        preY = rotation[2];
        Debug.Log($"fus: {(float)(rotation[0] * 180.0 / PI)},{(float)(rotation[1] * 180.0 / PI)},{(float)(rotation[2] * 180.0 / PI)}");
        Quaternion xRot = Quaternion.AngleAxis((float)(rotation[0] * 180.0 / PI), Vector3.right);
        Quaternion yRot = Quaternion.AngleAxis((float)(rotation[1] * 180.0 / PI), Vector3.up);
        Quaternion zRot = Quaternion.AngleAxis((float)(rotation[2] * 180.0 / PI), Vector3.forward);
        gameObject.transform.rotation = Quaternion.Euler(0, 0, 0) * xRot * yRot * zRot;
        /*gameObject.transform.Rotate((float)(rotation[0] * 180.0 / PI), 0, 0, Space.World);
        gameObject.transform.Rotate(0, (float)(rotation[1] * 180.0 / PI), 0, Space.World);
        gameObject.transform.Rotate(0, 0, (float)(rotation[2] * 180.0 / PI), Space.World);*/

        // センサ値と回転角をファイルに出力
        ExportSensorData(sensorData, i2cInterval);
        ExportRotateData(gyro, accMag);
    }

    public static void debugRotate(double[] rotateData, GameObject gameObject)
    {
        double[] rotation = new double[3];
        for (int i = 0; i < 2; i++)
        {
            rotation[i] = alpha * rotateData[i] + (1 - alpha) * rotateData[i + 3];
        }
        rotation[2] = rotateData[2];
        gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        gameObject.transform.Rotate((float)(rotation[0] * 180.0 / PI), 0, 0, Space.World);
        gameObject.transform.Rotate(0, (float)(rotation[1] * 180.0 / PI), 0, Space.World);
        gameObject.transform.Rotate(0, 0, (float)(rotation[2] * 180.0 / PI), Space.World);
    }

    public static void ExportSensorData(double[] sensorData, long i2cInterval)
    {
        StreamWriter sw = new StreamWriter("SensorData.txt", true);
        sw.Write("new double[] { ");
        for (int i = 0; i < sensorData.Length; i++)
        {
            sw.Write($"{sensorData[i]}, ");
        }
        sw.WriteLine($"{i2cInterval} }},");
        sw.Close();
    }

    public static void ExportRotateData(double[] gyroData, double[] accMagData)
    {
        StreamWriter sw = new StreamWriter("RotateData.txt", true);
        sw.Write("new double[] { ");
        for (int i = 0; i < gyroData.Length; i++)
        {
            sw.Write($"{gyroData[i]}, ");
        }
        for (int i = 0; i < accMagData.Length - 1; i++)
        {
            sw.Write($"{accMagData[i]}, ");
        }
        sw.WriteLine($"{accMagData[2]} }},");
        sw.Close();
    }

    public static void resetMag(double[] sensorData)
    {
        mBaseX = sensorData[6] - mOffsX;
        mBaseY = sensorData[7] - mOffsY;
    }

    static double[] calcAccMag(double[] sensorData)
    {
        // 加速度センサ
        double ax = sensorData[0];
        double ay = sensorData[1];
        double az = sensorData[2];

        // オフセット補正された加速度センサの値
        double cx = ax - aOffsX;
        double cy = ay - aOffsY;
        double cz = az - aOffsZ;

        // 地磁気センサ
        double mx = sensorData[6];
        double my = sensorData[7];
        double mz = sensorData[8];

        // オフセット補正された地磁気センサの値
        double dx = mx - mOffsX;
        double dy = my - mOffsY;
        double dz = mz - mOffsZ;

        // 正面補正
        /*double theta = PI / 2.0 - Atan2(mBaseY, mBaseX);
        dx = dx * Cos(theta) - dy * Sin(theta);
        dy = dx * Sin(theta) + dy * Cos(theta);*/

        double r = Atan2(cy, cz); // ロール回転角
        double p = Atan2(-cx, Sqrt(Pow(cy, 2) + Pow(cz, 2))); // ピッチ回転角

#if ONLY_ACC

        double y = 0; // ヨー回転角

#else

        double y = Atan2(-Cos(r) * dy + Sin(r) * dz, Cos(p) * dx + Sin(p) * Sin(r) * dy + Sin(p) * Cos(r) * dz); // ヨー回転角

#endif

        //Debug.Log($"{dx}, {dy}, {dz}, {y * 180.0 / PI}");

        double[] rotation = { r, p, y };
        return rotation;
    }

    static double[] calcGyro(double[] sensorData, long deltaT)
    {
        double multiplier = deltaT / 1000.0 * PI / 180.0;
        double gx = (sensorData[3] - gOffsX) * multiplier;
        double gy = (sensorData[4] - gOffsY) * multiplier;
        double gz = (sensorData[5] - gOffsZ) * multiplier;

        double r = gx + Sin(preR) * Sin(preP) / Cos(preP) * gy + Cos(preR) * Sin(preP) / Cos(preP) * gz;
        double p = Cos(preR) * gy - Sin(preR) * gz;
        double y = Sin(preR) / Cos(preP) * gy + Cos(preR) / Cos(preP) * gz;

        double[] rotation = { preR + r, preP + p, preY + y };
        return rotation;
    }

    public static void printSensorDataList()
    {
        StreamWriter sw = new StreamWriter("sensorData.csv", false);
        for (int i = 0; i < gyroSensorDataList.GetLength(0); i++)
        {
            sw.WriteLine($"{gyroSensorDataList[i, 0]}, {gyroSensorDataList[i, 1]}, {gyroSensorDataList[i, 2]}");
        }
        sw.Close();
        Debug.Log($"export is done");
    }
}
