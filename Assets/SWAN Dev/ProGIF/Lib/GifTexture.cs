using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gif Texture
/// </summary>
public class GifTexture
{
	// Sprite
	public Sprite m_Sprite = null;
	// Texture
	public Texture2D m_texture2d = null;
	// Delay time until the next texture.
	public float m_delaySec;

	public GifTexture(Texture2D texture2d, float delaySec)
	{
		m_texture2d = texture2d;
		m_delaySec = delaySec;
	}

	public GifTexture(Sprite sprite, float delaySec)
	{
		m_Sprite = sprite;
		m_delaySec = delaySec;
	}

	public Texture2D GetTexture2D()
	{
		if(m_texture2d != null)
		{
			return m_texture2d;
		}
		else
		{
			return m_Sprite.texture;
		}
	}

	public Sprite GetSprite()
	{
		if(m_Sprite == null)
		{
			Texture2D tex = new Texture2D(1, 1);
			tex.LoadImage(m_texture2d.EncodeToPNG());
			m_Sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
		}
		return m_Sprite;
	}
}