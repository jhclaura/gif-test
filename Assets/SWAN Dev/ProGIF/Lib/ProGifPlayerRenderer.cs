using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Renderer))]
public sealed class ProGifPlayerRenderer : ProGifPlayerComponent
{
	private List<Renderer> m_ExtraRenderers = new List<Renderer>();

	void Awake()
	{
		if(destinationRenderer == null)
		{
			destinationRenderer = gameObject.GetComponent<Renderer>();
		}
	}

	// Update is called once per frame
	void Update()
	{
		if(nowState == State.Playing && displayType == ProGifPlayerComponent.DisplayType.Renderer)
		{
			if(Time.time >= nextFrameTime)
			{
				spriteIndex = (spriteIndex >= gifTextures.Count - 1)? 0 : spriteIndex + 1;
				nextFrameTime = Time.time + interval;
			}

			if(spriteIndex < gifTextures.Count)
			{
				if(destinationRenderer != null) 
				{
					destinationRenderer.material.mainTexture = gifTextures[spriteIndex].GetSprite().texture;
				}

				if(m_ExtraRenderers != null && m_ExtraRenderers.Count > 0)
				{
					Texture2D tex = gifTextures[spriteIndex].GetSprite().texture;
					for(int i = 0; i < m_ExtraRenderers.Count; i++)
					{
						if(m_ExtraRenderers[i] != null)
						{
							m_ExtraRenderers[i].material.mainTexture = tex;
						}
						else
						{
							m_ExtraRenderers.Remove(m_ExtraRenderers[i]);
							m_ExtraRenderers.TrimExcess();
						}
					}
				}
			}
		}
	}

	public override void Play(int fps, Sprite[] sprites)
	{
		PrePlay(fps, sprites);

		if(destinationRenderer == null)
		{
			destinationRenderer = gameObject.GetComponent<Renderer>();
		}
		if(destinationRenderer != null && destinationRenderer.material != null)
		{
			destinationRenderer.material.mainTexture = gifTextures[0].GetSprite().texture;
		}
	}

	public void ChangeDestination(Renderer renderer)
	{
		destinationRenderer = renderer;
	}

	public void AddExtraDestination(Renderer renderer)
	{
		if(!m_ExtraRenderers.Contains(renderer))
		{
			m_ExtraRenderers.Add(renderer);
		}
	}

	public void RemoveFromExtraDestination(Renderer renderer)
	{
		if(m_ExtraRenderers.Contains(renderer))
		{
			m_ExtraRenderers.Remove(renderer);
			m_ExtraRenderers.TrimExcess();
		}
	}
}
