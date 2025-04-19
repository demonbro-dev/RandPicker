using DirectShowLib;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace RandPicker
{
    public partial class CVPick : UserControl, IDisposable
    {
        private VideoCapture? _capture;
        private CascadeClassifier? _faceCascade;
        private Thread _processingThread;
        private volatile bool _isRunning;
        private bool _isPicking;
        private Random _random = new Random();
        private readonly object _frameLock = new object();
        private bool _isResetting;
        private List<DsDevice> _videoDevices = new List<DsDevice>();
        private int _selectedCameraIndex = -1;

        public CVPick()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            EnumerateCameras();
            _faceCascade = null;
        }

        private void EnumerateCameras()
        {
            try
            {
                _videoDevices.Clear();
                var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                if (devices == null || devices.Length == 0)
                {
                    Dispatcher.Invoke(() => MessageBox.Show("未检测到可用摄像头"));
                    return;
                }

                _videoDevices.AddRange(devices);
                Dispatcher.Invoke(() =>
                {
                    CameraComboBox.ItemsSource = _videoDevices.Select(d => d.Name).ToList();
                    CameraComboBox.SelectedIndex = _videoDevices.Count > 0 ? 0 : -1;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"设备枚举失败: {ex.Message}"));
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 加载人脸检测分类器
            var cascadePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascade_frontalface_default.xml");
            if (!File.Exists(cascadePath))
            {
                MessageBox.Show("人脸检测配置文件缺失");
                return;
            }
            _faceCascade = new CascadeClassifier(cascadePath);

            if (_videoDevices.Count > 0 && _selectedCameraIndex == -1)
            {
                CameraComboBox.SelectedIndex = 0;
            }
        }

        private void CameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CameraComboBox.SelectedIndex == -1) return;

            Dispose(); // 释放旧资源
            _selectedCameraIndex = CameraComboBox.SelectedIndex;

            Task.Run(() =>
            {
                try
                {
                    // 直接使用设备索引初始化（兼容性更好）
                    _capture = new VideoCapture(_selectedCameraIndex, VideoCaptureAPIs.DSHOW);

                    if (!_capture.IsOpened())
                    {
                        Dispatcher.Invoke(() => MessageBox.Show($"无法打开摄像头: {_videoDevices[_selectedCameraIndex].Name}"));
                        return;
                    }

                    _isRunning = true;
                    _processingThread = new Thread(ProcessFrames);
                    _processingThread.Start();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"初始化错误: {ex.Message}"));
                    _capture?.Dispose();
                }
            });
        }

        private void ProcessFrames()
        {
            while (_isRunning)
            {
                try
                {
                    using (var frameMat = _capture.RetrieveMat())
                    {
                        if (frameMat == null || frameMat.Empty()) continue;
                        // 转换为灰度图并增强对比度
                        using (var grayMat = new Mat())
                        {
                            Cv2.CvtColor(frameMat, grayMat, ColorConversionCodes.BGR2GRAY);
                            Cv2.EqualizeHist(grayMat, grayMat);

                            // 人脸检测
                            var faces = _faceCascade.DetectMultiScale(
                                grayMat,
                                scaleFactor: 1.1,
                                minNeighbors: 3,
                                minSize: new OpenCvSharp.Size(50, 50)
                            );

                            // 绘制白框
                            var renderMat = frameMat.Clone();
                            if (!_isPicking)
                            {
                                foreach (var rect in faces)
                                {
                                    Cv2.Rectangle(renderMat, rect, Scalar.White, 2);
                                }
                            }

                            // 更新UI
                            Dispatcher.BeginInvoke(() =>
                            {
                                if (_isRunning)
                                {
                                    CameraImage.Source = renderMat.ToBitmapSource();
                                }
                                renderMat.Dispose();
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"帧处理错误: {ex.Message}");
                }
            }
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isResetting)
            {
                // 重置状态逻辑
                _isPicking = false;
                _isResetting = false;
                PickButton.Content = "立即抽选";

                // 重新启动摄像头线程
                _isRunning = true;
                Task.Run(() =>
                {
                    _processingThread = new Thread(ProcessFrames);
                    _processingThread.Start();
                });
                return;
            }
            if (!_isRunning) return;

            _isPicking = true;
            _isRunning = false; // 停止处理线程
            PickButton.Content = "重置抽选状态"; // 修改按钮文本
            _isResetting = true; // 标记为重置状态

            // 随机选择并保留一个白框
            var renderMat = new Mat();
            if (_capture.Retrieve(renderMat))
            {
                using (var grayMat = new Mat())
                {
                    Cv2.CvtColor(renderMat, grayMat, ColorConversionCodes.BGR2GRAY);
                    var faces = _faceCascade.DetectMultiScale(grayMat, 1.1, 3, minSize: new OpenCvSharp.Size(50, 50));

                    if (faces.Length > 0)
                    {
                        var selected = faces[_random.Next(faces.Length)];
                        Cv2.Rectangle(renderMat, selected, Scalar.White, 2);
                    }

                    CameraImage.Source = renderMat.ToBitmapSource();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
            if (System.Windows.Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.PlayReturnAnimation();
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _processingThread?.Join(500);
            _faceCascade?.Dispose();
            if (_capture != null)
            {
                _capture.Release();
                _capture.Dispose();
                _capture = null;
            }
            foreach (var device in _videoDevices)
            {
                device.Dispose();
            }
            _videoDevices.Clear();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Dispose();
    }
}