using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

//[RequireComponent(typeof(Image)), DisallowMultipleComponent]
[DisallowMultipleComponent]
public abstract class ProGifPlayerComponent : MonoBehaviour
{
	public string loadPath;

	[HideInInspector] public List<GifTexture> gifTextures = new List<GifTexture>();

	[HideInInspector] public int totalFrame = 0;

	[HideInInspector] public DisplayType displayType = DisplayType.None;	// Indicates the display target is an Image or a Renderer
	[HideInInspector] public Image destinationImage;						// The image for display sprites
	[HideInInspector] public Renderer destinationRenderer;					// The renderer for display textures

	[HideInInspector] public float nextFrameTime = 0.0f;					// The game time to show next frame
	[HideInInspector] public int spriteIndex = 0;							// The current sprite index to be played
	[HideInInspector] public float interval = 0.0f; 						// Waiting time among frames
	[HideInInspector] public bool shouldSaveFromWeb = false; 				// True: save file download from web

	public enum DisplayType
	{
		None = 0,
		Image,
		Renderer,
	}

	//Decode settings
	public enum DecodeMode
	{
		Normal = 0,			//Decode the entire gif normally
		Advanced			//Decode gif base on the input settings(targetDecodeFrameNum, etc)
	}
	public enum Decoder
	{
		UniGif = 0,			//Origin decoder
		ProGif,				//Support multiple gif decoding, faster than the origin decoder
	}

	//Advanced settings ------------------
	private Decoder decoder = Decoder.ProGif;   //Decoder.ProGif;
    private DecodeMode decodeMode = DecodeMode.Normal;
	private ProGifDecoder proGifDecoder;
	private int targetDecodeFrameNum = -1;	//if targetDecodeFrameNum <= 0: decode & play all frames (+/- 1 frame)
	//Advanced settings ------------------

	public void SetDecodeSettings(Decoder decoder, DecodeMode decodeMode, int targetDecodeFrameNum = -1)
	{
		this.decoder = decoder;
		this.decodeMode = decodeMode;
		this.targetDecodeFrameNum = targetDecodeFrameNum;
	}


	//-- Resize --------
	//	private int newFps = -1;
	//	private Vector2 newSize = Vector2.zero;
	//	private bool keepRatioForNewSize = true;
//	public void Resize_AdvancedMode(GifTexture gTex)
//	{
//		ImageResizer imageResizer = null;
//		bool reSize = false;
//		if(newSize.x > 0 && newSize.y > 0 && decodeMode == ProGifPlayerComponent.DecodeMode.Advanced) 
//		{
//			imageResizer = new ImageResizer();
//			reSize = true;
//		}
//
//		if(reSize) gTex.m_texture2d = (keepRatioForNewSize)?
//				imageResizer.ResizeTexture32_KeepRatio(gTex.m_texture2d, (int)newSize.x, (int)newSize.y):
//				imageResizer.ResizeTexture32(gTex.m_texture2d, (int)newSize.x, (int)newSize.y);
//	}
	//-- Resize ----------------

	// Textures filter mode
	[SerializeField]
	private FilterMode m_filterMode = FilterMode.Point;
	// Textures wrap mode
	[SerializeField]
	private TextureWrapMode m_wrapMode = TextureWrapMode.Clamp;

	/// <summary>
	/// Gets the progress when load Gif from path/url.
	/// </summary>
	/// <value>The loading progress.</value>
	public float LoadingProgress
	{
		get{
			return (float)gifTextures.Count/(float)totalFrame;
		}
	}

	/// <summary>
	/// This component state
	/// </summary>
	public enum State
	{
		None,
		Loading,
		Ready,
		Playing,
		Pause,
	}
	/// <summary>
	/// Now state
	/// </summary>
	public State nowState
	{
		get;
		private set;
	}
	public void SetState(State state)
	{
		nowState = state;
	}

	/// <summary>
	/// Animation loop count (0 is infinite)
	/// </summary>
	public int loopCount
	{
		get;
		private set;
	}

	/// <summary>
	/// Texture width (px)
	/// </summary>
	public int width
	{
		get;
		private set;
	}

	/// <summary>
	/// Texture height (px)
	/// </summary>
	public int height
	{
		get;
		private set;
	}

	void OnEnable()
	{
		if(string.IsNullOrEmpty(this.loadPath) == false)
		{
			Play(this.loadPath, false);
		}
	}

	public void Play(string loadPath, bool shouldSaveFromWeb)
	{
		this.shouldSaveFromWeb = shouldSaveFromWeb;
		Clear();
		gifTextures = new List<GifTexture>();
		LoadGifFromUrl(loadPath);
		this.loadPath = loadPath;
	}

