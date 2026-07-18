using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DoorPuzzle
{
    public sealed class PortalStencilRendererFeature : ScriptableRendererFeature
    {
        public const int ContentLayer = 29;
        public const int MaskLayer = 30;

        private StencilPass maskPass;
        private StencilPass contentPass;

        public override void Create()
        {
            maskPass = new StencilPass("Blues With You Portal Mask", MaskLayer, 1,
                CompareFunction.Always, StencilOp.Replace, RenderPassEvent.BeforeRenderingOpaques);
            contentPass = new StencilPass("Blues With You Portal Content", ContentLayer, 1,
                CompareFunction.Equal, StencilOp.Keep, RenderPassEvent.AfterRenderingOpaques);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            renderer.EnqueuePass(maskPass);
            renderer.EnqueuePass(contentPass);
        }

        private sealed class StencilPass : ScriptableRenderPass
        {
            private sealed class PassData
            {
                internal RendererListHandle rendererList;
            }

            private readonly string profilerName;
            private readonly ProfilingSampler passSampler;
            private FilteringSettings filtering;
            private RenderStateBlock stateBlock;
            private readonly List<ShaderTagId> shaderTags = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit")
            };

            public StencilPass(string name, int layer, int reference, CompareFunction compare,
                StencilOp passOperation, RenderPassEvent passEvent)
            {
                profilerName = name;
                passSampler = new ProfilingSampler(name);
                renderPassEvent = passEvent;
                filtering = new FilteringSettings(RenderQueueRange.all, 1 << layer);
                var stencil = new StencilState(true, 255, 255, compare, passOperation, StencilOp.Keep, StencilOp.Keep);
                stateBlock = new RenderStateBlock(RenderStateMask.Stencil)
                {
                    stencilReference = reference,
                    stencilState = stencil
                };
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();
                var sorting = renderPassEvent <= RenderPassEvent.BeforeRenderingOpaques
                    ? cameraData.defaultOpaqueSortFlags
                    : SortingCriteria.CommonTransparent;
                var drawing = RenderingUtils.CreateDrawingSettings(
                    shaderTags, renderingData, cameraData, lightData, sorting);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           profilerName, out var passData, passSampler))
                {
                    var tagValues = new NativeArray<ShaderTagId>(1, Allocator.Temp);
                    var stateBlocks = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
                    tagValues[0] = ShaderTagId.none;
                    stateBlocks[0] = stateBlock;
                    var rendererListParams = new RendererListParams(
                        renderingData.cullResults, drawing, filtering)
                    {
                        tagValues = tagValues,
                        stateBlocks = stateBlocks,
                        isPassTagName = false
                    };

                    passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
                    builder.UseRendererList(passData.rendererList);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                    builder.UseAllGlobalTextures(true);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.DrawRendererList(data.rendererList);
                    });
                }
            }

#pragma warning disable CS0618
            [System.Obsolete("Compatibility path for the WebGL Forward renderer.")]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var sorting = renderPassEvent <= RenderPassEvent.BeforeRenderingOpaques
                    ? SortingCriteria.CommonOpaque
                    : SortingCriteria.CommonTransparent;
                var drawing = CreateDrawingSettings(shaderTags[0], ref renderingData, sorting);
                for (var i = 1; i < shaderTags.Count; i++) drawing.SetShaderPassName(i, shaderTags[i]);
                var command = CommandBufferPool.Get(profilerName);
                using (new ProfilingScope(command, new ProfilingSampler(profilerName)))
                {
                    context.ExecuteCommandBuffer(command);
                    command.Clear();
                    context.DrawRenderers(renderingData.cullResults, ref drawing, ref filtering, ref stateBlock);
                }
                context.ExecuteCommandBuffer(command);
                CommandBufferPool.Release(command);
            }
#pragma warning restore CS0618
        }
    }
}
