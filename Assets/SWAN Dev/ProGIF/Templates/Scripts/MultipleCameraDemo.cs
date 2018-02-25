using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultipleCameraDemo : MonoBehaviour
{
	public Camera mCamera1;
	public Camera mCamera2;
	public Camera mCamera3;

	public Text cam1Text;
	public Text cam2Text;
	public Text cam3Text;

	public Image image1;
	public Image image2;
	public Image image3;

	// Use this for initialization
	void Start()
	{
		//Change setting before camera1 start recording
		PGif.iSetRecordSettings(true, 300, 300, 3, 24, 0, 50);
		//Start recording with camera1
		PGif.iStartRecord(mCamera1, "Cam1", OnRecordProgress1, OnRecordDurationMax1, OnPreProcessingDone1, OnFileSaveProgress1, OnFileSaved1, false);
		cam1Text.text = "Camera1 started recording";

		//Change setting before camera2 start recording
		PGif.iSetRecordSettings(new Vector2(1, 1), 300, 300, 5, 10, 1, 50);
		//Start recording with camera2
		PGif.iStartRecord(mCamera2, "Cam2", OnRecordProgress2, OnRecordDurationMax2, OnPreProcessingDone2, OnFileSaveProgress2, OnFileSaved2, false);
		cam2Text.text = "Camera2 started recording";

		//Change setting before camera3 start recording
		PGif.iSetRecordSettings(new Vector2(4, 3), 200, 200, 7, 10, 1, 50);
		//Start recording with camera3
		PGif.iStartRecord(mCamera3, "Cam3", OnRecordProgress3, OnRecordDurationMax3, OnPreProcessingDone3, OnFileSaveProgress3, OnFileSaved3);
		cam3Text.text = "Camera3 started recording";
	}


	public void OnRecordProgress1(float progress)
	{
		//Debug.Log("Cam1 - [MultipleCameraDemo] On record progress: " + progress);
	}

	public void OnRecordDurationMax1()
	{
		Debug.Log("Cam1 - [MultipleCameraDemo] On recorder buffer max.");
		cam1Text.text = "Camera1 duration Max";
	}

	public void OnPreProcessingDone1()
	{
		Debug.Log("Cam1 - [MultipleCameraDemo] On pre-processing done.");
		cam1Text.text = "Camera1 pre-processing done";
	}

	public void OnFileSaveProgress1(int id, float progress)
	{
		//Debug.Log("Cam1 - [MultipleCameraDemo] Save progress: " + progress);
		cam1Text.text = "Camera1 Save progress: " + progress;
	}

	public void OnFileSaved1(int id, string path)
	{
		Debug.Log("Cam1 - [MultipleCameraDemo] On saved, path: " + path);
		cam1Text.text = "Camera1 Saved: " + path;

		//Preview the saved gif or play it? Do not clear the recorder if you want to preview the gif, 
		//you can clear the recorder after you quit from the preview
		//PGif.iClearRecorder("Cam1");
		_PlayGif("Cam1", "GifPlayer1", image1);
	}


	public void OnRecordProgress2(float progress)
	{
		//Debug.Log("Cam2 - [MultipleCameraDemo] On record progress: " + progress);
	}

	public void OnRecordDurationMax2()
	{
		Debug.Log("Cam2 - [MultipleCameraDemo] On recorder buffer max.");
		cam2Text.text = "Camera2 duration Max";
	}

	public void OnPreProcessingDone2()
	{
		Debug.Log("Cam2 - [MultipleCameraDemo] On pre-processing done.");
		cam2Text.text = "Camera2 pre-processing done";
	}

	public void OnFileSaveProgress2(int id, float progress)
	{
		//Debug.Log("Cam2 - [MultipleCameraDemo] Save progress: " + progress);
		cam2Text.text = "Camera2 Save progress: " + progress;
	}

	public void OnFileSaved2(int id, string path)
	{
		Debug.Log("Cam2 - [MultipleCameraDemo] On saved, path: " + path);
		cam2Text.text = "Camera3 Saved: " + path;

		_PlayGif("Cam2", "GifPlayer2", image2);
	}


	public void OnRecordProgress3(float progress)
	{
		//Debug.Log("Cam3 - [MultipleCameraDemo] On record progress: " + progress);
	}

	public void OnRecordDurationMax3()
	{
		Debug.Log("Cam3 - [MultipleCameraDemo] On recorder buffer max.");
		cam3Text.text = "Camera3 duration Max";
	}

	public void OnPreProcessingDone3()
	{
		Debug.Log("Cam3 - [MultipleCameraDemo] On pre-processing done.");
		cam3Text.text = "Camera3 pre-processing done";
	}

	public void OnFileSaveProgress3(int id, float progress)
	{
		//Debug.Log("Cam3 - [MultipleCameraDemo] Save progress: " + progress);
		cam3Text.text = "Camera3 Save progress: " + progress;
	}

	public void OnFileSaved3(int id, string path)
	{
		Debug.Log("Cam3 - [MultipleCameraDemo] On saved, path: " + path);
		cam3Text.text = "Camera3 Saved: " + path;

		_PlayGif("Cam3", "GifPlayer3", image3);
	}

	private void _PlayGif(string recorderName, string playerName, Image destination)
	{
		PGif.iPlayGif(PGif.iGetRecorder(recorderName), destination, playerName, (progress)=>{
			//Set display size
			float gifRatio = (float)PGif.iGetRecorder(recorderName).Width/(float)PGif.iGetRecorder(recorderName).Height;
			_SetDisplaySize(gifRatio, destination);
		});
	}

	private void _SetDisplaySize(float gifWHRatio, Image destination)
	{
		int maxDisplayWidth = (int)destination.rectTransform.sizeDelta.x;
		int maxDisplayHeight = (int)destination.rectTransform.sizeDelta.y;

		int displayWidth = maxDisplayWidth;
		int displayHeight = maxDisplayHeight;
		if(gifWHRatio > 1f)
		{
			displayWidth = maxDisplayWidth;
			displayHeight = (int)((float)displayWidth / gifWHRatio);
		}
		else if(gifWHRatio < 1f)
		{
			displayHeight = maxDisplayHeight;
			displayWidth = (int)((float)displayHeight * gifWHRatio);
		}
		destination.rectTransform.sizeDelta = new Vector2(displayWidth, displayHeight);
	}

	#region ---- UI Control ----
	public void SaveRecord_Cam1()
	{
		PGif.iStopAndSaveRecord("Cam1");
	}

	public void SaveRecord_Cam2()
	{
		PGif.iStopAndSaveRecord("Cam2");
	}

	public void SaveRecord_Cam3()
	{
		PGif.iStopAndSaveRecord("Cam3");
	}

	int _counter = 0;
	public void UpdateCubeText(TextMesh tm)
	{
		_counter++;
		if(_counter > 9) _counter = 0;
		tm.text = _counter.ToString();
	}
	#endregion


}
