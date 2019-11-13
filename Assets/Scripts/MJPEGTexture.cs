using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MJPEGTexture : MonoBehaviour
{
	[SerializeField] private string Url;
	private int _chunkSize = 4;
	private Texture2D _texture;
	private const int _initWidth = 2;
	private const int _initHeight = 2;
	private bool _updateFrame = false;
	private MJPEGProcesser _processer;

	// Use this for initialization
	void Start ()
	{
		_processer = new MJPEGProcesser(_chunkSize * 1024);
		_processer.FrameReady += (obj, arg) => { _updateFrame = true; };
		_processer.Error += (sender, e) => { Debug.Log(e.Message); };
		Uri uri = new Uri(Url);
		_texture = new Texture2D(_initWidth, _initHeight, TextureFormat.PVRTC_RGB4, false);
		GetComponent<Renderer>().material.mainTexture = _texture;
		
		_processer.SetUri(uri);

	}
	
	// Update is called once per frame
	void Update ()
	{
		if (_updateFrame)
		{
			_texture.LoadImage(_processer.CurrentFrame);
		}
	}
}
