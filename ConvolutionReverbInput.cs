using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// A proxy for inputting impulse responses to the convolution reverb plugin.
/// </summary>
public class ConvolutionReverbInput
{
    /// <summary>
    /// Upload sample interface of the Convolution reverb plugin
    /// </summary>
    /// <param name="index">Index of the slot in which the sample is uploaded to</param>
    /// <param name="data">Sample data (different channels are interleaved)</param>
    /// <param name="numsamples">The length of the impulse in samples per channel</param>
    /// <param name="numchannels">Number of channels</param>
    /// <param name="samplerate">Sampling frequency of the data</param>
    /// <param name="name">Name of the sample</param>
    /// <returns></returns>
    [DllImport("libAudioPluginVADemo")]
    private static extern bool ConvolutionReverb_UploadSample(
        int index, 
        float[] data, 
        int numsamples, 
        int numchannels, 
        int samplerate, 
        [MarshalAs(UnmanagedType.LPStr)] string name);


    /// <summary>
    /// Upload new Sample to the Convolution reverb audio plugin
    /// </summary>
    /// <param name="data">Sample data</param>
    /// <param name="index">Index of the slot in which the sample is uploaded to</param>
    /// <param name="sampleName">Name of the sample</param>
    public static void UploadSample(float[] data, int index, string sampleName)
    {
        ConvolutionReverb_UploadSample(index, data, data.Length, 1, AudioSettings.outputSampleRate, sampleName);
    }
}
