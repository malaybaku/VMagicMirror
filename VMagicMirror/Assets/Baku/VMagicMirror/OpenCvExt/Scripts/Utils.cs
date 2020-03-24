#if VMAGICMIRROR_USE_OPENCV
using System;
using System.Runtime.InteropServices;
using OpenCVForUnity.CoreModule;
using UnityEngine;

namespace Baku.OpenCvExt
{
    /// <summary>
    /// OpenCVの処理でどうしても欲しいやつを定義する
    /// </summary>
    public static class OpenCvExtUtils
    {
        /// <summary>
        /// Colors配列の情報をMatに書き込みます。サイズが間違っていたりすると例外がスローされます。
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="mat"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="flip"></param>
        /// <param name="flipCode"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void ColorsToMat(Color32[] colors, Mat mat, int width, int height, bool flip = true, int flipCode = 0)
        {
            if (mat == null)
            {
                throw new ArgumentNullException(nameof(mat));
            }

            if (colors == null)
            {
                throw new ArgumentNullException(nameof(colors));
            }
            
            mat?.ThrowIfDisposed();

            if (mat.cols() != width || mat.rows() != height || colors.Length != width * height)
            {
                throw new ArgumentException("The Mat or color has invalid size");
            }

            var colorsHandle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            OpenCVForUnity_TextureToMat(colorsHandle.AddrOfPinnedObject(), mat.nativeObj, flip, flipCode);
            colorsHandle.Free();
        }
        
        //NOTE: ここはWindows前提でやや雑に書いてる(元のOpenCVforUnityではライブラリファイル名がもうちょっとダイナミックに変わる)
        [DllImport("opencvforunity")]
        private static extern void OpenCVForUnity_TextureToMat(IntPtr textureColors, IntPtr Mat, [MarshalAs(UnmanagedType.U1)] bool flip, int flipCode);

    }
}
#endif
