using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnimeCelShading
{
	public class AnimeStackPass : ScriptableRenderPass
	{
		private RTHandle _source;
		private RTHandle _target;
	
		public AnimeStackPass()
		{
			renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
		}
	
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (_source == null || _target == null)
			{
				return;
			}
			
			ref CameraData cameraData = ref renderingData.cameraData;
			VolumeStack stack = VolumeManager.instance?.stack;

			if (!cameraData.postProcessEnabled || stack == null)
			{
				return;
			}
			
			CommandBuffer cmd = CommandBufferPool.Get("Anime Stack");
			cmd.Clear();
			
			Blitter.BlitCameraTexture(cmd, _source, _target);
			cmd.SetGlobalTexture(_target.name, _target.nameID);
			
			CoreUtils.SetRenderTarget(
				cmd,
				_source,
				RenderBufferLoadAction.DontCare,
				RenderBufferStoreAction.Store,
				ClearFlag.None,
				Color.white
			);
			
			foreach (ComponentData data in AnimeStackFeature.Components)
			{
				VolumeComponent component = stack.GetComponent(data.Type);
			
				if (component == null || !component.active || component is not IPostProcessComponent postProcessComponent || !postProcessComponent.IsActive())
				{
					continue;
				}
			
				using (new ProfilingScope(cmd, data.ProfilingSampler))
				{
					Render(cmd, component, data);
				}
			}
			
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	
		public void Setup(RTHandle colorHandle, ref CameraData cameraData)
		{
			_source = colorHandle;
			
			Camera camera = cameraData.camera;
			RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
			descriptor.width = camera.pixelWidth;
			descriptor.height = camera.pixelHeight;
			descriptor.depthBufferBits = 0;
			descriptor.msaaSamples = 1;
			
			RenderingUtils.ReAllocateIfNeeded(
				ref _target,
				descriptor,
				FilterMode.Bilinear,
				TextureWrapMode.Clamp,
				name: "_MainTex"
			);
		}
	
		public void Dispose()
		{
			_source?.Release();
			_target?.Release();
	
			_source = null;
			_target = null;
		}
	
		private static void Render(CommandBuffer cmd, VolumeComponent component, ComponentData data)
		{
			if (data.Material == null)
			{
				return;
			}
	
			if (component is IAnimeStackComponent animeStackComponent)
			{
				animeStackComponent.UpdateMaterial(data.Material);
			}
			
			cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, data.Material);
		}
	}
}