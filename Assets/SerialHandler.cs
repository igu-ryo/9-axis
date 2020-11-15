#define DEBUG
#undef DEBUG
#define RADIAN
#undef RADIAN

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using static CalcRotation;

#if (DEBUG)

using static System.Math;
using static SensorData;
using static RotateData;

#endif

public class SerialHandler : MonoBehaviour
{
	public GameObject cube;

	public string portName = "COM3"; // ポート名
	public int baudRate = 9600;  // ボーレート(Arduinoに記述したものに合わせる)
	public long i2cInterval; // センサ値をArduinoから送られてくる間隔(ミリ秒)

	private SerialPort serialPort_;
	private Thread thread_;
	private bool isRunning_ = false;
	private bool gyroOffsAdjusting_ = false; // ジャイロセンサオフセット補正中はtrue
	private bool magOffsAdjusting_ = false; // 地磁気センサオフセット補正中はtrue
	private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

	private string message_;
	private bool isNewMessageReceived_ = false;

#if (DEBUG)

	private int dataIndex; // 再現するセンサデータの配列の添え字

#endif

	void Awake()
	{
		Open();
	}

	void Update()
	{
#if (DEBUG)



		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			changePose(ref sensorData, 1);
		}
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			changePose(ref sensorData, 10);
		}
		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			changePose(ref sensorData, -1);
		}
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			changePose(ref sensorData, -10);
		}

#else

		if (Input.GetKey(KeyCode.M) && !gyroOffsAdjusting_) magOffsAdjusting_ = true;
		if (Input.GetKey(KeyCode.G) && !magOffsAdjusting_) gyroOffsAdjusting_ = true;
		

		if (isNewMessageReceived_)
		{
			sw.Stop();
			i2cInterval = sw.ElapsedMilliseconds;

			// ax, ay , az, gx, gy, gz, mx, my, mz
			string[] msgArr = message_.Split(',');
			try
			{
				double[] sensorData = Array.ConvertAll(msgArr, double.Parse);

				//Debug.Log($"{sensorData[3]} {sensorData[4]} {sensorData[5]} ");
				if (magOffsAdjusting_)
				{
					magOffsAdjusting_ = !adjustMagOffs(sensorData);
				}
				else if (gyroOffsAdjusting_)
				{
					gyroOffsAdjusting_ = !adjustGyroOffs(sensorData);
				}
				else if (Input.GetKey(KeyCode.R)) resetMag(sensorData);
				else
				{
					rotate(sensorData, cube, i2cInterval);
				}
			}
			catch
			{

			}

			sw.Restart();
		}
		isNewMessageReceived_ = false;

#endif
	}

	void OnDestroy()
	{
		Close();
	}

	private void Open()
	{
#if (DEBUG)

#else

		serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);

		serialPort_.Open();

		isRunning_ = true;

		thread_ = new Thread(Read);
		thread_.Start();

		sw.Start();

#endif
	}

	private void Close()
	{
		isNewMessageReceived_ = false;
		isRunning_ = false;

		if (thread_ != null && thread_.IsAlive)
		{
			thread_.Join();
		}

		if (serialPort_ != null && serialPort_.IsOpen)
		{
			serialPort_.Close();
			serialPort_.Dispose();
		}
	}

	private void Read()
	{
		while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
		{
			try
			{
				message_ = serialPort_.ReadLine();
				isNewMessageReceived_ = true;
			}
			catch (System.Exception e)
			{
				Debug.LogWarning(e.Message);
			}
		}
	}

#if (DEBUG)

	private void changePose(ref double[][] sensorData, int indexDifference)
	{
		dataIndex += indexDifference;
		if (dataIndex < 0) { dataIndex = 0; return; }
		else if (dataIndex >= sensorData.Length) { dataIndex = sensorData.Length - 1; return; }

		debugRotate(rotateData[dataIndex], cube);

		string sensorDataList = "sensor data: acc: ";
		for (int i = 0; i < 3; i++)
		{
			sensorDataList += $"{sensorData[dataIndex][i]}, ";
		}
		sensorDataList += "gyro: ";
		for (int i = 0; i < 3; i++)
		{
			sensorDataList += $"{sensorData[dataIndex][i + 3]}, ";
		}
		sensorDataList += $"interval: {sensorData[dataIndex][9]}";
		Debug.Log(sensorDataList);

		double rotation;
		string gyroRotateDataList = "gyro: ";
		for (int i = 0; i < 3; i++)
		{
#if RADIAN
			rotation = rotateData[dataIndex][i];
#else
			rotation = rotateData[dataIndex][i] * 180.0 / PI;
#endif
			gyroRotateDataList += $"{rotation}, ";
		}
		Debug.Log(gyroRotateDataList);

		string accRotateDataList = "acc ";
		for (int i = 0; i < 3; i++)
		{
#if RADIAN
			rotation = rotateData[dataIndex][i + 3];
#else
			rotation = rotateData[dataIndex][i + 3] * 180.0 / PI;
#endif
			accRotateDataList += $"{rotation}, ";
		}
		Debug.Log(accRotateDataList);

		string fusionRotateDataList = "fus ";
		for (int i = 0; i < 2; i++)
		{
			rotation = alpha * rotateData[dataIndex][i] + (1 - alpha) * rotateData[dataIndex][i + 3];
#if !RADIAN
			rotation *= 180.0 / PI;
#endif
			fusionRotateDataList += $"{rotation}, ";
		}
#if RADIAN
		rotation = rotateData[dataIndex][2];
#else
		rotation = rotateData[dataIndex][2] * 180.0 / PI;
#endif
		fusionRotateDataList += $"{rotation}, ";
		Debug.Log(fusionRotateDataList);
		Debug.Log("");
	}

#endif

	}