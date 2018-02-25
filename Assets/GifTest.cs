using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using OldMoatGames;

public class GifTest : MonoBehaviour {

    public Dropdown m_Dropdown;
    public InputField m_InputFieldKeyWord;
    public GameObject gifPrefab;
    public GameObject copyGifPrefab;
    public float spawnRate;
    public int maxStickers = 100;

    private List<GameObject> gifs = new List<GameObject>();
    private List<AnimatedGifPlayer> gifPlayers = new List<AnimatedGifPlayer>();
    private List<string> urlList = new List<string>();
    private List<float> widthToHeightRatio = new List<float>();

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    //-------------------------------------------------
    public void OnButtonSend()
    {
        Debug.Log("OnButtonSend: " + m_Dropdown.options[m_Dropdown.value].text);
        _GiphyApi(m_Dropdown.options[m_Dropdown.value].text);
    }
    //-------------------------------------------------

    private void _GiphyApi(string text)
    {
        string tempStr = "";

        //------ GIF API -------
        if (text == "Search")
        {
            if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
            {
                Debug.LogWarning("_GiphyApi > Search, KeyWord is Empty!");
            }
            else
            {
                Debug.Log("_GiphyApi > Search");
                GifLoader.instance.Search(new List<string> { m_InputFieldKeyWord.text }, (result) => {
                    tempStr = "1. Search - All GIF URLs (KeyWord = " + m_InputFieldKeyWord.text + "): ";
                    urlList.Clear();
                    widthToHeightRatio.Clear();
                    for (int i = 0; i < result.data.Count; i++)
                    {
                        tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                        urlList.Add(result.data[i].images.downsized_large.url);
                        float _ratio = (float) System.Int32.Parse(result.data[i].images.original.height) / System.Int32.Parse(result.data[i].images.original.width);
                        widthToHeightRatio.Add(_ratio);
                    }

                    tempStr += "\n\nGif Id: " + result.data[0].id // gif unique id
                        + "\nbitly_gif_url: " + result.data[0].bitly_gif_url
                        + "\nbitly_url: " + result.data[0].bitly_url
                        + "\ncontent_url: " + result.data[0].content_url
                        + "\nembed_url: " + result.data[0].embed_url
                        + "\nurl: " + result.data[0].url
                        + "\nimages.original.url: " + result.data[0].images.original.url
                        + "\nimages.original.mp4: " + result.data[0].images.original.mp4
                        + "\nimages.original.webp: " + result.data[0].images.original.webp
                        + "\nimages.original_mp4.mp4: " + result.data[0].images.original_mp4.mp4;
                    Debug.Log(tempStr);

                    // Display gif previews
                    // - Based on how many result
                    // - create GifPlayer from prefab and assign url
                    StartCoroutine(Spawn());

                    /*
                    if (m_PlayerImages != null)
                    {
                        for (int i = 0; i < m_PlayerImages.Length; i++)
                        {
                            if (result.data.Count >= i + 1)
                            {
                                //Smaller size animated gifs preview 
                                PlayGif(result.data[i].images.preview_gif.url, m_PlayerImages[i], "Player" + i);
                            }
                        }
                    }
                    */
                });
            }
        }
        /*
        if (text == "GetByIds")
        {
            Debug.Log("_GiphyApi > GetByIds");
            if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
            {
                GiphyManager.Instance.GetByIds(new List<string> { "ZXKZWB13D6gFO", "3oEdva9BUHPIs2SkGk", "u23zXEvNsIbfO" }, (result) => {
                    tempStr = "2. GetByIds - (Ids hard-coded for demo = " + "ZXKZWB13D6gFO, 3oEdva9BUHPIs2SkGk, u23zXEvNsIbfO): ";
                    for (int i = 0; i < result.data.Count; i++)
                    {
                        tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                    }
                    _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                    _shareGifFullUrl = result.data[0].images.original.url;
                    _shareGifId = result.data[0].id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;


                    // Display gif previews
                    if (m_PlayerImages != null)
                    {
                        for (int i = 0; i < m_PlayerImages.Length; i++)
                        {
                            if (result.data.Count >= i + 1)
                            {
                                //Still gif preview
                                //PlayGif(result.data[i].images.fixed_height_small_still.url, m_PlayerImages[i], "Player" + i);

                                //Smaller size animated gifs preview 
                                PlayGif(result.data[i].images.preview_gif.url, m_PlayerImages[i], "Player" + i);
                            }
                        }
                    }
                });
            }
            else
            {
                GiphyManager.Instance.GetByIds(new List<string> { m_InputFieldKeyWord.text }, (result) => {
                    tempStr = "2. GetByIds: ";
                    for (int i = 0; i < result.data.Count; i++)
                    {
                        tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                    }
                    _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                    _shareGifFullUrl = result.data[0].images.original.url;
                    _shareGifId = result.data[0].id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;


                    // Display gif previews
                    if (m_PlayerImages != null)
                    {
                        for (int i = 0; i < m_PlayerImages.Length; i++)
                        {
                            if (result.data.Count >= i + 1)
                            {
                                //Still gif preview
                                //PlayGif(result.data[i].images.fixed_height_small_still.url, m_PlayerImages[i], "Player" + i);

                                //Smaller size animated gifs preview 
                                PlayGif(result.data[i].images.downsized_large.url, m_PlayerImages[i], "Player" + i);
                            }
                        }
                    }
                });
            }
        }

        if (text == "Random")
        {
            Debug.Log("_GiphyApi > Random");
            GiphyManager.Instance.Random((result) => {
                tempStr = "3. Random result: ";
                _shareGifBitlyUrl = result.data.image_url;
                _shareGifFullUrl = result.data.image_original_url;
                _shareGifId = result.data.id;

                tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                Debug.Log(tempStr);
                m_InputField.text = tempStr;
                m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;


                // Display gif previews
                if (m_PlayerImages != null)
                {
                    for (int i = 0; i < m_PlayerImages.Length; i++)
                    {
                        //Still gif preview
                        //PlayGif(result.data.fixed_height_small_still_url, m_PlayerImages[i], "Player" + i);

                        //Smaller size animated gifs preview 
                        PlayGif(result.data.fixed_height_small_url, m_PlayerImages[i], "Player" + i);
                    }
                }
            });
        }

        if (text == "Translate")
        {
            if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
            {
                Debug.LogWarning("_GiphyApi > Translate, KeyWord is Empty!");
            }
            else
            {
                Debug.Log("_GiphyApi > Translate");
                GiphyManager.Instance.Translate(m_InputFieldKeyWord.text, (result) => {
                    tempStr = "4. Translate result (KeyWord = " + m_InputFieldKeyWord.text + "): ";
                    _shareGifBitlyUrl = result.data.bitly_gif_url;
                    _shareGifFullUrl = result.data.images.original.url;
                    _shareGifId = result.data.id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;


                    // Display gif previews
                    if (m_PlayerImages != null)
                    {
                        for (int i = 0; i < m_PlayerImages.Length; i++)
                        {
                            //Smaller size animated gifs preview 
                            PlayGif(result.data.images.preview_gif.url, m_PlayerImages[i], "Player" + i);
                        }
                    }
                });
            }
        }

        if (text == "Trending")
        {
            Debug.Log("_GiphyApi > Trending");
            GiphyManager.Instance.Trending((result) => {
                tempStr = "5. Trending - All GIF URLs: ";
                for (int i = 0; i < result.data.Count; i++)
                {
                    tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                }
                _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                _shareGifFullUrl = result.data[0].images.original.url;
                _shareGifId = result.data[0].id;

                tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                Debug.Log(tempStr);
                m_InputField.text = tempStr;
                m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;


                // Display gif previews
                if (m_PlayerImages != null)
                {
                    for (int i = 0; i < m_PlayerImages.Length; i++)
                    {
                        if (result.data.Count >= i + 1)
                        {
                            //Smaller size animated gifs preview 
                            //PlayGif(result.data[i].images.preview_gif.url, m_PlayerImages[i], "Player" + i);
                            PlayGif(result.data[i].images.downsized_large.url, m_PlayerImages[i], "Player" + i);

                        }
                    }
                }

                if (m_PlayerRenderers != null)
                {
                    for (int i = 0; i < m_PlayerRenderers.Length; i++)
                    {
                        if (result.data.Count >= i + 1)
                        {
                            //Smaller size animated gifs preview 
                            //PlayGif(result.data[i].images.preview_gif.url, m_PlayerRenderers[i], "Player" + i);
                            PlayGif(result.data[i].images.downsized_large.url, m_PlayerRenderers[i], "Player" + i);

                        }
                    }
                }
            });
        }
        
        //------ Stickers API -------        
        if (text == "Search Sticker")
        {
            if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
            {
                Debug.LogWarning("_GiphyApi > Search Sticker, KeyWord is Empty!");
            }
            else
            {
                Debug.Log("_GiphyApi > Search Sticker");
                GiphyManager.Instance.Search_Sticker(new List<string> { m_InputFieldKeyWord.text }, (result) => {
                    tempStr = "6. Search_Sticker - All GIF URLs (KeyWord = " + m_InputFieldKeyWord.text + "): ";
                    for (int i = 0; i < result.data.Count; i++)
                    {
                        tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                    }
                    _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                    _shareGifFullUrl = result.data[0].images.original.url;
                    _shareGifId = result.data[0].id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;


                    // Display gif previews
                    if (m_PlayerImages != null)
                    {
                        for (int i = 0; i < m_PlayerImages.Length; i++)
                        {
                            if (result.data.Count >= i + 1)
                            {
                                //Smaller size animated gifs preview 
                                PlayGif(result.data[i].images.preview_gif.url, m_PlayerImages[i], "Player" + i);
                            }
                        }
                    }
                });
            }
        }
        */

        if (text == "Trending Sticker")
        {
            Debug.Log("_GiphyApi > Trending Sticker");
            GifLoader.instance.Trending_Sticker((result) => {
                tempStr = "7. Trending_Sticker - All GIF URLs: ";
                urlList.Clear();
                widthToHeightRatio.Clear();
                for (int i = 0; i < result.data.Count; i++)
                {
                    tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                    urlList.Add(result.data[i].images.downsized_large.url);
                    float _ratio = (float)System.Int32.Parse(result.data[i].images.original.height) / System.Int32.Parse(result.data[i].images.original.width);
                    widthToHeightRatio.Add(_ratio);
                }
                //string _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                //string _shareGifFullUrl = result.data[0].images.original.url;
                //string _shareGifId = result.data[0].id;

                //tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                Debug.Log(tempStr);

                // Display gif previews
                // - Based on how many result
                // - create GifPlayer from prefab and assign url
                StartCoroutine(Spawn());
                /*
                if (m_PlayerImages != null)
                {
                    for (int i = 0; i < m_PlayerImages.Length; i++)
                    {
                        if (result.data.Count >= i + 1)
                        {
                            //Smaller size animated gifs preview 
                            PlayGif(result.data[i].images.preview_gif.url, m_PlayerImages[i], "Player" + i);
                        }
                    }
                }

                if (m_PlayerRenderers != null)
                {
                    for (int i = 0; i < m_PlayerRenderers.Length; i++)
                    {
                        if (result.data.Count >= i + 1)
                        {
                            //Smaller size animated gifs preview 
                            PlayGif(result.data[i].images.preview_gif.url, m_PlayerRenderers[i], "Player" + i);
                        }
                    }
                }*/
            });
        }

        /*
        if (text == "Sticker Packs")
        {
            Debug.Log("_GiphyApi > Sticker Packs");
            GiphyManager.Instance.Sticker_Packs((result) => {
                tempStr = "Sticker_Packs - All Sticker Packs name: ";
                for (int i = 0; i < result.data.Count; i++)
                {
                    tempStr += "\n(" + i + ") " + result.data[i].short_display_name + ", id: " + result.data[i].id;
                }
                Debug.Log(tempStr);
            });
        }

        if (text == "Sticker Pack with Id")
        {
            Debug.Log("_GiphyApi > Sticker Pack with Id");
            int _packId = -1;

            if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
            {
                Debug.LogWarning("_GiphyApi > Sticker Pack with Id, Id is Empty!");
            }
            else
            {
                bool _result = int.TryParse(m_InputFieldKeyWord.text, out _packId);
                if (!_result)
                {
                    Debug.LogWarning("_GiphyApi > Sticker Pack with Id, Id needs to be number!");
                }
                else
                {
                    GiphyManager.Instance.Sticker_Pack_By_Id(_packId, (result) => {
                        tempStr = "Sticker_Pack - The Sticker Pack has: ";
                        for (int i = 0; i < result.data.Count; i++)
                        {
                            tempStr += "\n(" + i + ") " + result.data[i].title;
                        }
                        //_shareGifBitlyUrl = result.data[0].bitly_gif_url;
                        //_shareGifFullUrl = result.data[0].images.original.url;
                        //_shareGifId = result.data[0].id;

                        //tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                        Debug.Log(tempStr);
                        //m_InputField.text = tempStr;
                        //m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;

                        //Display gif previews
                        if (m_PlayerImages != null)
                        {
                            for (int i = 0; i < m_PlayerImages.Length; i++)
                            {
                                if (result.data.Count >= i + 1)
                                {
                                    //Smaller size animated gifs preview 
                                    PlayGif(result.data[i].images.downsized_large.url, m_PlayerImages[i], "Player" + i);
                                }
                            }
                        }

                        if (m_PlayerRenderers != null)
                        {
                            for (int i = 0; i < m_PlayerRenderers.Length; i++)
                            {
                                if (result.data.Count >= i + 1)
                                {
                                    //Smaller size animated gifs preview 
                                    PlayGif(result.data[i].images.downsized_large.url, m_PlayerRenderers[i], "Player" + i);
                                }
                            }
                        }
                    });
                }
            }
        }

        if (text == "Random Sticker")
        {
            Debug.Log("_GiphyApi > Random Sticker");
            GiphyManager.Instance.Random_Sticker((result) => {
                tempStr = "8. Random_Sticker result: ";
                _shareGifBitlyUrl = result.data.image_url;
                _shareGifFullUrl = result.data.image_original_url;
                _shareGifId = result.data.id;

                tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                Debug.Log(tempStr);
                m_InputField.text = tempStr;
                m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;


                // Display gif previews
                if (m_PlayerImages != null)
                {
                    for (int i = 0; i < m_PlayerImages.Length; i++)
                    {
                        //Smaller size animated gifs preview 
                        PlayGif(result.data.fixed_height_small_url, m_PlayerImages[i], "Player" + i);
                    }
                }
            });
        }

        if (text == "Random Sticker with Tag")
        {
            if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
            {
                Debug.LogWarning("_GiphyApi > Random Sticker with Tag, KeyWord is Empty!");
            }
            else
            {
                Debug.Log("_GiphyApi > Random Sticker with Tag");
                GiphyManager.Instance.Random_Sticker(m_InputFieldKeyWord.text, (result) => {
                    tempStr = "9. Random_Sticker with tag result (Tag = " + m_InputFieldKeyWord.text + "): ";
                    _shareGifBitlyUrl = result.data.image_url;
                    _shareGifFullUrl = result.data.image_original_url;
                    _shareGifId = result.data.id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;


                    // Display gif previews
                    if (m_PlayerImages != null)
                    {
                        for (int i = 0; i < m_PlayerImages.Length; i++)
                        {
                            //Smaller size animated gifs preview 
                            PlayGif(result.data.fixed_height_small_url, m_PlayerImages[i], "Player" + i);
                        }
                    }
                });
            }
        }

        if (text == "Translate Sticker")
        {
            if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
            {
                Debug.LogWarning("_GiphyApi > Translate Sticker, KeyWord is Empty!");
            }
            else
            {
                Debug.Log("Translate Sticker");
                GiphyManager.Instance.Translate_Sticker(m_InputFieldKeyWord.text, (result) => {
                    tempStr = "10. Translate_Sticker result (KeyWord = " + m_InputFieldKeyWord.text + "): ";
                    _shareGifBitlyUrl = result.data.bitly_gif_url;
                    _shareGifFullUrl = result.data.images.original.url;
                    _shareGifId = result.data.id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = GiphyManager.Instance.FullJsonResponseText;


                    // Display gif previews
                    if (m_PlayerImages != null)
                    {
                        for (int i = 0; i < m_PlayerImages.Length; i++)
                        {
                            //Smaller size animated gifs preview 
                            PlayGif(result.data.images.preview_gif.url, m_PlayerImages[i], "Player" + i);
                        }
                    }
                });
            }
        }
        */
    }

