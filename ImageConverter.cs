////////////////////////////////////////////////////////////////////////////////
// The MIT License (MIT)
//
// Copyright (c) 2024 Tim Stair
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Support.IO;

namespace I2I
{
    public enum ImageExportFormat
    {
        Bmp,
        Emf,
        Exif,
        Gif,
        Icon,
        Jpeg,
        Png,
        Tiff,
        Wmf,
#if !MONO_BUILD
        Webp,
#endif
    }

    public class ImageConverter
    {
        private static Dictionary<ImageExportFormat, string> s_dictionaryFormatToExtension =
            new Dictionary<ImageExportFormat, string>();

        public static readonly HashSet<ImageFormat> SupportedSystemDrawingImageFormat = new HashSet<ImageFormat>
        {
            ImageFormat.Bmp,
            ImageFormat.Emf,
            ImageFormat.Exif,
            ImageFormat.Gif,
            ImageFormat.Icon,
            ImageFormat.Jpeg,
            ImageFormat.Png,
            ImageFormat.Tiff,
            ImageFormat.Wmf
        };

        private static readonly Dictionary<string, ImageFormat> StringToImageFormatDictionary =
            SupportedSystemDrawingImageFormat.ToList().ToDictionary(
                i => i.ToString(), i => i);

        public static readonly Dictionary<ImageExportFormat, ImageFormat> CardMakerImageExportFormatToImageFormatDictionary =
            Enum.GetValues(typeof(ImageExportFormat))
                .Cast<ImageExportFormat>()
                .ToList()
                .ToDictionary(e => e, e => StringToImageFormatDictionary.TryGetValue(e.ToString(), out var eFormat) ? eFormat : null);

        private readonly ILogger m_zLogger;

        static ImageConverter()
        {
            s_dictionaryFormatToExtension = Enum.GetValues(typeof(ImageExportFormat))
                .Cast<ImageExportFormat>()
                .ToList()
                .ToDictionary(e => e, e => e.ToString().ToLower());
            // special cases below
            s_dictionaryFormatToExtension[ImageExportFormat.Jpeg] = "jpg";
        }

        public ImageConverter(ILogger zLogger)
        {
            m_zLogger = zLogger;
        }

        public void ConvertImageFile(string sFile, ImageExportFormat eExportFormat)
        {
            m_zLogger.AddLogLines(new[] { $"Reading: {sFile}" });
            Bitmap zSourceBitmap = null;
            var sExportPath = Path.Combine(
                                  Path.GetDirectoryName(sFile),
                                  Path.GetFileNameWithoutExtension(sFile)
                              ) +
                              "." + s_dictionaryFormatToExtension[eExportFormat];
            if (0 == string.Compare(sFile, sExportPath, StringComparison.InvariantCultureIgnoreCase))
            {
                m_zLogger.AddLogLine("Cannot overwrite existing file.");
                return;
            }

            try
            {
                zSourceBitmap = ReadBitmapFromFile(sFile);
            }
            catch (Exception e)
            {
                m_zLogger.AddLogLine($"Read failed: {e}");
                return;
            }
            m_zLogger.AddLogLines(new[] { $"Writing: {sExportPath}" });
            try
            {
                WriteImage(sExportPath, zSourceBitmap, eExportFormat);
            }
            catch (Exception e)
            {
                m_zLogger.AddLogLine($"Write failed: {e}");
                return;
            }
            finally
            {
                zSourceBitmap.Dispose();
            }
        }

        public Bitmap ReadBitmapFromFile(string sFile)
        {
            switch (Path.GetExtension(sFile).ToLower())
            {
#if !MONO_BUILD
                case ".webp":
                    using (var zStream = SKFileStream.OpenStream(sFile))
                    {
                        return SKBitmap.Decode(zStream).ToBitmap();
                    }
#endif
                default:
                    return new Bitmap(sFile);
            }
        }

        public void WriteImage(string sFile, Bitmap zSourceBitmap, ImageExportFormat eExportFormat)
        {
            switch (eExportFormat)
            {
                // Note: SkiaSharp does not support DPI on Webp files (!)
#if !MONO_BUILD
                case ImageExportFormat.Webp:
                    var zEncoderOptions = new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100);
                    using (var outFile = new FileInfo(sFile).Create())
                    {
                        zSourceBitmap.ToSKBitmap().PeekPixels()
                            .Encode(zEncoderOptions)
                            .SaveTo(outFile);
                    }

                    break;
#endif
                default:
                    var nOriginalHorizontalResolution = zSourceBitmap.HorizontalResolution;
                    var nOriginalVerticalResolution = zSourceBitmap.VerticalResolution;
                    var eExportImageFormat = CardMakerImageExportFormatToImageFormatDictionary[eExportFormat];
                    zSourceBitmap.SetResolution(nOriginalHorizontalResolution, nOriginalVerticalResolution);
                    zSourceBitmap.Save(sFile, eExportImageFormat);
                    break;
            }
        }
    }
}
