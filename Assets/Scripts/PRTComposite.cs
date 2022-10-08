using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PRTComposite : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        public Material blitMaterial;
        public RenderTargetHandle tempRTHandle;
        public RenderTargetIdentifier blitSrc;

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor rtDesc = renderingData.cameraData.cameraTargetDescriptor;
            cmd.GetTemporaryRT(tempRTHandle.id, rtDesc);

            blitSrc = renderingData.cameraData.renderer.cameraColorTarget;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            RenderTargetIdentifier tempRT = tempRTHandle.Identifier();

            ProbeVolume[] volumes = GameObject.FindObjectsOfType(typeof(ProbeVolume)) as ProbeVolume[];
            ProbeVolume volume = volumes.Length==0 ? null : volumes[0];
            if(volume != null)
            {
                cmd.Blit(blitSrc, tempRT, blitMaterial);
                cmd.Blit(tempRT, blitSrc);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempRTHandle.id);
        }
    }

    public Material compositeMaterial;
    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        m_ScriptablePass.blitMaterial = compositeMaterial;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


