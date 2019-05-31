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
		for (int i = 0; i < 500; i++)
		{
			using (var file = new BasisFile(fileData))
			{
				var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
				go.transform.Translate(0, 1, i);
				go.transform.Rotate(-90, 0, 0);
				go.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Texture");
				go.GetComponent<Renderer>().material.mainTexture = file.GetTexture(format: TranscoderTextureFormat.ETC1, mipChain: false);
			}
		}
		watch.Stop();
		Debug.Log($"{watch.Elapsed.TotalMilliseconds} ms");
	}

	void Update()
	{

	}
}
