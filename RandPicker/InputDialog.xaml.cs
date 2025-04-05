//
//
// InputDialog.xaml.cs : NameListPage中的输入对话框窗口实现
//
//
using System.Windows;

namespace NameListManager
{
    public partial class InputDialog : Window
    {
        public string? InputText { get; private set; }

        public InputDialog(string prompt)
        {
            InitializeComponent();
            tbPrompt.Text = prompt;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            InputText = txtInput.Text;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}