using Microsoft.Win32;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZXing.Windows.Compatibility;
using System.DirectoryServices.ActiveDirectory;
using System.Windows.Media.Media3D;
using ZXing.Common;

namespace Qryptic
{
    /// <summary>
    /// Interaction logic for PageBarcode.xaml
    /// </summary>
    public partial class PageBarcode : UserControl
    {
        public PageBarcode()
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
            BarcodeWriter qrGenerator = new()
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 300,
                    Height = 150,
                    Margin = 5
                }
            };
            qrCodeImage = qrGenerator.Write(tbox_text.Text);

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
            if (qrCodeImage != null)
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
