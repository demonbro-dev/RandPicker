//
//
// MainWindow.xaml.cs : RandPicker 程序主页面后台实现
//
//
using demonbro.UniLibs;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using static demonbro.UniLibs.AppConfig;
using System.Windows.Threading;

namespace RandPicker
{
    public partial class MainWindow : Window
    {
        public Brush TextColor
        {
            get => nameLabel.Foreground;
            set => nameLabel.Foreground = value;
        }
        public Brush BorderColor
        {
            get => nameBorder.BorderBrush;
            set => nameBorder.BorderBrush = value;
        }
        public MainWindow() : this(initialText: null)
        {
        }
        private PickerLogic logic;

        public MainWindow(string initialText = null, string initialList = null)
        {
            InitializeComponent();
            {
                if (nameLabel == null || startButton == null ||
        topMostCheckBox == null || listComboBox == null)
                {
                    MessageBox.Show("UI控件未正确初始化");
                    return;
                }
                mainFrame.Navigating += MainFrame_Navigating;
                this.SizeChanged += MainWindow_SizeChanged; // 添加窗口大小变化事件
                UpdateAnimations(); // 初始化动画参数

                var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config.json");
                var config = ConfigurationManager.LoadConfig(configPath)
                               .WithWpfAdaptations();
                BorderColor = new SolidColorBrush(UniLibsAdapter.FromHex(config.BorderColor));
                if (config.DefaultPage == DefaultPageMode.MultiPick)
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        MultiPickButton_Click(null, null);
                    }), DispatcherPriority.Loaded);
                }

                // 初始化PickerLogic
                this.Loaded += (s, e) =>
                {
                    logic = new PickerLogic(this, nameLabel, startButton, topMostCheckBox, listComboBox);

                    if (!string.IsNullOrEmpty(initialList))
                    {
                        logic.SwitchCurrentList(initialList);
                    }

                    if (!string.IsNullOrEmpty(initialText))
                    {
                        nameLabel.Text = initialText;
                        logic.CurrentDisplayText = initialText;
                    }
                };
                // 绑定speedSlider
                speedSlider.ValueChanged += (sender, e) =>
                logic.UpdateSpeed(e.NewValue);

                // 初始勾选框状态处理
                topMostCheckBox.Checked -= TopMostCheckBox_Checked;
                topMostCheckBox.Checked += TopMostCheckBox_Checked;
                topMostCheckBox.Unchecked += TopMostCheckBox_Unchecked;

                this.Closed += (s, e) => logic.Cleanup();
                this.Closing += MainWindow_Closing;
            };
        }

        private bool _isSubMenuOpen = false;
        private bool _isAnimating = false;
        private void SubMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating) return;

            _isAnimating = true;
            subMenuButton.IsEnabled = false; // 在播放动画时禁用按钮以防连续点击导致Init出bug

            try
            {
                this.BeginInit();

                var storyboard = _isSubMenuOpen ?
                    (Storyboard)FindResource("SlideDownAnimation") :
                    (Storyboard)FindResource("SlideUpAnimation");

                if (!_isSubMenuOpen)
                {
                    subMenuPanel.Visibility = Visibility.Visible;
                    subMenuTransform.Y = 100;
                    subMenuPanel.Opacity = 0;
                }

                EventHandler completionHandler = null;
                completionHandler = (s, args) =>
                {
                    storyboard.Completed -= completionHandler;
                    try
                    {
                        if (_isSubMenuOpen)
                        {
                            displayTransform.Y = -200;
                            buttonTransform.Y = -200;
                            subMenuTransform.Y = 0;
                        }
                        else
                        {
                            displayTransform.Y = 0;
                            buttonTransform.Y = 0;
                            subMenuPanel.Visibility = Visibility.Collapsed;
                        }
                    }
                    finally
                    {
                        this.EndInit();
                        _isAnimating = false;
                        subMenuButton.IsEnabled = true; // 重新启用按钮
                    }
                };

                storyboard.Completed += completionHandler;

                // 更新子菜单状态
                _isSubMenuOpen = !_isSubMenuOpen;
                subMenuButton.Content = _isSubMenuOpen ? "关闭子菜单" : "打开子菜单";

                storyboard.Begin(this);
            }
            catch
            {
                this.EndInit();
                _isAnimating = false;
                subMenuButton.IsEnabled = true;
                throw;
            }
        }

        // 隐藏与显示MainWindow初始界面的方法
        public void HideOriginalUI() => originalContentGrid.Visibility = Visibility.Collapsed;
        public void ShowOriginalUI() => originalContentGrid.Visibility = Visibility.Visible;
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateAnimations(); // 窗口大小变化时更新动画参数
        }
        private void UpdateAnimations()
        {
            var width = this.ActualWidth;

            // 更新SlideToLeft动画
            var slideToLeft = (Storyboard)Resources["SlideToLeft"];
            ((DoubleAnimation)slideToLeft.Children[0]).To = -width;
            ((DoubleAnimation)slideToLeft.Children[1]).From = width;

            // 更新SlideFromLeft动画
            var slideFromLeft = (Storyboard)Resources["SlideFromLeft"];
            ((DoubleAnimation)slideFromLeft.Children[0]).From = -width;
            ((DoubleAnimation)slideFromLeft.Children[1]).To = width;
        }
        private void BtnOpenNameListManager_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            UpdateAnimations();
            var storyboard = (Storyboard)FindResource("SlideToLeft");
            frameContainer.Visibility = Visibility.Visible;
            mainFrame.Navigate(new NameListPage());
            storyboard.Completed += (s, _) =>
            {
                HideOriginalUI();
                this.IsEnabled = true;
            };
            storyboard.Begin(this);
        }
        private void MultiPickButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            UpdateAnimations();
            var storyboard = (Storyboard)FindResource("SlideToLeft");
            frameContainer.Visibility = Visibility.Visible; // 显示容器
            mainFrame.Navigate(new MultiPickMode(logic));

            storyboard.Completed += (s, _) =>
            {
                HideOriginalUI();
                this.IsEnabled = true;
            };
            storyboard.Begin(this);
        }
        private void CVPickButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            UpdateAnimations();
            var storyboard = (Storyboard)FindResource("SlideToLeft");
            frameContainer.Visibility = Visibility.Visible;
            mainFrame.Navigate(new CVPick()); // 直接创建实例即可

            storyboard.Completed += (s, _) =>
            {
                HideOriginalUI();
                this.IsEnabled = true;
            };
            storyboard.Begin(this);
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            var settingsWindow = new Settings(configPath)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            settingsWindow.ShowDialog();
        }
        private void MainFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            // 离开页面时释放资源
            if (mainFrame.Content is CVPick cvPick)
            {
                cvPick.Dispose();
            }
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 释放当前活动页面中的CVPick
            if (mainFrame.Content is CVPick cvPick)
            {
                cvPick.Dispose();
            }
        }
        public void PlayReturnAnimation(bool isMultiPickMode = false) // 保持与原有参数兼容
        {
            this.IsEnabled = false;
            ShowOriginalUI();
            var storyboard = (Storyboard)FindResource("SlideFromLeft");
            storyboard.Completed += (s, _) =>
            {
                frameContainer.Visibility = Visibility.Collapsed;
                this.IsEnabled = true;
            };
            storyboard.Begin(this);
        }
        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string updaterDir = Path.GetFullPath(Path.Combine(baseDir, "..", "RandUpdater"));
                string updaterPath = Path.Combine(updaterDir, "RandUpdater.exe");

                if (!File.Exists(updaterPath))
                {
                    MessageBox.Show($"找不到更新程序路径: {updaterPath}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterPath,
                    WorkingDirectory = updaterDir  
                });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动更新程序失败: {ex.Message}",
                    "操作异常",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        public void ReturnToMain()
        {
            mainFrame.Visibility = Visibility.Collapsed;
            nameLabel.Visibility = Visibility.Visible;
            startButton.Visibility = Visibility.Visible;
        }
        public void ReloadLists()
        {
            logic?.ReloadLists(); // 调用PickerLogic的重新加载方法
        }
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new About();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void TopMostCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var floatWindow = new FloatWindow(logic.CurrentDisplayText, logic.CurrentList);
            floatWindow.TextColor = this.TextColor; // 传递颜色
            floatWindow.Owner = this; // 设置Owner为当前MainWindow
            floatWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; // 居中显示
            Application.Current.MainWindow = floatWindow; // 更新主窗口引用
            floatWindow.Show();
            floatWindow.Owner = null;
            this.Close();
        }
        private void TopMostCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        // 列表选择变更事件
        private void ListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listComboBox.SelectedItem == null || logic == null)
                return;

            // 获取选中项并更新逻
            string selectedList = listComboBox.SelectedItem.ToString();
            logic.SwitchCurrentList(selectedList);
        }
    }
}