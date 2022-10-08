using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public enum ProbeVolumeDebugMode
{
    None = 0,
    ProbeGrid = 1,
    ProbeRadiance = 2
}

[ExecuteAlways]
//[System.Serializable]
public class ProbeVolume : MonoBehaviour
{
    public GameObject probePrefab;

    RenderTexture RT_WorldPos;
    RenderTexture RT_Normal;
    RenderTexture RT_Albedo;

    public int probeSizeX = 8;
    public int probeSizeY = 4;
    public int probeSizeZ = 8;
    public float probeGridSize = 2.0f;

    public ProbeVolumeData data;

    public ComputeBuffer coefficientVoxel;          // array for each probe's SH coefficient
    public ComputeBuffer lastFrameCoefficientVoxel; // last frame for inf bounce
    int[] cofficientVoxelClearValue;

    [Range(0.0f, 50.0f)]
    public float skyLightIntensity = 1.0f;

    [Range(0.0f, 50.0f)]
    public float GIIntensity = 1.0f;

    public ProbeVolumeDebugMode debugMode = ProbeVolumeDebugMode.ProbeRadiance;

    public GameObject[] probes;

    void Start()
    {
        GenerateProbes();
        data.TryLoadSurfelData(this);
        debugMode = ProbeVolumeDebugMode.ProbeGrid;
    }

    void Update()
    {
        
    }

    void OnDestroy()
    {
        if(coefficientVoxel!=null) coefficientVoxel.Release();
        if(lastFrameCoefficientVoxel != null) lastFrameCoefficientVoxel.Release();
    }

    // for DEBUG
    void OnDrawGizmos()
    {
        Gizmos.DrawCube(GetVoxelMinCorner(), new Vector3(1,1,1));

        if(probes!=null)
        {
            foreach (var go in probes)
            {
                Probe probe = go.GetComponent<Probe>(); 
                if(debugMode==ProbeVolumeDebugMode.ProbeGrid)
                {
                    Vector3 cubeSize = new Vector3(probeGridSize/2, probeGridSize/2, probeGridSize/2);
                    Gizmos.DrawWireCube(probe.transform.position + cubeSize, cubeSize * 2.0f);
                }
                
                MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
                //meshRenderer.enabled = (debugMode == ProbeVolumeDebugMode.ProbeRadiance);
                // hide in game
                if(Application.isPlaying) meshRenderer.enabled = false;
                if(debugMode == ProbeVolumeDebugMode.None) meshRenderer.enabled = false;
            }
        }
    }

    // spawn probes to world
    public void GenerateProbes()
    {
        if(probes != null)
        {
            for(int i=0; i<probes.Length; i++)
            {
                DestroyImmediate(probes[i]);
            }
        }
        if(coefficientVoxel != null) coefficientVoxel.Release();
        if(lastFrameCoefficientVoxel != null) lastFrameCoefficientVoxel.Release();

        int probeNum = probeSizeX * probeSizeY * probeSizeZ;

        // generate probe actors
        probes = new GameObject[probeNum];
        for(int x=0; x<probeSizeX; x++)
        {
            for(int y=0; y<probeSizeY; y++)
            {
                for(int z=0; z<probeSizeZ; z++)
                {
                    Vector3 relativePos = new Vector3(x, y, z) * probeGridSize;
                    Vector3 parentPos = gameObject.transform.position;

                    // setup probe
                    int index = x * probeSizeY * probeSizeZ + y * probeSizeZ + z;
                    probes[index] = Instantiate(probePrefab, gameObject.transform) as GameObject;
                    probes[index].transform.position = relativePos + parentPos; 
                    probes[index].GetComponent<Probe>().indexInProbeVolume = index;
                    probes[index].GetComponent<Probe>().TryInit();
                }
            }
        }

        // generate 1D "Voxel" buffer to storage SH coefficients
        coefficientVoxel = new ComputeBuffer(probeNum * 27, sizeof(int));
        lastFrameCoefficientVoxel = new ComputeBuffer(probeNum * 27, sizeof(int));
        cofficientVoxelClearValue = new int[probeNum *  27];
        for(int i=0; i<cofficientVoxelClearValue.Length; i++) 
        {
            cofficientVoxelClearValue[i] = 0;
        }  
    }

    // precompute surfel
    public void ProbeCapture()
    {
        // hide debug sphere
        foreach (var go in probes)
        {
            go.GetComponent<MeshRenderer>().enabled = false;
        }

        // cap
        foreach (var go in probes)
        {
            Probe probe = go.GetComponent<Probe>(); 
            probe.CaptureGbufferCubemaps();
        }

        data.StorageSurfelData(this);
    }

    public void ClearCoefficientVoxel(CommandBuffer cmd)
    {
        if(coefficientVoxel==null || cofficientVoxelClearValue==null) return;
        cmd.SetBufferData(coefficientVoxel, cofficientVoxelClearValue);
    }

    // 保存上一帧的球谐系数
    public void SwapLastFrameCoefficientVoxel()
    {
        if(coefficientVoxel==null || lastFrameCoefficientVoxel==null) return;
        (coefficientVoxel, lastFrameCoefficientVoxel) = (lastFrameCoefficientVoxel, coefficientVoxel);
    }

    public Vector3 GetVoxelMinCorner()
    {
        return gameObject.transform.position;
    }
}
