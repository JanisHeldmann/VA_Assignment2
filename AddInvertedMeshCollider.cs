// Credits to Unity forum user roger_lew for this script
// Source: https://forum.unity.com/threads/can-you-invert-a-sphere-or-box-collider.118733/
using UnityEngine;
using System.Linq;
using System.Collections;

/// <summary>
/// AddInvertedMeshCollider: invert collider mesh faces attached to the object
/// </summary>
public class AddInvertedMeshCollider : MonoBehaviour
{
    public bool removeExistingColliders = true;


    /// <summary>
    /// Create a collider with inverted mesh faces
    /// </summary>
    public void CreateInvertedMeshCollider()
    {
        if (removeExistingColliders)
            RemoveExistingColliders();

        InvertMesh();

        gameObject.AddComponent<MeshCollider>();
    }


    /// <summary>
    /// Remove existing colliders from the game object
    /// </summary>
    private void RemoveExistingColliders()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            DestroyImmediate(colliders[i]);
    }


    /// <summary>
    /// Invert faces of a mesh
    /// </summary>
    private void InvertMesh()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.triangles = mesh.triangles.Reverse().ToArray();
    }
}