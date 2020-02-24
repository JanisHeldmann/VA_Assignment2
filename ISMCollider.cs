using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ISMCollider : MonoBehaviour {

    
    /// <summary>
    /// A list of points determining locations of the mirroring planes
    /// </summary>
    public List<Vector3> planeCenters = new List<Vector3>();

    /// <summary>
    /// A list of normals determining the orientations of the mirroring planes
    /// </summary>
    public List<Vector3> planeNormals = new List<Vector3>();

    /// <summary>
    /// A referece to renderSettings component.
    /// </summary>
    ISMRenderSettings renderSettings;


    int originalLayer = -1;
    int ISMLayer = 8;

    void Awake()
    {
        if (originalLayer == -1)
        {
            originalLayer = gameObject.layer;
        }
        gameObject.layer = ISMLayer;
    }


    void OnDestroy()
    {
        gameObject.layer = originalLayer;
        originalLayer = -1;
        planeCenters.Clear();
        planeNormals.Clear();
    }


    public void CalculateMirrorPlanes()
    {
        // clear old contents
        planeCenters.Clear();
        planeNormals.Clear();
        // determine triangle centers and normals in world coordinates
        Mesh room_mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] room_n = room_mesh.normals;
        Vector3[] room_vertices = room_mesh.vertices;
        int[] room_tri = room_mesh.triangles;
        Matrix4x4 M_scene = transform.localToWorldMatrix;
        Matrix4x4 N_scene = Matrix4x4.Transpose(Matrix4x4.Inverse(M_scene));
        for (var i_tri = 0; i_tri < room_tri.Length; i_tri += 3)
        {
            Vector3 new_center = (Vector3) (M_scene * ((room_vertices[room_tri[i_tri]]
                                             + room_vertices[room_tri[i_tri + 1]]
                                             + room_vertices[room_tri[i_tri + 2]]) / 3))
                                 + transform.position;
            Vector3 new_normal = ((Vector3) (N_scene * ((room_n[room_tri[i_tri]]
                                             + room_n[room_tri[i_tri + 1]]
                                             + room_n[room_tri[i_tri + 2]]) / 3))).normalized;
            // check for duplicate planes
            bool new_plane = true;
            for (var i_plane = 0; i_plane < planeCenters.Count; ++i_plane)
            {
                if (ISMMath.PlaneEQ(planeCenters[i_plane], planeNormals[i_plane], new_center, new_normal))
                {
                    new_plane = false;
                    break;
                }
            }
            if (new_plane)
            {
                // Add plane if not a duplicate
                planeCenters.Add(new_center);
                planeNormals.Add(new_normal);
            }
        }
    }
}
