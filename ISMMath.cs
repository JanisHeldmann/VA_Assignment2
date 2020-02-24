using UnityEngine;


/// <summary>
/// A math helper class for Image Source Method classes
/// </summary>
public class ISMMath
{
    /// <summary>
    /// Test whether the two planes are the same
    /// </summary>
    /// <param name="p1">Point on the first plane</param>
    /// <param name="n1">Normal of the first plane</param>
    /// <param name="p2">Point on the second plane</param>
    /// <param name="n2">Normal of the second plane</param>
    /// <returns>True if the planes are equal, false otherwise.</returns>
    public static bool PlaneEQ(Vector3 p1, Vector3 n1, Vector3 p2, Vector3 n2)
    {
        return DirectionEQ(n1, n2) && Mathf.Abs(Vector3.Dot(p1, n1) - Vector3.Dot(p2, n1)) < Vector3.kEpsilon;
    }


    /// <summary>
    /// Test whether the ray has hit the plane
    /// </summary>
    /// <param name="hit">Raycast hit information</param>
    /// <param name="im_src">An image source containing the mirroring plane information</param>
    /// <returns>True if the planes are equal, false otherwise.</returns>
    public static bool PlaneEQ(RaycastHit hit, ISMReverb.ImageSource im_src)
    {
        return ISMMath.DirectionEQ(hit.normal, im_src.n) && Mathf.Abs(Vector3.Dot(hit.point, im_src.n) - im_src.D) < Vector3.kEpsilon;
    }


    /// <summary>
    /// Test whether the two positions are equal
    /// </summary>
    /// <param name="p1">The first position to compare</param>
    /// <param name="p2">The second position to compare</param>
    /// <returns>True if the positions are equal, false otherwise</returns>
    public static bool PositionEQ(Vector3 p1, Vector3 p2)
    {
        return (p1 - p2).magnitude < Vector3.kEpsilon;
    }


    /// <summary>
    /// Test whether the two unit vectors are the same
    /// </summary>
    /// <param name="u1">The first unit vector to compare</param>
    /// <param name="u2">The second unit vector to compare</param>
    /// <returns></returns>
    public static bool DirectionEQ(Vector3 u1, Vector3 u2)
    {
        return Vector3.Dot(u1, u2) > (1.0f - Vector3.kEpsilon);
    }

}
