using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OldMoatGames;

public class GifInfoDebug : MonoBehaviour {

    public AnimatedGifPlayer gifPlayer;
    public List<GifDecoder.GifFrame> frameList;
    public GifDecoder.GifFrame currentFrame;
	
	// Update is called once per frame
	void Update () {
        frameList = gifPlayer._cachedFrames;
        currentFrame = gifPlayer.CurrentFrame;
	}
}
