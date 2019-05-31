using System;
using System.Runtime.InteropServices;
using UnityEngine;

public enum TranscoderTextureFormat
{
	ETC1,
	BC1,
	BC4,
	PVRTC1_4_OPAQUE_ONLY,
	BC7_M6_OPAQUE_ONLY,
	// ETC2_EAC_A8 block followed by a ETC1 block
	ETC2,
	// BC4 followed by a BC1 block
	BC3,
	// two BC4 blocks
	BC5,		

	TotalTextureFormats
}

[Flags]
public enum DecoderFlags
{
	// PVRTC1: texture will use wrap addressing vs. clamp (most PVRTC viewer tools assume wrap addressing, so we default to wrap although that can cause edge artifacts)
	PVRTCWrapAddressing = 1 << 0,

	// PVRTC1: decode non-pow2 ETC1S texture level to the next larger power of 2 (not implemented yet, but we're going to support it). Ignored if the slice's dimensions are already a power of 2.
	PVRTCDecodeToNextPow2 = 1 << 1,

	// When decoding to an opaque texture format, if the basis file has alpha, decode the alpha slice instead of the color slice to the output texture format
	TranscodeAlphaDataToOpaqueFormats = 1 << 2,

	// Forbid usage of BC1 3 color blocks (we don't support BC1 punchthrough alpha yet).
	BC1ForbidThreeColorBlocks = 1 << 3
}

///<summary>
///<para>This type is Disposable. Use using() or call Dispose() when done</para>
///<para>You can use BasisFile in a high-level and a low-level way.
///In both cases you create a BasisFile instance from the file contents of a .basis file.</para>
///<para>In the high level API you now call TranscodeImage(), all parameters are optional.</para>
///<para>In the low level API you first call StartTranscoding() once per file,
///then TrancodeImage() with the parameters you want.</para>
///<para>In your code you then create a texture (you can use ConvertBasisFormat() to convert
///between formats used in basis and formats used in unity)</para>
///<para>After creation you load the transcoded data with LoadRawTextureData(). And finally Apply()</para>
///</summary>
public struct BasisFile : IDisposable
{
	private static bool initted;
	private bool transcodingStarted;
	private IntPtr nativeFile;

	//NOTE(Simon): fileData gets copied on the C++ side. No need to keep it around after calling BasisFile()
	public BasisFile(byte[] fileData)
	{
		if (!initted)
		{
			basis_init();
			initted = true;
		}
		transcodingStarted = false;
		nativeFile = bf_new(fileData, fileData.Length);
	}

	public bool HasAlpha() => bf_getHasAlpha(nativeFile);
	public int NumImages() => bf_getNumImages(nativeFile);
	public int NumLevels(int imageIndex) => bf_getNumLevels(nativeFile, imageIndex);
	public int ImageWidth(int imageIndex, int levelIndex) => bf_getImageWidth(nativeFile, imageIndex, levelIndex);
	public int ImageHeight(int imageIndex, int levelIndex) => bf_getImageHeight(nativeFile, imageIndex, levelIndex);
	public int TranscodedSizeInBytes(int imageIndex, int levelIndex, int format) => bf_getImageTranscodedSizeInBytes(nativeFile, imageIndex, levelIndex, format);
	public bool StartTranscoding() => bf_startTranscoding(nativeFile);
	public IntPtr TranscodeImage(int imageIndex, int levelIndex, TranscoderTextureFormat format, bool pvrtcWrapAddressing, bool getAlphaForOpaqueFormats, out int size)
	{
		//NOTE(Simon): int size is a size_t in C++, but C# can't handle array size > 32bit anyways, so it's an int here.
		bf_transcodeImage(nativeFile, out IntPtr dst, out size, imageIndex, levelIndex, format, pvrtcWrapAddressing, getAlphaForOpaqueFormats);
		return dst;
	}

