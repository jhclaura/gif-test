using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public class ProGifDecoder
{
	public List<GifTexture> gifTexList = null;

	private List<GifTexture> _tempGifTextures = null;
	public List<GifTexture> TempGifTextures
	{
		get{
			return _tempGifTextures;
		}
	}

	//Decode settings
	private ProGifPlayerComponent.DecodeMode decodeMode = ProGifPlayerComponent.DecodeMode.Normal;
	private int targetDecodeFrameNum = -1;		//if targetDecodeFrameNum <= 0: decode & play all frames (+/- 1 frame)
	private float previewFrameSkipNum = 0f;

	public void SetDecodeSettings(ProGifPlayerComponent.DecodeMode inDecodeMode, int inMaxDecodeFrameCount)
	{
		decodeMode = inDecodeMode;
		targetDecodeFrameNum = inMaxDecodeFrameCount;
	}

	/// <summary>
	/// Decode to textures from GIF data
	/// </summary>
	/// <param name="gifData">GIF data</param>
	/// <param name="callback">Callback method(param is GIF texture list)</param>
	/// <param name="filterMode">Textures filter mode</param>
	/// <param name="wrapMode">Textures wrap mode</param>
	/// <returns>IEnumerator</returns>
	private IEnumerator DecodeTextureCoroutine(GifData gifData, Action<List<GifTexture>> callback, 
		FilterMode filterMode, TextureWrapMode wrapMode, Action<GifTexture> onFrameReady = null)
	{
		if (gifData.m_imageBlockList == null || gifData.m_imageBlockList.Count < 1)
		{
			yield break;
		}

		gifTexList = new List<GifTexture>();

		Color32? bgColor = GetGlobalBgColor(gifData);

		// Disposal Method
		// 0 (No disposal specified)
		// 1 (Do not dispose)
		// 2 (Restore to background color)
		// 3 (Restore to previous)
		ushort disposalMethod = 0;

		int imgBlockIndex = 0;

		//-- Advanced mode ------
		List<int> decodeFrames = new List<int>();
		if(decodeMode == ProGifPlayerComponent.DecodeMode.Advanced)
		{
			if(targetDecodeFrameNum > gifData.m_imageBlockList.Count || targetDecodeFrameNum <= 0) targetDecodeFrameNum = gifData.m_imageBlockList.Count;
			previewFrameSkipNum = (targetDecodeFrameNum > 0)? (float)gifData.m_imageBlockList.Count/(float)(targetDecodeFrameNum):0;

			float frameIndexF = 0f;
			for (int i = 0; i < gifData.m_imageBlockList.Count; i++)
			{
				if((int)frameIndexF < gifData.m_imageBlockList.Count) decodeFrames.Add((int)frameIndexF);
				frameIndexF += previewFrameSkipNum;
			}
		}
		//-- Advanced mode ------

		for (int i = 0; i < gifData.m_imageBlockList.Count; i++)
		{
			bool doDecode = true;

			//-- Advanced mode ------
			if(decodeMode == ProGifPlayerComponent.DecodeMode.Advanced)
			{
				doDecode = (decodeFrames.Contains(i));
			}
			//-- Advanced mode ------

			if(doDecode)
			{
				byte[] decodedData = GetDecodedData(gifData.m_imageBlockList[i]);

				List<byte[]> colorTable = GetColorTable(gifData, gifData.m_imageBlockList[i], ref bgColor);

				GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, imgBlockIndex);

				int transparentIndex = GetTransparentIndex(graphicCtrlEx);

				yield return 0;
				Texture2D tex = CreateTexture2D(gifData, gifTexList, imgBlockIndex, disposalMethod, filterMode, wrapMode, i, decodedData, colorTable, bgColor, transparentIndex);
				yield return 0;

				float delaySec = GetDelaySec(graphicCtrlEx);

				//Advanced mode
				if(decodeMode == ProGifPlayerComponent.DecodeMode.Advanced && previewFrameSkipNum > 0)
				{
					//Retain the origin playing speed
					delaySec *= previewFrameSkipNum;
				}

				// Add to GIF texture list
				GifTexture gTex = new GifTexture(tex, delaySec);
				gifTexList.Add(gTex);
				if(onFrameReady != null)
				{
					onFrameReady(gTex);
				}

				disposalMethod = GetDisposalMethod(graphicCtrlEx);

				imgBlockIndex++;
			}
		}

		if (callback != null)
		{
			callback(gifTexList);
		}

		yield break;
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
	public IEnumerator GetTextureListCoroutine(
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

	/// <summary>
	/// GIF Data Format
	/// </summary>
	private struct GifData
	{
		// Signature
		public byte m_sig0, m_sig1, m_sig2;
		// Version
		public byte m_ver0, m_ver1, m_ver2;
		// Logical Screen Width
		public ushort m_logicalScreenWidth;
		// Logical Screen Height
		public ushort m_logicalScreenHeight;
		// Global Color Table Flag
		public bool m_globalColorTableFlag;
		// Color Resolution
		public int m_colorResolution;
		// Sort Flag
		public bool m_sortFlag;
		// Size of Global Color Table
		public int m_sizeOfGlobalColorTable;
		// Background Color Index
		public byte m_bgColorIndex;
		// Pixel Aspect Ratio
		public byte m_pixelAspectRatio;
		// Global Color Table
		public List<byte[]> m_globalColorTable;
		// ImageBlock
		public List<ImageBlock> m_imageBlockList;
		// GraphicControlExtension
		public List<GraphicControlExtension> m_graphicCtrlExList;
		// Comment Extension
		public List<CommentExtension> m_commentExList;
		// Plain Text Extension
		public List<PlainTextExtension> m_plainTextExList;
		// Application Extension
		public ApplicationExtension m_appEx;
		// Trailer
		public byte m_trailer;

		public string signature
		{
			get
			{
				char[] c = { (char)m_sig0, (char)m_sig1, (char)m_sig2 };
				return new string(c);
			}
		}

		public string version
		{
			get
			{
				char[] c = { (char)m_ver0, (char)m_ver1, (char)m_ver2 };
				return new string(c);
			}
		}

		public void Dump()
		{
			Debug.Log("GIF Type: " + signature + "-" + version);
			Debug.Log("Image Size: " + m_logicalScreenWidth + "x" + m_logicalScreenHeight);
			Debug.Log("Animation Image Count: " + m_imageBlockList.Count);
			Debug.Log("Animation Loop Count (0 is infinite): " + m_appEx.loopCount);
			if (m_graphicCtrlExList != null && m_graphicCtrlExList.Count > 0)
			{
				var sb = new StringBuilder("Animation Delay Time (1/100sec)");
				for (int i = 0; i < m_graphicCtrlExList.Count; i++)
				{
					sb.Append(", ");
					sb.Append(m_graphicCtrlExList[i].m_delayTime);
				}
				Debug.Log(sb.ToString());
			}
			Debug.Log("Application Identifier: " + m_appEx.applicationIdentifier);
			Debug.Log("Application Authentication Code: " + m_appEx.applicationAuthenticationCode);
		}
	}

	/// <summary>
	/// Image Block
	/// </summary>
	private struct ImageBlock
	{
		// Image Separator
		public byte m_imageSeparator;
		// Image Left Position
		public ushort m_imageLeftPosition;
		// Image Top Position
		public ushort m_imageTopPosition;
		// Image Width
		public ushort m_imageWidth;
		// Image Height
		public ushort m_imageHeight;
		// Local Color Table Flag
		public bool m_localColorTableFlag;
		// Interlace Flag
		public bool m_interlaceFlag;
		// Sort Flag
		public bool m_sortFlag;
		// Size of Local Color Table
		public int m_sizeOfLocalColorTable;
		// Local Color Table
		public List<byte[]> m_localColorTable;
		// LZW Minimum Code Size
		public byte m_lzwMinimumCodeSize;
		// Block Size & Image Data List
		public List<ImageDataBlock> m_imageDataList;

		public struct ImageDataBlock
		{
			// Block Size
			public byte m_blockSize;
			// Image Data
			public byte[] m_imageData;
		}
	}

	/// <summary>
	/// Graphic Control Extension
	/// </summary>
	private struct GraphicControlExtension
	{
		// Extension Introducer
		public byte m_extensionIntroducer;
		// Graphic Control Label
		public byte m_graphicControlLabel;
		// Block Size
		public byte m_blockSize;
		// Disposal Mothod
		public ushort m_disposalMethod;
		// Transparent Color Flag
		public bool m_transparentColorFlag;
		// Delay Time
		public ushort m_delayTime;
		// Transparent Color Index
		public byte m_transparentColorIndex;
		// Block Terminator
		public byte m_blockTerminator;
	}

	/// <summary>
	/// Comment Extension
	/// </summary>
	private struct CommentExtension
	{
		// Extension Introducer
		public byte m_extensionIntroducer;
		// Comment Label
		public byte m_commentLabel;
		// Block Size & Comment Data List
		public List<CommentDataBlock> m_commentDataList;

		public struct CommentDataBlock
		{
			// Block Size
			public byte m_blockSize;
			// Image Data
			public byte[] m_commentData;
		}
	}

	/// <summary>
	/// Plain Text Extension
	/// </summary>
	private struct PlainTextExtension
	{
		// Extension Introducer
		public byte m_extensionIntroducer;
		// Plain Text Label
		public byte m_plainTextLabel;
		// Block Size
		public byte m_blockSize;
		// Block Size & Plain Text Data List
		public List<PlainTextDataBlock> m_plainTextDataList;

		public struct PlainTextDataBlock
		{
			// Block Size
			public byte m_blockSize;
			// Plain Text Data
			public byte[] m_plainTextData;
		}
	}

	/// <summary>
	/// Application Extension
	/// </summary>
	private struct ApplicationExtension
	{
		// Extension Introducer
		public byte m_extensionIntroducer;
		// Extension Label
		public byte m_extensionLabel;
		// Block Size
		public byte m_blockSize;
		// Application Identifier
		public byte m_appId1, m_appId2, m_appId3, m_appId4, m_appId5, m_appId6, m_appId7, m_appId8;
		// Application Authentication Code
		public byte m_appAuthCode1, m_appAuthCode2, m_appAuthCode3;
		// Block Size & Application Data List
		public List<ApplicationDataBlock> m_appDataList;

		public struct ApplicationDataBlock
		{
			// Block Size
			public byte m_blockSize;
			// Application Data
			public byte[] m_applicationData;
		}

		public string applicationIdentifier
		{
			get
			{
				char[] c = { (char)m_appId1, (char)m_appId2, (char)m_appId3, (char)m_appId4, (char)m_appId5, (char)m_appId6, (char)m_appId7, (char)m_appId8 };
				return new string(c);
			}
		}

		public string applicationAuthenticationCode
		{
			get
			{
				char[] c = { (char)m_appAuthCode1, (char)m_appAuthCode2, (char)m_appAuthCode3 };
				return new string(c);
			}
		}

		public int loopCount
		{
			get
			{
				if (m_appDataList == null || m_appDataList.Count < 1 ||
					m_appDataList[0].m_applicationData.Length < 3 ||
					m_appDataList[0].m_applicationData[0] != 0x01)
				{
					return 0;
				}
				return BitConverter.ToUInt16(m_appDataList[0].m_applicationData, 1);
			}
		}
	}


	#region Call from DecodeTexture methods

	/// <summary>
	/// Get background color from global color table
	/// </summary>
	private Color32? GetGlobalBgColor(GifData gifData)
	{
		Color32? bgColor = null;
		if (gifData.m_globalColorTableFlag)
		{
			// Set background color from global color table
			byte[] bgRgb = gifData.m_globalColorTable[gifData.m_bgColorIndex];
			bgColor = new Color32(bgRgb[0], bgRgb[1], bgRgb[2], 255);
		}
		return bgColor;
	}

	/// <summary>
	/// Get decoded image data from ImageBlock
	/// </summary>
	private byte[] GetDecodedData(ImageBlock imgBlock)
	{
		// Combine LZW compressed data
		List<byte> lzwData = new List<byte>();
		for (int i = 0; i < imgBlock.m_imageDataList.Count; i++)
		{
			for (int k = 0; k < imgBlock.m_imageDataList[i].m_imageData.Length; k++)
			{
				lzwData.Add(imgBlock.m_imageDataList[i].m_imageData[k]);
			}
		}

		// LZW decode
		int needDataSize = imgBlock.m_imageHeight * imgBlock.m_imageWidth;
		byte[] decodedData = DecodeGifLZW(lzwData, imgBlock.m_lzwMinimumCodeSize, needDataSize);

		// Sort interlace GIF
		if (imgBlock.m_interlaceFlag)
		{
			decodedData = SortInterlaceGifData(decodedData, imgBlock.m_imageWidth);
		}
		return decodedData;
	}

	/// <summary>
	/// Get color table (local or global)
	/// </summary>
	private List<byte[]> GetColorTable(GifData gifData, ImageBlock imgBlock, ref Color32? bgColor)
	{
		List<byte[]> colorTable = imgBlock.m_localColorTableFlag ? imgBlock.m_localColorTable : gifData.m_globalColorTable;

		if (imgBlock.m_localColorTableFlag)
		{
			// Set background color from local color table
			byte[] bgRgb = (gifData.m_bgColorIndex >= imgBlock.m_localColorTable.Count)? 
				imgBlock.m_localColorTable[imgBlock.m_localColorTable.Count-1]:imgBlock.m_localColorTable[gifData.m_bgColorIndex];
			bgColor = new Color32(bgRgb[0], bgRgb[1], bgRgb[2], 255);
		}

		return colorTable;
	}

	/// <summary>
	/// Get GraphicControlExtension from GifData
	/// </summary>
	private GraphicControlExtension? GetGraphicCtrlExt(GifData gifData, int imgBlockIndex)
	{
		if (gifData.m_graphicCtrlExList != null && gifData.m_graphicCtrlExList.Count > imgBlockIndex)
		{
			return gifData.m_graphicCtrlExList[imgBlockIndex];
		}
		return null;
	}

	/// <summary>
	/// Get transparent color index from GraphicControlExtension
	/// </summary>
	private int GetTransparentIndex(GraphicControlExtension? graphicCtrlEx)
	{
		int transparentIndex = -1;
		if (graphicCtrlEx != null && graphicCtrlEx.Value.m_transparentColorFlag)
		{
			transparentIndex = graphicCtrlEx.Value.m_transparentColorIndex;
		}
		return transparentIndex;
	}

	/// <summary>
	/// Get delay seconds from GraphicControlExtension
	/// </summary>
	private float GetDelaySec(GraphicControlExtension? graphicCtrlEx)
	{
		// Get delay sec from GraphicControlExtension
		float delaySec = graphicCtrlEx != null ? graphicCtrlEx.Value.m_delayTime / 100f : (1f / 60f);
		if (delaySec <= 0f)
		{
			delaySec = 0.1f;
		}
		return delaySec;
	}

	/// <summary>
	/// Get disposal method from GraphicControlExtension
	/// </summary>
	private ushort GetDisposalMethod(GraphicControlExtension? graphicCtrlEx)
	{
		return graphicCtrlEx != null ? graphicCtrlEx.Value.m_disposalMethod : (ushort)2;
	}

	/// <summary>
	/// Create Texture2D object and initial settings
	/// </summary>
	private Texture2D CreateTexture2D(GifData gifData, List<GifTexture> gifTexList, int imgBlockIndex, ushort disposalMethod, FilterMode filterMode, TextureWrapMode wrapMode,
				int a_imageIndex, byte[] a_decodedData, List<byte[]> a_colorTable, Color32? a_bgColor, int a_transparentIndex)
	{
		// Create texture
		Texture2D tex = new Texture2D(gifData.m_logicalScreenWidth, gifData.m_logicalScreenHeight, TextureFormat.ARGB32, false);
		tex.filterMode = filterMode;
		tex.wrapMode = wrapMode;

		// Check dispose
		bool useBeforeTex = false;
		int beforeIndex = -1;
		if (imgBlockIndex > 0 && disposalMethod == 0 || disposalMethod == 1)
		{
			// before 1
			beforeIndex = imgBlockIndex - 1;
		}
		else if (imgBlockIndex > 1 && disposalMethod == 3)
		{
			// before 2
			beforeIndex = imgBlockIndex - 2;
		}

		Color32[] pix;
		if(beforeIndex >= 0)
		{
			// Do not dispose
			useBeforeTex = true;
			pix = gifTexList[beforeIndex].m_texture2d.GetPixels32();
		}
		else
		{
			//pix = tex.GetPixels32();
			pix = new Color32[tex.width * tex.height];
		}

		// Set pixel data
		int dataIndex = 0;
		// Reverse set pixels. because GIF data starts from the top left.
		for (int y = tex.height - 1; y >= 0; y--)
		{
			SetTexturePixelRow(ref pix, tex.width, tex.height, y, gifData.m_imageBlockList[a_imageIndex], a_decodedData, ref dataIndex, a_colorTable, a_bgColor, a_transparentIndex, true);
		}
		tex.SetPixels32(pix);
		tex.Apply();

		return tex;
	}

	/// <summary>
	/// Set texture pixel row
	/// </summary>
	private void SetTexturePixelRow(ref Color32[] pixels, int texWidth, int texHeight, int y, ImageBlock imgBlock, byte[] decodedData, ref int dataIndex, List<byte[]> colorTable, Color32? bgColor, int transparentIndex, bool useBeforeTex)
	{
		// Row no (0~)
		int row = texHeight - 1 - y;

		for (int x = 0; x < texWidth; x++)
		{
			// Line no (0~)
			int line = x;

			// Out of image blocks
			if (row < imgBlock.m_imageTopPosition ||
				row >= imgBlock.m_imageTopPosition + imgBlock.m_imageHeight ||
				line < imgBlock.m_imageLeftPosition ||
				line >= imgBlock.m_imageLeftPosition + imgBlock.m_imageWidth)
			{
				if (useBeforeTex == false && bgColor != null)
				{
					pixels[x + y * texWidth] = bgColor.Value;
				}
				continue;
			}

			// Out of decoded data
			if (dataIndex >= decodedData.Length)
			{
				pixels[x + y * texWidth] = Color.black;
				if (dataIndex == decodedData.Length)
				{
					#if UNITY_EDITOR
//					Debug.LogError("dataIndex exceeded the size of decodedData. dataIndex:" + dataIndex + " decodedData.Length:" + decodedData.Length + " y:" + y + " x:" + x);
					#endif
				}
				dataIndex++;
				continue;
			}

			// Get pixel color from color table
			byte colorIndex = decodedData[dataIndex];
			if (colorTable == null || colorTable.Count <= colorIndex)
			{
				#if UNITY_EDITOR
//				Debug.LogError("colorIndex exceeded the size of colorTable. colorTable.Count:" + colorTable.Count + " colorIndex:" + colorIndex);
				#endif
				dataIndex++;
				continue;
			}
			byte[] rgb = colorTable[colorIndex];

			// Set alpha
			byte alpha = transparentIndex >= 0 && transparentIndex == colorIndex ? (byte)0 : (byte)255;

			if (alpha != 0 || useBeforeTex == false)
			{
				Color32 col = new Color32(rgb[0], rgb[1], rgb[2], alpha);
				pixels[x + y * texWidth] = col;
			}

			dataIndex++;
		}
	}

	#endregion

	#region Decode LZW & Sort interrace methods

	/// <summary>
	/// GIF LZW decode
	/// </summary>
	/// <param name="compData">LZW compressed data</param>
	/// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
	/// <param name="needDataSize">Need decoded data size</param>
	/// <returns>Decoded data array</returns>
	private byte[] DecodeGifLZW(List<byte> compData, int lzwMinimumCodeSize, int needDataSize)
	{
		int clearCode = 0;
		int finishCode = 0;

		// Initialize dictionary
		Dictionary<int, string> dic = new Dictionary<int, string>();
		int lzwCodeSize = 0;
		InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);
		int dicCount = dic.Count;

		// Convert to bit array
		byte[] compDataArr = compData.ToArray();
		BitArray bitData = new BitArray(compDataArr);

		byte[] output = new byte[needDataSize];
		int outputAddIndex = 0;

		string prevEntry = null;

		bool dicInitFlag = false;

		int bitDataIndex = 0;

		// LZW decode loop
		while (bitDataIndex < bitData.Length)
		{
			if (dicInitFlag)
			{
				InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);
				dicCount = dic.Count;
				dicInitFlag = false;
			}

			//int key = bitData.GetNumeral(bitDataIndex, lzwCodeSize);
			int key = GetNumeral(bitData, bitDataIndex, lzwCodeSize);

			string entry = null;

			if (key == clearCode)
			{
				// Clear (Initialize dictionary)
				dicInitFlag = true;
				bitDataIndex += lzwCodeSize;
				prevEntry = null;
				continue;
			}
			else if (key == finishCode)
			{
				// Exit
				//Debug.LogWarning("early stop code. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
				break;
			}
			else if (dic.ContainsKey(key))
			{
				// Output from dictionary
				entry = dic[key];
			}
			else if (key >= dicCount)
			{
				if (prevEntry != null)
				{
					// Output from estimation
					entry = prevEntry + prevEntry[0];
				}
				else
				{
					Debug.LogWarningFormat("It is strange that come here. bitDataIndex:{0} lzwCodeSize:{1} key:{2} dic.Count:{3}", bitDataIndex, lzwCodeSize, key, dic.Count);
					bitDataIndex += lzwCodeSize;
					continue;
				}
			}
			else
			{
				Debug.LogWarningFormat("It is strange that come here. bitDataIndex:{0} lzwCodeSize:{1} key:{2} dic.Count:{3}", bitDataIndex, lzwCodeSize, key, dic.Count);
				bitDataIndex += lzwCodeSize;
				continue;
			}

			// Output
			// Take out 8 bits from the string.
			byte[] temp = Encoding.Unicode.GetBytes(entry);
			for (int i = 0; i < temp.Length; i++)
			{
				if (i % 2 == 0)
				{
					output[outputAddIndex] = temp[i];
					outputAddIndex++;
				}
			}

			if (outputAddIndex >= needDataSize)
			{
				// Exit
				break;
			}

			if (prevEntry != null)
			{
				// Add to dictionary
				dic.Add(dicCount, prevEntry + entry[0]);
				++dicCount;
			}

			prevEntry = entry;

			bitDataIndex += lzwCodeSize;

			if (lzwCodeSize == 3 && dicCount >= 8)
			{
				lzwCodeSize = 4;
			}
			else if (lzwCodeSize == 4 && dicCount >= 16)
			{
				lzwCodeSize = 5;
			}
			else if (lzwCodeSize == 5 && dicCount >= 32)
			{
				lzwCodeSize = 6;
			}
			else if (lzwCodeSize == 6 && dicCount >= 64)
			{
				lzwCodeSize = 7;
			}
			else if (lzwCodeSize == 7 && dicCount >= 128)
			{
				lzwCodeSize = 8;
			}
			else if (lzwCodeSize == 8 && dicCount >= 256)
			{
				lzwCodeSize = 9;
			}
			else if (lzwCodeSize == 9 && dicCount >= 512)
			{
				lzwCodeSize = 10;
			}
			else if (lzwCodeSize == 10 && dicCount >= 1024)
			{
				lzwCodeSize = 11;
			}
			else if (lzwCodeSize == 11 && dicCount >= 2048)
			{
				lzwCodeSize = 12;
			}
			else if (lzwCodeSize == 12 && dicCount >= 4096)
			{
				int nextKey = GetNumeral(bitData, bitDataIndex, lzwCodeSize);
				if (nextKey != clearCode)
				{
					dicInitFlag = true;
				}
			}
		}
		return output;
	}


	/// <summary>
	/// Initialize dictionary
	/// </summary>
	/// <param name="dic">Dictionary</param>
	/// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
	/// <param name="lzwCodeSize">out LZW code size</param>
	/// <param name="clearCode">out Clear code</param>
	/// <param name="finishCode">out Finish code</param>
	private void InitDictionary(Dictionary<int, string> dic, int lzwMinimumCodeSize, out int lzwCodeSize, out int clearCode, out int finishCode)
	{
		int dicLength = (int)Math.Pow(2, lzwMinimumCodeSize);

		clearCode = dicLength;
		finishCode = clearCode + 1;

		dic.Clear();

		for (int i = 0; i < dicLength + 2; i++)
		{
			dic.Add(i, ((char)i).ToString());
		}

		lzwCodeSize = lzwMinimumCodeSize + 1;
	}

	/// <summary>
	/// Sort interlace GIF data
	/// </summary>
	/// <param name="decodedData">Decoded GIF data</param>
	/// <param name="xNum">Pixel number of horizontal row</param>
	/// <returns>Sorted data</returns>
	private byte[] SortInterlaceGifData(byte[] decodedData, int xNum)
	{
		int rowNo = 0;
		int dataIndex = 0;
		var newArr = new byte[decodedData.Length];
		// Every 8th. row, starting with row 0.
		for (int i = 0; i < newArr.Length; i++)
		{
			if (rowNo % 8 == 0)
			{
				newArr[i] = decodedData[dataIndex];
				dataIndex++;
			}
			if (i != 0 && i % xNum == 0)
			{
				rowNo++;
			}
		}
		rowNo = 0;
		// Every 8th. row, starting with row 4.
		for (int i = 0; i < newArr.Length; i++)
		{
			if (rowNo % 8 == 4)
			{
				newArr[i] = decodedData[dataIndex];
				dataIndex++;
			}
			if (i != 0 && i % xNum == 0)
			{
				rowNo++;
			}
		}
		rowNo = 0;
		// Every 4th. row, starting with row 2.
		for (int i = 0; i < newArr.Length; i++)
		{
			if (rowNo % 4 == 2)
			{
				newArr[i] = decodedData[dataIndex];
				dataIndex++;
			}
			if (i != 0 && i % xNum == 0)
			{
				rowNo++;
			}
		}
		rowNo = 0;
		// Every 2nd. row, starting with row 1.
		for (int i = 0; i < newArr.Length; i++)
		{
			if (rowNo % 8 != 0 && rowNo % 8 != 4 && rowNo % 4 != 2)
			{
				newArr[i] = decodedData[dataIndex];
				dataIndex++;
			}
			if (i != 0 && i % xNum == 0)
			{
				rowNo++;
			}
		}

		return newArr;
	}
	#endregion

	/// <summary>
	/// Set GIF data
	/// </summary>
	/// <param name="gifBytes">GIF byte data</param>
	/// <param name="gifData">ref GIF data</param>
	/// <param name="debugLog">Debug log flag</param>
	/// <returns>Result</returns>
	private bool SetGifData(byte[] gifBytes, ref GifData gifData, bool debugLog)
	{
		if (debugLog)
		{
			Debug.Log("SetGifData Start.");
		}

		if (gifBytes == null || gifBytes.Length <= 0)
		{
			Debug.LogError("bytes is nothing.");
			return false;
		}

		int byteIndex = 0;

		if (SetGifHeader(gifBytes, ref byteIndex, ref gifData) == false)
		{
			Debug.LogError("GIF header set error.");
			return false;
		}

		if (SetGifBlock(gifBytes, ref byteIndex, ref gifData) == false)
		{
			Debug.LogError("GIF block set error.");
			return false;
		}

		if (debugLog)
		{
			gifData.Dump();
			Debug.Log("SetGifData Finish.");
		}
		return true;
	}

	private bool SetGifHeader(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
	{
		// Signature(3 Bytes)
		// 0x47 0x49 0x46 (GIF)
		if (gifBytes[0] != 'G' || gifBytes[1] != 'I' || gifBytes[2] != 'F')
		{
			Debug.LogError("This is not GIF image.");
			return false;
		}
		gifData.m_sig0 = gifBytes[0];
		gifData.m_sig1 = gifBytes[1];
		gifData.m_sig2 = gifBytes[2];

		// Version(3 Bytes)
		// 0x38 0x37 0x61 (87a) or 0x38 0x39 0x61 (89a)
		if ((gifBytes[3] != '8' || gifBytes[4] != '7' || gifBytes[5] != 'a') &&
			(gifBytes[3] != '8' || gifBytes[4] != '9' || gifBytes[5] != 'a'))
		{
			Debug.LogError("GIF version error.\nSupported only GIF87a or GIF89a.");
			return false;
		}
		gifData.m_ver0 = gifBytes[3];
		gifData.m_ver1 = gifBytes[4];
		gifData.m_ver2 = gifBytes[5];

		// Logical Screen Width(2 Bytes)
		gifData.m_logicalScreenWidth = BitConverter.ToUInt16(gifBytes, 6);

		// Logical Screen Height(2 Bytes)
		gifData.m_logicalScreenHeight = BitConverter.ToUInt16(gifBytes, 8);

		// 1 Byte
		{
			// Global Color Table Flag(1 Bit)
			gifData.m_globalColorTableFlag = (gifBytes[10] & 128) == 128; // 0b10000000

			// Color Resolution(3 Bits)
			switch (gifBytes[10] & 112)
			{
			case 112: // 0b01110000
				gifData.m_colorResolution = 8;
				break;
			case 96: // 0b01100000
				gifData.m_colorResolution = 7;
				break;
			case 80: // 0b01010000
				gifData.m_colorResolution = 6;
				break;
			case 64: // 0b01000000
				gifData.m_colorResolution = 5;
				break;
			case 48: // 0b00110000
				gifData.m_colorResolution = 4;
				break;
			case 32: // 0b00100000
				gifData.m_colorResolution = 3;
				break;
			case 16: // 0b00010000
				gifData.m_colorResolution = 2;
				break;
			default:
				gifData.m_colorResolution = 1;
				break;
			}

			// Sort Flag(1 Bit)
			gifData.m_sortFlag = (gifBytes[10] & 8) == 8; // 0b00001000

			// Size of Global Color Table(3 Bits)
			int val = (gifBytes[10] & 7) + 1;
			gifData.m_sizeOfGlobalColorTable = (int)Math.Pow(2, val);
		}

		// Background Color Index(1 Byte)
		gifData.m_bgColorIndex = gifBytes[11];

		// Pixel Aspect Ratio(1 Byte)
		gifData.m_pixelAspectRatio = gifBytes[12];

		byteIndex = 13;
		if (gifData.m_globalColorTableFlag)
		{
			// Global Color Table(0～255×3 Bytes)
			gifData.m_globalColorTable = new List<byte[]>();
			for (int i = byteIndex; i < byteIndex + (gifData.m_sizeOfGlobalColorTable * 3); i += 3)
			{
				gifData.m_globalColorTable.Add(new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
			}
			byteIndex = byteIndex + (gifData.m_sizeOfGlobalColorTable * 3);
		}

		return true;
	}

	private bool SetGifBlock(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
	{
		try
		{
			int lastIndex = 0;
			while (true)
			{
				int nowIndex = byteIndex;

				if (gifBytes[nowIndex] == 0x2c)
				{
					// Image Block(0x2c)
					SetImageBlock(gifBytes, ref byteIndex, ref gifData);

				}
				else if (gifBytes[nowIndex] == 0x21)
				{
					// Extension
					switch (gifBytes[nowIndex + 1])
					{
					case 0xf9:
						// Graphic Control Extension(0x21 0xf9)
						SetGraphicControlExtension(gifBytes, ref byteIndex, ref gifData);
						break;
					case 0xfe:
						// Comment Extension(0x21 0xfe)
						SetCommentExtension(gifBytes, ref byteIndex, ref gifData);
						break;
					case 0x01:
						// Plain Text Extension(0x21 0x01)
						SetPlainTextExtension(gifBytes, ref byteIndex, ref gifData);
						break;
					case 0xff:
						// Application Extension(0x21 0xff)
						SetApplicationExtension(gifBytes, ref byteIndex, ref gifData);
						break;
					default:
						break;
					}
				}
				else if (gifBytes[nowIndex] == 0x3b)
				{
					// Trailer(1 Byte)
					gifData.m_trailer = gifBytes[byteIndex];
					byteIndex++;
					break;
				}

				if (lastIndex == nowIndex)
				{
					Debug.LogError("Infinite loop error.");
					return false;
				}

				lastIndex = nowIndex;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message);
			return false;
		}

		return true;
	}

	private void SetImageBlock(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
	{
		ImageBlock ib = new ImageBlock();

		// Image Separator(1 Byte)
		// 0x2c
		ib.m_imageSeparator = gifBytes[byteIndex];
		byteIndex++;

		// Image Left Position(2 Bytes)
		ib.m_imageLeftPosition = BitConverter.ToUInt16(gifBytes, byteIndex);
		byteIndex += 2;

		// Image Top Position(2 Bytes)
		ib.m_imageTopPosition = BitConverter.ToUInt16(gifBytes, byteIndex);
		byteIndex += 2;

		// Image Width(2 Bytes)
		ib.m_imageWidth = BitConverter.ToUInt16(gifBytes, byteIndex);
		byteIndex += 2;

		// Image Height(2 Bytes)
		ib.m_imageHeight = BitConverter.ToUInt16(gifBytes, byteIndex);
		byteIndex += 2;

		// 1 Byte
		{
			// Local Color Table Flag(1 Bit)
			ib.m_localColorTableFlag = (gifBytes[byteIndex] & 128) == 128; // 0b10000000

			// Interlace Flag(1 Bit)
			ib.m_interlaceFlag = (gifBytes[byteIndex] & 64) == 64; // 0b01000000

			// Sort Flag(1 Bit)
			ib.m_sortFlag = (gifBytes[byteIndex] & 32) == 32; // 0b00100000

			// Reserved(2 Bits)
			// Unused

			// Size of Local Color Table(3 Bits)
			int val = (gifBytes[byteIndex] & 7) + 1;
			ib.m_sizeOfLocalColorTable = (int)Math.Pow(2, val);

			byteIndex++;
		}

		if (ib.m_localColorTableFlag)
		{
			// Local Color Table(0～255×3 Bytes)
			ib.m_localColorTable = new List<byte[]>();
			for (int i = byteIndex; i < byteIndex + (ib.m_sizeOfLocalColorTable * 3); i += 3)
			{
				ib.m_localColorTable.Add(new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
			}
			byteIndex = byteIndex + (ib.m_sizeOfLocalColorTable * 3);
		}

		// LZW Minimum Code Size(1 Byte)
		ib.m_lzwMinimumCodeSize = gifBytes[byteIndex];
		byteIndex++;

		// Block Size & Image Data List
		while (true)
		{
			// Block Size(1 Byte)
			byte blockSize = gifBytes[byteIndex];
			byteIndex++;

			if (blockSize == 0x00)
			{
				// Block Terminator(1 Byte)
				break;
			}

			var imageDataBlock = new ImageBlock.ImageDataBlock();
			imageDataBlock.m_blockSize = blockSize;

			// Image Data(? Bytes)
			imageDataBlock.m_imageData = new byte[imageDataBlock.m_blockSize];
			for (int i = 0; i < imageDataBlock.m_imageData.Length; i++)
			{
				imageDataBlock.m_imageData[i] = gifBytes[byteIndex];
				byteIndex++;
			}

			if (ib.m_imageDataList == null)
			{
				ib.m_imageDataList = new List<ImageBlock.ImageDataBlock>();
			}
			ib.m_imageDataList.Add(imageDataBlock);
		}

		if (gifData.m_imageBlockList == null)
		{
			gifData.m_imageBlockList = new List<ImageBlock>();
		}
		gifData.m_imageBlockList.Add(ib);
	}

	private void SetGraphicControlExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
	{
		GraphicControlExtension gcEx = new GraphicControlExtension();

		// Extension Introducer(1 Byte)
		// 0x21
		gcEx.m_extensionIntroducer = gifBytes[byteIndex];
		byteIndex++;

		// Graphic Control Label(1 Byte)
		// 0xf9
		gcEx.m_graphicControlLabel = gifBytes[byteIndex];
		byteIndex++;

		// Block Size(1 Byte)
		// 0x04
		gcEx.m_blockSize = gifBytes[byteIndex];
		byteIndex++;

		// 1 Byte
		{
			// Reserved(3 Bits)
			// Unused

			// Disposal Mothod(3 Bits)
			// 0 (No disposal specified)
			// 1 (Do not dispose)
			// 2 (Restore to background color)
			// 3 (Restore to previous)
			switch (gifBytes[byteIndex] & 28)
			{ // 0b00011100
			case 4:     // 0b00000100
				gcEx.m_disposalMethod = 1;
				break;
			case 8:     // 0b00001000
				gcEx.m_disposalMethod = 2;
				break;
			case 12:    // 0b00001100
				gcEx.m_disposalMethod = 3;
				break;
			default:
				gcEx.m_disposalMethod = 0;
				break;
			}

			// User Input Flag(1 Bit)
			// Unknown

			// Transparent Color Flag(1 Bit)
			gcEx.m_transparentColorFlag = (gifBytes[byteIndex] & 1) == 1; // 0b00000001

			byteIndex++;
		}

		// Delay Time(2 Bytes)
		gcEx.m_delayTime = BitConverter.ToUInt16(gifBytes, byteIndex);
		byteIndex += 2;

		// Transparent Color Index(1 Byte)
		gcEx.m_transparentColorIndex = gifBytes[byteIndex];
		byteIndex++;

		// Block Terminator(1 Byte)
		gcEx.m_blockTerminator = gifBytes[byteIndex];
		byteIndex++;

		if (gifData.m_graphicCtrlExList == null)
		{
			gifData.m_graphicCtrlExList = new List<GraphicControlExtension>();
		}
		gifData.m_graphicCtrlExList.Add(gcEx);
	}

	private void SetCommentExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
	{
		CommentExtension commentEx = new CommentExtension();

		// Extension Introducer(1 Byte)
		// 0x21
		commentEx.m_extensionIntroducer = gifBytes[byteIndex];
		byteIndex++;

		// Comment Label(1 Byte)
		// 0xfe
		commentEx.m_commentLabel = gifBytes[byteIndex];
		byteIndex++;

		// Block Size & Comment Data List
		while (true)
		{
			// Block Size(1 Byte)
			byte blockSize = gifBytes[byteIndex];
			byteIndex++;

			if (blockSize == 0x00)
			{
				// Block Terminator(1 Byte)
				break;
			}

			var commentDataBlock = new CommentExtension.CommentDataBlock();
			commentDataBlock.m_blockSize = blockSize;

			// Comment Data(n Byte)
			commentDataBlock.m_commentData = new byte[commentDataBlock.m_blockSize];
			for (int i = 0; i < commentDataBlock.m_commentData.Length; i++)
			{
				commentDataBlock.m_commentData[i] = gifBytes[byteIndex];
				byteIndex++;
			}

			if (commentEx.m_commentDataList == null)
			{
				commentEx.m_commentDataList = new List<CommentExtension.CommentDataBlock>();
			}
			commentEx.m_commentDataList.Add(commentDataBlock);
		}

		if (gifData.m_commentExList == null)
		{
			gifData.m_commentExList = new List<CommentExtension>();
		}
		gifData.m_commentExList.Add(commentEx);
	}

	private void SetPlainTextExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
	{
		PlainTextExtension plainTxtEx = new PlainTextExtension();

		// Extension Introducer(1 Byte)
		// 0x21
		plainTxtEx.m_extensionIntroducer = gifBytes[byteIndex];
		byteIndex++;

		// Plain Text Label(1 Byte)
		// 0x01
		plainTxtEx.m_plainTextLabel = gifBytes[byteIndex];
		byteIndex++;

		// Block Size(1 Byte)
		// 0x0c
		plainTxtEx.m_blockSize = gifBytes[byteIndex];
		byteIndex++;

		// Text Grid Left Position(2 Bytes)
		// Not supported
		byteIndex += 2;

		// Text Grid Top Position(2 Bytes)
		// Not supported
		byteIndex += 2;

		// Text Grid Width(2 Bytes)
		// Not supported
		byteIndex += 2;

		// Text Grid Height(2 Bytes)
		// Not supported
		byteIndex += 2;

		// Character Cell Width(1 Bytes)
		// Not supported
		byteIndex++;

		// Character Cell Height(1 Bytes)
		// Not supported
		byteIndex++;

		// Text Foreground Color Index(1 Bytes)
		// Not supported
		byteIndex++;

		// Text Background Color Index(1 Bytes)
		// Not supported
		byteIndex++;

		// Block Size & Plain Text Data List
		while (true)
		{
			// Block Size(1 Byte)
			byte blockSize = gifBytes[byteIndex];
			byteIndex++;

			if (blockSize == 0x00)
			{
				// Block Terminator(1 Byte)
				break;
			}

			var plainTextDataBlock = new PlainTextExtension.PlainTextDataBlock();
			plainTextDataBlock.m_blockSize = blockSize;

			// Plain Text Data(n Byte)
			plainTextDataBlock.m_plainTextData = new byte[plainTextDataBlock.m_blockSize];
			for (int i = 0; i < plainTextDataBlock.m_plainTextData.Length; i++)
			{
				plainTextDataBlock.m_plainTextData[i] = gifBytes[byteIndex];
				byteIndex++;
			}

			if (plainTxtEx.m_plainTextDataList == null)
			{
				plainTxtEx.m_plainTextDataList = new List<PlainTextExtension.PlainTextDataBlock>();
			}
			plainTxtEx.m_plainTextDataList.Add(plainTextDataBlock);
		}

		if (gifData.m_plainTextExList == null)
		{
			gifData.m_plainTextExList = new List<PlainTextExtension>();
		}
		gifData.m_plainTextExList.Add(plainTxtEx);
	}

	private void SetApplicationExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
	{
		// Extension Introducer(1 Byte)
		// 0x21
		gifData.m_appEx.m_extensionIntroducer = gifBytes[byteIndex];
		byteIndex++;

		// Extension Label(1 Byte)
		// 0xff
		gifData.m_appEx.m_extensionLabel = gifBytes[byteIndex];
		byteIndex++;

		// Block Size(1 Byte)
		// 0x0b
		gifData.m_appEx.m_blockSize = gifBytes[byteIndex];
		byteIndex++;

		// Application Identifier(8 Bytes)
		gifData.m_appEx.m_appId1 = gifBytes[byteIndex];
		byteIndex++;
		gifData.m_appEx.m_appId2 = gifBytes[byteIndex];
		byteIndex++;
		gifData.m_appEx.m_appId3 = gifBytes[byteIndex];
		byteIndex++;
		gifData.m_appEx.m_appId4 = gifBytes[byteIndex];
		byteIndex++;
		gifData.m_appEx.m_appId5 = gifBytes[byteIndex];
		byteIndex++;
		gifData.m_appEx.m_appId6 = gifBytes[byteIndex];
		byteIndex++;
		gifData.m_appEx.m_appId7 = gifBytes[byteIndex];
		byteIndex++;
		gifData.m_appEx.m_appId8 = gifBytes[byteIndex];
		byteIndex++;

		// Application Authentication Code(3 Bytes)
		gifData.m_appEx.m_appAuthCode1 = gifBytes[byteIndex];
		byteIndex++;
		gifData.m_appEx.m_appAuthCode2 = gifBytes[byteIndex];
		byteIndex++;
		gifData.m_appEx.m_appAuthCode3 = gifBytes[byteIndex];
		byteIndex++;

		// Block Size & Application Data List
		while (true)
		{
			// Block Size (1 Byte)
			byte blockSize = gifBytes[byteIndex];
			byteIndex++;

			if (blockSize == 0x00)
			{
				// Block Terminator(1 Byte)
				break;
			}

			var appDataBlock = new ApplicationExtension.ApplicationDataBlock();
			appDataBlock.m_blockSize = blockSize;

			// Application Data(n Byte)
			appDataBlock.m_applicationData = new byte[appDataBlock.m_blockSize];
			for (int i = 0; i < appDataBlock.m_applicationData.Length; i++)
			{
				appDataBlock.m_applicationData[i] = gifBytes[byteIndex];
				byteIndex++;
			}

			if (gifData.m_appEx.m_appDataList == null)
			{
				gifData.m_appEx.m_appDataList = new List<ApplicationExtension.ApplicationDataBlock>();
			}
			gifData.m_appEx.m_appDataList.Add(appDataBlock);
		}
	}


	#region ----- Extension -----
	/// <summary>
	/// Convert BitArray to int (Specifies the start index and bit length)
	/// </summary>
	/// <param name="startIndex">Start index</param>
	/// <param name="bitLength">Bit length</param>
	/// <returns>Converted int</returns>
	private int GetNumeral(BitArray array, int startIndex, int bitLength)
	{
		int result = 0;
		int arrayLength = array.Length;
		for(int i = 0; i < bitLength && arrayLength > startIndex + i; ++i)
		{
			bool bit = array.Get(startIndex + i);
			if(bit)
			{
				result += 1 << i;
			}
		}
		return result;
	}
	#endregion
}

