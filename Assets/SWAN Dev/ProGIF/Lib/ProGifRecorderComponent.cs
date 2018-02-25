/*
 * Copyright (c) 2015 Thomas Hourdel
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 *    1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 
 *    2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 
 *    3. This notice may not be removed or altered from any source
 *    distribution.
 */

/// <summary>
/// Modified by SWAN DEV
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ThreadPriority = System.Threading.ThreadPriority;

[RequireComponent(typeof(Camera)), DisallowMultipleComponent]
public class ProGifRecorderComponent : MonoBehaviour
{
	//onDurationEnd: you can set something to do at the recordTime finish, 
	//the recorder is still recording, use ProGifRecorder->Stop() to stop
	public Action onDurationEnd = null;

	#region Exposed fields

	// These fields aren't public, the user shouldn't modify them directly as they can't break
	// everything if not used correctly. Use Setup() instead.
	[SerializeField]
	Vector2 m_GifRatio = new Vector2(0, 0);

	[SerializeField]
	bool m_AutoAspect = true;

	[SerializeField, Min(8)]
	int m_Width = 320;

	[SerializeField, Min(8)]
	int m_Height = 200;

	[SerializeField, Range(1, 30)]
	int m_FramePerSecond = 15;

	[SerializeField, Min(-1)]
	int m_Repeat = 0;		//Repeat times, 0: infinte

	[SerializeField, Range(1, 100)]
	int m_Quality = 15;		//1: best, 100: fastest

	[SerializeField, Min(0.1f)]
	float m_RecordTime = 3f;

	#endregion

	#region Public fields

	/// <summary>
	/// Current state of the recorder.
	/// </summary>
	public ProGifRecorder.RecorderState State{ get; private set; }

	/// <summary>
	/// The folder to save the gif to. No trailing slash.
	/// </summary>
	public string SaveFolder{ get; set; }

	/// <summary>
	/// Sets the worker threads priority. This will only affect newly created threads (on save).
	/// </summary>
	public ThreadPriority WorkerPriority = ThreadPriority.BelowNormal;

	/// <summary>
	/// Returns the estimated VRam used (in MB) for recording. 
	/// This is not the storage file size. The storage file size can be retrieved with FileInfo after saved
	/// </summary>
	public float EstimatedMemoryUse
	{
		get
		{
			float mem = m_FramePerSecond * m_RecordTime;
			mem *= m_Width * m_Height * 4;
			mem /= 1024 * 1024;
			return mem;
		}
	}

	public int Width
	{
		get
		{
			return m_Width;
		}
	}

	public int Height
	{
		get
		{
			return m_Height;
		}
	}

	public bool IsCustomRatio
	{
		get
		{
			return (m_GifRatio.x > 0 && m_GifRatio.y > 0);
		}
	}

	#endregion

	#region Delegates

	/// <summary>
	/// Called when the pre-processing step has finished.
	/// </summary>
	public event Action OnPreProcessingDone = delegate{};

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

	#endregion

	#region Internal fields

	int m_MaxFrameCount;
	public float RecordProgress
	{
		get{
			return (float)m_Frames.Count/(float)m_MaxFrameCount;
		}
	}
	private Action<float> _OnRecordAction = null;
	public void SetOnRecordAction(Action<float> onRecordAction)
	{
		_OnRecordAction = onRecordAction;
	}

	float m_Time;
	float m_TimePerFrame;
	Queue<RenderTexture> m_Frames;
	RenderTexture m_RecycledRenderTexture;
	ProGifReflectionUtils<ProGifRecorderComponent> m_ReflectionUtils;

	int id = 0;
	float progress = 0.0f;
	string filePath = string.Empty;
	bool invokeFileProgress = false;
	bool invokeFileSaved = false;

	#endregion

	#region Public API

	public int FPS
	{
		get{
			return m_FramePerSecond;
		}
	}

	public Queue<RenderTexture> Frames
	{
		get{
			return m_Frames;
		}
	}

	/// <summary>
	/// Initializes the component. Use this if you need to change the recorder settings in a script.
	/// This will flush the previously saved frames as settings can't be changed while recording.
	/// </summary>
	/// <param name="autoAspect">Automatically compute height from the current aspect ratio</param>
	/// <param name="width">Width in pixels</param>
	/// <param name="height">Height in pixels</param>
	/// <param name="fps">Frames per second</param>
	/// <param name="recorderTime">Maximum amount of seconds to record to memory</param>
	/// <param name="repeat">-1: no repeat, 0: infinite, >0: repeat count</param>
	/// <param name="quality">Quality of color quantization (conversion of images to the maximum
	/// 256 colors allowed by the GIF specification). Lower values (minimum = 1) produce better
	/// colors, but slow processing significantly. Higher values will speed up the quantization
	/// pass at the cost of lower image quality (maximum = 100).</param>
	public void Setup(bool autoAspect, int width, int height, int fps, float recorderTime, int repeat, int quality)
	{
		_Setup(autoAspect, width, height, fps, recorderTime, repeat, quality, new Vector2(0,0));
	}

