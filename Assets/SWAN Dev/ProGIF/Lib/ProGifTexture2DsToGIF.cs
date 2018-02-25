using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using ThreadPriority = System.Threading.ThreadPriority;

public class ProGifTexture2DsToGIF : MonoBehaviour
{
	public List<Texture2D> previewTextures = new List<Texture2D>();
	public ThreadPriority workerPriority = ThreadPriority.BelowNormal;

	private List<string> _fileExtensions = new List<string>{".jpg", ".png"};
	private ImageResizer _imageResizer = new ImageResizer();

	public ResolutionHandle resolutionHandle = ResolutionHandle.ResizeKeepRatio;
	public enum ResolutionHandle
	{
		Resize = 0,
		ResizeKeepRatio,
	}

	private string FileName
	{
		get{
			return new FilePathName().GetGifFileName();
		}
	}

	private string SaveFolder
	{
		get{
			return new FilePathName().GetSaveDirectory();
		}
	}

	private static ProGifTexture2DsToGIF _instance;
	public static ProGifTexture2DsToGIF Instance
	{
		get{
			if(_instance == null)
			{
				_instance = new GameObject("[Texture2DsToGIF]").AddComponent<ProGifTexture2DsToGIF>();
			}
			return _instance;
		}
	}

	private void Awake()
	{
		if(_instance == null)
		{
			_instance = this;
		}
	}

	int id = 0;
	float progress = 0.0f;
	string filePath = string.Empty;
	bool invokeFileProgress = false;
	bool invokeFileSaved = false;

	/// <summary>
	/// Called by each worker thread every time a frame is processed during the save process.
	/// The first parameter holds the worker ID and the second one a value in range [0;1] for
	/// the actual progress. This callback is probably not thread-safe, use at your own risks.
	/// </summary>
	public event Action<int, float> OnFileSaveProgress = delegate{};

	/// <summary>
	/// Called once a gif file has been saved. The first parameter will hold the worker ID and
	/// the second one the absolute file path.
	/// </summary>
	public event Action<int, string> OnFileSaved = delegate{};

	void Update()
	{
		if(invokeFileProgress)
		{
			invokeFileProgress = false;
			OnFileSaveProgress(id, progress);
		}

		if(invokeFileSaved)
		{
			invokeFileSaved = false;
			OnFileSaved(id, filePath);
		}
	}

	void FileSaved(int id, string path)
	{
		this.id = id;
		this.filePath = path;
		this.invokeFileSaved = true;
	}

	void FileSaveProgress(int id, float progress)
	{
		this.id = id;
		this.progress = progress;
		this.invokeFileProgress = true;
	}

	/// <summary>
	/// Convert and save a List of Texture2D to GIF.
	/// </summary>
	/// <param name="textureList">Texture list.</param>
	/// <param name="fps">Frame count per second.</param>
	/// <param name="loop">Repeat time, -1: no repeat, 0: infinite, >0: repeat count.</param>
	/// <param name="quality">Quality, (1 - 100) 1: best(larger storage size), 100: faster(smaller storage size)</param>
	/// <param name="workerPriority">Worker priority.</param>
	/// <param name="onFileSaved">On file saved callback.</param>
	/// <param name="onFileSaveProgress">On file save progress callback.</param>
	/// <param name="autoClear">If set to <c>true</c> Clear when gif saved?</param>
	public string Save(List<Texture2D> textureList, int width, int height, int fps, int loop, int quality,
		Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null, 
		ResolutionHandle resolutionHandle = ResolutionHandle.ResizeKeepRatio, bool autoClear = true)
	{
		this.resolutionHandle = resolutionHandle;
		return _Save(textureList, width, height, fps, loop, quality, onFileSaved, onFileSaveProgress, autoClear);
	}

