/// <summary>
/// Created by SWAN DEV
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProGifDemoMgr : MonoBehaviour
{
	#region ----- Prefabs -----
	public GameObject prefab_GifControlPanel;
	public GameObject prefab_GifPreviewAndSharePanel;
	public GameObject prefab_GifPlayerPanel;

	public static T InstantiatePrefab<T>(GameObject prefab) where T: MonoBehaviour
	{
		if(prefab != null)
		{
			GameObject go = GameObject.Instantiate(prefab) as GameObject;
			if(go != null)
			{
				go.name = "[Prefab]" + prefab.name;
				go.transform.localScale = Vector3.one;
				return go.GetComponent<T>();
			}
			else
			{	
				Debug.Log("prefab is null!") ;
				return null ;
			}
		}
		else
			return null ;
	}
	#endregion


	public Transform componentContainerT;
	public Camera m_MainCamera;
	public CanvasScaler m_MainCanvasScaler;

	public TextMesh m_TM_Counter;


	private static ProGifDemoMgr _instance;
	public static ProGifDemoMgr Instance
	{
		get{
			return _instance;
		}
	}

	void Start()
	{
		_instance = this;
		SetButtonState(btn_PauseRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
		SetButtonState(btn_ResumeRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);

		//Check screen orientation for setting canvas resolution
		if(Screen.width > Screen.height)
		{
			m_MainCanvasScaler.referenceResolution = new Vector2(1920, 1080);
		}
		else
		{
			m_MainCanvasScaler.referenceResolution = new Vector2(1080, 1920);
		}
	}


	int counter = 0;
	public void AddCounter() 
	{
		if(m_TM_Counter == null) return;

		counter++;
		if(counter > 9) counter = 0;
		m_TM_Counter.text = counter.ToString();
	}

	#region ----- GIF Recording -----
	private float nextBubbleMessageTime = 0f;
	private ProGifControlPanel m_ProGifPanel = null;

	public Button btn_ShowGifPanel;
	public Button btn_PauseRecord;
	public Button btn_ResumeRecord;
	public Button btn_StopRecord;
	public Button btn_ShowGifPlayerPanel;

	public Slider sld_Progress;
	public Text text_Progress;


	public void UpdateRecordOrSaveProgress(float progress)
	{
		if(sld_Progress != null) sld_Progress.value = progress;
		if(text_Progress != null) text_Progress.text = "Progress: " + (int)(100 * progress) + " %";

		if(ProGifManager.Instance.m_GifRecorder != null && ProGifManager.Instance.m_GifRecorder.State == ProGifRecorder.RecorderState.Recording)
		{
			SetButtonState(btn_PauseRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
			SetButtonState(btn_ResumeRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
		}
	}

	public void SetGifProgressColor(Color color)
	{
		if(sld_Progress.fillRect.GetComponent<Image>().color != color)
		{
			sld_Progress.fillRect.GetComponent<Image>().color = color;
			//Debug.Log("SetGifProgressColor: " + color.ToString());
		}
	}

	public void ShowGIFPanel()
	{
		if(ProGifManager.Instance.m_GifRecorder != null)
		{
			if(Time.time > nextBubbleMessageTime)
			{
				nextBubbleMessageTime = Time.time + 2f;

				if(ProGifManager.Instance.m_GifRecorder.State == ProGifRecorder.RecorderState.Paused)
				{
					//Encoding all stored frames into a GIF file 
					Debug.Log("Making GIF, please wait");
				}
				else if(ProGifManager.Instance.m_GifRecorder.State == ProGifRecorder.RecorderState.Recording)
				{
					StopRecord();
				}
			}
		}
		else
		{
			m_ProGifPanel = ProGifControlPanel.Create(prefab_GifControlPanel, componentContainerT);
			m_ProGifPanel.Setup(()=>{
				//Update UI
				SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.LightYellow));
				SetButtonState(btn_StopRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
				SetButtonState(btn_ShowGifPlayerPanel, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);

			}, UpdateRecordOrSaveProgress, ()=>{
				Debug.Log("DemoMgr - Record duration MAX.");
				SetButtonState(btn_StopRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
			});
		}
	}

	public void PauseRecord()
	{
		Debug.Log("Pause Recording");
		ProGifManager.Instance.PauseRecord();
	}

	public void ResumeRecord()
	{
		Debug.Log("Resume Recording");
		ProGifManager.Instance.ResumeRecord();
	}

	public void StopRecord()
	{
		Debug.Log("Start making GIF");
		ProGifManager.Instance.StopAndSaveRecord(
			()=>{
				Debug.Log("On recorder pre-processing done.");
			}, 

			(id, progress)=>{
				UpdateRecordOrSaveProgress(progress);
				SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.Red));
			}, 

			(id, path)=>{
				//If a specify aspect ratio is provided, then we should play the gif using its path: Set loadFile = true
				bool loadFile = ProGifManager.Instance.LoadFile;

				ShowGifPreviewAndSharePanel(path, loadFile);
				SetButtonState(btn_ShowGifPlayerPanel, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
				UpdateRecordOrSaveProgress(1f);
				StartCoroutine(_OnFileSaved());
			}
		);
		SetButtonState(btn_PauseRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
		SetButtonState(btn_ResumeRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
		SetButtonState(btn_StopRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
	}

	private IEnumerator _OnFileSaved()
	{
		yield return new WaitForSeconds(2f);
		_ResetGifProgress();
	}

	private void _ResetGifProgress()
	{
		UpdateRecordOrSaveProgress(0f);
		SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.White));
	}

	private IEnumerator _OnLoadingComplete()
	{
		yield return new WaitForSeconds(2f);
		_ResetGifProgress();
	}

	public void ShowGifPreviewAndSharePanel(string gifPath, bool loadFile)
	{
		ProGifPreviewSharePanel gifPreview = ProGifPreviewSharePanel.Create(prefab_GifPreviewAndSharePanel, componentContainerT);
		gifPreview.Setup(gifPath, loadFile, (progress)=>{
			UpdateRecordOrSaveProgress(progress);
			SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.Green));

			//Check progress
			if(progress >= 1f)
			{
				StartCoroutine(_OnLoadingComplete());
			}
		});
	}

	public void ShowPlayerPanel(string gifPath)
	{
		ProGifPlayerPanel playerPanel = ProGifPlayerPanel.Create(prefab_GifPlayerPanel, componentContainerT);
		playerPanel.Setup(gifPath, (progress)=>{
			UpdateRecordOrSaveProgress(progress);
			SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.Green));

			//Check progress
			if(progress >= 1f)
			{
				StartCoroutine(_OnLoadingComplete());
			}
		});
	}

	public void SetButtonState(Button button, Color color, bool enable)
	{
		button.enabled = enable;
		button.image.color = color;
	}

	#endregion

}
