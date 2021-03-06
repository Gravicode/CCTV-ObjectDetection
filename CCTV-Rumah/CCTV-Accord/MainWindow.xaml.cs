﻿using System;
using System.Collections.Generic;
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

//using Ozeki.Media;
//using Ozeki.Camera;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Net.Http;
using Newtonsoft.Json;
using System.Configuration;
using Accord.Video.FFMPEG;
using System.Drawing;
using System.IO;
//using Emgu.CV;
//using Emgu.CV.Structure;

namespace CCTV_Accord
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        #region Variables
        static bool IsPlaying { set; get; } = false;
        static EngineContainer ApiContainer = new EngineContainer();
        static HttpClient client = new HttpClient();
        //static CascadeClassifier _localObjectDetector = new CascadeClassifier("Data/haarcascade_frontalface_alt2.xml");

        //head
        //static CascadeClassifier _localObjectDetector = new CascadeClassifier("Data/cascadeH5.xml");

        //human
        //static CascadeClassifier _localObjectDetector = new CascadeClassifier("Data/cascadG.xml");

        //head and shoulder
        private static CascadeClassifier _localObjectDetector = new CascadeClassifier("Data/haarcascade_upperbody.xml");

        static AzureBlobHelper BlobEngine = new AzureBlobHelper();

        //VideoViewerWPF[] videoViewer = new VideoViewerWPF[APPCONTANTS.CameraCount];


        static Dictionary<string, string> FrameId = new Dictionary<string, string>();
        #endregion

        #region Forms
        public MainWindow()
        {
            InitializeComponent();

            var DurationParams = ConfigurationManager.AppSettings["CheckDuration"].Split(':');

            //video cctv wpf
            //_drawingImageProvider[i] = new DrawingImageProvider();
            //videoViewer[i].SetImageProvider(_drawingImageProvider[i]);

            //frame for analysis

            //_frameHandler[i].SetInterval(new TimeSpan(int.Parse(DurationParams[0]), int.Parse(DurationParams[1]), int.Parse(DurationParams[2])));





            ApiContainer.Register<ComputerVisionService>(new ComputerVisionService());
            ConnectBtn.Click += ConnectBtn_Click;
            DisconnectBtn.Click += DisconnectBtn_Click;
            DisconnectBtn.IsEnabled = false;
            //_localObjectDetector.Load("Data/haarcascade_frontalface_alt2.xml");
            //_localObjectDetector.Load("Data/haarcascade_upperbody.xml");

        }
        private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {

            IsPlaying = true;
            ConnectBtn.IsEnabled = false;
            DisconnectBtn.IsEnabled = true;
            await DoPlaying();

        }
        /*
        private void SaveFrameButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Portable Network Graphics (*.png)|*.png";
            dialog.DefaultExt = "png";
            dialog.AddExtension = true;

            var result = dialog.ShowDialog();
            if (result.HasValue == false || result.Value == false) return;

            if (File.Exists(dialog.FileName))
                File.Delete(dialog.FileName);

            using (var fileStream = File.OpenWrite(dialog.FileName))
            {
                var encoder = new PngBitmapEncoder();
                var bitmap = videoViewer.GetCurrentFrame();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
        }*/

        private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            ConnectBtn.IsEnabled = true;
            DisconnectBtn.IsEnabled = false;
            IsPlaying = false;
        }
        #endregion

        #region Frame Processing
        private async Task DoPlaying()
        {

            var reader = new List<VideoFileReader>();

            for (int i = 0; i < APPCONTANTS.CameraCount; i++)
            {
                var cam = new VideoFileReader();
                reader.Add(cam);
                cam.Open(ConfigurationManager.AppSettings["cam" + (i + 1)]);
            }
            while (true)
            {
                for (int i = 0; i < APPCONTANTS.CameraCount; i++)
                {
                    Bitmap frame = reader[i].ReadVideoFrame();
                    await ProcessFrame(frame, $"cam{i + 1}");
                }
                //Do whatever with the frame...
                if (!IsPlaying) break;
                //Thread.Sleep(500);
            }
            for (int i = 0; i < APPCONTANTS.CameraCount; i++)
            {
                reader[i].Close();
            }
            //Debug.WriteLine("frame captured : " + e.DateStamp);
            //await ProcessFrame(e.ToImage(), FrameId[(sender as FrameCapture).ID]);


        }
        /*
        public BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            // bitmap.PixelFormat
            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }*/
        public static BitmapSource Convert(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }
        /*
        public static Bitmap FindPeople(Image<Bgr, Byte> image, out List<System.Drawing.Rectangle> rects)
        {
            System.Drawing.Rectangle[] regions;
            //regions = _localObjectDetector.DetectMultiScale(image);

            //detect human
            //regions = _localObjectDetector.DetectMultiScale(image,scaleFactor:1.04,minNeighbors:4,minSize:new System.Drawing.Size(30,80),maxSize:new System.Drawing.Size(80,200));
            // human, 1.04, 4, 0 | 1, Size(30, 80), Size(80,200));

            //detect head
            regions = _localObjectDetector.DetectMultiScale(image, scaleFactor: 1.1, minNeighbors: 4, minSize: new System.Drawing.Size(20, 20), maxSize: new System.Drawing.Size(60, 60));
            //head, 1.1, 4, 0 | 1, Size(40, 40), Size(100, 100));

            //upper body
            //detect human
            //regions = _localObjectDetector.DetectMultiScale(image, scaleFactor: 1.05, minNeighbors: 4, minSize: new System.Drawing.Size(100, 68), maxSize: new System.Drawing.Size(200, 171));


            foreach (var rec in regions)
            {
                image.Draw(rec, new Bgr(System.Drawing.Color.Red), 2);
            }
            rects = new List<System.Drawing.Rectangle>(regions);
            return image.ToBitmap();
        }
        */
        /*
        public static Bitmap FindPeople(Image<Bgr, Byte> image, out List<System.Drawing.Rectangle> rects)
        {
            MCvObjectDetection[] regions;

            var Rects = new List<System.Drawing.Rectangle>();
            {  //this is the CPU version
                using (HOGDescriptor des = new HOGDescriptor())
                {
                    des.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
                    regions = des.DetectMultiScale(image, winStride : new System.Drawing.Size (4, 4),
                    padding : new System.Drawing.Size(8, 8), scale : 1.05);
                }
            }


            foreach (var pedestrain in regions)
            {
                var rec = pedestrain.Rect;
                Debug.WriteLine("detect : " + pedestrain.Score);
                image.Draw(rec, new Bgr(System.Drawing.Color.Red), 2);
                Rects.Add(rec);
            }
            rects = Rects;
            return image.ToBitmap();
        }
        */

        public static System.Drawing.Bitmap FindPeople(Bitmap bmp, out OpenCvSharp.Rect[] Rect)
        {
            Mat mat = bmp.ToMat();
            //var rects = _localObjectDetector.DetectMultiScale(image: mat, scaleFactor: 1.05, minNeighbors: 4, flags: HaarDetectionType.DoCannyPruning,
            //        minSize: new OpenCvSharp.Size(10, 10), maxSize: new OpenCvSharp.Size(60, 60));
            var rects = _localObjectDetector.DetectMultiScale(image: mat, scaleFactor: 1.05, minNeighbors: 4, flags: HaarDetectionType.DoCannyPruning,
                    minSize: new OpenCvSharp.Size(60, 60), maxSize: new OpenCvSharp.Size(200, 200));
            Rect = rects;
            foreach (var rec in rects)
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawRectangle(Pens.Red, new System.Drawing.Rectangle(rec.Left, rec.Top, rec.Width, rec.Height));
                }
            }
            return bmp;
        }
        async Task ProcessFrame(System.Drawing.Image image, string CamName)
        {
            string BlobName = CamName + DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss") + ".jpg";

            Bitmap bmp = new Bitmap(image, new System.Drawing.Size(600, 337));

            //emgu cv
            //List<System.Drawing.Rectangle> rects;
            //Image<Bgr, Byte> img = new Image<Bgr, byte>(bmp);
            //bmp = FindPeople(img, out rects);

            //opencv3
            OpenCvSharp.Rect[] rects=null;
            bmp = FindPeople(bmp, out rects);
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                switch (CamName)
                {
                    case "cam1":
                        Cam1.Source = Convert(bmp);
                        break;
                    case "cam2":
                        Cam2.Source = Convert(bmp);
                        break;
                    case "cam3":
                        Cam3.Source = Convert(bmp);
                        break;
                    case "cam4":
                        Cam4.Source = Convert(bmp);
                        break;
                }
            }));
            //call computer vision
            if (rects != null)
            {
                Debug.WriteLine($"person detected = {rects.Length}");
                
                if (rects.Length > 0)
                {
                    //call computer vision

                    bmp.Save("Photos/" + BlobName);

                    var res = await ApiContainer.GetApi<ComputerVisionService>().RecognizeImage("Photos/" + BlobName);
                    res = res.ToLower();
                    bool PeopleExistx = false;
                    if (res.Contains("person") || res.Contains("people") || res.Contains("man") || res.Contains("woman") || res.Contains("kid") || res.Contains("baby"))
                    {
                        PeopleExistx = true;
                        MemoryStream ms = new MemoryStream();
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                        var IsUploaded = await BlobEngine.UploadFile(ms, BlobName);
                        string UrlImg = "https://storagemurahaje.blob.core.windows.net/cctv/" + BlobName;
                        if (IsUploaded)
                        {
                            await PostToCloud(new CCTVData() { camName = CamName, description = res, imageUrl = UrlImg, tanggal = DateTime.Now });
                        }
                    }
                    //if (!PeopleExistx) return;
                   
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        res = CamName + ":" + res;
                        TxtDetect.Document.Blocks.Clear();
                        TxtDetect.Document.Blocks.Add(new Paragraph(new Run(res)));
                    }));



                    //File.Delete("Photos/"+BlobName);
                }

            }
        }
        #endregion

        #region Cloud Processing
        async Task PostToCloud(CCTVData item)
        {
            string Url = "http://gravicodeabsensiweb.azurewebsites.net/api/CCTVs";
            var res = await client.PostAsync(Url, new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json"));
            if (res.IsSuccessStatusCode)
            {
                Debug.WriteLine("post to azure succeed");
            }
        }

        #endregion
    }

    public class CCTVData
    {
        public int id { get; set; }
        public string camName { get; set; }
        public string description { get; set; }
        public DateTime tanggal { get; set; }
        public string imageUrl { get; set; }
    }
}