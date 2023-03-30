using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Extensions;

namespace CG_Task1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BitmapImage? OriginalImg { get; set; }
        public BitmapImage? ProcessedImg { get; set; }

        byte[]? imageData;

        bool? isGrayscale;

        int stride;


        public MainWindow()
        {
            InitializeComponent();
            SizeToContent = SizeToContent.Height;

            OriginalImg = null;
            ProcessedImg = null;
            imageData = null;
            isGrayscale = null;
            stride = 0;


            SaveItem.IsEnabled= false;
            foreach (System.Windows.Controls.Button button in LeftToolbar.Children.OfType<System.Windows.Controls.Button>())
            {
                button.IsEnabled = false;
            }
            DataContext = this;
        }

        private void UpdateView(int wheelN = 0)
        {
            BitmapSource newImg;

            if (wheelN== 0)
            {
                newImg = BitmapSource.Create(
                    OriginalImg.PixelWidth,
                    OriginalImg.PixelHeight,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null,
                    imageData,
                    stride);
            }
            else 
                newImg = BitmapSource.Create(
                    wheelN,
                    wheelN,
                    96,
                    96,
                    PixelFormats.Bgr24,
                    null,
                    imageData,
                    stride);


            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(newImg));
            using(MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                ProcessedImg = new BitmapImage();
                ProcessedImg.BeginInit();
                ProcessedImg.CacheOption = BitmapCacheOption.OnLoad;
                ProcessedImg.StreamSource= stream;
                ProcessedImg.EndInit();
            }

            ProcessedImageView.Source = ProcessedImg;
        }


        
        private void InvertChannel(Channels channel)
        {
            // Assuming imageData is not null
            for(int i = (int)channel; i < imageData.Length; i += 4)
            {
                    imageData[i] = (byte)(255 - imageData[i]);
            }

        }

        private void BrightenChannel(Channels channel, int offset)
        {
            // Assuming imageData is not null
            for (int i = (int)channel; i < imageData.Length; i += 4)
            {
                if ((int)(imageData[i] + offset) > 255)
                    imageData[i] = 255;
                else
                    imageData[i] += (byte)offset;
            }
        }

        private void DarkenChannel(Channels channel, int offset)
        {
            // Assuming imageData is not null
            for (int i = (int)channel; i < imageData.Length; i += 4)
            {
                if ((int)(imageData[i] - offset) < 0)
                    imageData[i] = 0;
                else
                    imageData[i] -= (byte)offset;
            }
        }

        private void EnchanceContrastChannel(Channels channel, int offset)
        {
            // Assuming imageData is not null
            for (int i = (int)channel; i < imageData.Length; i += 4)
            {
                if (imageData[i] < offset)
                    imageData[i] = 0;
                else if (imageData[i] > 255 - offset)
                    imageData[i] = 255;
                else
                    imageData[i] = (byte)(255 * (imageData[i] - offset) / (255 - 2 * offset));
            }
        }

        private void CorrectGammaChannel(Channels channel, double gamma)
        {
            // Assuming imageData is not null
            for (int i = (int)channel; i < imageData.Length; i += 4)
            {
                imageData[i] = (byte)(Math.Pow((double)imageData[i] / 255, gamma) * 255);
            }
        }

        private void Convolve(Channels channel, Convolution convolution, double normalizingSum = 1)
        {
            // Assuming imageData is not null
            byte[] convolvedPixels = new byte[imageData.Length / 4];

            for (int i = (int)channel; i < imageData.Length; i += 4)
            {
                double sum = 0;
                for (int j = -convolution.Size / 2; j <= convolution.Size / 2; j++)
                {
                    for (int k = -convolution.Size / 2; k <= convolution.Size / 2; k++)
                    {
                        if(i + j * stride + 4 * k >= 0 && i + j * stride + 4 * k < imageData.Length)
                            sum += imageData[i + j * stride + 4 * k] * convolution.Kernel[j + convolution.Size / 2, k + convolution.Size / 2];
                    }
                }
                if (sum / normalizingSum < 0)
                    convolvedPixels[i / 4] = 0;
                else if (sum / normalizingSum < 0)
                    convolvedPixels[i / 4] = 255;
                else
                    convolvedPixels[i / 4] = (byte)(sum / normalizingSum);
            }

            for (int i = (int)channel; i < imageData.Length; i += 4)
            {
                imageData[i] = convolvedPixels[i / 4];
            }
        }


        // GUI event handlers
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tif";
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                FileInfo imgFileInfo = new FileInfo(dialog.FileName);
                OriginalImg = new BitmapImage();
                OriginalImg.BeginInit();
                OriginalImg.UriSource = new System.Uri(imgFileInfo.FullName);
                OriginalImg.EndInit();

                ProcessedImg = new BitmapImage();
                ProcessedImg.BeginInit();
                ProcessedImg.UriSource = new System.Uri(imgFileInfo.FullName);
                ProcessedImg.EndInit();

                OriginalImageView.Source = OriginalImg;
                ProcessedImageView.Source = ProcessedImg;
                OriginalImageView.Height = (double)App.Current.Resources["DefaultImageViewHeight"];
                ProcessedImageView.Height = (double)App.Current.Resources["DefaultImageViewHeight"];
                //OriginalImageScrollViewer.ScrollToHorizontalOffset(OriginalImageScrollViewer.ScrollableHeight / 2);
                //OriginalImageScrollViewer.ScrollToHorizontalOffset(OriginalImageScrollViewer.ScrollableWidth / 2);
                //ProcessedImageScrollViewer.ScrollToHorizontalOffset(ProcessedImageScrollViewer.ScrollableHeight / 2);
                //ProcessedImageScrollViewer.ScrollToHorizontalOffset(ProcessedImageScrollViewer.ScrollableWidth / 2);

                
                // stride = [width] * [bytes per pixel]
                stride = (ProcessedImg.PixelWidth * ProcessedImg.Format.BitsPerPixel + 7) / 8;
                imageData = new byte[ProcessedImg.PixelHeight * stride];
                ProcessedImg.CopyPixels(imageData, stride, 0);

                SaveItem.IsEnabled = true;
                foreach (System.Windows.Controls.Button button in LeftToolbar.Children.OfType<System.Windows.Controls.Button>())
                {
                    button.IsEnabled = true;
                }

                isGrayscale = false;
            }

        }

        private void InverseBt_Click(object sender, RoutedEventArgs e)
        {
           if(imageData != null)
            {
                Parallel.Invoke(
                    () => InvertChannel(Channels.Blue),
                    () => InvertChannel(Channels.Green),
                    () => InvertChannel(Channels.Red));
                UpdateView();
            } 
        }

        private void BrightenBt_Click(object sender, RoutedEventArgs e)
        {
            int offset = 10;

            Parallel.Invoke(
                () => BrightenChannel(Channels.Blue, offset),
                () => BrightenChannel(Channels.Green, offset),
                () => BrightenChannel(Channels.Red, offset));
            UpdateView();
        }

        private void DarkenBt_Click(object sender, RoutedEventArgs e)
        {
            int offset = 10;

            Parallel.Invoke(
                () => DarkenChannel(Channels.Blue, offset),
                () => DarkenChannel(Channels.Green, offset),
                () => DarkenChannel(Channels.Red, offset));
            UpdateView();
        }

        private void EnchanceContrastBt_Click(object sender, RoutedEventArgs e)
        {
            int offset = 10;

            Parallel.Invoke(
                () => EnchanceContrastChannel(Channels.Blue, offset),
                () => EnchanceContrastChannel(Channels.Green, offset),
                () => EnchanceContrastChannel(Channels.Red, offset));
            UpdateView();
        }

        private void GammaCompressionBt_Click(object sender, RoutedEventArgs e)
        {
            double gamma = 0.5;

            Parallel.Invoke(
                () => CorrectGammaChannel(Channels.Blue, gamma),
                () => CorrectGammaChannel(Channels.Green, gamma),
                () => CorrectGammaChannel(Channels.Red, gamma));
            UpdateView();
        }

        private void GammaExpansionBt_Click(object sender, RoutedEventArgs e)
        {
            double gamma = 2;

            Parallel.Invoke(
                () => CorrectGammaChannel(Channels.Blue, gamma),
                () => CorrectGammaChannel(Channels.Green, gamma),
                () => CorrectGammaChannel(Channels.Red, gamma));
            UpdateView();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tif";
                saveFileDialog.FilterIndex = 2;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ProcessedImg.Save(saveFileDialog.FileName);
                }
            }

        }

        private void ResetFiltersBt_Click(object sender, RoutedEventArgs e)
        {
            if (OriginalImg != null)
            {
                ProcessedImg = new BitmapImage();
                ProcessedImg.BeginInit();
                ProcessedImg.UriSource = OriginalImg.UriSource;
                ProcessedImg.EndInit();
                ProcessedImg.CopyPixels(imageData, stride, 0);

                ProcessedImageView.Source = ProcessedImg;

                isGrayscale = false;
            }
        }

        private void SimpleBlurBt_Click(object sender, RoutedEventArgs e)
        {
            Convolution simpleBlur = new Convolution(new double[,] { {1, 1, 1}, {1, 1, 1}, {1, 1, 1} });

            Parallel.Invoke(
                () => Convolve(Channels.Blue, simpleBlur, simpleBlur.KernelSum),
                () => Convolve(Channels.Green, simpleBlur, simpleBlur.KernelSum),
                () => Convolve(Channels.Red, simpleBlur, simpleBlur.KernelSum));
            UpdateView();
        }

        private void GaussianBlurBt_Click(object sender, RoutedEventArgs e)
        {
            int size = 3;
            int stdev = 10;
            double[,] kernel = new double[size, size];

            for (int j = -size / 2; j <= size / 2; j++)
            {
                for (int k = -size / 2; k <= size / 2; k++)
                {
                    kernel[j + size / 2, k + size / 2] = Math.Exp((-1) * (double)(j * j + k * k) / (2 * stdev * stdev)) / (2 * Math.PI * stdev * stdev);
                }
            }

            Convolution gaussianBlur = new Convolution(kernel);

            Parallel.Invoke(
                () => Convolve(Channels.Blue, gaussianBlur, gaussianBlur.KernelSum),
                () => Convolve(Channels.Green, gaussianBlur, gaussianBlur.KernelSum),
                () => Convolve(Channels.Red, gaussianBlur, gaussianBlur.KernelSum));
            UpdateView();
        }

        private void SharpenBt_Click(object sender, RoutedEventArgs e)
        {
            int size = 3;
            double[,] kernel = new double[size, size];

            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    kernel[j, k] = -1; 

                }
            }
            kernel[size / 2, size / 2] = size * size;

            
            Convolution sharpen = new Convolution(kernel);

            Parallel.Invoke(
                () => Convolve(Channels.Blue, sharpen),
                () => Convolve(Channels.Green, sharpen),
                () => Convolve(Channels.Red, sharpen));
            UpdateView();
        }

        private void DetectEdgesBt_Click(object sender, RoutedEventArgs e)
        {
            int size = 3;
            double[,] kernel = new double[size, size];

            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    kernel[j, k] = 0; 
                }
            }
            kernel[size / 2, 0] = -1;
            kernel[size / 2, size / 2] = 1;

            Convolution conv = new Convolution(kernel);

            Parallel.Invoke(
                () => Convolve(Channels.Blue, conv),
                () => Convolve(Channels.Green, conv),
                () => Convolve(Channels.Red, conv));
            UpdateView();
        }

        private void EmbossBt_Click(object sender, RoutedEventArgs e)
        {
            int size = 3;
            double[,] kernel = new double[size, size];

            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    if(k == 0)
                        kernel[j, k] = -1;
                    else
                        kernel[j, k] = 1;
                }
            }
            kernel[0, size / 2] = 0;
            kernel[size - 1, size / 2] = 0;

            Convolution conv = new Convolution(kernel);

            Parallel.Invoke(
                () => Convolve(Channels.Blue, conv, conv.KernelSum),
                () => Convolve(Channels.Green, conv, conv.KernelSum),
                () => Convolve(Channels.Red, conv, conv.KernelSum));
            UpdateView();
        }

        private void FilterEditor_Click(object sender, RoutedEventArgs e)
        {
            FilterEditor filterEditorWindow = new FilterEditor();
            filterEditorWindow.Show();

        }

        // In-class part
        private byte[] OtherConvolve(Channels channel, Convolution convolution, double normalizingSum = 1)
        {
            // Assuming imageData is not null
            byte[] convolvedPixels = new byte[imageData.Length / 4];

            for (int i = (int)channel; i < imageData.Length; i += 4)
            {
                double sum = 0;
                for (int j = -convolution.Size / 2; j <= convolution.Size / 2; j++)
                {
                    for (int k = -convolution.Size / 2; k <= convolution.Size / 2; k++)
                    {
                        if(i + j * stride + 4 * k >= 0 && i + j * stride + 4 * k < imageData.Length)
                            sum += imageData[i + j * stride + 4 * k] * convolution.Kernel[j + convolution.Size / 2, k + convolution.Size / 2];
                    }
                }
                convolvedPixels[i / 4] = (byte)(Math.Abs(sum) / normalizingSum);
            }

            return convolvedPixels;
        }
        private void TaskFilterBt_Click(object sender, RoutedEventArgs e)
        {
            Convolution hConv = new Convolution(new double[,] { {0, -1, 0}, {0, 1, 0}, {0, 0, 0} });
            Convolution vConv = new Convolution(new double[,] { {0, 0, 0}, {-1, 1, 0}, {0, 0, 0} });

            byte[] Bhres = OtherConvolve(Channels.Blue, hConv);
            byte[] Ghres = OtherConvolve(Channels.Green, hConv);
            byte[] Rhres = OtherConvolve(Channels.Red, hConv);

            byte[] Bvres = OtherConvolve(Channels.Blue, vConv);
            byte[] Gvres = OtherConvolve(Channels.Green, vConv);
            byte[] Rvres = OtherConvolve(Channels.Red, vConv);

            int threshold = 80;

            for (int i = 0; i < imageData.Length; i += 4)
            {

                if (Bhres[i / 4] > threshold || Ghres[i / 4] > threshold || Rhres[i / 4] > threshold ||
                    Bvres[i / 4] > threshold || Gvres[i / 4] > threshold || Rvres[i / 4] > threshold)
                {
                    imageData[i + (int)Channels.Blue] = 255;
                    imageData[i + (int)Channels.Green] = 255;
                    imageData[i + (int)Channels.Red] = 255;
                }
                else
                {
                    imageData[i + (int)Channels.Blue] = 0;
                    imageData[i + (int)Channels.Green] = 0;
                    imageData[i + (int)Channels.Red] = 0;
                }
            }

            UpdateView();
        }

        // Task 2
        private void ResizeBt_Click(object sender, RoutedEventArgs e)
        {
            if(ProcessedImageView.Height != OriginalImg.Height)
            {
                ProcessedImageView.Height = OriginalImg.Height;
                OriginalImageView.Height = OriginalImg.Height;

                //OriginalImageScrollViewer.ScrollToHorizontalOffset(OriginalImageScrollViewer.ScrollableHeight / 2);
                //OriginalImageScrollViewer.ScrollToHorizontalOffset(OriginalImageScrollViewer.ScrollableWidth / 2);
                //ProcessedImageScrollViewer.ScrollToHorizontalOffset(ProcessedImageScrollViewer.ScrollableHeight / 2);
                //ProcessedImageScrollViewer.ScrollToHorizontalOffset(ProcessedImageScrollViewer.ScrollableWidth / 2);
            }
            else
            {
                ProcessedImageView.Height = (double)App.Current.Resources["DefaultImageViewHeight"];
                OriginalImageView.Height = (double)App.Current.Resources["DefaultImageViewHeight"];
            }


        }

        private void GrayscaleBt_Click(object sender, RoutedEventArgs e)
        {
            double RCoefficient = 0.3;
            double GCoefficient = 0.6;
            double BCoefficient = 0.1;

            // Assuming imageData is not null
            for (int i = 0; i < imageData.Length; i += 4)
            {
                int GrayscaleIntensity = (int)(RCoefficient * imageData[i + (int)Channels.Red]
                                             + GCoefficient * imageData[i + (int)Channels.Green] 
                                             + BCoefficient * imageData[i + (int)Channels.Blue]);
                Debug.Assert(GrayscaleIntensity >= 0 && GrayscaleIntensity <= 255);

                imageData[i + (int)Channels.Red] = (byte)GrayscaleIntensity;
                imageData[i + (int)Channels.Green] = (byte)GrayscaleIntensity;
                imageData[i + (int)Channels.Blue] = (byte)GrayscaleIntensity;
            }

            isGrayscale = true;
            UpdateView();
        }

        private void AverageDitherBt_Click(object sender, RoutedEventArgs e)
        {
            int k = 2;

            Parallel.Invoke(
                () => DitherChannel(Channels.Blue, k),
                () => DitherChannel(Channels.Green, k),
                () => DitherChannel(Channels.Red, k));
            UpdateView();

        }
            
        private void DitherChannel(Channels channel, int k)
        {
            Debug.Assert(k >= 2 && k <= 254);

            // Assuming imageData is not null
            for (int i = (int)channel; i < imageData.Length; i += 4)
            {
                imageData[i] = (byte)((int)Math.Floor((int)imageData[i] / (256.0 / k)) * 255.0 / (k - 1));
            }

        }

        private void KMeansBt_Click(object sender, RoutedEventArgs e)
        {
            int k = 2;
            int[] centroids = new int[k * 3];
            int[] newCentroids = new int[k * 3];
            int[] newCentroidsCounts;
            Random rnd = new Random(1);
            int[] pixelAssignedCentroids = new int[imageData.Length / 4];
            
            // Initialize centroids randomly
            for(int i = 0; i < newCentroids.Length; i++)
                newCentroids[i] = rnd.Next(0, 256);

            do
            {
                newCentroids.CopyTo(centroids, 0);
                newCentroidsCounts = new int[k];
                newCentroids = new int[k * 3];

                // Assign points to centroids
                for (int i = 0; i < imageData.Length; i += 4)
                {
                    HashSet<(double, int)> distances = new HashSet<(double, int)>();


                    // Determine the number of the assigned centroid
                    for (int j = 0; j < k; j++)
                        distances.Add((SquareEuclideanDistance(imageData[i + (int)Channels.Red],
                                                               imageData[i + (int)Channels.Green],
                                                               imageData[i + (int)Channels.Blue],
                                                               centroids[j + (int)Channels.Red],
                                                               centroids[j + (int)Channels.Green],
                                                               centroids[j + (int)Channels.Blue]), j));

                    pixelAssignedCentroids[i / 4] = distances.Min().Item2;


                    // Increment the sum & the count for the assigned centroid
                    newCentroids[pixelAssignedCentroids[i / 4] * 3 + (int)Channels.Red] += imageData[i + (int)Channels.Red];
                    newCentroids[pixelAssignedCentroids[i / 4] * 3 + (int)Channels.Green] += imageData[i + (int)Channels.Green];
                    newCentroids[pixelAssignedCentroids[i / 4] * 3 + (int)Channels.Blue] += imageData[i + (int)Channels.Blue];
                    newCentroidsCounts[pixelAssignedCentroids[i / 4]]++;
                }


                // Calculate new centroids
                for (int i = 0; i < newCentroids.Length; i += 3)
                {
                    if (newCentroidsCounts[i / 3] != 0)
                    {
                        newCentroids[i + (int)Channels.Red] = (int)(newCentroids[i + (int)Channels.Red] / (double)newCentroidsCounts[i / 3]);
                        newCentroids[i + (int)Channels.Green] = (int)(newCentroids[i + (int)Channels.Green] / (double)newCentroidsCounts[i / 3]);
                        newCentroids[i + (int)Channels.Blue] = (int)(newCentroids[i + (int)Channels.Blue] / (double)newCentroidsCounts[i / 3]);
                    }
                    else
                    {
                        newCentroids[i + (int)Channels.Red] = centroids[i + (int)Channels.Red];
                        newCentroids[i + (int)Channels.Blue] = centroids[i + (int)Channels.Blue];
                        newCentroids[i + (int)Channels.Green] = centroids[i + (int)Channels.Green];
                    }
                         
                }

                // Replace the colors with the centroid values after convergence of the algorithm
                if (Enumerable.SequenceEqual(centroids, newCentroids))
                {
                    for (int i = 0; i < imageData.Length; i += 4)
                    {
                        imageData[i + (int)Channels.Red] = (byte)centroids[pixelAssignedCentroids[i / 4] * 3 + (int)Channels.Red];
                        imageData[i + (int)Channels.Green] = (byte)centroids[pixelAssignedCentroids[i / 4] * 3 + (int)Channels.Green];
                        imageData[i + (int)Channels.Blue] = (byte)centroids[pixelAssignedCentroids[i / 4] * 3 + (int)Channels.Blue];
                    }
                    break;
                }

            }
            while (true);

            UpdateView();
        }

        private double SquareEuclideanDistance(int Ax, int Ay, int Az, int Bx, int By, int Bz)
        {
            return (Math.Pow(Ax - Bx, 2) + Math.Pow(Ay - By, 2) + Math.Pow(Az - Bz, 2));
        }

        private void ColorWheelBt_Click(object sender, RoutedEventArgs e)
        {
            int n = 500;

            byte[] wheelData = new byte[n * n * 3];
            
            for(int i = 0; i < 3 * n; i += 3)
                for (int j = 0; j < 3 * n; j += 3)
                {
                    (int R, int G, int B) = RGBtoHSV(i - n / 2, j - n / 2, n);

                    wheelData[i * n + j + (int)Channels.Red] = (byte)R;
                    wheelData[i * n + j + (int)Channels.Green] = (byte)G;
                    wheelData[i * n + j + (int)Channels.Blue] = (byte)B;
                }

            imageData = wheelData;
            stride = (n * 24 + 7) / 8;
            UpdateView(n);
        }

        private (int R, int G, int B) RGBtoHSV(int x, int y, int n)
        {
            double S = Math.Sqrt(SquareEuclideanDistance(0, 0, 0, x, y, 0));

            if (S > (double)n / 2)
                return (255, 255, 255);

            double H = Math.Atan((double)y / x);

            if (x < 0 && y > 0)
                H = Math.PI - H;
            else if (x < 0 && y < 0)
                H += Math.PI;
            else if (x > 0 && y < 0)
                H = 2 * Math.PI - H;


            double M = 255;
            double m = M * (1 - S);
            double z = (M - m) * (1 - Math.Abs(H / Math.PI / 3 % 2 - 1));

            int R, G, B;

            if (H >= 0 && H < Math.PI / 3)
            {
                R = (int)M;
                G = (int)(z + M);
                B = (int)m;
            }
            else if (H >= Math.PI / 3 && H < Math.PI * 2 / 3)
            {
                R = (int)(z + M);
                G = (int)M;
                B = (int)m;
            }
            else if (H >= Math.PI * 2 / 3 && H < Math.PI)
            {
                R = (int)m;
                G = (int)M;
                B = (int)(z + M);
            }
            else if (H >= Math.PI && H < Math.PI * 4 / 3)
            {
                R = (int)m;
                G = (int)(z + M);
                B = (int)M;

            }
            else if (H >= Math.PI * 4 / 3 && H < Math.PI * 5 / 3)
            {
                R = (int)(z + M);
                G = (int)m;
                B = (int)M;

            }
            else
            {
                R = (int)M;
                G = (int)m;
                B = (int)(z + m);
            }
            

            return (R, G, B);
        }
    }
}