	//TODO(Simon): If image has multiple levels (mips), add them to LoadRawTextureData if mipChain == true. if false, set mipChain to false
	public Texture2D GetTexture(int imageIndex = 0, int levelIndex = 0, TranscoderTextureFormat format = TranscoderTextureFormat.ETC1, bool mipChain = true, bool linear = false)
	{
		if (!transcodingStarted)
		{
			StartTranscoding();
			transcodingStarted = true;
		}

		if (format == TranscoderTextureFormat.PVRTC1_4_OPAQUE_ONLY)
		{
		}

		var width = ImageWidth(imageIndex, levelIndex);
		var height = ImageHeight(imageIndex, levelIndex);

		var unityTF = ConvertBasisTextureFormat(format);

		var transcoded = TranscodeImage(imageIndex, levelIndex, format, false, false, out int size);
		var texture = new Texture2D(width, height, unityTF, mipChain, linear);
		texture.LoadRawTextureData(transcoded, size);
		texture.Apply();
		return texture;
	}

	public void Dispose()
	{
		bf_close(nativeFile);
	}

	public static TextureFormat ConvertBasisTextureFormat(TranscoderTextureFormat format)
	{
		TextureFormat unityTF = 0;

		switch (format)
		{
			case TranscoderTextureFormat.ETC1:
				unityTF = TextureFormat.ETC_RGB4;
				break;
			case TranscoderTextureFormat.ETC2:
				//???
				goto default;
			case TranscoderTextureFormat.PVRTC1_4_OPAQUE_ONLY:
				unityTF = TextureFormat.PVRTC_RGB4;
				break;
			case TranscoderTextureFormat.BC1:
				unityTF = TextureFormat.DXT1;
				break;
			case TranscoderTextureFormat.BC3:
				//???
				goto default;
			case TranscoderTextureFormat.BC4:
				unityTF = TextureFormat.BC4;
				break;
			case TranscoderTextureFormat.BC5:
				unityTF = TextureFormat.BC5;
				break;
			case TranscoderTextureFormat.BC7_M6_OPAQUE_ONLY:
				unityTF = TextureFormat.BC7;
				break;
			default:
				throw new Exception($"TranscoderTextureFormat {format.ToString()} not supported by Unity");
		}

		return unityTF;
	}


	[DllImport("BASISULIB", EntryPoint = "basis_init")]
	private static extern void basis_init();

	[DllImport("BASISULIB", EntryPoint = "bf_new")]
	private static extern IntPtr bf_new(byte[] fileData, int fileSize);

	[DllImport("BASISULIB", EntryPoint = "bf_close")]
	private static extern void bf_close(IntPtr file);

	[DllImport("BASISULIB", EntryPoint = "bf_getHasAlpha")]
	private static extern bool bf_getHasAlpha(IntPtr file);

	[DllImport("BASISULIB", EntryPoint = "bf_getNumImages")]
	private static extern int bf_getNumImages(IntPtr file);

	[DllImport("BASISULIB", EntryPoint = "bf_getNumLevels")]
	private static extern int bf_getNumLevels(IntPtr file, int image_index);

	[DllImport("BASISULIB", EntryPoint = "bf_getImageWidth")]
	private static extern int bf_getImageWidth(IntPtr file, int image_index, int level_index);

	[DllImport("BASISULIB", EntryPoint = "bf_getImageHeight")]
	private static extern int bf_getImageHeight(IntPtr file, int image_index, int level_index);

	[DllImport("BASISULIB", EntryPoint = "bf_getImageTranscodedSizeInBytes")]
	private static extern int bf_getImageTranscodedSizeInBytes(IntPtr file, int image_index, int level_index, int format);

	[DllImport("BASISULIB", EntryPoint = "bf_startTranscoding")]
	private static extern bool bf_startTranscoding(IntPtr file);

	[DllImport("BASISULIB", EntryPoint = "bf_transcodeImage")]
	public static extern bool bf_transcodeImage(IntPtr file, out IntPtr dst, out int size, int image_index, int level_index, TranscoderTextureFormat format, bool pvrtc_wrap_addressing, bool get_alpha_for_opaque_formats);

}