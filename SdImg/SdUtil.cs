﻿using SixLabors.ImageSharp;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace SdImg
{
    public class SdUtil
    {
        public byte[] LoadImageAsBitmapBytes(string filePath)
        {
            using (var m = new MemoryStream())
            {
                try
                {
                    using (var image = System.Drawing.Image.FromFile(filePath))
                    {
                        image.Save(m, ImageFormat.Bmp);
                    }
                }
                catch
                {
                    using (var image = SixLabors.ImageSharp.Image.Load(filePath))
                    {
                        image.SaveAsBmp(m);
                    }
                }
                return m.ToArray();
            }
        }

        public void SaveAsPng(byte[] imageBytes, string outputPath, string pngInfo, bool overwrite)
        {
            using (var img = SixLabors.ImageSharp.Image.Load(imageBytes, out var fmt))
            {
                var pngMetaData = img.Metadata.GetFormatMetadata(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);

                const string Key = "parameters";
                var textData = pngMetaData.TextData.FirstOrDefault(x => x.Keyword == Key);
                pngMetaData.TextData.Remove(textData);
                pngMetaData.TextData.Add(new SixLabors.ImageSharp.Formats.Png.PngTextData(Key, pngInfo, "", ""));

                using (var fs = new FileStream(outputPath, overwrite ? FileMode.Create : FileMode.CreateNew))
                {
                    var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
                    img.SaveAsPng(fs);
                }
            }
        }

        /// <summary>
        /// BitmapをJPEGフォーマットで保存
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="path"></param>
        /// <param name="pngInfo"></param>
        /// <param name="quality">JPEG品質 (1-100)</param>
        /// <param name="overwrite">上書き</param>
        public void SaveBitmapAsJpeg(Bitmap bitmap, string path, string pngInfo, int quality, bool overwrite)
        {
            if (quality < 1 || 100 < quality) throw new ArgumentException("invalid quality:" + quality, nameof(quality));

            using (var fs = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew))
            {
                if (pngInfo != null)
                {
                    var userComment = CreateUserCommentPropertyItem(pngInfo);
                    bitmap.SetPropertyItem(userComment);
                }

                var jpegEncoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
                var encoderParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = encoderParam;

                bitmap.Save(fs, jpegEncoder, encoderParams);
            }
        }

        public string GetPngInfoFromPng(SixLabors.ImageSharp.Image sixLaborsImg, SixLabors.ImageSharp.Formats.IImageFormat format)
        {
            if (format.Name == "PNG")
            {
                var pngMetaData = sixLaborsImg.Metadata.GetFormatMetadata(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);
                var parametersMetaData = pngMetaData.TextData.FirstOrDefault(x => x.Keyword == "parameters");
                var pngInfo = parametersMetaData.Value;
                return string.IsNullOrWhiteSpace(pngInfo) ? null : pngInfo;
            }
            else
            {
                return null;
            }
        }

        public string GetPngInfoFromJpeg(System.Drawing.Image image)
        {
            const int USER_COMMENT = 0x9286;
            var userComment = image.PropertyItems.FirstOrDefault(x => x.Id == USER_COMMENT);
            return (userComment == null) ? null : new UTF8Encoding(false).GetString(userComment.Value);
        }

        /// <summary>
        /// PngInfoを取得
        /// </summary>
        /// <param name="path"></param>
        /// <param name="image">既にpathを読み込んだデータがあれば</param>
        /// <param name="slImage">既にpathを読み込んだデータがあれば</param>
        /// <param name="slFormat">既にpathを読み込んだデータがあれば</param>
        /// <returns></returns>
        public string GetPngInfo(
            string path,
            System.Drawing.Image image,
            SixLabors.ImageSharp.Image slImage,
            SixLabors.ImageSharp.Formats.IImageFormat slFormat)
        {
            if (image != null)
            {
                var pngInfo = GetPngInfoFromJpeg(image);
                if (pngInfo != null) return pngInfo;
            }

            if (slImage != null && slFormat != null)
            {
                var pngInfo = GetPngInfoFromPng(slImage, slFormat);
                if (pngInfo != null) return pngInfo;
            }

            if (path.ToLowerInvariant().EndsWith(".png"))
            {
                using (var sixLaborsImg = SixLabors.ImageSharp.Image.Load(path, out var fmt))
                {
                    var pngInfo = GetPngInfoFromPng(sixLaborsImg, fmt);
                    if (pngInfo != null) return pngInfo;
                }
            }
            else // 拡張子が png ではない場合
            {
                using (var img2 = System.Drawing.Image.FromFile(path))
                {
                    var pngInfo = GetPngInfoFromJpeg(img2);
                    if (pngInfo != null) return pngInfo;
                }
            }

            using (var sixLaborsImg = SixLabors.ImageSharp.Image.Load(path, out var fmt))
            {
                var pngInfo = GetPngInfoFromPng(sixLaborsImg, fmt);
                if (pngInfo != null) return pngInfo;
            }

            return null;
        }

        public Bitmap ResizeImageToBitmap(System.Drawing.Image image, int width, int height)
        {
            Bitmap ret = null;
            try
            {
                ret = new Bitmap(width, height);
                ret.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                using (var attr = new ImageAttributes())
                using (var graphics = Graphics.FromImage(ret))
                {
                    attr.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.DrawImage(image, new System.Drawing.Rectangle(0, 0, width, height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attr);
                }
                return ret;
            }
            catch
            {
                ret?.Dispose();
                throw;
            }
        }

        public FileStream GetTempFile(int randomLength, string suffix)
        {
            var tempDir = Path.GetTempPath();
            for (var i = 0; i < 10; i++)
            {
                var random = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    .Substring(0, randomLength).Replace('/', '_').Replace('+', '-').ToLowerInvariant();
                var path = Path.Combine(tempDir, random + suffix);

                try
                {
                    return new FileStream(path, FileMode.CreateNew);
                }
                catch
                {
                    if (9 < i) throw;
                }
            }
            throw new InvalidOperationException("This should never reached");
        }

        private static readonly byte[] SmallJpegBytes = new byte[] { 255, 216, 255, 224, 0, 16, 74, 70, 73, 70, 0, 1, 1, 1, 0, 96, 0, 96, 0, 0, 255, 225, 0, 54, 69, 120, 105, 102, 0, 0, 77, 77, 0, 42, 0, 0, 0, 8, 0, 1, 135, 105, 0, 4, 0, 0, 0, 1, 0, 0, 0, 26, 0, 0, 0, 0, 0, 1, 146, 134, 0, 7, 0, 0, 0, 1, 95, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 219, 0, 67, 0, 8, 6, 6, 7, 6, 5, 8, 7, 7, 7, 9, 9, 8, 10, 12, 20, 13, 12, 11, 11, 12, 25, 18, 19, 15, 20, 29, 26, 31, 30, 29, 26, 28, 28, 32, 36, 46, 39, 32, 34, 44, 35, 28, 28, 40, 55, 41, 44, 48, 49, 52, 52, 52, 31, 39, 57, 61, 56, 50, 60, 46, 51, 52, 50, 255, 219, 0, 67, 1, 9, 9, 9, 12, 11, 12, 24, 13, 13, 24, 50, 33, 28, 33, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 255, 192, 0, 17, 8, 0, 1, 0, 1, 3, 1, 34, 0, 2, 17, 1, 3, 17, 1, 255, 196, 0, 31, 0, 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 255, 196, 0, 181, 16, 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 125, 1, 2, 3, 0, 4, 17, 5, 18, 33, 49, 65, 6, 19, 81, 97, 7, 34, 113, 20, 50, 129, 145, 161, 8, 35, 66, 177, 193, 21, 82, 209, 240, 36, 51, 98, 114, 130, 9, 10, 22, 23, 24, 25, 26, 37, 38, 39, 40, 41, 42, 52, 53, 54, 55, 56, 57, 58, 67, 68, 69, 70, 71, 72, 73, 74, 83, 84, 85, 86, 87, 88, 89, 90, 99, 100, 101, 102, 103, 104, 105, 106, 115, 116, 117, 118, 119, 120, 121, 122, 131, 132, 133, 134, 135, 136, 137, 138, 146, 147, 148, 149, 150, 151, 152, 153, 154, 162, 163, 164, 165, 166, 167, 168, 169, 170, 178, 179, 180, 181, 182, 183, 184, 185, 186, 194, 195, 196, 197, 198, 199, 200, 201, 202, 210, 211, 212, 213, 214, 215, 216, 217, 218, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 255, 196, 0, 31, 1, 0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 255, 196, 0, 181, 17, 0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 119, 0, 1, 2, 3, 17, 4, 5, 33, 49, 6, 18, 65, 81, 7, 97, 113, 19, 34, 50, 129, 8, 20, 66, 145, 161, 177, 193, 9, 35, 51, 82, 240, 21, 98, 114, 209, 10, 22, 36, 52, 225, 37, 241, 23, 24, 25, 26, 38, 39, 40, 41, 42, 53, 54, 55, 56, 57, 58, 67, 68, 69, 70, 71, 72, 73, 74, 83, 84, 85, 86, 87, 88, 89, 90, 99, 100, 101, 102, 103, 104, 105, 106, 115, 116, 117, 118, 119, 120, 121, 122, 130, 131, 132, 133, 134, 135, 136, 137, 138, 146, 147, 148, 149, 150, 151, 152, 153, 154, 162, 163, 164, 165, 166, 167, 168, 169, 170, 178, 179, 180, 181, 182, 183, 184, 185, 186, 194, 195, 196, 197, 198, 199, 200, 201, 202, 210, 211, 212, 213, 214, 215, 216, 217, 218, 226, 227, 228, 229, 230, 231, 232, 233, 234, 242, 243, 244, 245, 246, 247, 248, 249, 250, 255, 218, 0, 12, 3, 1, 0, 2, 17, 3, 17, 0, 63, 0, 249, 254, 138, 40, 160, 15, 255, 217 };
        public static PropertyItem CreateUserCommentPropertyItem(string pngInfo)
        {
            const int USER_COMMENT = 0x9286;
            using (var stream = new MemoryStream(SmallJpegBytes))
            using (var bitmap = new Bitmap(stream))
            {
                var ret = bitmap.GetPropertyItem(USER_COMMENT);
                ret.Id = USER_COMMENT;
                ret.Type = 7;
                ret.Value = new UTF8Encoding(false).GetBytes(pngInfo);
                ret.Len = ret.Value.Length;
                return ret;
            }
        }
    }
}