    IEnumerator Spawn()
    {
        int counter = 0;
        //Debug.Log(urlList.Count);

        AnimatedGifPlayer _gifPlayer;
        _gifPlayer = gifPrefab.GetComponentInChildren<AnimatedGifPlayer>();

        while (counter < urlList.Count)
        {
            _gifPlayer.FileName = urlList[counter];

            GameObject _gif = Instantiate(
                    gifPrefab,
                    new Vector3(Random.Range(-2f, 2f), transform.position.y + Random.Range(-2f, 2f), Random.Range(-2f, 2f)),
                    RandomQuaternion()
                );
            //AnimatedGifPlayer _gifPlayer = gifPrefab.GetComponentInChildren<AnimatedGifPlayer>();
            Debug.Log(_gifPlayer.FileName);
            _gifPlayer.AutoPlay = true;
            _gif.transform.localScale = new Vector3(1f, widthToHeightRatio[counter], 1f);

            gifs.Add(_gif);
            gifPlayers.Add(_gifPlayer);

            counter++;
            yield return new WaitForSeconds(1f / spawnRate);            
        }
        gifPrefab.GetComponentInChildren<AnimatedGifPlayer>().FileName = "";

        Debug.Log("Done spawning originally stickers. Now spawned the duplicated");

        //StartCoroutine(SpawnCopy());
    }

    IEnumerator SpawnCopy()
    {
        int counter = 0;

        while (counter < gifs.Count)
        {
            for(int i=0; i<9; i++)
            {
                GameObject _gif = Instantiate(
                    copyGifPrefab,
                    new Vector3(Random.Range(-2f, 2f), transform.position.y + Random.Range(-2f, 2f), Random.Range(-2f, 2f)),
                    RandomQuaternion()
                );
                _gif.GetComponentInChildren<Renderer>().material = gifs[counter].GetComponentInChildren<Renderer>().sharedMaterial;
            }           
            counter++;
            yield return new WaitForSeconds(1f / spawnRate / 2f);
        }
        Debug.Log("Done spawning duplicated");
    }

    Quaternion RandomQuaternion()
    {
        return new Quaternion(Random.Range(-1, 1), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
    }
}
