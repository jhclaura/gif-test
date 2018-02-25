using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class ProGifPlayer
{
	private ProGifPlayerComponent player = null;

	//Advanced settings ------------------
	private ProGifPlayerComponent.Decoder decoder = ProGifPlayerComponent.Decoder.ProGif;   // ProGif
    private ProGifPlayerComponent.DecodeMode decodeMode = ProGifPlayerComponent.DecodeMode.Normal;
	private int targetDecodeFrameNum = -1;	//if targetDecodeFrameNum <= 0: decode & play all frames
	//Advanced settings ------------------

	/// <summary>
	/// Gif width (only available after the first gif frame is loaded)
	/// </summary>
	public int width
	{
		get{
			return (player == null)? 0:player.width;
		}
	}

	/// <summary>
	/// Gif height (only available after the first gif frame is loaded)
	/// </summary>
	public int height
	{
		get{
			return (player == null)? 0:player.height;
		}
	}

	/// <summary>
	/// Decoded gif texture list (get all the gif textures at the decoding process finished)
	/// </summary>
	public List<GifTexture> gifTextures
	{
		get{
			return (player == null)? null:player.gifTextures;
		}
	}

	private Sprite[] _Setup(ProGifRecorder recorder)
	{
		RenderTexture[] gifFrames = recorder.Frames;
		Sprite[] sprites = new Sprite[gifFrames.Length];

		if(recorder.IsCustomRatio)
		{
			for(int i = 0; i < gifFrames.Length; i++)
			{
				Texture2D tex = new Texture2D(recorder.Width, recorder.Height);
				RenderTexture.active = gifFrames[i];
				tex.ReadPixels(new Rect((gifFrames[i].width - tex.width)/2, (gifFrames[i].height - tex.height)/2, tex.width, tex.height), 0, 0);
				tex.Apply();

				sprites[i] = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
			}
		}
		else
		{
			for (int i = 0; i < gifFrames.Length; i++)
			{
				Texture2D tex = new Texture2D(gifFrames[i].width, gifFrames[i].height);
				RenderTexture.active = gifFrames[i];
				tex.ReadPixels(new Rect (0.0f, 0.0f, gifFrames[i].width, gifFrames[i].height), 0, 0);
				tex.Apply();

				sprites[i] = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
			}
		}

		return sprites;
	}

	private void _SetupPlayerComponent(Image destination)
	{
		player = destination.gameObject.GetComponent<ProGifPlayerImage>();
		if(player == null)
		{
			player = destination.gameObject.AddComponent<ProGifPlayerImage>();
		}
		player.displayType = ProGifPlayerComponent.DisplayType.Image;
	}

	private void _SetupPlayerComponent(Renderer destination)
	{
		player = destination.gameObject.GetComponent<ProGifPlayerRenderer>();
		if(player == null)
		{
			player = destination.gameObject.AddComponent<ProGifPlayerRenderer>();
		}
		player.displayType = ProGifPlayerComponent.DisplayType.Renderer;
	}

	/// <summary>
	/// Play gif frames in the specified recorder, display with image.
	/// </summary>
	public void Play(ProGifRecorder recorder, Image destination)
	{
		_SetupPlayerComponent(destination);
		player.Play(recorder.FPS, _Setup(recorder));
	}

	/// <summary>
	/// Load & Decode to Play gif at the loadPath, display with image.
	/// </summary>
	public void Play(string loadPath, Image destination, bool shouldSaveFromWeb)
	{
		_SetupPlayerComponent(destination);
		player.SetDecodeSettings(decoder, decodeMode, targetDecodeFrameNum);
		player.Play(loadPath, shouldSaveFromWeb);
	}

	/// <summary>
	/// Play gif frames in the specified recorder, display with renderer.
	/// </summary>
	public void Play(ProGifRecorder recorder, Renderer destination)
	{
		_SetupPlayerComponent(destination);
		player.Play(recorder.FPS, _Setup(recorder));
	}

	/// <summary>
	/// Load & Decode to Play gif at the loadPath, display with renderer.
	/// </summary>
	public void Play(string loadPath, Renderer destination, bool shouldSaveFromWeb)
	{
		_SetupPlayerComponent(destination);
		player.SetDecodeSettings(decoder, decodeMode, targetDecodeFrameNum);
		player.Play(loadPath, shouldSaveFromWeb);
	}

	public void SetDecodeSettings(ProGifPlayerComponent.Decoder inDecoder, ProGifPlayerComponent.DecodeMode inDecodeMode, int inTargetDecodeFrameNum = -1)
	{
		decoder = inDecoder;
		decodeMode = inDecodeMode;
		targetDecodeFrameNum = inTargetDecodeFrameNum;
		if(player != null) player.SetDecodeSettings(decoder, decodeMode, targetDecodeFrameNum);
	}

	public void Pause()
	{
		player.Pause();
	}

	public void Resume()
	{
		player.Resume();
	}

	public void Stop()
	{
		player.Stop();
	}

	/// <summary>
	/// Set the callback for checking the decode progress.
	/// </summary>
	/// <param name="onLoading">On loading callback, returns the decode progress(float).</param>
	public void SetLoadingCallback(Action<float> onLoading)
	{
		if(player != null)
		{
			player.SetLoadingCallback(onLoading);
		}
		else
		{
			Debug.LogWarning("Gif player not exist, please set callback after the player is set!");
		}
	}

	/// <summary>
	/// Set the callback to be fired when the first gif frame ready.
	/// </summary>
	/// <param name="onFirstFrame">On first frame callback, returns the first gifTexture and related data.</param>
	public void SetOnFirstFrameCallback(Action<ProGifPlayerComponent.FirstGifFrame> onFirstFrame)
	{
		if(player != null)
		{
			player.SetOnFirstFrameCallback(onFirstFrame);
		}
		else
		{
			Debug.LogWarning("Gif player not exist, please set callback after the player is set!");
		}
	}

	/// <summary>
	/// Set the callback to be fired when all frames decode complete.
	/// </summary>
	/// <param name="onDecodeComplete">On decode complete callback, returns the gifTextures list and related data.</param>
	public void SetOnDecodeCompleteCallback(Action<ProGifPlayerComponent.DecodedResult> onDecodeComplete)
	{
		if(player != null)
		{
			player.SetOnDecodeCompleteCallback(onDecodeComplete);
		}
		else
		{
			Debug.LogWarning("Gif player not exist, please set callback after the player is set!");
		}
	}

	/// <summary>
	/// Change the destination image for displaying gif.
	/// </summary>
	public void ChangeDestination(Image destination)
	{
		if(player.GetComponent<ProGifPlayerImage>() != null)
		{
			player.GetComponent<ProGifPlayerImage>().ChangeDestination(destination);
		}
	}

	/// <summary>
	/// Change the destination renderer for displaying gif.
	/// </summary>
	public void ChangeDestination(Renderer destination)
	{
		if(player.GetComponent<ProGifPlayerRenderer>() != null)
		{
			player.GetComponent<ProGifPlayerRenderer>().ChangeDestination(destination);
		}
	}

	/// <summary>
	/// Add an extra destination image for displaying gif.
	/// </summary>
	public void AddExtraDestination(Image destination)
	{
		if(player.GetComponent<ProGifPlayerImage>() != null)
		{
			player.GetComponent<ProGifPlayerImage>().AddExtraDestination(destination);
		}
	}

	/// <summary>
	/// Add an extra destination renderer for displaying gif.
	/// </summary>
	public void AddExtraDestination(Renderer destination)
	{
		if(player.GetComponent<ProGifPlayerRenderer>() != null)
		{
			player.GetComponent<ProGifPlayerRenderer>().AddExtraDestination(destination);
		}
	}

	/// <summary>
	/// Remove a specific extra destination image from the extra list.
	/// </summary>
	public void RemoveFromExtraDestination(Image destination)
	{
		if(player.GetComponent<ProGifPlayerImage>() != null)
		{
			player.GetComponent<ProGifPlayerImage>().RemoveFromExtraDestination(destination);
		}
	}

	/// <summary>
	/// Remove a specific extra destination renderer from the extra list.
	/// </summary>
	public void RemoveFromExtraDestination(Renderer destination)
	{
		if(player.GetComponent<ProGifPlayerRenderer>() != null)
		{
			player.GetComponent<ProGifPlayerRenderer>().RemoveFromExtraDestination(destination);
		}
	}

	/// <summary>
	/// Clear this instance, clear all textures/sprites.
	/// </summary>
	public void Clear()
	{
		if(player != null)
		{
			player.Clear();
		}
	}
}
