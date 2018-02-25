using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OldMoatGames;
using System;
using System.IO;
using Newtonsoft.Json;

public class GifLoader : MonoBehaviour {

    // GIPHY related (reference ProGif)
    //-------------------------------------------------------------------------------
    // rating
    public enum Rating
    {
        None = 0,
        y,
        g,
        pg,
        pg_13,
        r
    }

    //lang - specify default country for regional content; format is 2-letter ISO 639-1 language code
    public enum Language
    {
        None = 0,
        en, //English
        es, //Spanish
        pt, //Portuguese
        id, //Indonesian
        fr, //French
        ar, //Arabic
        tr, //Turkish
        th, //Thai
        vi, //Vietnamese
        de, //German
        it, //Italian
        ja, //Japanese
        ru, //Russian
        ko, //Korean
        pl, //Polish
        nl, //Dutch
        ro, //Romanian
        hu, //Hungarian
        sv, //Swedish
        cs, //Czech
        hi, //Hindi
        bn, //Bengali
        da, //Danish
        fa, //Farsi
        tl, //Filipino
        fi, //Finnish
        iw, //Hebrew
        ms, //Malay
        no, //Norwegian
        uk, //Ukrainian
        CN, //Chinese Simplified
        TW, //Chinese Traditional
    }
    //-------------------------------------------------------------------------------

    [Header("[Giphy APIs]")]
    public string m_GiphyApiKey = "";       //Your API key associated with your app on Giphy (for NormalGifApi & StickerApi)
    public string m_NormalGifApi = "http://api.giphy.com/v1/gifs";
    public string m_StickerApi = "http://api.giphy.com/v1/stickers";
    public string m_UploadApi = "http://upload.giphy.com/v1/gifs";

    [Header("[Optional-Settings]")]
    public int m_ResultLimit = 10;              //Number of results to return, maximum 100. Default 25
    public int m_ResultOffset = 0;              //Results offset, defaults to 0
    public Rating m_Rating = Rating.None;       //Limit results to those rated GIFs (y,g, pg, pg-13 or r)
    public Language m_Language = Language.None; //Language use with Sticker API call, specify default country for regional content

    //-------------------------------------------------------------------------------

    private bool HasApiKey
    {
        get
        {
            bool hasApiKey = !string.IsNullOrEmpty(m_GiphyApiKey);
            if (!hasApiKey) Debug.LogWarning("Giphy API Key is required!");
            return hasApiKey;
        }
    }

    private string _FullJsonResponseText = "Full Json Response Text";
    public string FullJsonResponseText
    {
        get
        {
            return _FullJsonResponseText;
        }
        set
        {
            string json = value.Replace("\\", "");
            Debug.Log(json);

            _FullJsonResponseText = value;
        }
    }

    public static GifLoader instance = null;

    public GameObject gifPlayerPrefab;

    //-------------------------------------------------------------------------------
    public void Awake()
    {
        if(instance==null)
        {
            instance = this;
        }
        else if(instance!=this)
        {
            Destroy(gameObject);
        }

        /*
        // Set the file to use. File has to be in StreamingAssets folder or a remote url (For example: http://www.example.com/example.gif).
        AnimatedGifPlayer.FileName = "https://media2.giphy.com/media/l0Iyb5cXNjdvT8keY/giphy.gif";      //AnimatedGIFPlayerExampe 3.gif

        // Disable autoplay
        AnimatedGifPlayer.AutoPlay = true;

        // Init the GIF player
        AnimatedGifPlayer.Init();
        */
    }

