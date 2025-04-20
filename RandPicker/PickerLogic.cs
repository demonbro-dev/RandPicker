//
//
// PickerLogic.cs : RandPicker 主程序业务逻辑实现
//
//
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using demonbro.UniLibs;
using Newtonsoft.Json;
using static demonbro.UniLibs.AppConfig;
using ComboBox = System.Windows.Controls.ComboBox;
using Window = System.Windows.Window;

namespace RandPicker
{
    public class PickerLogic
    {
        public string CurrentDisplayText { get; set; } = "点击开始";
        private ComboBox? listComboBox;
        private string _lastStoppedName = string.Empty;

        protected Window window;
        protected TextBlock nameLabel;
        protected Button startButton;
        protected CheckBox topMostCheckBox;

        protected Dictionary<string, List<string>> lists;
        protected string currentList;
        protected bool isRunning = false;
        protected DispatcherTimer timer;
        protected Random random;

        public PickerLogic(Window window, TextBlock nameLabel, Button startButton, CheckBox topMostCheckBox, ComboBox? listComboBox = null, Brush borderColor = null)
        {
            // 空值检查
            this.window = window ?? throw new ArgumentNullException(nameof(window));
            this.nameLabel = nameLabel ?? throw new ArgumentNullException(nameof(nameLabel));
            this.startButton = startButton ?? throw new ArgumentNullException(nameof(startButton));
            this.topMostCheckBox = topMostCheckBox ?? throw new ArgumentNullException(nameof(topMostCheckBox));
            this.listComboBox = listComboBox;

            InitializeLogic();
        }

        public PickerLogic()
        {
        }

        private void InitializeLogic()
        {
            LoadListData();
            InitializeTimer();
            BindEvents();
        }
        private void CreateDefaultNamelist(string filePath) //创建默认namelist的方法
        {
            var initialData = new RootObject
            {
                name_lists = new List<NameList>
        {
            new NameList
            {
                name = "列表1",
                members = new List<string> { "1", "2", "3", "4", "5" }
            },
            new NameList
            {
                name = "列表2",
                members = new List<string> { "6", "7", "8", "9", "10" }
            }
        }
            };

            var json = JsonConvert.SerializeObject(initialData, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        private class RootObject
        {
            public List<NameList> name_lists { get; set; }
        }
        private void SetFontColor()
        {
            var configPath = Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "config.json");
            var config = ConfigurationManager.LoadConfig(configPath)
                           .WithWpfAdaptations();
            nameLabel.Foreground = new SolidColorBrush(UniLibsAdapter.FromHex(config.BorderColor));
        }

        private void LoadListData()
        {
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "namelist.json");

            try
            {
                // 当namelist.json不存在时，创建默认namelist
                if (!File.Exists(jsonPath))
                {
                    CreateDefaultNamelist(jsonPath);
                    MessageBox.Show("未找到namelist.json，已创建初始名单文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                var jsonData = File.ReadAllText(jsonPath);
                var wrapper = JsonConvert.DeserializeObject<NamelistWrapper>(jsonData);
                lists = wrapper.NameLists.ToDictionary(d => d.Name, d => d.Members);

                if (lists.Count > 0)
                {
                    currentList = lists.Keys.First();

                    if (listComboBox != null)
                    {
                        listComboBox.Dispatcher.Invoke(() =>
                        {
                            listComboBox.ItemsSource = lists.Keys;
                            listComboBox.SelectedItem = currentList;
                        });
                    }
                    nameLabel.Text = "点击开始";
                }
                else
                {
                    nameLabel.Text = "无列表数据";
                }
            }
            catch (FileNotFoundException)
            {
                CreateDefaultNamelist(jsonPath);
                LoadListData();
            }
            catch (JsonException ex)
            {
                ShowError($"数据解码失败: {ex.Message}");
                lists = new Dictionary<string, List<string>>();
            }
            catch (Exception ex)
            {
                ShowError($"加载列表数据失败: {ex.Message}");
                lists = new Dictionary<string, List<string>>();
            }
        }
        public void ReloadLists()
        {
            // 重新加载namelist.json数据并更新列表的方法
            var json = File.ReadAllText("namelist.json");
            var data = JsonConvert.DeserializeObject<RootObject>(json);

            if (window is MainWindow mainWindow)
            {
                mainWindow.listComboBox.ItemsSource = data.name_lists.Select(x => x.name);
                mainWindow.listComboBox.SelectedIndex = 0;
            }
        }
        private void InitializeTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
            timer.Tick += Timer_Tick;
            random = new Random();
        }

        private void BindEvents()
        {
            startButton.Click += StartButton_Click;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                StopSelection();
            }
            else
            {
                StartSelection();
            }
            isRunning = !isRunning;
        }

        private void StartSelection()
        {
            if (lists.Count == 0)
            {
                nameLabel.Text = "无列表数据";
                return;
            }
            timer.Start();
            startButton.Content = "停止抽选";
            nameLabel.Foreground = Brushes.Black;
            nameLabel.FontWeight = FontWeights.Normal;
        }

        private void StopSelection()
        {
            timer.Stop();
            startButton.Content = "开始抽选";
            SetFontColor();
            nameLabel.FontWeight = FontWeights.Bold;
            _lastStoppedName = CurrentDisplayText;
        }
        public void SwitchCurrentList(string listName)
        {
            if (lists.ContainsKey(listName))
            {
                currentList = listName;

                if (listComboBox != null)
                {
                    listComboBox.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        listComboBox.SelectedItem = currentList;
                        listComboBox.Items.Refresh();
                    }));
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                var names = lists[currentList];
                string selectedName;

                selectedName = names[random.Next(names.Count)];
                if (names.Count == 1)
                {
                    nameLabel.Text = names[0];
                    CurrentDisplayText = names[0];
                    StopSelection();
                    MessageBox.Show("列表中只有一个名字，无法避免重复", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                // 防止抽选结果与上次重复
                if (selectedName == _lastStoppedName && names.Count > 1)
                {
                    int retryCount = 0;
                    while (selectedName == _lastStoppedName && retryCount < 3)
                    {
                        selectedName = names[random.Next(names.Count)];
                        retryCount++;
                    }
                }

                CurrentDisplayText = selectedName;
                nameLabel.Text = CurrentDisplayText;
            }
            catch (KeyNotFoundException)
            {
                CurrentDisplayText = "列表数据异常";
                nameLabel.Text = CurrentDisplayText;
                StopSelection();
            }
        }
        public void UpdateSpeed(double speedValue)
        {
            if (timer != null)
                timer.Interval = TimeSpan.FromMilliseconds(1000 / speedValue);
        }

        public void Cleanup()
        {
            timer?.Stop();
            startButton.Click -= StartButton_Click;
            timer.Tick -= Timer_Tick;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public string CurrentList
        {
            get => currentList;
            set
            {
                if (lists.ContainsKey(value))
                    currentList = value;
            }
        }
        public List<string> GetCurrentList() => lists.ContainsKey(currentList) ? lists[currentList] : new List<string>();

        public string GetRandomName()
        {
            if (lists.TryGetValue(currentList, out var list) && list.Count > 0)
                return list[random.Next(list.Count)];
            return "无数据";
        }

        private class NamelistWrapper
        {
            [JsonProperty("name_lists")]
            public List<Namelist> NameLists { get; set; }
        }

        private class Namelist
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("members")]
            public List<string> Members { get; set; }
        }
    }
}
