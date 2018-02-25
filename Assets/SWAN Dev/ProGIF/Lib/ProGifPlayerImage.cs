using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class ProGifPlayerImage : ProGifPlayerComponent
{
	private List<Image> m_ExtraImages = new List<Image>();

	void Awake()
	{
		if(destinationImage == null)
		{
			destinationImage = gameObject.GetComponent<Image>();
		}
	}

	// Update is called once per frame
	void Update()
	{
		if(nowState == State.Playing && displayType == ProGifPlayerComponent.DisplayType.Image)
		{
			if(Time.time >= nextFrameTime)
			{
				spriteIndex = (spriteIndex >= gifTextures.Count - 1)? 0 : spriteIndex + 1;
				nextFrameTime = Time.time + interval;
			}

			if(spriteIndex < gifTextures.Count)
			{
				if(destinationImage != null)
				{
					destinationImage.sprite = gifTextures[spriteIndex].GetSprite();
				}

				if(m_ExtraImages != null && m_ExtraImages.Count > 0)
				{
					Sprite sp = gifTextures[spriteIndex].GetSprite();
					for(int i = 0; i < m_ExtraImages.Count; i++)
					{
						if(m_ExtraImages[i] != null)
						{
							m_ExtraImages[i].sprite = sp;
						}
						else
						{
							m_ExtraImages.Remove(m_ExtraImages[i]);
							m_ExtraImages.TrimExcess();
						}
					}
				}
			}
		}
	}

	public override void Play(int fps, Sprite[] sprites)
	{
		PrePlay(fps, sprites);

		if(destinationImage == null)
		{
			destinationImage = gameObject.GetComponent<Image>();
		}
		if(destinationImage != null)
		{
			destinationImage.sprite = gifTextures[0].GetSprite();
		}
	}

	public void ChangeDestination(Image image)
	{
		destinationImage = image;
	}

	public void AddExtraDestination(Image image)
	{
		if(!m_ExtraImages.Contains(image))
		{
			m_ExtraImages.Add(image);
		}
	}

	public void RemoveFromExtraDestination(Image image)
	{
		if(m_ExtraImages.Contains(image))
		{
			m_ExtraImages.Remove(image);
			m_ExtraImages.TrimExcess();
		}
	}
}
