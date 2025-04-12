//
//
// MainWindow.xaml.cs : RandPicker 程序主页面后台实现
//
//
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RandPicker
{
    public partial class MainWindow : Window
    {
        public Brush TextColor
        {
            get => nameLabel.Foreground;
            set => nameLabel.Foreground = value;
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
            };
            mainFrame.Navigated += (s, e) =>
            {
                // 如果导航到非NameListPage，恢复原界面
                if (e.Content is not NameListPage)
                    ShowOriginalUI();
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

        private void BtnOpenNameListManager_Click(object sender, RoutedEventArgs e)
        {
            HideOriginalUI();
            mainFrame.Visibility = Visibility.Visible;
            mainFrame.Navigate(new NameListPage());
        }
        private void MultiPickButton_Click(object sender, RoutedEventArgs e)
        {
            HideOriginalUI();
            mainFrame.Visibility = Visibility.Visible;
            mainFrame.Navigate(new MultiPickMode(logic));
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