	protected void PrePlay(int fps, Sprite[] sprites)
	{
		if(sprites == null)
		{
			Debug.LogWarning("Sprites is null!");
			return;
		}
		if(sprites.Length <= 0)
		{
			Debug.LogWarning("Sprites is empty!");
			return;
		}

		interval = 1.0f / fps;

		Clear();

		gifTextures = new List<GifTexture>();
		for(int i=0; i<sprites.Length; i++)
		{
			if(sprites[i] != null) gifTextures.Add(new GifTexture(sprites[i], interval));
		}

		width = sprites[0].texture.width;
		height = sprites[0].texture.height;
		totalFrame = gifTextures.Count;

		//Ensure the sprite is updated, call onLoading at next frame
		StartCoroutine(_DelayCallOnloading());

		nowState = State.Playing;
	}

	public abstract void Play(int fps, Sprite[] sprites);

	IEnumerator _DelayCallOnloading()
	{
		yield return new WaitForEndOfFrame();
		if(_OnLoading != null) _OnLoading(LoadingProgress);
	}

	public void Pause()
	{
		nowState = State.Pause;
	}

	public void Resume()
	{
		nowState = State.Playing;
	}

	public void Stop()
	{
		nowState = State.Pause;
		spriteIndex = 0;
	}

	/// <summary>
	/// Set GIF texture from url
	/// </summary>
	/// <param name="url">GIF image url (WEB or StreamingAssets path)</param>
	public void LoadGifFromUrl(string url)
	{
		StartCoroutine(_LoadGifFromUrl(url));
	}

	/// <summary>
	/// Set GIF texture from url
	/// </summary>
	/// <param name="url">GIF image url (WEB or StreamingAssets path)</param>
	/// <returns>IEnumerator</returns>
	private IEnumerator _LoadGifFromUrl(string url)
	{
		if(string.IsNullOrEmpty(url))
		{
			Debug.LogError("URL is nothing.");
			yield break;
		}

		if(nowState == State.Loading)
		{
			Debug.LogWarning("Already loading.");
			yield break;
		}
		nowState = State.Loading;

		bool isFromWeb = false;
		string path;
		if(url.StartsWith("http"))
		{
			// from WEB
			path = url;
			isFromWeb = true;
		}
		else
		{
			// from Local
			//path = Path.Combine("file:///" + Application.streamingAssetsPath, url);
			path = "file://" + url;
			//The local file path should be like this : file:///Users/..../DefaultCompany/ProGIF/GIF_2017-05-12-00-35-36.gif
			Debug.Log("Local file path: " + path);
		}

		// Load file
		using(WWW www = new WWW(path))
		{
			yield return www;

			if (string.IsNullOrEmpty(www.error) == false)
			{
				Debug.LogError("File load error.\n" + www.error);
				nowState = State.None;
				yield break;
			}

			nowState = State.Loading;
			this.interval = -1f;

			//Save bytes to gif file if it is downloaded from web
			if(isFromWeb && shouldSaveFromWeb)
			{
				ByteArrayToFile(new FilePathName().GetDownloadedGifSaveFullPath(), www.bytes);
			}

			if(decoder == Decoder.UniGif)
			{
				#if UNITY_EDITOR
				Debug.Log("Use UniGif Decoder");
				#endif
				UniGif.SetDecodeSettings(decodeMode, targetDecodeFrameNum);
				yield return StartCoroutine(UniGif.GetTextureListCoroutine(www.bytes, (gifTexList, loopCount, width, height) =>
					{
						if(gifTexList != null)
						{
							this.loopCount = loopCount;
							this.width = width;
							this.height = height;

							//clear un-use gifTextures
							_ClearGifTexture2Ds(gifTexList);

							_OnComplete();
						}
						else
						{
							Debug.LogError("Gif texture get error.");
							nowState = State.None;
						}
					},
					m_filterMode, m_wrapMode, false, (gTex)=>{

						_OnFrameReady(gTex);

					}, (frameCount)=>{
						totalFrame = frameCount;
					}));
			}
			else
			{
				#if UNITY_EDITOR
				Debug.Log("Use ProGIF Decoder");
				#endif
				proGifDecoder = new ProGifDecoder();
				proGifDecoder.SetDecodeSettings(decodeMode, targetDecodeFrameNum);
				yield return StartCoroutine(proGifDecoder.GetTextureListCoroutine(www.bytes, (gifTexList, loopCount, width, height) =>
					{
						if(gifTexList != null)
						{
							this.loopCount = loopCount;
							this.width = width;
							this.height = height;

							//clear un-use gifTextures
							_ClearGifTexture2Ds(gifTexList);

							_OnComplete();
						}
						else
						{
							Debug.LogError("Gif texture get error.");
							nowState = State.None;
						}
					},
					m_filterMode, m_wrapMode, false, (gTex)=>{

						_OnFrameReady(gTex);

					}, (frameCount)=>{
						totalFrame = frameCount;
					}));
			}
		}
	}

