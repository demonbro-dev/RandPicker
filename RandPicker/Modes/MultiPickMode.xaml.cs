﻿using RandPicker.SubModules;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace RandPicker.Modes
{
    public partial class MultiPickMode : Page
    {
        private PickerLogic _logic;
        private DispatcherTimer _timer;
        private bool _isRunning = false;
        private ObservableCollection<string> _results = new ObservableCollection<string>();
        private Random _random = new Random();
        private HashSet<string> _pickedNames = new HashSet<string>();
        private string _currentListName;

        public MultiPickMode(PickerLogic logic)
        {
            InitializeComponent();
            _logic = logic;
            resultItemsControl.ItemsSource = _results;
            SetBorderColor();
            InitializeTimer();
            _currentListName = _logic.CurrentList;
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
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

                    string currentListName = _logic.CurrentList;
                    if (_currentListName != currentListName)
                    {
                        _pickedNames.Clear();
                        _currentListName = currentListName;
                    }

                    var availableNames = currentList.Where(name => !_pickedNames.Contains(name)).ToList();

                    if (availableNames.Count < count)
                    {
                        _pickedNames.Clear();
                        availableNames = new List<string>(currentList);
                    }

                    if (availableNames.Count == 0)
                    {
                        _pickedNames.Clear();
                        availableNames = new List<string>(currentList);
                    }

                    _results.Clear();
                    int requiredCount = Math.Min(count, availableNames.Count);
                    var tempList = new List<string>(availableNames);

                    for (int i = 0; i < requiredCount; i++)
                    {
                        int index = _random.Next(tempList.Count);
                        _results.Add(tempList[index]);
                        tempList.RemoveAt(index);
                    }

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
        private void SetBorderColor()
        {
            var configPath = Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "config.json");
            var config = ConfigurationManager.LoadConfig(configPath)
                           .WithWpfAdaptations();
            BorderMultiPick.BorderBrush = new SolidColorBrush(RandPckrCoupler.FromHex(config.BorderColor));
        }

        private void InstantMultiSelection()
        {
            if (!int.TryParse(countTextBox.Text, out int count) || count < 1) return;

            var currentList = _logic.GetCurrentList();
            if (currentList.Count == 0)
            {
                _results.Clear();
                _results.Add("无数据");
                return;
            }

            // 去重逻辑优化
            var availableNames = currentList
                .Where(name => !_pickedNames.Contains(name))
                .ToList();

            if (availableNames.Count < count)
            {
                _pickedNames.Clear();
                availableNames = new List<string>(currentList);
            }

            _results.Clear();
            int requiredCount = Math.Min(count, availableNames.Count);
            var tempList = new List<string>(availableNames);

            for (int i = 0; i < requiredCount; i++)
            {
                if (tempList.Count == 0) break; // 安全防护

                int index = _random.Next(tempList.Count);
                string selectedName = tempList[index]; // 先保存值
                _results.Add(selectedName);
                tempList.RemoveAt(index); // 再移除元素
                _pickedNames.Add(selectedName); // 使用保存的值
            }

            // 补充不足数量的占位符
            if (count > requiredCount)
            {
                for (int i = requiredCount; i < count; i++)
                {
                    _results.Add("无更多数据");
                }
            }
        }

        private void StartMultiButton_Click(object sender, RoutedEventArgs e)
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            var config = ConfigurationManager.LoadConfig(configPath);

            if (config.UseInstantMode)
            {
                InstantMultiSelection();
                startMultiButton.Content = "开始抽选";
                _isRunning = false;
            }
            else
            {
                if (_isRunning)
                {
                    _timer.Stop();
                    startMultiButton.Content = "开始抽选";

                    foreach (var item in _results)
                    {
                        if (item != "无更多数据" && item != "无数据")
                        {
                            _pickedNames.Add(item);
                        }
                    }
                }
                else
                {
                    if (!ValidateInput()) return;
                    _timer.Start();
                    startMultiButton.Content = "停止抽选";
                }
                _isRunning = !_isRunning;
            }
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.PlayReturnAnimation(isMultiPickMode: true); // 传递参数区分模式
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