    //---------------------------- GIPHY API (reference ProGif) ----------------------------------
    //---------------------------- Normal GIF ----------------------------------------------------
    /// <summary>
    /// Returns a GIF given that GIF's unique ID
    /// </summary>
    /// <param name="giphyGifId">Giphy GIF identifier.</param>
    /// <param name="onComplete">On complete.</param>
    public void GetById(string giphyGifId, Action<GiphyGetById.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_GetById(giphyGifId, onComplete));
    }

    IEnumerator _GetById(string giphyGifId, Action<GiphyGetById.Response> onComplete)
    {
        if (!string.IsNullOrEmpty(giphyGifId))
        {
            string url = m_NormalGifApi + "/" + giphyGifId + "?api_key=" + m_GiphyApiKey;

            WWW www = new WWW(url);
            yield return www;

            if (www.error == null)
            {
                FullJsonResponseText = www.text;
                GiphyGetById.Response response = JsonConvert.DeserializeObject<GiphyGetById.Response>(www.text);
                if (onComplete != null) onComplete(response);
            }
            else
            {
                Debug.Log("Error during get by id: " + giphyGifId + ", Error: " + www.error);
            }
            www.Dispose();
            www = null;
        }
        else
        {
            Debug.LogWarning("GIF id is empty!");
        }
    }

    /// <summary>
    /// Returns an array of GIFs given that GIFs' unique IDs
    /// </summary>
    /// <param name="giphyGifIds">Giphy GIF identifiers.</param>
    /// <param name="onComplete">On complete.</param>
    public void GetByIds(List<string> giphyGifIds, Action<GiphyGetByIds.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_GetByIds(giphyGifIds, onComplete));
    }

    IEnumerator _GetByIds(List<string> giphyGifIds, Action<GiphyGetByIds.Response> onComplete)
    {
        string giphyGifIdsStr = "";
        foreach (string id in giphyGifIds)
        {
            if (!string.IsNullOrEmpty(id)) giphyGifIdsStr += id + ",";
        }

        if (!string.IsNullOrEmpty(giphyGifIdsStr))
        {
            giphyGifIdsStr = giphyGifIdsStr.Substring(0, giphyGifIdsStr.Length - 1);

            string url = m_NormalGifApi + "?ids=" + giphyGifIdsStr + "&api_key=" + m_GiphyApiKey;

            WWW www = new WWW(url);
            yield return www;

            if (www.error == null)
            {
                FullJsonResponseText = www.text;
                GiphyGetByIds.Response response = JsonConvert.DeserializeObject<GiphyGetByIds.Response>(www.text);
                if (onComplete != null) onComplete(response);
            }
            else
            {
                Debug.Log("Error during get by ids: " + giphyGifIdsStr + ", Error: " + www.error);
            }
            www.Dispose();
            www = null;
        }
        else
        {
            Debug.LogWarning("GIF ids is empty!");
        }
    }

    /// <summary>
    /// Search all GIPHY GIFs for a word or phrase. Punctuation will be stripped and ignored.
    /// </summary>
    /// <param name="keyWords">Key words.</param>
    /// <param name="onComplete">On complete.</param>
    public void Search(List<string> keyWords, Action<GiphySearch.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Search(keyWords, onComplete));
    }

    IEnumerator _Search(List<string> keyWords, Action<GiphySearch.Response> onComplete)
    {
        string keyWordsStr = "";
        foreach (string k in keyWords)
        {
            keyWordsStr += k + "+";
        }
        keyWordsStr = keyWordsStr.Substring(0, keyWordsStr.Length - 1);

        string url = m_NormalGifApi + "/search?q=" + keyWordsStr + "&api_key=" + m_GiphyApiKey;
        if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
        if (m_ResultOffset > 0) url += "&offset=" + m_ResultOffset;
        if (m_Rating != Rating.None)
        {
            if (m_Rating == Rating.pg_13)
                url += "&rating=pg-13";
            else
                url += "&rating=" + m_Rating.ToString();
        }

        WWW www = new WWW(url);
        yield return www;

        if (www.error == null)
        {
            FullJsonResponseText = www.text;
            GiphySearch.Response response = JsonConvert.DeserializeObject<GiphySearch.Response>(www.text);
            if (onComplete != null) onComplete(response);
        }
        else
        {
            Debug.Log("Error during search: " + www.error);
        }

        www.Dispose();
        www = null;
    }

    /// <summary>
    /// Get a random GIF from Giphy
    /// </summary>
    /// <param name="onComplete">On complete.</param>
    public void Random(Action<GiphyRandom.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Random(null, onComplete));
    }

    /// <summary>
    /// Get a random GIF, limited by tag.
    /// </summary>
    /// <param name="tag">Tag: the GIF tag to limit randomness by</param>
    /// <param name="onComplete">On complete.</param>
    public void Random(string tag, Action<GiphyRandom.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Random(tag, onComplete));
    }

    IEnumerator _Random(string tag, Action<GiphyRandom.Response> onComplete)
    {
        string url = m_NormalGifApi + "/random?api_key=" + m_GiphyApiKey;
        if (!string.IsNullOrEmpty(tag)) url += "&tag=" + tag;
        if (m_Rating != Rating.None)
        {
            if (m_Rating == Rating.pg_13)
                url += "&rating=pg-13";
            else
                url += "&rating=" + m_Rating.ToString();
        }

        WWW www = new WWW(url);
        yield return www;

        if (www.error == null)
        {
            FullJsonResponseText = www.text;
            GiphyRandom.Response response = JsonConvert.DeserializeObject<GiphyRandom.Response>(www.text);
            if (onComplete != null) onComplete(response);
        }
        else
        {
            Debug.Log("Error during Random: " + www.error);
        }

        www.Dispose();
        www = null;
    }

    /// <summary>
    /// The translate API draws on search, but uses the GIPHY special sauce to handle translating from one vocabulary to another. 
    /// In this case, words and phrases to GIFs. The result is Random even for the same term.
    /// </summary>
    /// <param name="term">term.</param>
    /// <param name="onComplete">On complete.</param>
    public void Translate(string term, Action<GiphyTranslate.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Translate(term, onComplete));
    }

    IEnumerator _Translate(string term, Action<GiphyTranslate.Response> onComplete)
    {
        if (!string.IsNullOrEmpty(term))
        {
            string url = m_NormalGifApi + "/translate?api_key=" + m_GiphyApiKey + "&s=" + term;
            if (m_Rating != Rating.None)
            {
                if (m_Rating == Rating.pg_13)
                    url += "&rating=pg-13";
                else
                    url += "&rating=" + m_Rating.ToString();
            }

            WWW www = new WWW(url);
            yield return www;

            if (www.error == null)
            {
                FullJsonResponseText = www.text;
                GiphyTranslate.Response response = JsonConvert.DeserializeObject<GiphyTranslate.Response>(www.text);
                if (onComplete != null) onComplete(response);
            }
            else
            {
                Debug.Log("Error during Translate: " + www.error);
            }

            www.Dispose();
            www = null;
        }
        else
        {
            Debug.LogWarning("Search term is empty!");
        }
    }

    /// <summary>
    /// Fetch GIFs currently trending online. Hand curated by the GIPHY editorial team. 
    /// The data returned mirrors the GIFs showcased on the GIPHY homepage. 
    /// Returns 25 results by default.
    /// </summary>
    /// <param name="onComplete">On complete.</param>
    public void Trending(Action<GiphyTrending.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Trending(onComplete));
    }

    IEnumerator _Trending(Action<GiphyTrending.Response> onComplete)
    {
        string url = m_NormalGifApi + "/trending?api_key=" + m_GiphyApiKey;
        if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
        if (m_ResultOffset > 0) url += "&offset=" + m_ResultOffset;
        if (m_Rating != Rating.None)
        {
            if (m_Rating == Rating.pg_13)
                url += "&rating=pg-13";
            else
                url += "&rating=" + m_Rating.ToString();
        }

        WWW www = new WWW(url);
        yield return www;

        if (www.error == null)
        {
            FullJsonResponseText = www.text;
            GiphyTrending.Response response = JsonConvert.DeserializeObject<GiphyTrending.Response>(www.text);
            if (onComplete != null) onComplete(response);
        }
        else
        {
            Debug.Log("Error during Trending: " + www.error);
        }

        www.Dispose();
        www = null;
    }

    private string _GetLanguageString(Language lang)
    {
        string langStr = "";
        switch (lang)
        {
            case Language.None:
                //Do nothing
                break;

            case Language.CN:
                langStr = "zh-CN";
                break;

            case Language.TW:
                langStr = "zh-TW";
                break;

            default:
                langStr = lang.ToString().ToLower();
                break;
        }
        return langStr;
    }

    //---------------------------- Sticker GIF ----------------------------------------------------
    public void Search_Sticker(List<string> keyWords, Action<GiphyStickerSearch.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Search_Sticker(keyWords, onComplete));
    }

    IEnumerator _Search_Sticker(List<string> keyWords, Action<GiphyStickerSearch.Response> onComplete)
    {
        string keyWordsStr = "";
        foreach (string k in keyWords)
        {
            keyWordsStr += k + "+";
        }
        keyWordsStr = keyWordsStr.Substring(0, keyWordsStr.Length - 1);

        string url = m_StickerApi + "/search?q=" + keyWordsStr + "&api_key=" + m_GiphyApiKey;
        if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
        if (m_ResultOffset > 0) url += "&offset=" + m_ResultOffset;
        if (m_Rating != Rating.None)
        {
            if (m_Rating == Rating.pg_13)
                url += "&rating=pg-13";
            else
                url += "&rating=" + m_Rating.ToString();
        }
        if (m_Language != Language.None) url += "&lang=" + _GetLanguageString(m_Language);

        WWW www = new WWW(url);
        yield return www;

        if (www.error == null)
        {
            FullJsonResponseText = www.text;
            GiphyStickerSearch.Response searchResponse = JsonConvert.DeserializeObject<GiphyStickerSearch.Response>(www.text);
            if (onComplete != null) onComplete(searchResponse);
        }
        else
        {
            Debug.Log("Error during Search_Sticker: " + www.error);
        }

        www.Dispose();
        www = null;
    }

    public void Random_Sticker(Action<GiphyStickerRandom.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Random_Sticker(null, onComplete));
    }

    public void Random_Sticker(string tag, Action<GiphyStickerRandom.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Random_Sticker(tag, onComplete));
    }

    IEnumerator _Random_Sticker(string tag, Action<GiphyStickerRandom.Response> onComplete)
    {
        string url = m_StickerApi + "/random?api_key=" + m_GiphyApiKey;
        if (!string.IsNullOrEmpty(tag)) url += "&tag=" + tag;
        if (m_Rating != Rating.None)
        {
            if (m_Rating == Rating.pg_13)
                url += "&rating=pg-13";
            else
                url += "&rating=" + m_Rating.ToString();
        }

        WWW www = new WWW(url);
        yield return www;

        if (www.error == null)
        {
            FullJsonResponseText = www.text;
            GiphyStickerRandom.Response searchResponse = JsonConvert.DeserializeObject<GiphyStickerRandom.Response>(www.text);
            if (onComplete != null) onComplete(searchResponse);
        }
        else
        {
            Debug.Log("Error during Random_Sticker: " + www.error);
        }

        www.Dispose();
        www = null;
    }

    public void Translate_Sticker(string term, Action<GiphyStickerTranslate.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Translate_Sticker(term, onComplete));
    }

    IEnumerator _Translate_Sticker(string term, Action<GiphyStickerTranslate.Response> onComplete)
    {
        if (!string.IsNullOrEmpty(term))
        {
            string url = m_StickerApi + "/translate?api_key=" + m_GiphyApiKey + "&s=" + term;

            WWW www = new WWW(url);
            yield return www;

            if (www.error == null)
            {
                FullJsonResponseText = www.text;
                GiphyStickerTranslate.Response searchResponse = JsonConvert.DeserializeObject<GiphyStickerTranslate.Response>(www.text);
                if (onComplete != null) onComplete(searchResponse);
            }
            else
            {
                Debug.Log("Error during Translate_Sticker: " + www.error);
            }

            www.Dispose();
            www = null;
        }
        else
        {
            Debug.LogWarning("Search term is empty!");
        }
    }

    public void Trending_Sticker(Action<GiphyStickerTrending.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Trending_Sticker(onComplete));
    }

    IEnumerator _Trending_Sticker(Action<GiphyStickerTrending.Response> onComplete)
    {
        string url = m_StickerApi + "/trending?api_key=" + m_GiphyApiKey;
        if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
        if (m_Rating != Rating.None)
        {
            if (m_Rating == Rating.pg_13)
                url += "&rating=pg-13";
            else
                url += "&rating=" + m_Rating.ToString();
        }
        Debug.Log(url);

        WWW www = new WWW(url);
        yield return www;

        if (www.error == null)
        {
            FullJsonResponseText = www.text;
            GiphyStickerTrending.Response searchResponse = JsonConvert.DeserializeObject<GiphyStickerTrending.Response>(www.text);
            if (onComplete != null) onComplete(searchResponse);
        }
        else
        {
            Debug.Log("Error during Trending_Sticker: " + www.error);
        }

        www.Dispose();
        www = null;
    }

    public void Sticker_Packs(Action<GiphyStickerPacks.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Sticker_Packs(onComplete));
    }

    IEnumerator _Sticker_Packs(Action<GiphyStickerPacks.Response> onComplete)
    {
        string url = m_StickerApi + "/packs?api_key=" + m_GiphyApiKey;
        if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
        if (m_Rating != Rating.None)
        {
            if (m_Rating == Rating.pg_13)
                url += "&rating=pg-13";
            else
                url += "&rating=" + m_Rating.ToString();
        }

        WWW www = new WWW(url);
        yield return www;

        if (www.error == null)
        {
            FullJsonResponseText = www.text;
            GiphyStickerPacks.Response searchResponse = JsonConvert.DeserializeObject<GiphyStickerPacks.Response>(www.text);
            if (onComplete != null) onComplete(searchResponse);
        }
        else
        {
            Debug.Log("Error during Sticker_Packs: " + www.error);
        }

        www.Dispose();
        www = null;
    }

    public void Sticker_Pack_By_Id(int packId, Action<GiphyStickerPackById.Response> onComplete)
    {
        if (!HasApiKey) return;
        StartCoroutine(_Sticker_Pack_By_Id(packId, onComplete));
    }

    IEnumerator _Sticker_Pack_By_Id(int packId, Action<GiphyStickerPackById.Response> onComplete)
    {
        string url = m_StickerApi + "/packs/" + packId + "/stickers?api_key=" + m_GiphyApiKey;

        if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
        if (m_Rating != Rating.None)
        {
            if (m_Rating == Rating.pg_13)
                url += "&rating=pg-13";
            else
                url += "&rating=" + m_Rating.ToString();
        }

        WWW www = new WWW(url);
        yield return www;

        if (www.error == null)
        {
            FullJsonResponseText = www.text;
            GiphyStickerPackById.Response searchResponse = JsonConvert.DeserializeObject<GiphyStickerPackById.Response>(www.text);
            if (onComplete != null) onComplete(searchResponse);
        }
        else
        {
            Debug.Log("Error during Sticker_Pack_By_Id: " + www.error);
        }

        www.Dispose();
        www = null;
    }
}
