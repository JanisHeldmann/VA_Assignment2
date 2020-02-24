using UnityEngine;


/// <summary>
/// Visualize impulse response by using LineRenderer
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ISMDebugLine : MonoBehaviour
{
    /// <summary>
    /// Number of vertices in the debug line
    /// </summary>
    public int resolution = 100;

    /// <summary>
    /// A line renderer object
    /// </summary>
    LineRenderer line;

    
    void Start ()
    {
        line = GetComponent<LineRenderer>();
        // create impulse response debug line
        Vector3[] dbgLine = new Vector3[resolution];
        Vector3[] posToLerp = new Vector3[2];
        line.GetPositions(posToLerp);
        for (var i = 0; i < resolution; ++i)
        {
            dbgLine[i] = Vector3.Lerp(posToLerp[0], posToLerp[1], i / (float)(resolution - 1));
        }
        line.positionCount = resolution;
        line.SetPositions(dbgLine);
    }
	
	
    /// <summary>
    /// Draw the given impulse response
    /// </summary>
    /// <param name="ir">Impulse response to draw</param>
    public void DrawIR(float[] ir)
    {
        // Check for all-zero signal
        bool is_zero = true;
        float epsilon = Mathf.Epsilon;
        foreach (float x in ir)
        {
            is_zero &= Mathf.Abs(x) < epsilon;
        }
        int posLen = line.positionCount;
        Vector3[] pos = new Vector3[posLen];
        line.GetPositions(pos);
        if (is_zero)
        {
            // All-zero signal, bypass the averaging
            for (var i = 0; i < posLen; ++i)
            {
                pos[i].y = 0.0f;
            }
        }
        else
        {
            // Data found, draw
            int irLen = ir.Length;
            float[] y = new float[posLen];
            int[] posCount = new int[posLen];
            // Calculate means of the sample bins in the debug line
            for (var i = 0; i < irLen; ++i)
            {
                var i_pos = (int)((posLen - 1) * i / (float)irLen + 0.5f);
                y[i_pos] += ir[i];
                ++posCount[i_pos];
            }
            for (var i = 0; i < y.Length; ++i)
            {
                y[i] /= posCount[i] != 0 ? posCount[i] : 1.0f;
            }
            // get absolute maximum for normalization
            var y_max = Mathf.Max(Mathf.Max(y), -Mathf.Min(y));
            for (var i = 0; i < posLen; ++i)
            {
                pos[i].y = y[i] / y_max;
            }
        }
        line.SetPositions(pos);
    }
}
