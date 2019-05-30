using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Test : MonoBehaviour
{
	public

	void Start()
	{
		var watch = new System.Diagnostics.Stopwatch();
		watch.Start();
		var fileData = File.ReadAllBytes(Path.Combine(Application.dataPath, "kodim20.basis"));
		var file = new BasisFile(fileData);
		var numImages = file.NumImages();
		for (int i = 0; i < numImages; i++)
		{
			var numLevels = file.NumLevels(i);
			for (int j = 0; j < numLevels; j++)
			{
				var width = file.ImageWidth(i, j);
				var height = file.ImageHeight(i, j);
				Debug.Log($"image {i}, level {j}: {width}x{height}");
				file.StartTranscoding();
				var transcoded = file.TranscodeImage(i, j, TranscoderTextureFormat.cTFETC1, false, false, out int size);
				var texture = new Texture2D(width, height, TextureFormat.ETC_RGB4, false);
				texture.LoadRawTextureData(transcoded, size);
				texture.Apply();

				var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
				go.GetComponent<Renderer>().material.mainTexture = texture;
			}
		}
		watch.Stop();
		Debug.Log($"{watch.Elapsed.TotalMilliseconds} ms");
	}

	void Update()
	{

	}
}
