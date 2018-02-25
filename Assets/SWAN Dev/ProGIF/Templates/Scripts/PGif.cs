using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PGif : MonoBehaviour
{
	#region ----- GIF Recorder Settings -----
	public Vector2 m_AspectRatio = new Vector2(0, 0);
	public bool m_AutoAspect = true;
	public int m_Width = 360;
	public int m_Height = 360;
	public float m_Duration = 3f;
	[Range(1, 60)] public int m_Fps = 15;
	public int m_Loop = 0;								//-1: no repeat, 0: infinite, >0: repeat count
	[Range(1, 100)] public int m_Quality = 20;			//(1 - 100), 1: best(larger storage size), 100: faster(smaller storage size)

	public bool LoadFile{
		get{
			return (m_AspectRatio.x > 0 && m_AspectRatio.y > 0)? true:false;
		}
	}
	#endregion

	#region ----- GIF Player Settings -----
	//Advanced settings ------------------
	public ProGifPlayerComponent.DecodeMode m_DecodeMode = ProGifPlayerComponent.DecodeMode.Normal;
	[Range(-1, 9999)] public int m_TargetDecodeFrameNum = -1;		//if targetDecodeFrameNum <= 0: decode & play all frames
	//Advanced settings ------------------
	#endregion

	public Dictionary<string, ProGifRecorder> m_GifRecorderDict = new Dictionary<string, ProGifRecorder>();
	public Dictionary<string, ProGifPlayer> m_GifPlayerDict = new Dictionary<string, ProGifPlayer>();

	private static PGif _instance = null;
	/// <summary>
	/// Gets the instance of PGif. Create new one if no existing instance.
	/// Use this instance to control gif Record, Playback and Settings.
	/// </summary>
	/// <value>The instance.</value>
	public static PGif Instance
	{
		get{
			if(_instance == null)
			{
				_instance = new GameObject("[PGif]").AddComponent<PGif>();
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

	#region ----- Recorders -----
	/// <summary>
	/// (Settings-1) Sets the recording settings before StartRecord
	/// </summary>
	/// <param name="autoAspect">If set to true, auto aspect. Else force scale gif size to width*height.</param>
	/// <param name="width">Width.</param>
	/// <param name="height">Height. If autoAspect, height will be recalculated.</param>
	/// <param name="duration">Total time to record.</param>
	/// <param name="fps">Frames per second.</param>
	/// <param name="loop">Loop. -1: no repeat, 0: infinite, >0: repeat count</param>
	/// <param name="quality">Quality. (1 - 100), 1: best, 100: faster</param>
	public void SetRecordSettings(bool autoAspect, int width, int height, float duration, int fps, int loop, int quality)
	{
		m_AutoAspect = autoAspect;
		m_Width = width;
		m_Height = height;
		m_Fps = fps;
		m_Duration = duration;
		m_Loop = loop;
		m_Quality = quality;

		m_AspectRatio = new Vector2(0, 0); //Use auto aspect
	}

	/// <summary>
	/// (Settings-2) Sets the recording settings before StartRecord
	/// </summary>
	/// <param name="aspectRatio">A Specify aspect ratio for cropping gif. Set (0,0) if dont use, or use Settings-1 instead.</param>
	/// <param name="width">Width.</param>
	/// <param name="height">Height. If autoAspect, height will be recalculated.</param>
	/// <param name="duration">Total time to record.</param>
	/// <param name="fps">Frames per second.</param>
	/// <param name="loop">Loop. -1: no repeat, 0: infinite, >0: repeat count</param>
	/// <param name="quality">Quality. (1 - 100), 1: best, 100: faster</param>
	public void SetRecordSettings(Vector2 aspectRatio, int width, int height, float duration, int fps, int loop, int quality)
	{
		m_AspectRatio = aspectRatio;
		m_Width = width;
		m_Height = height;
		m_Fps = fps;
		m_Duration = duration;
		m_Loop = loop;
		m_Quality = quality;
	}

	/// <summary>
	/// Create/Start a new recorder to store frames with specific camera. 
	/// </summary>
	/// <param name="camera">The target Camera to attach the newly create gif recroder.</param>
	/// <param name="recorderName">Recorder Name for identifying recorders in the dictionary.</param>
	/// <param name="onRecordProgress">Update the record progress. Return values: record progress(float)</param>
	/// <param name="onRecordDurationMax">To be fired when target duration frames reached.</param>
	/// <param name="onPreProcessingDone">On pre processing done.</param>
	/// <param name="onFileSaveProgress">On file save progress. Retrun values: worker id(int), save progress(float).</param>
	/// <param name="onFileSaved">On file saved. Return values: id(int), saved path(string).</param>
	/// <param name="autoClear">If set to <c>true</c> Clear the recorder when gif saved?</param>
	public void StartRecord(Camera camera, string recorderName,
		Action<float> onRecordProgress = null, Action onRecordDurationMax = null, 
		Action onPreProcessingDone = null, Action<int, float> onFileSaveProgress = null, Action<int, string> onFileSaved = null, bool autoClear = true)
	{
		if(camera.GetComponent<ProGifRecorderComponent>() != null)
		{
			Debug.LogWarning("The target camera already has a recorder attached!");
			return;
		}

		ProGifRecorder newGifRecorder = new ProGifRecorder(camera);

		//Add the new recorder to dictionary
		if(m_GifRecorderDict.ContainsKey(recorderName))
		{
			m_GifRecorderDict[recorderName] = newGifRecorder;
		}
		else
		{
			m_GifRecorderDict.Add(recorderName, newGifRecorder);
		}

		if(m_AspectRatio.x > 0 && m_AspectRatio.y > 0)
		{
			newGifRecorder.Setup(
				m_AspectRatio, 	//a specify aspect ratio for cropping gif
				m_Width,  		//width
				m_Height,  		//height
				m_Fps,   		//fps
				m_Duration, 	//recorder time
				m_Loop,    		//repeat, -1: no repeat, 0: infinite, >0: repeat count
				m_Quality);  	//quality (1 - 100), 1: best, 100: faster
		}
		else
		{
			newGifRecorder.Setup(
				m_AutoAspect, 	//autoAspect
				m_Width,  		//width
				m_Height,  		//height
				m_Fps,   		//fps
				m_Duration, 	//recorder time
				m_Loop,    		//repeat, -1: no repeat, 0: infinite, >0: repeat count
				m_Quality);  	//quality (1 - 100), 1: best, 100: faster
		}

		//Start the recording with a callback that will be called when max. frames are stored in recorder
		newGifRecorder.Record(onRecordDurationMax);

		//Set the callback to update the record progress during recording.
		newGifRecorder.SetOnRecordAction(onRecordProgress);

		//Set the callback to be called when pre-processing complete
		newGifRecorder.OnPreProcessingDone += onPreProcessingDone;

		//Set the callback to update the gif save progress
		newGifRecorder.OnFileSaveProgress += onFileSaveProgress;

		//Set the callback to be called when gif file saved
		newGifRecorder.OnFileSaved += onFileSaved;

		//Set the callback to clear the recorder after gif saved
		if(autoClear)
		{
			Action<int, string> clearRecorder =(id, path)=>{
				newGifRecorder.FlushMemory();
				newGifRecorder.Clear();
				newGifRecorder = null;
			};
			newGifRecorder.OnFileSaved += clearRecorder;
		}
	}

	public ProGifRecorder GetRecorder(string recorderName)
	{
		ProGifRecorder recorder = null;
		if(!m_GifRecorderDict.TryGetValue(recorderName, out recorder))
		{
			Debug.LogWarning("GetRecorder - Recorder not found: " + recorderName);
		}
		return recorder;
	}

	public void PauseRecord(string recorderName)
	{
		ProGifRecorder recorder = null;
		if(m_GifRecorderDict.TryGetValue(recorderName, out recorder))
		{
			recorder.Pause();
		}
		else
		{
			Debug.LogWarning("PauseRecord - Recorder not found: " + recorderName);
		}
	}

	public void ResumeRecord(string recorderName)
	{
		ProGifRecorder recorder = null;
		if(m_GifRecorderDict.TryGetValue(recorderName, out recorder))
		{
			recorder.Resume();
		}
		else
		{
			Debug.LogWarning("ResumeRecord - Recorder not found: " + recorderName);
		}
	}

	public void StopRecord(string recorderName)
	{
		ProGifRecorder recorder = null;
		if(m_GifRecorderDict.TryGetValue(recorderName, out recorder))
		{
			recorder.Stop();
		}
		else
		{
			Debug.LogWarning("StopRecord - Recorder not found: " + recorderName);
		}
	}

	public void SaveRecord(string recorderName)
	{
		ProGifRecorder recorder = null;
		if(m_GifRecorderDict.TryGetValue(recorderName, out recorder))
		{
			recorder.Save();
		}
		else
		{
			Debug.LogWarning("SaveRecord - Recorder not found: " + recorderName);
		}
	}

	public void StopAndSaveRecord(string recorderName)
	{
		ProGifRecorder recorder = null;
		if(m_GifRecorderDict.TryGetValue(recorderName, out recorder))
		{
			recorder.Stop();
			recorder.Save();
		}
		else
		{
			Debug.LogWarning("StopAndSaveRecord - Recorder not found: " + recorderName);
		}
	}

	public void ClearRecorder(string recorderName)
	{
		ProGifRecorder recorder = null;
		if(m_GifRecorderDict.TryGetValue(recorderName, out recorder))
		{
			recorder.FlushMemory();
			recorder.Clear();
			recorder = null;
		}
		else
		{
			Debug.LogWarning("ClearRecorder - Recorder not found: " + recorderName);
		}
	}
	#endregion


	#region ----- Players -----
	/// <summary>
	/// Set the decode settings for playing gif with path/url.
	/// </summary>
	/// <param name="inDecodeMode">Decode mode: Normal, Advanced</param>
	/// <param name="inTargetDecodeFrameNum">Max num of frames to be decoded, the others will be skipped</param>
	public void SetPlayerSettings(ProGifPlayerComponent.DecodeMode inDecodeMode, int inTargetDecodeFrameNum = -1)
	{
		m_DecodeMode = inDecodeMode;
		m_TargetDecodeFrameNum = inTargetDecodeFrameNum;
	}

	/// <summary>
	/// Play gif from Recorder, display with Image
	/// Play gif with gifPath if gifAspectRatio is set, because frames stored in recorder are not crop yet.
	/// </summary>
	/// <param name="recorderSource">The recorder which the gif frames are stored.</param>
	/// <param name="playerImage">Target image for display gif.</param>
	/// <param name="playerName">The Name for identifying players in the dictionary.</param>
	/// <param name="onLoading">On loading. Return value: loading progress(float)</param>
	public void PlayGif(ProGifRecorder recorderSource, Image playerImage, string playerName, Action<float> onLoading = null)
	{
		if(recorderSource == null)
		{
			Debug.Log("GIF recorder not found!");
			return;
		}

		if(playerImage.GetComponent<ProGifPlayerComponent>() != null)
		{
			//If the target image already has a player attached, clear it before play
			playerImage.GetComponent<ProGifPlayerComponent>().Clear();
		}

		ProGifPlayer newGifPlayer = new ProGifPlayer();

		//Add the new player to dictionary
		if(m_GifPlayerDict.ContainsKey(playerName))
		{
			m_GifPlayerDict[playerName] = newGifPlayer;
		}
		else
		{
			m_GifPlayerDict.Add(playerName, newGifPlayer);
		}

		newGifPlayer.Play(recorderSource, playerImage);
		newGifPlayer.SetLoadingCallback((progress)=>{
			if(onLoading != null)
			{
				onLoading(progress);
			}
		});
	}

	/// <summary>
	/// Load GIF from path/url for playback, display with Image
	/// The memory footprint could be quite large for high-resolution gifs or large frame count gifs. 
	/// If you need to play multiple gifs at a time(e.g.from Giphy API), suggest use the preview(low-resolution) version of the gifs.
	/// </summary>
	/// <param name="gifPath">GIF path or url.</param>
	/// <param name="playerImage">Target image for display gif.</param>
	/// <param name="playerName">The Name for identifying players in the dictionary.</param>
	/// <param name="onLoading">On loading. Return value: loading progress(float)</param>
	/// <param name="shouldSaveFromWeb">The flag indicating to save or not to save file that download from web.</param>
	public void PlayGif(string gifPath, Image playerImage, string playerName, Action<float> onLoading = null, bool shouldSaveFromWeb = false)
	{
		if(string.IsNullOrEmpty(gifPath))
		{
			Debug.LogWarning("Gif path is null or empty!");
			return;
		}

		if(playerImage.GetComponent<ProGifPlayerComponent>() != null)
		{
			//If the target image already has a player attached, clear it before play
			playerImage.GetComponent<ProGifPlayerComponent>().Clear();
		}

		ProGifPlayer newGifPlayer = new ProGifPlayer();

		//Add the new player to dictionary
		if(m_GifPlayerDict.ContainsKey(playerName))
		{
			m_GifPlayerDict[playerName] = newGifPlayer;
		}
		else
		{
			m_GifPlayerDict.Add(playerName, newGifPlayer);
		}

        newGifPlayer.SetDecodeSettings(ProGifPlayerComponent.Decoder.ProGif, m_DecodeMode, m_TargetDecodeFrameNum);
        newGifPlayer.Play(gifPath, playerImage, shouldSaveFromWeb);
		newGifPlayer.SetLoadingCallback((progress)=>{
			//Check progress
			if(progress >= 1f)
			{
				newGifPlayer.SetLoadingCallback(null);
			}

			if(onLoading != null)
			{
				onLoading(progress);
			}
		});
	}

	/// <summary>
	/// Play gif from Recorder, display with Renderer
	/// Play gif with gifPath if gifAspectRatio is set, because frames stored in recorder are not crop yet.
	/// </summary>
	/// <param name="recorderSource">The recorder which the gif frames are stored.</param>
	/// <param name="playerRenderer">Target image for display gif.</param>
	/// <param name="playerName">The Name for identifying players in the dictionary.</param>
	/// <param name="onLoading">On loading. Return value: loading progress(float)</param>
	public void PlayGif(ProGifRecorder recorderSource, Renderer playerRenderer, string playerName, Action<float> onLoading = null)
	{
		if(recorderSource == null)
		{
			Debug.Log("GIF recorder not found!");
			return;
		}

		if(playerRenderer.GetComponent<ProGifPlayerComponent>() != null)
		{
			//If the target renderer already has a player attached, clear it before play
			playerRenderer.GetComponent<ProGifPlayerComponent>().Clear();
		}

		ProGifPlayer newGifPlayer = new ProGifPlayer();

		//Add the new player to dictionary
		if(m_GifPlayerDict.ContainsKey(playerName))
		{
			m_GifPlayerDict[playerName] = newGifPlayer;
		}
		else
		{
			m_GifPlayerDict.Add(playerName, newGifPlayer);
		}

		newGifPlayer.Play(recorderSource, playerRenderer);
		newGifPlayer.SetLoadingCallback((progress)=>{
			if(onLoading != null)
			{
				onLoading(progress);
			}
		});
	}

	/// <summary>
	/// Load GIF from path/url for playback, display with Renderer
	/// The memory footprint could be quite large for high-resolution gifs or large frame count gifs. 
	/// If you need to play multiple gifs at a time(e.g.from Giphy API), suggest use the preview(low-resolution) version of the gifs.
	/// </summary>
	/// <param name="gifPath">GIF path or url.</param>
	/// <param name="playerRenderer">Target renderer for display gif.</param>
	/// <param name="playerName">The Name for identifying players in the dictionary.</param>
	/// <param name="onLoading">On loading. Return value: loading progress(float)</param>
	/// <param name="shouldSaveFromWeb">The flag indicating to save or not to save file that download from web.</param>
	public void PlayGif(string gifPath, Renderer playerRenderer, string playerName, Action<float> onLoading = null, bool shouldSaveFromWeb = false)
	{
		if(string.IsNullOrEmpty(gifPath))
		{
			Debug.LogWarning("Gif path is null or empty!");
			return;
		}

		if(playerRenderer.GetComponent<ProGifPlayerComponent>() != null)
		{
			//If the target renderer already has a player attached, clear it before play
			playerRenderer.GetComponent<ProGifPlayerComponent>().Clear();
		}

		ProGifPlayer newGifPlayer = new ProGifPlayer();

		//Add the new player to dictionary
		if(m_GifPlayerDict.ContainsKey(playerName))
		{
			m_GifPlayerDict[playerName] = newGifPlayer;
		}
		else
		{
			m_GifPlayerDict.Add(playerName, newGifPlayer);
		}

		newGifPlayer.SetDecodeSettings(ProGifPlayerComponent.Decoder.ProGif, m_DecodeMode, m_TargetDecodeFrameNum);
		//newGifPlayer.SetDecodeSettings(ProGifPlayerComponent.Decoder.UniGif, m_DecodeMode, m_TargetDecodeFrameNum);
        newGifPlayer.Play(gifPath, playerRenderer, shouldSaveFromWeb);
		newGifPlayer.SetLoadingCallback((progress)=>{
			//Check progress
			if(progress >= 1f)
			{
				newGifPlayer.SetLoadingCallback(null);
			}

			if(onLoading != null)
			{
				onLoading(progress);
			}
		});
	}

	/// <summary>
	/// Set the callback for checking the decode progress.
	/// </summary>
	/// <param name="onLoading">On loading callback, returns the decode progress(float).</param>
	public void SetPlayerOnLoading(string playerName, Action<float> onLoading)
	{
		ProGifPlayer player = null;
		if(m_GifPlayerDict.TryGetValue(playerName, out player))
		{
			player.SetLoadingCallback(onLoading);
		}
		else
		{
			Debug.LogWarning("SetPlayerOnLoading - Player not found: " + playerName);
		}
	}

	/// <summary>
	/// Set the callback to be fired when the first gif frame ready.
	/// </summary>
	/// <param name="onFirstFrame">On first frame callback, returns the first gifTexture and related data.</param>
	public void SetPlayerOnFirstFrame(string playerName, Action<ProGifPlayerComponent.FirstGifFrame> onFirstFrame)
	{
		ProGifPlayer player = null;
		if(m_GifPlayerDict.TryGetValue(playerName, out player))
		{
			player.SetOnFirstFrameCallback(onFirstFrame);
		}
		else
		{
			Debug.LogWarning("SetPlayerOnFirstFrame - Player not found: " + playerName);
		}
	}

	/// <summary>
	/// Set the callback to be fired when all frames decode complete.
	/// </summary>
	/// <param name="onDecodeComplete">On decode complete callback, returns the gifTextures list and related data.</param>
	public void SetPlayerOnDecodeComplete(string playerName, Action<ProGifPlayerComponent.DecodedResult> onDecodeComplete)
	{
		ProGifPlayer player = null;
		if(m_GifPlayerDict.TryGetValue(playerName, out player))
		{
			player.SetOnDecodeCompleteCallback(onDecodeComplete);
		}
		else
		{
			Debug.LogWarning("SetPlayerOnDecodeComplete - Player not found: " + playerName);
		}
	}

	public ProGifPlayer GetPlayer(string playerName)
	{
		ProGifPlayer player = null;
		if(!m_GifPlayerDict.TryGetValue(playerName, out player))
		{
			Debug.LogWarning("GetPlayer - Player not found: " + playerName);
		}
		return player;
	}

	public void PausePlayer(string playerName)
	{
		ProGifPlayer player = null;
		if(m_GifPlayerDict.TryGetValue(playerName, out player))
		{
			player.Pause();
		}
		else
		{
			Debug.LogWarning("PausePlayer - Player not found: " + playerName);
		}
	}

	public void ResumePlayer(string playerName)
	{
		ProGifPlayer player = null;
		if(m_GifPlayerDict.TryGetValue(playerName, out player))
		{
			player.Resume();
		}
		else
		{
			Debug.LogWarning("ResumePlayer - Player not found: " + playerName);
		}
	}

	public void StopPlayer(string playerName)
	{
		ProGifPlayer player = null;
		if(m_GifPlayerDict.TryGetValue(playerName, out player))
		{
			player.Stop();
		}
		else
		{
			Debug.LogWarning("StopPlayer - Player not found: " + playerName);
		}
	}

	public void ClearPlayer(string playerName)
	{
		ProGifPlayer player = null;
		if(m_GifPlayerDict.TryGetValue(playerName, out player))
		{
			player.Clear();
			player = null;
		}
		else
		{
			Debug.LogWarning("ClearPlayer - Player not found: " + playerName);
		}
	}
	#endregion


	#region ----- Static methods -----

	//================= Recorder ===================
	public static void iSetRecordSettings(bool autoAspect, int width, int height, float duration, int fps, int loop, int quality)
	{
		Instance.SetRecordSettings(autoAspect, width, height, duration, fps, loop, quality);
	}

	public static void iSetRecordSettings(Vector2 aspectRatio, int width, int height, float duration, int fps, int loop, int quality)
	{
		Instance.SetRecordSettings(aspectRatio, width, height, duration, fps, loop, quality);
	}

	public static void iStartRecord(Camera camera, string recorderName,
		Action<float> onRecordProgress = null, Action onRecordDurationMax = null, 
		Action onPreProcessingDone = null, Action<int, float> onFileSaveProgress = null, Action<int, string> onFileSaved = null, bool autoClear = true)
	{
		Instance.StartRecord(camera, recorderName, onRecordProgress, onRecordDurationMax, onPreProcessingDone, onFileSaveProgress, onFileSaved, autoClear);
	}

	public static ProGifRecorder iGetRecorder(string recorderName)
	{
		return Instance.GetRecorder(recorderName);
	}

	public static void iPauseRecord(string recorderName)
	{
		Instance.PauseRecord(recorderName);
	}

	public static void iResumeRecord(string recorderName)
	{
		Instance.ResumeRecord(recorderName);
	}

	public static void iStopRecord(string recorderName)
	{
		Instance.StopRecord(recorderName);
	}

	public static void iSaveRecord(string recorderName)
	{
		Instance.SaveRecord(recorderName);
	}

	public static void iStopAndSaveRecord(string recorderName)
	{
		Instance.StopAndSaveRecord(recorderName);
	}

	public static void iClearRecorder(string recorderName)
	{
		Instance.ClearRecorder(recorderName);
	}

	//================= Player ===================
	public static void iSetPlayerSettings(ProGifPlayerComponent.DecodeMode inDecodeMode, int inTargetDecodeFrameNum = -1)
	{
		Instance.SetPlayerSettings(inDecodeMode, inTargetDecodeFrameNum);
	}

	public static void iPlayGif(ProGifRecorder recorderSource, Image playerImage, string playerName, Action<float> onLoading = null)
	{
		Instance.PlayGif(recorderSource, playerImage, playerName, onLoading);
	}

	public static void iPlayGif(string gifPath, Image playerImage, string playerName, Action<float> onLoading = null, bool shouldSaveFromWeb = false)
	{
		Instance.PlayGif(gifPath, playerImage, playerName, onLoading, shouldSaveFromWeb);
	}

	public static void iPlayGif(ProGifRecorder recorderSource, Renderer playerRenderer, string playerName, Action<float> onLoading = null)
	{
		Instance.PlayGif(recorderSource, playerRenderer, playerName, onLoading);
	}

	public static void iPlayGif(string gifPath, Renderer playerRenderer, string playerName, Action<float> onLoading = null, bool shouldSaveFromWeb = false)
	{
		Instance.PlayGif(gifPath, playerRenderer, playerName, onLoading, shouldSaveFromWeb);
	}

	public static ProGifPlayer iGetPlayer(string playerName)
	{
		return Instance.GetPlayer(playerName);
	}

	public static void iPausePlayer(string playerName)
	{
		Instance.PausePlayer(playerName);
	}

	public static void iResumePlayer(string playerName)
	{
		Instance.ResumePlayer(playerName);
	}

	public static void iStopPlayer(string playerName)
	{
		Instance.StopPlayer(playerName);
	}

	public static void iClearPlayer(string playerName)
	{
		Instance.ClearPlayer(playerName);
	}
	#endregion
}
