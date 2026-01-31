using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    // These are used to pass data to future passes.
    // The ContextItem used to store the texture reference at.
    public class TexRefData : ContextItem
    {
        // The texture reference variable.
        public TextureHandle texture = TextureHandle.nullHandle;

        // Reset function required by ContextItem. It should reset all variables not carried
        // over to next frame.
        public override void Reset()
        {
            // We should always reset texture handles since they are only vaild for the current frame.
            texture = TextureHandle.nullHandle;
        }
    }

    public class EdgeDetectionPass : ScriptableRenderPass
    {
        private static readonly int EdgeThickness = Shader.PropertyToID("_EdgeThickness");
        private static readonly int OutlineColorsTex = Shader.PropertyToID("_OutlineColorsTex");

        private readonly Material blitMaterial;

        private class EdgeDetectionPassData
        {
            public TextureHandle source;
            public Material blitMat;
        }

        public EdgeDetectionPass(List<OutlineGroup> outlineGroups, Shader outlineShader, EdgeDetectionSettings edgeDetectionSettings)
        {
            blitMaterial = new Material(outlineShader);
            blitMaterial.SetFloat(EdgeThickness, edgeDetectionSettings.edgeThickness);
            
            int groupCount = outlineGroups.Count;
            // Create a small 1D texture
            Texture2D colorTex = new Texture2D(groupCount, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point, // no interpolation
                wrapMode = TextureWrapMode.Clamp // avoid bleeding
            };

            // Fill the texture with your outline colors
            for (int i = 0; i < groupCount; i++)
                colorTex.SetPixel(i, 0, outlineGroups[i].color);

            colorTex.Apply(); // upload to GPU
            
            blitMaterial.SetTexture(OutlineColorsTex, colorTex);
        }

        // Scale bias is used to control how the blit operation is done. The x and y parameter controls the scale
        // and z and w controls the offset.
        private static readonly Vector4 scaleBias = new Vector4(1f, 1f, 0f, 0f);

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!frameData.Contains<TexRefData>())
            {
                return;
            }
            
            // ------
            var resourceData = frameData.Get<UniversalResourceData>();
            var texRef = frameData.Get<TexRefData>();
            // renderGraph.AddBlitPass(texRef.texture, resourceData.activeColorTexture, Vector2.one, Vector2.zero, passName:"Blit Pass");
            
            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            using (var builder = renderGraph.AddRasterRenderPass<EdgeDetectionPassData>($"Blit Pass", out var passData))
            {
                passData.source = texRef.texture;

                // Material used in the blit operation.
                passData.blitMat = blitMaterial;

                // Sets input attachment.
                builder.UseTexture(passData.source);
                // Sets color attachment 0.
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                // Sets the render function.
                builder.SetRenderFunc((EdgeDetectionPassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
            }
        }

        // ExecutePass is the render function for each of the blit render graph recordings.
        // This is good practice to avoid using variables outside of the lambda it is called from.
        // It is static to avoid using member variables which could cause unintended behaviour.
        static void ExecutePass(EdgeDetectionPassData data, RasterGraphContext rgContext)
        {
            Blitter.BlitTexture(rgContext.cmd, data.source, scaleBias, data.blitMat, 0);
        }
    }
    
    public class ObjectMaskPass : ScriptableRenderPass
    {
        private static readonly int OutlineID = Shader.PropertyToID("_OutlineID");
        private static readonly int OutlineGroupCount = Shader.PropertyToID("_OutlineGroupCount");

        private class MaskGroup
        {
            public FilteringSettings FilteringSettings;
            public Material MaskMaterial;
        }

        private readonly List<MaskGroup> maskGroups;
        
        private class PassData
        {
            public List<RendererListHandle> rendererListHandles;
        }

        public ObjectMaskPass(List<OutlineGroup> outlineGroups, Shader maskShader)
        {
            // TODO Use colors to create a 1D texture to sample from EdgeDetection (so not here?)
            
            int groupCount = outlineGroups.Count;
            maskGroups = new List<MaskGroup>(groupCount);

            Shader.SetGlobalFloat(OutlineGroupCount, groupCount);
            
            for (int i = 0; i < groupCount; i++)
            {
                var mat = new Material(maskShader);
                mat.SetInt(OutlineID, i);
                
                maskGroups.Add(new MaskGroup()
                {
                    FilteringSettings = new FilteringSettings(RenderQueueRange.all, -1, outlineGroups[i].renderingLayerMask),
                    MaskMaterial = mat,
                });
                
                maskGroups[i].MaskMaterial = mat;
            }
        }
        
        private void InitRendererList(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            
            var sortFlags = cameraData.defaultOpaqueSortFlags;
            
            ShaderTagId shaderTagId = new ShaderTagId("UniversalForward");

            passData.rendererListHandles = new List<RendererListHandle>();

            foreach (MaskGroup outlineGroup in maskGroups)
            {
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagId, universalRenderingData, cameraData, lightData, sortFlags);
                drawingSettings.overrideMaterial = outlineGroup.MaskMaterial;
                RendererListParams rendererListParams = new RendererListParams(universalRenderingData.cullResults, drawingSettings, outlineGroup.FilteringSettings);
                passData.rendererListHandles.Add(renderGraph.CreateRendererList(rendererListParams));
            }
        }
        
        private static void ExecutePass(PassData data, RasterGraphContext context)
        {
            context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1,0);

            foreach (RendererListHandle rendererListHandle in data.rendererListHandles)
            {
                context.cmd.DrawRendererList(rendererListHandle);
            }
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            
            // Pass 1: render objects to mask
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("Create Mask Pass", out var passData))
            {
                InitRendererList(frameData, ref passData, renderGraph);
                
                // Describe mask texture
                var desc = renderGraph.GetTextureDesc(resourceData.activeDepthTexture);
                desc.name = "CustomObjectMask";
                desc.clearBuffer = true;
                desc.clearColor = Color.black;
                desc.format = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm; // ensure alpha
                TextureHandle maskTex = renderGraph.CreateTexture(desc);
                
                TexRefData texRef = frameData.GetOrCreate<TexRefData>();

                // Draw object masks
                foreach (RendererListHandle rendererListHandle in passData.rendererListHandles)
                {
                    builder.UseRendererList(rendererListHandle);
                }
                
                // Output
                builder.SetRenderAttachment(maskTex, 0);
                
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                
                texRef.texture = maskTex;
            }
        }
    }

    [Serializable]
    public class OutlineGroup
    {
        public RenderingLayerMask renderingLayerMask;
        public Color color;
    }

    [Serializable]
    public class EdgeDetectionSettings
    {
        public float edgeThickness = 1f;
    }

    [Header("Outline Groups")]
    public List<OutlineGroup> outlineGroups;
    public Shader outlineMaskShader;
    
    [Header("Edge Detection")]
    public Shader outlineEdgeDetectionShader;
    public EdgeDetectionSettings edgeDetectionSettings = new EdgeDetectionSettings();

    [Header("Other")]
    public RenderPassEvent injectionPoint;

    private ObjectMaskPass objectMaskPass;
    private EdgeDetectionPass edgeDetectionPass;

    public override void Create()
    {
        if (outlineMaskShader == null || outlineEdgeDetectionShader == null || outlineGroups.Count == 0)
            return;
        
        objectMaskPass = new ObjectMaskPass(outlineGroups, outlineMaskShader);
        edgeDetectionPass = new EdgeDetectionPass(outlineGroups, outlineEdgeDetectionShader, edgeDetectionSettings);
        objectMaskPass.renderPassEvent = injectionPoint;
        edgeDetectionPass.renderPassEvent = injectionPoint;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (outlineMaskShader == null || outlineEdgeDetectionShader == null || outlineGroups.Count == 0)
            return;
        
        renderer.EnqueuePass(objectMaskPass);
        renderer.EnqueuePass(edgeDetectionPass);
    }
}