	private string _Save(List<Texture2D> textureList, int width, int height, int fps, int loop, int quality,
		Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null, bool autoClear = true)
	{
		this.OnFileSaveProgress = (onFileSaveProgress != null)? onFileSaveProgress:(id, progress)=>{};
		this.OnFileSaved = (onFileSaved != null)? onFileSaved:(id, path)=>{};

		if(autoClear)
		{
			Action<int, string> clearCallback =(id, path)=>{
				Clear();
			};
			this.OnFileSaved += clearCallback;
		}

		string filepath = SaveFolder + "/" + FileName + ".gif";
		float timePerFrame = 1f / fps;
		List<Frame> frames = Texture2DsToFrames(textureList, width, height);

		ProGifEncoder encoder = new ProGifEncoder(loop, quality);
		encoder.SetDelay(Mathf.RoundToInt(timePerFrame * 1000f));
		ProGifWorker worker = new ProGifWorker(workerPriority)
		{
			m_Encoder = encoder,
			m_Frames = frames,
			m_FilePath = filepath,
			m_OnFileSaved = FileSaved,
			m_OnFileSaveProgress = FileSaveProgress
		};
		worker.Start();

		return filepath;
	}

	private List<Frame> Texture2DsToFrames(List<Texture2D> textureList, int width, int height)
	{
		previewTextures = new List<Texture2D>();
		List<Frame> frames = new List<Frame>();
		for(int i=0; i<textureList.Count; i++)
		{
			frames.Add(Texture2DToFrame(textureList[i], width, height));
		}
		return frames;
	}

	private Frame Texture2DToFrame(Texture2D texture2d, int width, int height)
	{
		if(texture2d.width != width || texture2d.height != height)
		{
			switch(resolutionHandle)
			{
			case ResolutionHandle.Resize:
				texture2d = _imageResizer.ResizeTexture32(texture2d, width, height);
				break;
			case ResolutionHandle.ResizeKeepRatio:
				texture2d = _imageResizer.ResizeTexture32_KeepRatio(texture2d, width, height);
				break;
			}

		}
		previewTextures.Add(texture2d);
		return new Frame(){Width = width, Height = height, Data = texture2d.GetPixels32()};
	}

	/// <summary>
	/// Set the file extensions.
	/// </summary>
	/// <param name="fileExtensions">File extension names in lower case</param>
	public void SetFileExtension(List<string> fileExtensions)
	{
		_fileExtensions = fileExtensions;
	}

	/// <summary>
	/// Load images in target directory, to a texture2D list.
	/// </summary>
	/// <returns>The images.</returns>
	/// <param name="directory">Directory.</param>
	public List<Texture2D> LoadImages(string directory)
	{
		List<Texture2D> textureList = new List<Texture2D>();

		string[] allFiles_src = Directory.GetFiles(directory);
		foreach(string f in allFiles_src)
		{
			if(_fileExtensions.Contains(Path.GetExtension(f).ToLower()))
			{
				byte[] bytes = File.ReadAllBytes(f);

				Texture2D tex2D = new Texture2D(4, 4);
				tex2D.LoadImage(bytes);

				textureList.Add(tex2D);
			}
		}
		return textureList;
	}

	public List<Texture2D> LoadImageFromResourcesFloder(string resourcesFolderPath = "Photo/")
	{
		//Load image as texture 2D from resources folder, do not support File Extension
		List<Texture2D> tex2DList = new List<Texture2D>();
		Texture2D[] tex2Ds = Resources.LoadAll<Texture2D>(resourcesFolderPath);
		if(tex2Ds != null && tex2Ds.Length > 0)
		{
			for(int i=0; i<tex2Ds.Length; i++)
			{
				tex2DList.Add(tex2Ds[i]);
			}
		}
		return tex2DList;
	}

	public Sprite GetSprite(int index)
	{
		index = Mathf.Clamp(index, 0, previewTextures.Count - 1);
		return ToSprite(previewTextures[index]);
	}

	public Sprite ToSprite(Texture2D texture)
	{
		Vector2 pivot = new Vector2(0.5f, 0.5f);
		float pixelPerUnit = 100;
		return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, pixelPerUnit);
	}

	/// <summary>
	/// It is important to Clear textures every time (prevent memory leak)
	/// </summary>
	public void Clear()
	{
		//Clear texture
		if(previewTextures != null)
		{
			foreach(Texture2D tex in previewTextures)
			{
				if(tex != null)
				{
					Texture2D.Destroy(tex);
				}
			}
			previewTextures = null;
		}
	}
}