	private void _OnFrameReady(GifTexture gTex)
	{
		gifTextures.Add(gTex);
		if(interval < 0)
		{
			width = gTex.GetTexture2D().width;
			height = gTex.GetTexture2D().height;

			interval = gTex.m_delaySec; //(decodeMode == DecodeMode.Normal)? gTex.m_delaySec:gTex.m_delaySec*;

			if(destinationImage != null) 
			{
				destinationImage.sprite = gifTextures[0].GetSprite();
			}

			if(destinationRenderer != null && destinationRenderer.material != null) 
			{
				destinationRenderer.material.mainTexture = gifTextures[0].GetTexture2D();
			}

			_OnFirstFrameReady(gTex);

			nowState = State.Playing;
		}

		if(_OnLoading != null) _OnLoading(LoadingProgress);
	}

	private void _OnFirstFrameReady(GifTexture gifTex)
	{
		if(_OnFirstFrame != null)
		{
			_OnFirstFrame(new FirstGifFrame(){
				gifTexture = gifTex,
				width = this.width,
				height = this.height,
				interval = this.interval,
				totalFrame = this.totalFrame,
			});
		}
	}

	private void _OnComplete()
	{
		if(_OnDecodeComplete != null)
		{
			_OnDecodeComplete(new DecodedResult(){
				gifTextures = this.gifTextures,
				loopCount = this.loopCount,
				width = this.width,
				height = this.height,
				interval = this.interval,
				totalFrame = this.totalFrame,
			});
		}
	}

	private Action<FirstGifFrame> _OnFirstFrame = null;
	public void SetOnFirstFrameCallback(Action<FirstGifFrame> onFirstFrame)
	{
		_OnFirstFrame = onFirstFrame;
	}

	public class FirstGifFrame
	{
		public GifTexture gifTexture;
		public int width;
		public int height;
		public float interval;
		public int totalFrame;

		public int fps
		{
			get{
				return (int)(1f/interval);
			}
		}
	}

	private Action<float> _OnLoading = null;
	public void SetLoadingCallback(Action<float> onLoading)
	{
		_OnLoading = onLoading;
	}
		
	private Action<DecodedResult> _OnDecodeComplete = null;
	public void SetOnDecodeCompleteCallback(Action<DecodedResult> onDecodeComplete)
	{
		_OnDecodeComplete = onDecodeComplete;
	}

	public class DecodedResult
	{
		public List<GifTexture> gifTextures;
		public int width;
		public int height;
		public float interval;
		public int loopCount;
		public int totalFrame;

		public int fps
		{
			get{
				return (int)(1f/interval);
			}
		}

	}

	public bool ByteArrayToFile(string path, byte[] byteArray)
	{
		try
		{
			using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				fs.Write(byteArray, 0, byteArray.Length);
				return true;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Exception caught in process: {0}", ex);
			return false;
		}
	}

	/// <summary>
	/// Clear the texture2D in the list of GifTexture
	/// </summary>
	private void _ClearGifTexture2Ds(List<GifTexture> gifTexList)
	{
		if (gifTexList != null)
		{
			for (int i = 0; i < gifTexList.Count; i++)
			{
				if (gifTexList[i] != null && gifTexList[i].m_texture2d != null)
				{
					gifTexList[i].GetSprite();

					Destroy(gifTexList[i].m_texture2d);
					gifTexList[i].m_texture2d = null;
				}
			}
		}
	}

	/// <summary>
	/// Clear the sprite & texture2D in the list of GifTexture
	/// </summary>
	private void _ClearGifTextures(List<GifTexture> gifTexList)
	{
		if(gifTexList != null)
		{
			for(int i=0; i<gifTexList.Count; i++)
			{
				if(gifTexList[i] != null)
				{
					if(gifTexList[i].m_texture2d != null)
					{
						Texture2D.Destroy(gifTexList[i].m_texture2d);
						gifTexList[i].m_texture2d = null;
					}

					if(gifTexList[i].m_Sprite != null && gifTexList[i].m_Sprite.texture != null)
					{
						Texture2D.Destroy(gifTexList[i].m_Sprite.texture);
						gifTexList[i].m_Sprite = null;
					}
				}
			}
		}
	}

	/// <summary>
	/// Clear this instance.
	/// </summary>
	public void Clear()
	{
		nowState = State.None;

		StopAllCoroutines();

		//Clear textures in loading coroutine(s)
		_ClearGifTextures(UniGif.gifTexList);
		_ClearGifTextures(UniGif.TempGifTextures);
		if(proGifDecoder != null)
		{
			_ClearGifTextures(proGifDecoder.gifTexList);
			_ClearGifTextures(proGifDecoder.TempGifTextures);
		}

		//Clear sprite & texture in gifTextures of the PlayerComponent
		_ClearGifTextures(gifTextures);

		//Clear texture of the display image
		if(destinationImage != null && destinationImage.sprite != null && destinationImage.sprite.texture != null)
		{
			Texture2D.Destroy(destinationImage.sprite.texture);
		}

		if(destinationRenderer != null && destinationRenderer.material != null && destinationRenderer.material.mainTexture != null)
		{
			Texture2D.Destroy(destinationRenderer.material.mainTexture);
		}

		//Clear un-referenced textures
		Resources.UnloadUnusedAssets();
	}

}