	/// <summary>
	/// Initializes the component. Use this if you need to change the recorder settings in a script.
	/// This will flush the previously saved frames as settings can't be changed while recording.
	/// (Use this Setup if you need to crop the image to a specify aspect ratio. 
	/// The pixels out of the provided aspect ratio will be cut.)
	/// </summary>
	/// <param name="gifAspectRatio">Image ratio, 1:1, 16:9, 4:3, 3:2, etc. Use autoAspect if x or y of gifAspectRatio not greater than 0.</param>
	/// <param name="width">Width.</param>
	/// <param name="height">Height.</param>
	/// <param name="fps">Frames per second</param>
	/// <param name="recorderTime">Maximum amount of seconds to record to memory</param>
	/// <param name="repeat">-1: no repeat, 0: infinite, >0: repeat count</param>
	/// <param name="quality">Quality of color quantization (conversion of images to the maximum
	/// 256 colors allowed by the GIF specification). Lower values (minimum = 1) produce better
	/// colors, but slow processing significantly. Higher values will speed up the quantization
	/// pass at the cost of lower image quality (maximum = 100).</param>
	public void Setup(Vector2 gifAspectRatio, int width, int height, int fps, float recorderTime, int repeat, int quality)
	{
		_Setup(false, width, height, fps, recorderTime, repeat, quality, gifAspectRatio);
	}

	private void _Setup(bool autoAspect, int width, int height, int fps, float recorderTime, int repeat, int quality, Vector2 gifAspectRatio)
	{
		if(State == ProGifRecorder.RecorderState.PreProcessing)
		{
			Debug.LogWarning("Attempting to setup the component during the pre-processing step.");
			return;
		}

		// Start fresh
		FlushMemory();

		// Set values and validate them
		SetGifAspectRatio(gifAspectRatio);
		m_AutoAspect = IsCustomRatio? true:autoAspect;
		m_ReflectionUtils.ConstrainMin(x => x.m_Width, width);

		if(m_AutoAspect)
		{
			m_ReflectionUtils.ConstrainMin(x => x.m_Height, Mathf.RoundToInt(m_Width/Camera.main.aspect));
		}
		else
		{
			m_ReflectionUtils.ConstrainMin(x => x.m_Height, height);
		}

		m_ReflectionUtils.ConstrainRange(x => x.m_FramePerSecond, fps);
		m_ReflectionUtils.ConstrainMin(x => x.m_RecordTime, recorderTime);
		m_ReflectionUtils.ConstrainMin(x => x.m_Repeat, repeat);
		m_ReflectionUtils.ConstrainRange(x => x.m_Quality, quality);

		Init();
	}

	/// <summary>
	/// Sets the image ratio (before save gif).
	/// </summary>
	/// <param name="gifAspectRatio">Aspect ratio for gif. Can not be changed during PreProcessing state(cropping the stored textures)</param>
	public void SetGifAspectRatio(Vector2 gifAspectRatio)
	{
		if(State == ProGifRecorder.RecorderState.PreProcessing) return;
		m_GifRatio = gifAspectRatio;
	}

	/// <summary>
	/// Pauses recording.
	/// </summary>
	public void Pause()
	{
		if(State == ProGifRecorder.RecorderState.PreProcessing)
		{
			Debug.LogWarning("Attempting to pause recording during the pre-processing step. The recorder is automatically paused when pre-processing.");
			return;
		}
		else if(State == ProGifRecorder.RecorderState.Stopped)
		{
			Debug.LogWarning("Attempting to pause recording after it has been stopped.");
			return;
		}

		State = ProGifRecorder.RecorderState.Paused;
	}

	/// <summary>
	/// Resumes recording. You can't resume while it's pre-processing data to be saved.
	/// </summary>
	public void Resume()
	{
		if(State == ProGifRecorder.RecorderState.PreProcessing)
		{
			Debug.LogWarning("Attempting to resume recording during the pre-processing step.");
			return;
		}
		else if(State == ProGifRecorder.RecorderState.Stopped)
		{
			Debug.LogWarning("Attempting to resume recording after it has been stopped.");
			return;
		}

		State = ProGifRecorder.RecorderState.Recording;
	}

