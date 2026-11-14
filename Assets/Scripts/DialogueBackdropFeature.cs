using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class DialogueBackdropFeature : ScriptableRendererFeature
{
    [SerializeField] private Material passMaterial;
    private BlurPass blurPass;

    public override void Create()
    {
        blurPass = new BlurPass
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
            requiresIntermediateTexture = true
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (passMaterial == null || !DialogueBackdrop.AppliesTo(renderingData.cameraData.camera))
            return;

        blurPass.Material = passMaterial;
        renderer.EnqueuePass(blurPass);
    }

    private sealed class BlurPass : ScriptableRenderPass
    {
        public Material Material { get; set; }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resources.activeColorTexture;
            TextureDesc descriptor = renderGraph.GetTextureDesc(source);
            descriptor.name = "Dialogue Camera Color";
            descriptor.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(descriptor);
            var blit = new RenderGraphUtils.BlitMaterialParameters(
                source, destination, Material, 0);
            renderGraph.AddBlitPass(blit, passName: "Dialogue Backdrop Blur");
            resources.cameraColor = destination;
        }
    }
}

