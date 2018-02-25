using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class Texture2DToGIFDemo : MonoBehaviour
{
	public Text text1;
	public Image displayImage;

	private ProGifTexture2DsToGIF tex2Gif = null;
	private List<Texture2D> tex2DList = null;

	public void ConvertTex2DToGIF()
	{
		Clear();

		tex2Gif = ProGifTexture2DsToGIF.Instance;

		//Set file extensions for loading images
		tex2Gif.SetFileExtension(new List<string>{".jpg", ".png"});
		//tex2Gif.SetFileExtension(new List<string>{".jpg"});

		string loadImagePath = Application.streamingAssetsPath;

		//Load images as texture2D list from target directory
		tex2DList = tex2Gif.LoadImages(loadImagePath);

		if(tex2DList != null && tex2DList.Count > 0)
		{
			//Save the provided texture2Ds to a GIF file with settings
			tex2Gif.Save(tex2DList, 512, 512, 1, 0, 30, OnFileSaved, OnFileSaveProgress, ProGifTexture2DsToGIF.ResolutionHandle.ResizeKeepRatio, false);
			text1.text = "Load images and start convert/save GIF..";
		}
		else
		{
			Debug.LogWarning("No image/texture found at: " + loadImagePath);
		}
	}

	private void OnFileSaveProgress(int id, float progress)
	{
		//Debug.Log("On file save progress: " + progress);
		text1.text = "Save progress: " + Mathf.CeilToInt(progress * 100) + "%";
	}

	private void OnFileSaved(int id, string path)
	{
		Debug.Log("On file saved: " + path);
		text1.text = "GIF saved: " + path;
		ShowGIF(path);

		displayImage.sprite = tex2Gif.GetSprite(0);
		displayImage.SetNativeSize();
	}

	void ShowGIF(string path)
	{
		//Open the Pro GIF player to show the converted GIF
		ProGifManager.Instance.PlayGif(path, displayImage, (loadProgress)=>{
			if(loadProgress < 1f)
			{
				displayImage.SetNativeSize();
			}
		});
	}

	//It is important to Clear textures every time (prevent memory leak)
	void Clear()
	{
		if(tex2Gif != null) tex2Gif.Clear();

		if(displayImage != null && displayImage.sprite != null && displayImage.sprite.texture != null)
		{
			Texture2D.Destroy(displayImage.sprite.texture);
			displayImage.sprite = null;
		}

		//Clear texture
		if(tex2DList != null)
		{
			foreach(Texture2D tex in tex2DList)
			{
				if(tex != null)
				{
					Texture2D.Destroy(tex);
				}
			}
			tex2DList = null;
		}
	}

}