	/// <summary>
	/// Starts or resumes recording. You can't start or resume while it's pre-processing data to be saved.
	/// </summary>
	public void Record(Action onDurationEnd = null)
	{
		this.onDurationEnd = onDurationEnd;

		if(State == ProGifRecorder.RecorderState.PreProcessing)
		{
			Debug.LogWarning("Attempting to start recording during the pre-processing step.");
			return;
		} 
		else if(State == ProGifRecorder.RecorderState.Stopped)
		{
			Debug.LogWarning("Attempting to start recording after it has been stopped.");
			return;
		}

		State = ProGifRecorder.RecorderState.Recording;
	}

	/// <summary>
	/// Stops the recording. You can't resume the record after it has been stopped.
	/// </summary>
	public void Stop()
	{
		State = ProGifRecorder.RecorderState.Stopped;
	}

	/// <summary>
	/// Clears all saved frames from memory and starts fresh.
	/// </summary>
	public void FlushMemory()
	{
		if(State == ProGifRecorder.RecorderState.PreProcessing)
		{
			Debug.LogWarning("Attempting to flush memory during the pre-processing step.");
			return;
		}

		Init();

		if(m_RecycledRenderTexture != null) Flush(m_RecycledRenderTexture);

		if(m_Frames == null) return;

		foreach(RenderTexture rt in m_Frames)
		{
			Flush(rt);
		}

		m_Frames.Clear();
	}

	/// <summary>
	/// Saves the stored frames to a gif file. The filename will automatically be generated.
	/// Recording will be paused and won't resume automatically. You can use the 
	/// <code>OnPreProcessingDone</code> callback to be notified when the pre-processing
	/// step has finished.
	/// </summary>
	public void Save()
	{
		Save(GenerateFileName());
	}

	/// <summary>
	/// Saves the stored frames to a gif file. If the filename is null or empty, an unique one
	/// will be generated. You don't need to add the .gif extension to the name. Recording will
	/// be paused and won't resume automatically. You can use the <code>OnPreProcessingDone</code>
	/// callback to be notified when the pre-processing step has finished.
	/// </summary>
	/// <param name="filename">File name without extension</param>
	public void Save(string filename)
	{
		if(State == ProGifRecorder.RecorderState.PreProcessing)
		{
			Debug.LogWarning("Attempting to save during the pre-processing step.");
			return;
		}

		if(m_Frames.Count == 0)
		{
			Debug.LogWarning("Nothing to save. Maybe you forgot to start the recorder ?");
			return;
		}

		State = ProGifRecorder.RecorderState.PreProcessing;

		if(string.IsNullOrEmpty(filename)) filename = GenerateFileName();

		StartCoroutine(PreProcess(filename));
	}

	#endregion

	#region Unity events

	void Awake()
	{
		m_ReflectionUtils = new ProGifReflectionUtils<ProGifRecorderComponent>(this);
		m_Frames = new Queue<RenderTexture>();
		Init();
	}

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

	void OnDestroy()
	{
		FlushMemory();
	}

	public void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if(State != ProGifRecorder.RecorderState.Recording)
		{
			Graphics.Blit(source, destination);
			return;
		}

		m_Time += Time.unscaledDeltaTime;

		if(m_Time >= m_TimePerFrame)
		{
			if(_OnRecordAction != null) _OnRecordAction(RecordProgress);

			// Limit the amount of frames stored in memory
			if(m_Frames.Count >= m_MaxFrameCount)
			{
				m_RecycledRenderTexture = m_Frames.Dequeue();
				if(onDurationEnd != null)
				{
					onDurationEnd();
					onDurationEnd = null;
				}
			}

			m_Time -= m_TimePerFrame;

			// Frame data
			RenderTexture rt = m_RecycledRenderTexture;
			m_RecycledRenderTexture = null;

			if(rt == null)
			{
				rt = new RenderTexture(m_Width, m_Height, 0, RenderTextureFormat.ARGB32);
				rt.wrapMode = TextureWrapMode.Clamp;
				rt.filterMode = FilterMode.Bilinear;
				rt.anisoLevel = 0;
			}

			rt.DiscardContents();

			Graphics.Blit(source, rt);
			m_Frames.Enqueue(rt);
		}

