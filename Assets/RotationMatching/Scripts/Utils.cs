using System;
using UnityEngine;

public static class Utils
{

	public static float DistMatrices(Matrix4x4 a, Matrix4x4 b)
	{
		float r = 0;
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				float v = a[i, j] - b[i, j];
				r += v * v;
			}
		}
		return (float)Math.Sqrt(r);
	}

}