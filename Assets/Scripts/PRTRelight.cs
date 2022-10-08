using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class PRTRelight : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            // 如果存在 probe volume, 那么还要 clear 一下探针组的球谐系数 buffer
            ProbeVolume[] volumes = GameObject.FindObjectsOfType(typeof(ProbeVolume)) as ProbeVolume[];
            ProbeVolume volume = volumes.Length==0 ? null : volumes[0];
            if(volume != null)
            {
                volume.SwapLastFrameCoefficientVoxel();
                volume.ClearCoefficientVoxel(cmd);

                Vector3 corner = volume.GetVoxelMinCorner();
                Vector4 voxelCorner = new Vector4(corner.x, corner.y, corner.z, 0);
                Vector4 voxelSize = new Vector4(volume.probeSizeX, volume.probeSizeY, volume.probeSizeZ, 0);
                cmd.SetGlobalFloat("_coefficientVoxelGridSize", volume.probeGridSize);
                cmd.SetGlobalVector("_coefficientVoxelSize", voxelSize);
                cmd.SetGlobalVector("_coefficientVoxelCorner", voxelCorner);
                cmd.SetGlobalBuffer("_coefficientVoxel", volume.coefficientVoxel);
                cmd.SetGlobalBuffer("_lastFrameCoefficientVoxel", volume.lastFrameCoefficientVoxel);
                cmd.SetGlobalFloat("_skyLightIntensity", volume.skyLightIntensity);
                cmd.SetGlobalFloat("_GIIntensity", volume.GIIntensity);
            }

            // dispatch
            Probe[] probes = GameObject.FindObjectsOfType(typeof(Probe)) as Probe[];
            foreach(var probe in probes)
            {
                if(probe==null) continue;
                probe.TryInit();
                probe.ReLight(cmd);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}