		Graphics.Blit(source, destination);
	}

	#endregion

	#region Methods

	// Used to reset internal values, called on Start(), Setup() and FlushMemory()
	void Init()
	{
		State = ProGifRecorder.RecorderState.Paused;
		ComputeHeight();
		m_MaxFrameCount = Mathf.RoundToInt(m_RecordTime * m_FramePerSecond);
		m_TimePerFrame = 1f / m_FramePerSecond;
		m_Time = 0f;

		// Make sure the output folder is set or use the default one
		if(string.IsNullOrEmpty(SaveFolder))
		{
			SaveFolder = new FilePathName().GetSaveDirectory();
		}
	}

	// Automatically computes height from the current aspect ratio if auto aspect is set to true
	public void ComputeHeight()
	{
		if(!m_AutoAspect || IsCustomRatio) return;

		if(Camera.main != null)
		{
			m_Height = Mathf.RoundToInt(m_Width / Camera.main.aspect);
		}
	}

	// Flushes a single Texture object
	void Flush(Texture texture)
	{
		if(RenderTexture.active == texture) return;

		#if UNITY_EDITOR
		Texture2D.DestroyImmediate(texture);
		#else
		Texture2D.Destroy(texture);
		#endif
	}

	// Gets a filename
	string GenerateFileName()
	{
		return new FilePathName().GetGifFileName();
	}

	// Pre-processing coroutine to extract frame data and send everything to a separate worker thread
	IEnumerator PreProcess(string filename)
	{
		string filepath = SaveFolder + "/" + filename + ".gif";
		List<Frame> frames = new List<Frame>(m_Frames.Count);

		// Recaculate image Width & Height if gifAspectRatio is used
		if(IsCustomRatio)
		{
			float frameRatio = (float)m_Width / (float)m_Height;
			float gifRatio = m_GifRatio.x / m_GifRatio.y;
//			Debug.Log("frameRatio: " + frameRatio + "m_Width / m_Height: " + m_Width + "x" + m_Height + "\n" +
//				"gifRatio: " + gifRatio + "m_GifRatio.x / m_GifRatio.y: " + m_GifRatio.x + "x" + m_GifRatio.y);

			if(frameRatio > gifRatio)
			{
				if(gifRatio == 1)
				{
					//for 1:1 gif
					if(m_Width > m_Height)
					{
						m_Width = m_Height;
					}
					else if(m_Height > m_Width)
					{
						m_Height = m_Width;
					}
				}
				else
				{
					//m_Height remain unchange, cal m_Width with gif ratio
					m_Width = (int)((float)m_Height * gifRatio);
				}
			}
			else if(frameRatio < gifRatio)
			{
				if(gifRatio == 1){ 
					//for 1:1 gif
					if(m_Width > m_Height)
					{
						m_Width = m_Height;
					}
					else if(m_Height > m_Width)
					{ 
						m_Height = m_Width;
					}
				}
				else
				{
					//m_Width remain unchange, cal m_Height with gif ratio
					m_Height = (int)((float)m_Width / gifRatio);
				}
			}
			else
			{
				//Do nothing
			}
		}

		// Get a temporary texture to read RenderTexture data
		Texture2D temp = new Texture2D(m_Width, m_Height, TextureFormat.RGB24, false);
		temp.hideFlags = HideFlags.HideAndDontSave;
		temp.wrapMode = TextureWrapMode.Clamp;
		temp.filterMode = FilterMode.Bilinear;
		temp.anisoLevel = 0;

		// Process the frame queue
		RenderTexture[] textures = m_Frames.ToArray();
		foreach(RenderTexture rt in textures)
		{
			Frame frame = ToGifFrame(rt, temp);
			frames.Add(frame);
			yield return null;
		}

		// Dispose the temporary texture
		Flush(temp);

		// Switch the state to pause, let the user choose to keep recording or not
		State = ProGifRecorder.RecorderState.Paused;

		// Callback
		if(OnPreProcessingDone != null) OnPreProcessingDone();

		// Setup a worker thread and let it do its magic
		ProGifEncoder encoder = new ProGifEncoder(m_Repeat, m_Quality);
		encoder.SetDelay(Mathf.RoundToInt(m_TimePerFrame * 1000f));
		ProGifWorker worker = new ProGifWorker(WorkerPriority)
		{
			m_Encoder = encoder,
			m_Frames = frames,
			m_FilePath = filepath,
			m_OnFileSaved = FileSaved,
			m_OnFileSaveProgress = FileSaveProgress
		};
		worker.Start();
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

	// Converts a RenderTexture to a GifFrame
	// Should be fast enough for low-res textures but will tank the framerate at higher res
	Frame ToGifFrame(RenderTexture source, Texture2D target)
	{
		RenderTexture.active = source;
		if(IsCustomRatio) target.ReadPixels(new Rect((source.width - target.width)/2, (source.height - target.height)/2, target.width, target.height), 0, 0);
		else target.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
		target.Apply();
		RenderTexture.active = null;

		return new Frame(){ Width = target.width, Height = target.height, Data = target.GetPixels32() };
	}

	/// <summary>
	/// Removes the instance of this script from camera
	/// </summary>
	public void RemoveScript()
	{
		OnDestroy();
		Destroy(this);
	}

	#endregion

}
