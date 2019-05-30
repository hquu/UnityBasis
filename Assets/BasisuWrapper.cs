using System;
using System.Runtime.InteropServices;

public enum TranscoderTextureFormat
{
	cTFETC1,
	cTFBC1,
	cTFBC4,
	cTFPVRTC1_4_OPAQUE_ONLY,
	cTFBC7_M6_OPAQUE_ONLY,
	// ETC2_EAC_A8 block followed by a ETC1 block
	cTFETC2,
	// BC4 followed by a BC1 block
	cTFBC3,
	// two BC4 blocks
	cTFBC5,		

	cTFTotalTextureFormats
};

public struct BasisFile
{
	[DllImport ("BASISULIB", EntryPoint = "basis_init")]
	private static extern void basis_init();

	[DllImport("BASISULIB", EntryPoint = "bf_new")]
	private static extern IntPtr bf_new(byte[] fileData, int fileSize);
	
	[DllImport ("BASISULIB", EntryPoint = "bf_close")]
	private static extern void bf_close(IntPtr file);
	
	[DllImport ("BASISULIB", EntryPoint = "bf_getHasAlpha")]
	private static extern bool bf_getHasAlpha(IntPtr file);
	
	[DllImport ("BASISULIB", EntryPoint = "bf_getNumImages")]
	private static extern int bf_getNumImages(IntPtr file);
	
	[DllImport ("BASISULIB", EntryPoint = "bf_getNumLevels")]
	private static extern int bf_getNumLevels(IntPtr file, int image_index);
	
	[DllImport ("BASISULIB", EntryPoint = "bf_getImageWidth")]
	private static extern int bf_getImageWidth(IntPtr file, int image_index, int level_index);
	
	[DllImport ("BASISULIB", EntryPoint = "bf_getImageHeight")]
	private static extern int bf_getImageHeight(IntPtr file, int image_index, int level_index);
	
	[DllImport ("BASISULIB", EntryPoint = "bf_getImageTranscodedSizeInBytes")]
	private static extern int bf_getImageTranscodedSizeInBytes(IntPtr file, int image_index, int level_index, int format);
	
	[DllImport ("BASISULIB", EntryPoint = "bf_startTranscoding")]
	private static extern bool bf_startTranscoding(IntPtr file);

	[DllImport ("BASISULIB", EntryPoint = "bf_transcodeImage")]
	public static extern bool bf_transcodeImage(IntPtr file, out IntPtr dst, out int size, int image_index, int level_index, TranscoderTextureFormat format, bool pvrtc_wrap_addressing, bool get_alpha_for_opaque_formats);

	private static bool initted;
	private IntPtr nativeFile;

	//NOTE(Simon): fileData gets copied on the C++ side. No need to keep it around after calling BasisFile()
	public BasisFile(byte[] fileData)
	{
		if (!initted)
		{
			basis_init();
			initted = true;
		}
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
}