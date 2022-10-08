using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[Serializable]
[CreateAssetMenu(fileName = "ProbeVolumeData", menuName = "ProbeVolumeData")]
public class ProbeVolumeData: ScriptableObject
{
    [SerializeField]
    public Vector3 volumePosition;

    [SerializeField]
    public float[] surfelStorageBuffer;

    // pack all probe's data to 1D array
    public void StorageSurfelData(ProbeVolume volume)
    {
        int probeNum = volume.probeSizeX * volume.probeSizeY * volume.probeSizeZ;
        int surfelPerProbe = 512;
        int floatPerSurfel = 10;
        Array.Resize<float>(ref surfelStorageBuffer, probeNum * surfelPerProbe * floatPerSurfel);
        int j = 0;
        for(int i=0; i<volume.probes.Length; i++)
        {
            Probe probe = volume.probes[i].GetComponent<Probe>();
            foreach (var surfel in probe.readBackBuffer)
            {
                surfelStorageBuffer[j++] = surfel.position.x;
                surfelStorageBuffer[j++] = surfel.position.y;
                surfelStorageBuffer[j++] = surfel.position.z;
                surfelStorageBuffer[j++] = surfel.normal.x;
                surfelStorageBuffer[j++] = surfel.normal.y;
                surfelStorageBuffer[j++] = surfel.normal.z;
                surfelStorageBuffer[j++] = surfel.albedo.x;
                surfelStorageBuffer[j++] = surfel.albedo.y;
                surfelStorageBuffer[j++] = surfel.albedo.z;
                surfelStorageBuffer[j++] = surfel.skyMask;
            }
        }

        volumePosition = volume.gameObject.transform.position;

        // save
        //EditorUtility.SetDirty(volumePosition);
        //EditorUtility.SetDirty(surfelStorageBuffer);
        EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }

    // load surfel data from storage
    public void TryLoadSurfelData(ProbeVolume volume)
    {
        int probeNum = volume.probeSizeX * volume.probeSizeY * volume.probeSizeZ;
        int surfelPerProbe = 512;
        int floatPerSurfel = 10;
        bool dataDirty = surfelStorageBuffer.Length != probeNum * surfelPerProbe * floatPerSurfel;
        bool posDirty = volume.gameObject.transform.position != volumePosition;
        if(posDirty || dataDirty)
        {
            Debug.LogWarning("volume data is old! please re capture!");
            Debug.LogWarning("探针组数据需要重新捕获");
            return;
        }

        int j = 0;
        foreach (var go in volume.probes)
        {
            Probe probe = go.GetComponent<Probe>();
            for(int i=0; i<probe.readBackBuffer.Length; i++)
            {
                probe.readBackBuffer[i].position.x = surfelStorageBuffer[j++];
                probe.readBackBuffer[i].position.y = surfelStorageBuffer[j++];
                probe.readBackBuffer[i].position.z = surfelStorageBuffer[j++];
                probe.readBackBuffer[i].normal.x = surfelStorageBuffer[j++];
                probe.readBackBuffer[i].normal.y = surfelStorageBuffer[j++];
                probe.readBackBuffer[i].normal.z = surfelStorageBuffer[j++];
                probe.readBackBuffer[i].albedo.x = surfelStorageBuffer[j++];
                probe.readBackBuffer[i].albedo.y = surfelStorageBuffer[j++];
                probe.readBackBuffer[i].albedo.z = surfelStorageBuffer[j++];
                probe.readBackBuffer[i].skyMask = surfelStorageBuffer[j++];
            }
            probe.surfels.SetData(probe.readBackBuffer);
        }
    }
}
