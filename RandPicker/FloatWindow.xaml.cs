//
//
// FloatWindow.xaml.cs : RandPicker 程序浮动小窗模式后台实现
//
//
using System.Windows;
using System.Windows.Media;

namespace RandPicker
{
    public partial class FloatWindow : Window
    {
        public Brush TextColor
        {
            get => nameLabel.Foreground;
            set => nameLabel.Foreground = value;
        }
        private PickerLogic logic;

        public FloatWindow(string initialText = null, string initialList = null)
        {
            InitializeComponent();

            // 初始化PickerLogic
            logic = new PickerLogic(this, nameLabel, startButton, topMostCheckBox, listComboBox);

            // 恢复MainWindow中抽选文本
            if (!string.IsNullOrEmpty(initialList))
            {
                logic.SwitchCurrentList(initialList);
            }

            if (!string.IsNullOrEmpty(initialText))
            {
                nameLabel.Text = initialText;
                logic.CurrentDisplayText = initialText;
            }

            this.Closed += (s, e) =>
            {
                logic?.Cleanup();
                logic = null;
            };
        }

        private void TopMostCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void TopMostCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
            var mainWindow = new MainWindow(logic.CurrentDisplayText, logic.CurrentList);
            mainWindow.TextColor = this.TextColor;
            //
            //这两行代码理论上能实现MainWindow显示于FloatWindow处，实际上却不行
            mainWindow.Owner = this; 
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //
            Application.Current.MainWindow = mainWindow; 
            mainWindow.Owner = null;
            mainWindow.Show();
            this.Close();
        }
    }
}