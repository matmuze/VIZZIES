using System;
using System.IO;
using UnityEngine;

using SimpleJSON;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Helper
{
    public static Matrix4x4 quaternion_outer(Quaternion q1, Quaternion q2)
    {
        Matrix4x4 m = Matrix4x4.identity;
        for (int i = 0; i < 4; i++)
        {
            m.SetRow(i, new Vector4(q1[i] * q2[0], q1[i] * q2[1], q1[i] * q2[2], q1[i] * q2[3]));
        }
        return m;
    }

    public static Matrix4x4 quaternion_matrix(Quaternion quat)
    {
        float _EPS = 8.8817841970012523e-16f;
        float n = Quaternion.Dot(quat, quat);
        //Debug.Log (n.ToString());
        //Debug.Log (Vector4.Dot (QuanternionToVector4(quat),QuanternionToVector4(quat)).ToString ());
        Matrix4x4 m = Matrix4x4.identity;
        if (n > _EPS)
        {
            quat = new Quaternion(quat[0] * Mathf.Sqrt(2.0f / n), quat[1] * Mathf.Sqrt(2.0f / n), quat[2] * Mathf.Sqrt(2.0f / n), quat[3] * Mathf.Sqrt(2.0f / n));
            //Debug.Log (quat.ToString());
            Matrix4x4 q = quaternion_outer(quat, quat);
            //Debug.Log (q.ToString());
            m.SetRow(0, new Vector4(1.0f - q[2, 2] - q[3, 3], q[1, 2] - q[3, 0], q[1, 3] + q[2, 0], 0.0f));
            m.SetRow(1, new Vector4(q[1, 2] + q[3, 0], 1.0f - q[1, 1] - q[3, 3], q[2, 3] - q[1, 0], 0.0f));
            m.SetRow(2, new Vector4(q[1, 3] - q[2, 0], q[2, 3] + q[1, 0], 1.0f - q[1, 1] - q[2, 2], 0.0f));
            m.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            //m.SetColumn(2,m.GetColumn(2)*-1.0f);
        }
        return m;
    }

    public static Matrix4x4 quatToMatrix(Quaternion q)
    {
        float sqw = q.w * q.w;
        float sqx = q.x * q.x;
        float sqy = q.y * q.y;
        float sqz = q.z * q.z;

        // invs (inverse square length) is only required if quaternion is not already normalised
        float invs = 1 / (sqx + sqy + sqz + sqw);
        Matrix4x4 m = Matrix4x4.identity;
        m[0, 0] = (sqx - sqy - sqz + sqw) * invs; // since sqw + sqx + sqy + sqz =1/invs*invs
        m[1, 1] = (-sqx + sqy - sqz + sqw) * invs;
        m[2, 2] = (-sqx - sqy + sqz + sqw) * invs;

        float tmp1 = q.x * q.y;
        float tmp2 = q.z * q.w;
        m[1, 0] = 2.0f * (tmp1 + tmp2) * invs;
        m[0, 1] = 2.0f * (tmp1 - tmp2) * invs;

        tmp1 = q.x * q.z;
        tmp2 = q.y * q.w;
        m[2, 0] = 2.0f * (tmp1 - tmp2) * invs;
        m[0, 2] = 2.0f * (tmp1 + tmp2) * invs;
        tmp1 = q.y * q.z;
        tmp2 = q.x * q.w;
        m[2, 1] = 2.0f * (tmp1 + tmp2) * invs;
        m[1, 2] = 2.0f * (tmp1 - tmp2) * invs;

        //convertion to actual matrix
        Matrix4x4 m2 = Matrix4x4.identity;
        m2[0, 0] = -m[0, 1];
        m2[1, 0] = m[2, 1];
        m2[2, 0] = m[1, 1];

        m2[0, 1] = -m[0, 0];
        m2[1, 1] = m[2, 0];
        m2[2, 1] = m[1, 0];

        m2[0, 2] = m[0, 2];
        m2[1, 2] = -m[2, 2];
        m2[2, 2] = -m[1, 2];

        return m2;
    }

    public static Vector3 euler_from_matrix(Matrix4x4 M)
    {
        /*
         * _AXES2TUPLE = {
            'sxyz': (0, 0, 0, 0), 'sxyx': (0, 0, 1, 0), 'sxzy': (0, 1, 0, 0),
            'sxzx': (0, 1, 1, 0), 'syzx': (1, 0, 0, 0), 'syzy': (1, 0, 1, 0),
            'syxz': (1, 1, 0, 0), 'syxy': (1, 1, 1, 0), 'szxy': (2, 0, 0, 0),
            'szxz': (2, 0, 1, 0), 'szyx': (2, 1, 0, 0), 'szyz': (2, 1, 1, 0),
            'rzyx': (0, 0, 0, 1), 'rxyx': (0, 0, 1, 1), 'ryzx': (0, 1, 0, 1),
            'rxzx': (0, 1, 1, 1), 'rxzy': (1, 0, 0, 1), 'ryzy': (1, 0, 1, 1),
            'rzxy': (1, 1, 0, 1), 'ryxy': (1, 1, 1, 1), 'ryxz': (2, 0, 0, 1),
            'rzxz': (2, 0, 1, 1), 'rxyz': (2, 1, 0, 1), 'rzyz': (2, 1, 1, 1)}
            */
        float _EPS = 8.8817841970012523e-16f;
        Vector4 _NEXT_AXIS = new Vector4(1, 2, 0, 1);
        Vector4 _AXES2TUPLE = new Vector4(0, 0, 0, 0);//sxyz
        int firstaxis = (int)_AXES2TUPLE[0];
        int parity = (int)_AXES2TUPLE[1];
        int repetition = (int)_AXES2TUPLE[2];
        int frame = (int)_AXES2TUPLE[3];

        int i = firstaxis;
        int j = (int)_NEXT_AXIS[i + parity];
        int k = (int)_NEXT_AXIS[i - parity + 1];
        float ax = 0.0f;
        float ay = 0.0f;
        float az = 0.0f;
        //Matrix4x4 M = matrix;// = numpy.array(matrix, dtype=numpy.float64, copy=False)[:3, :3]
        if (repetition == 1)
        {
            float sy = Mathf.Sqrt(M[i, j] * M[i, j] + M[i, k] * M[i, k]);
            if (sy > _EPS)
            {
                ax = Mathf.Atan2(M[i, j], M[i, k]);
                ay = Mathf.Atan2(sy, M[i, i]);
                az = Mathf.Atan2(M[j, i], -M[k, i]);
            }
            else
            {
                ax = Mathf.Atan2(-M[j, k], M[j, j]);
                ay = Mathf.Atan2(sy, M[i, i]);
                az = 0.0f;
            }
        }
        else
        {
            float cy = Mathf.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
            if (cy > _EPS)
            {
                ax = Mathf.Atan2(M[k, j], M[k, k]);
                ay = Mathf.Atan2(-M[k, i], cy);
                az = Mathf.Atan2(M[j, i], M[i, i]);
            }
            else
            {
                ax = Mathf.Atan2(-M[j, k], M[j, j]);
                ay = Mathf.Atan2(-M[k, i], cy);
                az = 0.0f;
            }
        }
        if (parity == 1)
        {
            ax = -ax;
            ay = -ay;
            az = -az;
        }
        if (frame == 1)
        {
            ax = az;
            az = ax;
        }
        //radians to degrees
        //array([-124.69515353,  -54.90319877,  108.43494882])
        //to -56.46 �110.427 �126.966 �
        //Vector3 euler = new Vector3 (ay * Mathf.Rad2Deg, az * Mathf.Rad2Deg, -ax * Mathf.Rad2Deg);
        Vector3 euler = new Vector3(ax * Mathf.Rad2Deg, ay * Mathf.Rad2Deg, az * Mathf.Rad2Deg);
        //if (euler.x < 0) euler.x += 360.0f;
        //if (euler.y < 0) euler.y += 360.0f;
        //if (euler.z < 0) euler.z += 360.0f;
        return euler;
    }

    public static Quaternion MayaRotationToUnity(Vector3 rotation)
    {
        var flippedRotation = new Vector3(rotation.x, -rotation.y, -rotation.z); // flip Y and Z axis for right->left handed conversion
        // convert XYZ to ZYX
        var qy90 = Quaternion.AngleAxis(90.0f, Vector3.up);
        var qy180 = Quaternion.AngleAxis(180.0f, Vector3.up);
        var qx = Quaternion.AngleAxis(flippedRotation.x, Vector3.right);
        var qy = Quaternion.AngleAxis(flippedRotation.y, Vector3.up);
        var qz = Quaternion.AngleAxis(flippedRotation.z, Vector3.forward);
        var qq = qz * qy * qx; // this is the order
        //Vector3 new_up = qq * Vector3.up;
        //qy90 = Quaternion.AngleAxis(90.0f,new_up);
        return qq;
    }
    
    public static Quaternion RotationMatrixToQuaternion(Matrix4x4 a)
    {
        Quaternion q = Quaternion.identity;
        float trace = a[0, 0] + a[1, 1] + a[2, 2]; // I removed + 1.0f; see discussion with Ethan
        if (trace > 0)
        {// I changed M_EPSILON to 0
            float s = 0.5f / Mathf.Sqrt(trace + 1.0f);
            q.w = 0.25f / s;
            q.x = (a[2, 1] - a[1, 2]) * s;
            q.y = (a[0, 2] - a[2, 0]) * s;
            q.z = (a[1, 0] - a[0, 1]) * s;
        }
        else
        {
            if (a[0, 0] > a[1, 1] && a[0, 0] > a[2, 2])
            {
                float s = 2.0f * Mathf.Sqrt(1.0f + a[0, 0] - a[1, 1] - a[2, 2]);
                q.w = (a[2, 1] - a[1, 2]) / s;
                q.x = 0.25f * s;
                q.y = (a[0, 1] + a[1, 0]) / s;
                q.z = (a[0, 2] + a[2, 0]) / s;
            }
            else if (a[1, 1] > a[2, 2])
            {
                float s = 2.0f * Mathf.Sqrt(1.0f + a[1, 1] - a[0, 0] - a[2, 2]);
                q.w = (a[0, 2] - a[2, 0]) / s;
                q.x = (a[0, 1] + a[1, 0]) / s;
                q.y = 0.25f * s;
                q.z = (a[1, 2] + a[2, 1]) / s;
            }
            else
            {
                float s = 2.0f * Mathf.Sqrt(1.0f + a[2, 2] - a[0, 0] - a[1, 1]);
                q.w = (a[1, 0] - a[0, 1]) / s;
                q.x = (a[0, 2] + a[2, 0]) / s;
                q.y = (a[1, 2] + a[2, 1]) / s;
                q.z = 0.25f * s;
            }
        }
        return q;
    }

    public static Vector3 QuaternionTransform(Quaternion q, Vector3 v)
    {
        var tt = new Vector3(q.x, q.y, q.z);
        var t = 2 * Vector3.Cross(tt, v);
        return v + q.w * t + Vector3.Cross(tt, t);
    }

    public static Vector4 QuanternionToVector4(Quaternion q)
    {
        return new Vector4(q.x, q.y, q.z, q.w);
    }

    public static Vector4 PlaneToVector4(Plane plane)
    {
        return new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
    }

    public static Quaternion Vector4ToQuaternion(Vector4 v)
    {
        return new Quaternion(v.x, v.y, v.z, v.w);
    }

    public static Color GetRandomColor()
    {
        return new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
    }

    public static float[] ReadBytesAsFloats(string filePath)
    {
        if (!File.Exists(filePath)) throw new Exception("File not found: " + filePath);

        var bytes = File.ReadAllBytes(filePath);
		//BitConverter.IsLittleEndian (bytes [0]);
        var floats = new float[bytes.Length / sizeof(float)];//4 ?
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    //try to read the json so I can test different quaternion/angle/matrice
    //int i indice of name
    //float x y z 
    //float x y z w
    public static JSONNode ParseJson(string filePath)
    {
        //JSONNode.Parse (filestring);
        //StreamReader inp_stm = new StreamReader(file_path);
        //return JSONNode.LoadFromFile (filePath);
        var sourse = new StreamReader(filePath);
        var fileContents = sourse.ReadToEnd();
        var data = JSONNode.Parse(fileContents);
        return data;
    }

	public static JSONNode GetAllIngredientsInfo()
	{
		Debug.Log("Downloading all ingredients file");
		var www = new WWW("https://raw.githubusercontent.com/mesoscope/cellPACK_data/master/cellPACK_database_1.1.0/recipes/allIngredients.json");
		var path = PdbLoader.DefaultPdbDirectory + "allIngredients.json";
		if (!File.Exists (path)) {
			#if UNITY_EDITOR
			while (!www.isDone)
			{
				EditorUtility.DisplayProgressBar("Downloading all ingredient info", "Downloading...", www.progress);
			}
			EditorUtility.ClearProgressBar();
			#endif

			if (!string.IsNullOrEmpty (www.error))
				throw new Exception ("allIngredients.json" + www.error);
			//var path = (string.IsNullOrEmpty (dstPath) ? DefaultPdbDirectory : dstPath) + "allIngredients.json";
			File.WriteAllText (path, www.text);
		}
		var resultData = Helper.ParseJson(path);
		return resultData;
	}

    public static int GetIdFromColor(Color color)
    {
        int b = (int)(color.b * 255.0f);
        int g = (int)(color.g * 255.0f) << 8;
        int r = (int)(color.r * 255.0f) << 16;

        //Debug.Log("r: " + r + " g: " + g + " b:" + b);
        //Debug.Log("id: " + (r + g + b));
        //Debug.Log("color: " + color);

        return r + g + b;
    }
    
    public static Vector3 CubicInterpolate(Vector3 y0, Vector3 y1, Vector3 y2, Vector3 y3, float mu)
    {
        float mu2 = mu * mu;
        Vector3 a0, a1, a2, a3;

        a0 = y3 - y2 - y0 + y1;
        a1 = y0 - y1 - a0;
        a2 = y2 - y0;
        a3 = y1;

        return (a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3);
    }

    public static Matrix4x4 FloatArrayToMatrix4X4(float[] floatArray)
    {
        var matrix = new Matrix4x4();

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                matrix[i, j] = floatArray[i * 4 + j];
            }
        }

        return matrix;
    }

    public static float[] Matrix4X4ToFloatArray(Matrix4x4 matrix)
    {
        var floatArray = new float[16];

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                floatArray[i * 4 + j] = matrix[j, i];
            }
        }

        return floatArray;
    }

    public static float[] FrustrumPlanesAsFloats(Camera _camera)
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(_camera);
        var planesAsFloats = new float[6 * 4];
        for (int i = 0; i < planes.Length; i++)
        {
            planesAsFloats[i * 4] = planes[i].normal.x;
            planesAsFloats[i * 4 + 1] = planes[i].normal.y;
            planesAsFloats[i * 4 + 2] = planes[i].normal.z;
            planesAsFloats[i * 4 + 3] = planes[i].distance;
        }

        return planesAsFloats;
    }

    public static int ReadPixelId(RenderTexture texture, Vector2 coord)
    {
        var outBuffer = new ComputeBuffer(1, sizeof(int));

        ComputeShaderManager.Instance.ReadPixelCS.SetInts("_Coord", (int)coord.x, Screen.height - (int)coord.y);
        ComputeShaderManager.Instance.ReadPixelCS.SetTexture(0, "_IdTexture", texture);
        ComputeShaderManager.Instance.ReadPixelCS.SetBuffer(0, "_OutputBuffer", outBuffer);
        ComputeShaderManager.Instance.ReadPixelCS.Dispatch(0, 1, 1, 1);

        var pixelId = new[] { 0 };
        outBuffer.GetData(pixelId);
        outBuffer.Release();

        return pixelId[0];
    }
}


