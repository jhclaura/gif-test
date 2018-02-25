#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(OnEditorGifRecorder))]
public class OnEditorGifRecorderCustomEditor : Editor
{
	private static string[] cameraOptions = new string[]{}; 
	private static int cameraSelection = 0;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		OnEditorGifRecorder recorder = (OnEditorGifRecorder)target;

		cameraSelection = GUILayout.SelectionGrid(cameraSelection, cameraOptions, 2);
		recorder.SetCamera(cameraSelection);

		GUILayout.Label("Find all Cameras in the scene:\n(Drag the camera you want to Recorder Camera)");
		if(GUILayout.Button("Find Cameras"))
		{
			recorder.FindCameras(this);
		}

		GUILayout.Label("Start record GIF with Recorder Camera, or main camera:");
		if(GUILayout.Button("Start Record"))
		{
			recorder.StartRecord();
		}

		GUILayout.Label("Stop and save the stored frames as GIF:");
		if(GUILayout.Button("Save Record"))
		{
			recorder.SaveRecord();
		}

	}

	public void SetCameraOptions(Camera[] cameras)
	{
		cameraSelection = 0;
		cameraOptions = new string[cameras.Length];
		for(int i=0; i<cameras.Length; i++)
		{
			cameraOptions[i] = cameras[i].name;
		}
	}
}


public class OnEditorGifRecorder : MonoBehaviour
{
	[Header("-- Drop this prefab to the scene, Play in the Editor --")]
	public Vector2 m_AspectRatio = new Vector2(0, 0);
	public bool m_AutoAspect = true;
	public int m_Width = 360;
	public int m_Height = 360;
	public float m_Duration = 3f;
	[Range(1, 60)] public int m_Fps = 15;
	public int m_Loop = 0;								//-1: no repeat, 0: infinite, >0: repeat count
	[Range(1, 100)] public int m_Quality = 20;			//(1 - 100), 1: best(larger storage size), 100: faster(smaller storage size)

	[Header("Progress:")]
	public string m_RecordingProgress = "0%";
	public string m_SaveProgress = "0%";
	public string m_State = "Idle";
	[TextArea(1, 2)] public string m_SavePath = "GIF Path";

	public Camera m_RecorderCamera;
	public Camera[] m_AllCameras;
	private int _currCameraIndex = 0;

	private const string _recorderName = "OnEditorGifRecorder";

	public void FindCameras(OnEditorGifRecorderCustomEditor editorScript)
	{
		m_AllCameras = Camera.allCameras;
		editorScript.SetCameraOptions(m_AllCameras);

		if(m_AllCameras != null && m_AllCameras.Length > 0 && m_RecorderCamera == null)
		{
			m_RecorderCamera = m_AllCameras[0];
		}
	}

	public void SetCamera(int index)
	{
		if(_currCameraIndex == index) return;
		_currCameraIndex = index;
		if(index < m_AllCameras.Length) m_RecorderCamera = m_AllCameras[index];
	}

	public void StartRecord()
	{
		if(!Application.isPlaying || !Application.isEditor)
		{
			Debug.LogWarning("This script is designed to work in the Editor Mode with Editor Playing.");
			return;
		}

		Debug.Log("Start Record");
		m_State = "Recording..";
		PGif.iSetRecordSettings(m_AutoAspect, m_Width, m_Height, m_Duration, m_Fps, m_Loop, m_Quality);
		PGif.iStartRecord(((m_RecorderCamera == null)?Camera.main:m_RecorderCamera), _recorderName, 
			(progress)=>{
				m_RecordingProgress = Mathf.CeilToInt(progress*100) + "%";
			}, 
			()=>{
				m_State = "Press the <Save Record> button to save GIF";
			},
			null,
			(id, progress)=>{
				m_SaveProgress = Mathf.CeilToInt(progress*100) + "%";
			},
			(id, path)=>{
				m_SavePath = path;
				m_RecordingProgress = "0%";
				m_SaveProgress = "0%";
				m_State = "Idle";
			}
		);
	}

	public void SaveRecord()
	{
		if(!Application.isPlaying || !Application.isEditor)
		{
			Debug.LogWarning("This script is designed to work in the Editor Mode with Editor Playing.");
			return;
		}

		Debug.Log("Save Record");
		m_State = "Saving..";
		PGif.iStopAndSaveRecord(_recorderName);
	}

}
#endif