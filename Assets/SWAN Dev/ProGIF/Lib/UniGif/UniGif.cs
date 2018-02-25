﻿/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class UniGif
{
	private static List<GifTexture> _tempGifTextures = null;
	public static List<GifTexture> TempGifTextures
	{
		get{
			return _tempGifTextures;
		}
	}

    /// <summary>
    /// Get GIF texture list Coroutine
    /// </summary>
    /// <param name="bytes">GIF file byte data</param>
    /// <param name="callback">Callback method(param is GIF texture list, Animation loop count, GIF image width (px), GIF image height (px))</param>
    /// <param name="filterMode">Textures filter mode</param>
    /// <param name="wrapMode">Textures wrap mode</param>
    /// <param name="debugLog">Debug Log Flag</param>
    /// <returns>IEnumerator</returns>
    public static IEnumerator GetTextureListCoroutine(
        byte[] bytes,
        Action<List<GifTexture>, int, int, int> callback,
        FilterMode filterMode = FilterMode.Bilinear,
        TextureWrapMode wrapMode = TextureWrapMode.Clamp,
		bool debugLog = false, Action<GifTexture> onFrameReady = null, Action<int> getTotalFrame = null)
    {
        int loopCount = -1;
        int width = 0;
        int height = 0;

        // Set GIF data
        var gifData = new GifData();
        if (SetGifData(bytes, ref gifData, debugLog) == false)
        {
            Debug.LogError("GIF file data set error.");
            if (callback != null)
            {
                callback(null, loopCount, width, height);
            }
            yield break;
        }

		if(getTotalFrame != null)
		{
			int totalFrame = gifData.m_imageBlockList.Count;
			getTotalFrame(totalFrame);
		}

        // Decode to textures from GIF data
		yield return DecodeTextureCoroutine(gifData, result => _tempGifTextures = result, filterMode, wrapMode, onFrameReady);

        if (_tempGifTextures == null || _tempGifTextures.Count <= 0)
        {
            Debug.LogError("GIF texture decode error.");
            if (callback != null)
            {
                callback(null, loopCount, width, height);
            }
            yield break;
        }

        loopCount = gifData.m_appEx.loopCount;
        width = gifData.m_logicalScreenWidth;
        height = gifData.m_logicalScreenHeight;

        if (callback != null)
        {
            callback(_tempGifTextures, loopCount, width, height);
        }

        yield break;
    }
}