public static class HalfHelper
{
    private static uint[] mantissaTable = GenerateMantissaTable();
    private static uint[] exponentTable = GenerateExponentTable();
    private static ushort[] offsetTable = GenerateOffsetTable();
    private static ushort[] baseTable = GenerateBaseTable();
    private static sbyte[] shiftTable = GenerateShiftTable();

    // Transforms the subnormal representation to a normalized one. 
    private static uint ConvertMantissa(int i)
    {
        uint m = (uint)(i << 13); // Zero pad mantissa bits
        uint e = 0; // Zero exponent

        // While not normalized
        while ((m & 0x00800000) == 0)
        {
            e -= 0x00800000; // Decrement exponent (1<<23)
            m <<= 1; // Shift mantissa                
        }
        m &= unchecked((uint)~0x00800000); // Clear leading 1 bit
        e += 0x38800000; // Adjust bias ((127-14)<<23)
        return m | e; // Return combined number
    }

    private static uint[] GenerateMantissaTable()
    {
        uint[] mantissaTable = new uint[2048];
        mantissaTable[0] = 0;
        for (int i = 1; i < 1024; i++)
        {
            mantissaTable[i] = ConvertMantissa(i);
        }
        for (int i = 1024; i < 2048; i++)
        {
            mantissaTable[i] = (uint)(0x38000000 + ((i - 1024) << 13));
        }

        return mantissaTable;
    }
    private static uint[] GenerateExponentTable()
    {
        uint[] exponentTable = new uint[64];
        exponentTable[0] = 0;
        for (int i = 1; i < 31; i++)
        {
            exponentTable[i] = (uint)(i << 23);
        }
        exponentTable[31] = 0x47800000;
        exponentTable[32] = 0x80000000;
        for (int i = 33; i < 63; i++)
        {
            exponentTable[i] = (uint)(0x80000000 + ((i - 32) << 23));
        }
        exponentTable[63] = 0xc7800000;

        return exponentTable;
    }
    private static ushort[] GenerateOffsetTable()
    {
        ushort[] offsetTable = new ushort[64];
        offsetTable[0] = 0;
        for (int i = 1; i < 32; i++)
        {
            offsetTable[i] = 1024;
        }
        offsetTable[32] = 0;
        for (int i = 33; i < 64; i++)
        {
            offsetTable[i] = 1024;
        }

        return offsetTable;
    }
    private static ushort[] GenerateBaseTable()
    {
        ushort[] baseTable = new ushort[512];
        for (int i = 0; i < 256; ++i)
        {
            sbyte e = (sbyte)(127 - i);
            if (e > 24)
            { // Very small numbers map to zero
                baseTable[i | 0x000] = 0x0000;
                baseTable[i | 0x100] = 0x8000;
            }
            else if (e > 14)
            { // Small numbers map to denorms
                baseTable[i | 0x000] = (ushort)(0x0400 >> (18 + e));
                baseTable[i | 0x100] = (ushort)((0x0400 >> (18 + e)) | 0x8000);
            }
            else if (e >= -15)
            { // Normal numbers just lose precision
                baseTable[i | 0x000] = (ushort)((15 - e) << 10);
                baseTable[i | 0x100] = (ushort)(((15 - e) << 10) | 0x8000);
            }
            else if (e > -128)
            { // Large numbers map to Infinity
                baseTable[i | 0x000] = 0x7c00;
                baseTable[i | 0x100] = 0xfc00;
            }
            else
            { // Infinity and NaN's stay Infinity and NaN's
                baseTable[i | 0x000] = 0x7c00;
                baseTable[i | 0x100] = 0xfc00;
            }
        }

        return baseTable;
    }
    private static sbyte[] GenerateShiftTable()
    {
        sbyte[] shiftTable = new sbyte[512];
        for (int i = 0; i < 256; ++i)
        {
            sbyte e = (sbyte)(127 - i);
            if (e > 24)
            { // Very small numbers map to zero
                shiftTable[i | 0x000] = 24;
                shiftTable[i | 0x100] = 24;
            }
            else if (e > 14)
            { // Small numbers map to denorms
                shiftTable[i | 0x000] = (sbyte)(e - 1);
                shiftTable[i | 0x100] = (sbyte)(e - 1);
            }
            else if (e >= -15)
            { // Normal numbers just lose precision
                shiftTable[i | 0x000] = 13;
                shiftTable[i | 0x100] = 13;
            }
            else if (e > -128)
            { // Large numbers map to Infinity
                shiftTable[i | 0x000] = 24;
                shiftTable[i | 0x100] = 24;
            }
            else
            { // Infinity and NaN's stay Infinity and NaN's
                shiftTable[i | 0x000] = 13;
                shiftTable[i | 0x100] = 13;
            }
        }

        return shiftTable;
    }

    public static float HalfToSingle(ushort half)
    {
        uint result = mantissaTable[offsetTable[half >> 10] + (half & 0x3ff)] + exponentTable[half >> 10];
        byte[] bytes = BitConverter.GetBytes(result);
        return BitConverter.ToSingle(bytes, 0);
    }

    public static ushort SingleToHalf(float single)
    {
        byte[] bytes = BitConverter.GetBytes(single);
        uint value = BitConverter.ToUInt32(bytes, 0);

        ushort result = (ushort)(baseTable[(value >> 23) & 0x1ff] + ((value & 0x007fffff) >> shiftTable[value >> 23]));
        return result; //Half.ToHalf(result);
    }

    public static float SingleToSingle(float single)
    {
        ushort half = SingleToHalf(single);
        return HalfToSingle(half);
    }
}
