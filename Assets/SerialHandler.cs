using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using static CalcRotation;

public class SerialHandler : MonoBehaviour
{
	public GameObject cube1, cube2, cube3;

	public double alpha1, alpha2, alpha3; // 回転の混合割合

	public string portName = "COM3"; // ポート名
	public int baudRate = 9600;  // ボーレート(Arduinoに記述したものに合わせる)
	public long i2cInterval; // センサ値をArduinoから送られてくる間隔(ミリ秒)

	private SerialPort serialPort_;
	private Thread thread_;
	private bool isRunning_ = false;
	private bool gyroOffsAdjusting_ = false; // ジャイロセンサオフセット補正中はtrue
	private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

	private string message_;
	private bool isNewMessageReceived_ = false;

	void Awake()
	{
		Open();
	}

	void Update()
	{
		if (Input.GetKey(KeyCode.G)) gyroOffsAdjusting_ = true;


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
				if (gyroOffsAdjusting_)
				{
					gyroOffsAdjusting_ = !adjustGyroOffs(sensorData);
				}
				else
				{
					rotate(sensorData, cube1, i2cInterval, alpha1);
					rotate(sensorData, cube2, i2cInterval, alpha2);
					rotate(sensorData, cube3, i2cInterval, alpha3);
				}
			}
			catch
			{

			}

			sw.Restart();
		}
		isNewMessageReceived_ = false;

	}

	void OnDestroy()
	{
		Close();
	}

	private void Open()
	{
		serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);

		serialPort_.Open();

		isRunning_ = true;

		thread_ = new Thread(Read);
		thread_.Start();

		sw.Start();
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
}