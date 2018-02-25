/// <summary>
/// Created by SWAN DEV
/// </summary>

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class ProGifPlayerPanel : MonoBehaviour
{
	public GameObject containerGO;
	public GameObject playerControlBar;
	public Image m_GifImage;
	public Renderer m_GifRenderer;

	public Button btn_Submit;
	public InputField input_Url;

	public Button btn_Play;
	public Button btn_Pause;
	public Button btn_Resume;
	public Button btn_Stop;

	public Action<float> _OnLoading = null;

	public ProGifPlayerComponent.Decoder m_Decoder = ProGifPlayerComponent.Decoder.ProGif;  
	public int m_TargetDecodeFrameNum = -1;

	//gifPath = "https://media.giphy.com/media/xUPGcreVxpx1AXOHwQ/giphy.gif";
	//gifPath = "https://media.giphy.com/media/xUPGctftozEFipUic0/giphy.gif";
	//gifPath = "https://media.giphy.com/media/eDUHhtooZxyhi/giphy.gif";
	public string m_Url = "";

	private string gifPath = "";

	/// <summary>
	/// Create an instance of ProGifPlayerPanel from provided prefab, and set parent.
	/// </summary>
	/// <param name="prefab">The Prefab of ProGifPlayerPanel.</param>
	/// <param name="parentT">The container/parent for this instance.</param>
	public static ProGifPlayerPanel Create(GameObject prefab, Transform parentT)
	{
		ProGifPlayerPanel gifPanel = ProGifManager.InstantiatePrefab<ProGifPlayerPanel>(prefab);
		gifPanel.transform.SetParent(parentT);
		gifPanel.transform.rotation = parentT.rotation;
		gifPanel.transform.localScale = Vector3.one;
		gifPanel.transform.localPosition = Vector3.zero;
		gifPanel.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
		gifPanel.GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
		return gifPanel;
	}

	// Use this for initialization
	public void Setup(string gifPath, Action<float> onLoading = null)
	{
		_OnLoading = onLoading;

		if(string.IsNullOrEmpty(gifPath))
		{
			gifPath = PlayerPrefs.GetString(ProGifManager.PP_LastGifPathKey, "");
		}

		SetInputText(gifPath);
		playerControlBar.SetActive(false);
		_Show();
	}

	public void OnInputValueChange(InputField input)
	{
		gifPath = input.text;
	}

	public void SetInputText(string path)
	{
		gifPath = path;
		input_Url.placeholder.GetComponent<Text>().text = path;
	}

	public void Play()
	{
		if(!string.IsNullOrEmpty(m_Url))
		{
			gifPath = m_Url;
		}
		Debug.Log("Play: " + gifPath);

		input_Url.placeholder.GetComponent<Text>().text = "Enter gif image url/path...";
		if(string.IsNullOrEmpty(gifPath)) return;

		//The flag indicating to save or not to save file that download from web 
		bool shouldSaveFromWeb = true;

		ProGifManager.Instance.SetPlayerSettings(m_Decoder, ProGifPlayerComponent.DecodeMode.Advanced, m_TargetDecodeFrameNum);

		if(m_GifRenderer != null)
		{
			ProGifManager.Instance.PlayGif(gifPath, m_GifRenderer, (progress)=>{
				//Set the gif size when the first frame decode is finished and assigned to m_GifRenderer
				//Set renderer transform scale here:
				int gifWidth = ProGifManager.Instance.m_GifPlayer.width;
				int gifHeight = ProGifManager.Instance.m_GifPlayer.height;
				//m_GifRenderer.gameObject.GetComponent<Transform>().localScale = new Vector3(gifWidth/2, gifHeight/2, 
				//m_GifRenderer.gameObject.GetComponent<Transform>().localScale.z);

				if(_OnLoading != null)
				{
					_OnLoading(progress);
				}
			}, shouldSaveFromWeb);
		}
		else if(m_GifImage != null)
		{
			ProGifManager.Instance.PlayGif(gifPath, m_GifImage, (progress)=>{
				//Set the gif size when the first frame decode is finished and assigned to m_GifImage
				//Set image scale here:
				m_GifImage.SetNativeSize();

				if(_OnLoading != null)
				{
					_OnLoading(progress);
				}
			}, shouldSaveFromWeb);
		}
	}

	public void Pause()
	{
		ProGifManager.Instance.PausePlayer();
	}

	public void Resume()
	{
		ProGifManager.Instance.ResumePlayer();
	}

	public void Stop()
	{
		ProGifManager.Instance.StopPlayer();
	}


	private void _Show()
	{
		//Show Gif image
		gameObject.SetActive(true);
		SDemoAnimation.Instance.Scale(containerGO, Vector3.zero, Vector3.one, 0.3f, SDemoAnimation.LoopType.None, ()=>{
			//Show control bar
			playerControlBar.SetActive(true);

			float startY = playerControlBar.transform.localPosition.y;
			SDemoAnimation.Instance.Move(playerControlBar, new Vector3(0f, startY, 0f), new Vector3(0f, startY+280, 0f), 0.3f, SDemoAnimation.LoopType.None);
		});
	}

	public void Close()
	{
		_Close();
	}

	private void _Close()
	{
		//Hide control bar 
		float startY = playerControlBar.transform.localPosition.y;
		SDemoAnimation.Instance.Move(playerControlBar, new Vector3(0f, startY, 0f), new Vector3(0f, startY-280, 0f), 0.3f, SDemoAnimation.LoopType.None, ()=>{
			//Hide control bar
			playerControlBar.SetActive(false);

			//Hide Gif image
			SDemoAnimation.Instance.Rotate(containerGO, Vector3.zero, new Vector3(0f, 90f, 0f), 0.3f, SDemoAnimation.LoopType.None, ()=>{

				//Clear un-use resources in the recorder and player to avoid memory leak
				ProGifManager.Instance.Clear();

//				//Clear texture in preview image to avoid memory leak
//				if(m_GifImage.sprite != null && m_GifImage.sprite.texture != null)
//				{
//					Texture2D.Destroy(m_GifImage.sprite.texture);
//				}

				//Remove panel
				Destroy(gameObject);
			});
		});
	}

}
