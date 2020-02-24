using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.Audio;


/// <summary>
/// Apply reverberation on AudioSource by using Image Source Method and ray 
/// tracing
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ISMReverb : MonoBehaviour
{
    // === STRUCTS ===

    /// <summary>
    /// A container struct for image source data
    /// </summary>
    public struct ImageSource
    {
        /// <summary>
        /// Normal of the mirroring plane of the image source
        /// </summary>
        public Vector3 n;
        
        /// <summary>
        /// A constant value from the plane equation Ax + By + Cz + D = 0. Can
        /// also be thought as a dot product between the normal and the known 
        /// point on the plane, i.e. Vector3.Dot(p0, n)
        /// </summary>
        public float D;

        /// <summary>
        /// Position of the image source
        /// </summary>
        public Vector3 pos;

        /// <summary>
        /// Index of the parent image source
        /// </summary>
        public int i_parent;

        /// <summary>
        /// A main constructor, use this for image sources.
        /// </summary>
        /// <param name="pos_in">Position of the image source</param>
        /// <param name="p0_in">A point in the mirroring plane</param>
        /// <param name="n_in">Normal of the mirroring plane</param>
        /// <param name="i_parent_in">Index of the parent (-1 if root)</param>
        public ImageSource(Vector3 pos_in, Vector3 p0_in, Vector3 n_in, int i_parent_in = -1)
        {
            pos = pos_in;
            D = Vector3.Dot(p0_in, n_in);
            n = n_in;
            i_parent = i_parent_in;
        }


        /// <summary>
        /// A constructor for the real source.
        /// </summary>
        /// <param name="pos_in">Position of the real source</param>
        public ImageSource(Vector3 pos_in)
        {
            pos = pos_in;
            D = 0;
            n = Vector3.zero;
            i_parent = -1;
        }
    }


    /// <summary>
    /// A container struct for ray paths
    /// </summary>
    public struct RaycastHitPath
    {
        /// <summary>
        /// List of ray wall hit points in the path
        /// </summary>
        public List<Vector3> points;

        /// <summary>
        /// Total path length
        /// </summary>
        public float totalPathLength;
        
        
        /// <summary>
        /// A constructor.
        /// </summary>
        /// <param name="pathLength">Length of the path</param>
        public RaycastHitPath(float pathLength)
        {
            points = new List<Vector3>();
            totalPathLength = pathLength;
        }
    }


    // === PUBLIC ATTRIBUTES ===

    /// <summary>
    /// Name of the audio mixer group on which the audio source connects to
    /// </summary>
    public string mixerGroupName;

    /// <summary>
    /// The time interval between two consecutive impulse response updates 
    /// (in seconds)
    /// </summary>
    public double updateInterval = 1.0;

    /// <summary>
    /// Current image sources
    /// </summary>
    public List<ImageSource> imageSources = new List<ImageSource>();

    /// <summary>
    /// Valid ray hit paths between the source and the listener
    /// </summary>
    public List<RaycastHitPath> hitPaths = new List<RaycastHitPath>();


    // === PUBLIC PROPERTIES ===

    /// <summary>
    /// Position of the audio source
    /// </summary>
    public Vector3 SourcePosition
    {
        get { return source.transform.position; }
        set { source.transform.position = value; }
    }



    /// <summary>
    /// Position of the audio listener
    /// </summary>
    public Vector3 ListenerPosition
    {
        get { return renderSettings.ListenerPosition; }
    }


    /// <summary>
    /// Access to the generated impulse response
    /// </summary>
    public float[] IR
    {
        get { return ir; }
    }


    // === PRIVATE ATTRIBUTES ===

    /// <summary>
    /// The full impulse response
    /// </summary>
    float[] ir;

    /// <summary>
    /// The source on which the reverb is applied
    /// </summary>
    AudioSource source;

    /// <summary>
    /// Position of the source in the previous frame
    /// </summary>
    Vector3 oldSourcePosition;

    /// <summary>
    /// A reference to the renderSettings script
    /// </summary>
    ISMRenderSettings renderSettings;

    /// <summary>
    /// Index of the impulse response slot in the convolution reverb plugin
    /// </summary>
    int paramIdx;

    /// <summary>
    /// The time when the impulse response is synced next time
    /// </summary>
    double nextSync;


    // === METHODS ===
    
    // Use this for initialization
    void Start()
    {
        renderSettings = FindObjectOfType<ISMRenderSettings>();
        // Plug the associated audio source to the given mixer group
        source = GetComponent<AudioSource>();
        AudioMixerGroup mixerGroup = 
            renderSettings.mixer.FindMatchingGroups(mixerGroupName)[0];
        source.outputAudioMixerGroup = mixerGroup;
        // initialize impulse responses
        var irNumSamples = Mathf.CeilToInt(
            renderSettings.IRLength * AudioSettings.outputSampleRate);
        ir = new float[irNumSamples];
        ir[0] = 1.0f;
        // Set up Convolution Reverb
        float fParamIdx;
        renderSettings.mixer.GetFloat(mixerGroupName, out fParamIdx);
        paramIdx = (int)fParamIdx;
        ConvolutionReverbInput.UploadSample(ir, paramIdx, name);
        nextSync = AudioSettings.dspTime;
        // Select random vectors as placeholders for the old positions
        oldSourcePosition = Random.insideUnitSphere * 1e3f;
    }


    // Update is called once per frame
    void Update()
    {
        // Check if either the source or the listener has moved or the 
        // simulation parameters have changed
        if (LocationsHaveChanged() || renderSettings.IRUpdateRequested)
        {
            // Calculate ISM impulse response
            ISMUpdate();
        }
        // Check if it is time to update the impulse response
        if (AudioSettings.dspTime > nextSync)
        {
            // Upload the final impulse response
            ConvolutionReverbInput.UploadSample(ir, paramIdx, name);
            nextSync = nextSync + updateInterval;
        }
        // Update old source position
        oldSourcePosition = SourcePosition;
    }


    /// <summary>
    /// Apply Image Source Method (ISM) to calculate specular reflections
    /// </summary>
    void ISMUpdate()
    {
        // Clear old data
        hitPaths.Clear();
        System.Array.Clear(ir, 0, ir.Length);
        // Check if the image source positions must be updated
        if (SourceHasMoved() || renderSettings.IRUpdateRequested)
        {
            // Clear old image sources
            imageSources.Clear();
            // === E1: Add direct sound ===
            // Add the original source to the image sources list
            // (E1) YOUR CODE HERE
            ImageSource OriginalSource = new ImageSource(transform.position);
            imageSources.Add(OriginalSource);
            

            // For each order of reflection
            int i_end = 0;
            for (var i_refl = 0; 
                 i_refl < renderSettings.NumberOfISMReflections; 
                 ++i_refl)
            {
                // === E4: Higher order reflections ===
                // (E4) YOUR CODE HERE: Update parent interval
                int i_begin = 0;
                i_end = imageSources.Count;
                // i_end = ... Mathf.Min(1, imageSources.Count
                // For each parent to reflect  = i_begin; i_parent < i_end;
                /* <-- (E4) YOUR CODE HERE: use i_begin and i_end to go through the parent image sources */
                for (var i_parent = i_begin; i_parent < i_end; ++i_parent) 
                   
                {
                    // === E2: Calculate image source positions ===
                    // Parent source on this iteration
                    ImageSource parentSource = imageSources[i_parent];
                    // For each mirroring plane
                    for (var i_child = 0; 
                         i_child < renderSettings.PlaneCenters.Length; 
                         ++i_child)
                    {
                        // Get the current mirroring plane
                        Vector3 p_plane = renderSettings.PlaneCenters[i_child];
                        Vector3 n_plane = renderSettings.PlaneNormals[i_child];
                        // (E2) YOUR CODE HERE: calculate the distance from the plane to the source
                        
                        
                        Plane wall_plane = new Plane();
                        wall_plane.SetNormalAndPosition(n_plane,p_plane);
 
                        //Debug.Log(wall_plane.GetDistanceToPoint(transform.position));
                        float sourcePlaneDistance = wall_plane.GetDistanceToPoint(imageSources[i_parent].pos);
                        
                       
                        // Is the parent source in front of the plane?
                        if (sourcePlaneDistance > 0 )/* <-- (E2) YOUR CODE HERE */
                        { 
                            // Parent source is in front of the plane,
                            // calculate mirrored position
                            Vector3 mirroredPosition = imageSources[i_parent].pos + 2 * sourcePlaneDistance * (-n_plane);

                            // Add the image source
                            // (E2) YOUR CODE HERE
                            ImageSource IMGSource = new ImageSource(mirroredPosition, p_plane, n_plane, i_parent);
                            imageSources.Add(IMGSource);
                        }
                    }
                }
            }
        }
        // === E3: Cast rays ===
        // A mask for game objects using ISMCollider (You define this "User Layer")
        int ism_colliders_only = LayerMask.GetMask("ISM colliders");
        // For each image source
        for (var i = 0; i < imageSources.Count; ++i)
        {
            // Calculate path length
            float pathLength = Vector3.Distance(imageSources[i].pos, ListenerPosition);/* <-- (E3) YOUR CODE HERE */
            // Check that the path can contribute to the impulse response
            if (pathLength < renderSettings.MaximumRayLength)
            {
                // Create a container for this path
                RaycastHitPath path = new RaycastHitPath(pathLength);
                // (E3) YOUR CODE HERE: Set the listener as the starting point for
                // the ray
                Vector3 origin = ListenerPosition;
                //Debug.Log(ListenerPosition.x);
                Vector3 originNormal =  imageSources[i].pos - origin;
                int i_next = i;
                bool isValidPath = true;
                // Loop until we have either processed the original source or 
                // found the path invalid
                while (i_next != -1 && isValidPath)
                {
                    // Get the current source
                    ImageSource imageSource = imageSources[i_next];
                    // (E3) YOUR CODE HERE: Determine ray direction and length
                    Vector3 dir = originNormal;
                    float max_length =  Vector3.Distance(origin, imageSource.pos); //I OR I NEXT?
                    //Debug.Log(max_length);
                    // Trace the ray
                    RaycastHit hit;
                    //Physics.Raycast(origin, dir, out hit, max_length, ism_colliders_only);
                    Physics.Raycast(origin, dir, out hit, max_length);

                    //Debug.Log(hit.point);
                    //Debug.Log(hit.distance);
                    //Debug.Log(hit.collider);
                  
                    if (imageSource.i_parent == -1)
                    {
                        // Handle the real source
                        // (E3) YOUR CODE HERE: check that the path is not obstructed
                        //if (Mathf.Abs(max_length - hit.distance) < 0.2)
                        if (Mathf.Abs(max_length - hit.distance) < 0.2)
                        {
                            
                            isValidPath = true; //Physics.Raycast(origin, dir); 
                            Debug.Log("path is not obstructed");
                        }
                        else
                        {
                            isValidPath = false;
                            Debug.Log("path is invalid");
                        }
                      
                    }
                    else
                    {
                        // Handle image sources
                        // (E3) YOUR CODE HERE: check that the ray hits a wall on mirroring plane
                        isValidPath = ISMMath.PlaneEQ(hit, imageSource);
                        
                    }
                    // (E3) Are there more checks needed? This depends on your previous implementation.

                    // Add the traced path if it is still valid
                    if (isValidPath)
                    {
                        // Path is valid, add the hit point to the ray path
                        // (E3) YOUR CODE HERE
                        path.points.Add(hit.point);
                        Debug.Log("path is valid, add hit point");
                        // Prepare to send the ray towards the next image source // it that okay?
                        origin = hit.point;
                        i_next = imageSource.i_parent;
                        //originNormal =  imageSources[i_next].pos - origin;
                        if (i_next != -1)
                        {
                            originNormal = imageSources[i_next].pos - origin;
                            
                        }
                    }
                }
                
                // Add the traced path if it is still valid
                if (isValidPath)
                {
                    // (E3) YOUR CODE HERE
                    Debug.Log("path added");
                    hitPaths.Add(path);
                    
                }
                
            }
        }
        // === E5: create image source impulse response ===
        foreach (var path in hitPaths)
        {
            // Calculate the sample that the ray path contributes to
            int i_path = Mathf.RoundToInt(
                AudioSettings.outputSampleRate * path.totalPathLength / ISMRenderSettings.speedOfSound);  // <-- (E5) YOUR CODE HERE
            if (i_path < ir.Length)
            {
                float abs = renderSettings.Absorption;
                float diff = renderSettings.DiffuseProportion;
                float num = path.points.Count;
                float eps = Mathf.Pow(10 , -6);

                float p_ray = (Mathf.Pow((1 - abs) * (1 - diff), num / 2) / (path.totalPathLength + eps));
               

               // (E5) YOUR CODE HERE: Determine the signal magnitude  w.r.t.
               // the amount of wall hits in the path
               ir[i_path] += p_ray;
               
            }
            
        }
    }


    /// <summary>
    /// Check whether either the source or the listener has moved
    /// </summary>
    /// <returns>True if either the listener or this source has moved, false otherwise.</returns>
    bool LocationsHaveChanged()
    {
        return renderSettings.ListenerHasMoved() || SourceHasMoved();
    }


    /// <summary>
    /// Check whether the source has moved after the last update
    /// </summary>
    /// <returns>True if the source has moved, false otherwise.</returns>
    public bool SourceHasMoved()
    {
        return !ISMMath.PositionEQ(SourcePosition, oldSourcePosition);
    }

}
