using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProbeVolume))]
public class ProbeVolumeDebug : Editor
{
    public override void OnInspectorGUI() 
    {
        DrawDefaultInspector();

        if(GUILayout.Button("Generate Probes")) 
        {
            ProbeVolume probeVolume = (ProbeVolume)target;
            probeVolume.GenerateProbes();
        }

        if(GUILayout.Button("Capture Scene Probes")) 
        {
            ProbeVolume probeVolume = (ProbeVolume)target;
            probeVolume.ProbeCapture();
        }
    }
}
