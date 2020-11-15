using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalcMatrix
{
    // Ax=bの解xを返す(一番上は使わないから求めない)
    public static double[] GaussianElimination(double[,] A, double[] b)
    {
        int dim = b.Length; // 解xの次元
        double[] x = new double[dim];
        
        // 前進消去(掃き出し)処理
        for (int i = 0; i < dim; i++)
        {
            // Aとbのi行目をA[i, i]で割る
            double divisor = A[i, i];
            for (int j = 0; j < dim; j++)
            {
                A[i, j] /= divisor;
            }
            b[i] /= divisor;
            
            // Aとbのi行目にA[k, i]をかけたものを下の行から引く(掃き出し)
            for (int k = i + 1; k < dim; k++)
            {
                double multiplier = A[k, i];
                for (int j = 0; j < dim; j++)
                {
                    A[k, j] -= A[i, j] * multiplier;
                }
                b[k] -= b[i] * multiplier;
            }
        }
        
        // 後退代入処理
        for (int i = dim - 1; i >= 0; i--)
        {
            double sum = 0;
            for (int j = i + 1; j < dim; j++)
            {
                sum += A[i, j] * x[j];
            }
            x[i] = b[i] - sum;
        }

        return x;
    }

    public static void logMatrix(double[,] A)
    {
        for (int i = 0; i < A.GetLength(0); i++)
            for (int j = 0; j < A.GetLength(0); j++) 
            { 
                Debug.Log(A[i, j]);
            }
        Debug.Log("");
    }
}
