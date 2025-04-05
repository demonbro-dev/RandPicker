//
//
// MultiPickMode.xaml.cs : RandPicker 多抽选模式页面后台实现
//
//
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Diagnostics;

namespace RandPicker
{
    public partial class MultiPickMode : Page
    {
        private PickerLogic _logic;
        private DispatcherTimer _timer;
        private bool _isRunning = false;
        private ObservableCollection<string> _results = new ObservableCollection<string>();
        private Random _random = new Random();

        public MultiPickMode(PickerLogic logic)
        {
            InitializeComponent();
            _logic = logic;
            resultItemsControl.ItemsSource = _results;
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(25)
            };
            _timer.Tick += (s, e) =>
            {
                try
                {
                    if (!int.TryParse(countTextBox.Text, out int count) || count < 1) return;

                    var currentList = _logic.GetCurrentList();
                    if (currentList.Count == 0)
                    {
                        _results.Clear();
                        _results.Add("无数据");
                        return;
                    }

                    // 避免抽选到多个相同名字
                    // 复制当前列表避免修改原始数据
                    var tempList = new List<string>(currentList);
                    _results.Clear();

                    int requiredCount = Math.Min(count, tempList.Count);
                    for (int i = 0; i < requiredCount; i++)
                    {
                        int index = _random.Next(tempList.Count);
                        _results.Add(tempList[index]);
                        tempList.RemoveAt(index); // 移除已选名字
                    }

                    // 补充提示（如果需求数量超过列表长度）
                    if (count > requiredCount)
                    {
                        for (int i = requiredCount; i < count; i++)
                        {
                            _results.Add("无更多数据");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Timer Error: {ex}");
                }
            };
        }

        private void StartMultiButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _timer.Stop();
                startMultiButton.Content = "开始抽选";
            }
            else
            {
                if (!ValidateInput()) return;
                _timer.Start();
                startMultiButton.Content = "停止抽选";
            }
            _isRunning = !_isRunning;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ShowOriginalUI();
                mainWindow.mainFrame.Visibility = Visibility.Collapsed;
            }
        }
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(countTextBox.Text, out int currentValue))
            {
                currentValue++;
                countTextBox.Text = currentValue.ToString();
            }
            else
            {
                countTextBox.Text = "1";
            }
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(countTextBox.Text, out int currentValue))
            {
                currentValue = Math.Max(1, currentValue - 1);
                countTextBox.Text = currentValue.ToString();
            }
            else
            {
                countTextBox.Text = "1";
            }
        }
        private bool ValidateInput()
        {
            if (!int.TryParse(countTextBox.Text, out int count) || count < 1)
            {
                MessageBox.Show("请输入有效的抽选人数（≥1）", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
    }
}