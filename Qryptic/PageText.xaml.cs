using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZXing.Common;
using ZXing;
using System.Windows.Media.Imaging;
using ZXing.Windows.Compatibility;
using System.Drawing.Drawing2D;
using System.Drawing;
using ZXing.Rendering;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Policy;
using QRCoder;
using Color = System.Drawing.Color;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Diagnostics;

namespace Qryptic
{
    /// <summary>
    /// Interaction logic for PageText.xaml
    /// </summary>
    public partial class PageText : UserControl
    {
        public PageText()
        {
            InitializeComponent();
        }

        private ViewModel viewModel = ViewModel.Instance;
        Bitmap? qrCodeImage;

        public static BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private void FlatButton_Click(object sender, RoutedEventArgs e)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(tbox_text.Text, QRCodeGenerator.ECCLevel.Q);
            /*ArtQRCode qrCode = new ArtQRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20, Color.Black, Color.White, Color.White, null, 0.7, true, ArtQRCode.QuietZoneStyle.Flat);*/
            QRCode qrCode = new(qrCodeData);
            qrCodeImage = qrCode.GetGraphic(10);

            img.Source = BitmapToBitmapSource(qrCodeImage);
            viewModel.ShowToast("QR Code Generated", "QR code generated successfully!", ToastType.Success);

        }

        private void BackBtn_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
           viewModel.NavigateTo(1);
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (qrCodeImage != null)
            {
                try
                {
                    Clipboard.SetImage(BitmapToBitmapSource(qrCodeImage));
                    viewModel.ShowToast("QR Code Copied", "QR code copied to clipboard!", ToastType.Success);
                }
                catch (Exception ex)
                {
                    viewModel.ShowToast("Copy Error", ex.Message, ToastType.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
           if(qrCodeImage != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
                    Title = "Save Image",
                    DefaultExt = "png"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        string extension = System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower();
                        Debug.WriteLine($"Extension is {extension}");
                        switch (extension)
                        {
                            case ".jpg":
                            case ".jpeg":
                                qrCodeImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                                break;
                            case ".png":
                                qrCodeImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                                break;
                            case ".bmp":
                                qrCodeImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                                break;
                            default:
                                qrCodeImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        viewModel.ShowToast("Save Error", ex.Message, ToastType.Error);
                    }
                }
            }
        }
    }
}
