//
//
// NameListPage.xaml.cs : RandPicker 名单管理器页面后台实现
//
//
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NameListManager;
using Newtonsoft.Json;

namespace RandPicker
{
    public partial class NameListPage : UserControl
    {
        private RootObject _data;
        private string _filePath = "namelist.json";

        public NameListPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _data = JsonConvert.DeserializeObject<RootObject>(json);
                lstLists.ItemsSource = _data.name_lists;
            }
            else
            {
                _data = new RootObject { name_lists = new List<NameList>() };
            }
        }

        private void BtnAddList_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("请输入新列表名称：");
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                string input = dialog.InputText?.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("列表名称不能为空");
                    return;
                }

                if (_data.name_lists.Any(x => x.name == input))
                {
                    MessageBox.Show("列表名称已存在");
                    return;
                }

                _data.name_lists.Add(new NameList
                {
                    name = input,
                    members = new List<string>()
                });
                lstLists.Items.Refresh();
            }
        }
        private void BtnDeleteList_Click(object sender, RoutedEventArgs e)
        {
            if (lstLists.SelectedItem is NameList selectedList)
            {
                if (MessageBox.Show($"确定要删除列表 {selectedList.name} 吗？", "确认删除", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _data.name_lists.Remove(selectedList);
                    lstLists.Items.Refresh();
                    lstMembers.ItemsSource = null;
                }
            }
        }
        private void BtnImportList_Click(object sender, RoutedEventArgs e) // 列表导入按钮功能
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "文本文件|*.txt",
                Title = "选择要导入的名单文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var listName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);

                    if (string.IsNullOrWhiteSpace(listName))
                    {
                        MessageBox.Show("文件名无效");
                        return;
                    }

                    if (_data.name_lists.Any(x => x.name == listName))
                    {
                        MessageBox.Show("同名列表已存在");
                        return;
                    }

                    // 读取文件内容，并分割其中的名字
                    var content = File.ReadAllText(openFileDialog.FileName);
                    var members = content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Distinct()
                                        .ToList();

                    var newList = new NameList
                    {
                        name = listName,
                        members = members
                    };

                    _data.name_lists.Add(newList);
                    lstLists.Items.Refresh();
                    MessageBox.Show($"成功导入 {members.Count} 个成员");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败: {ex.Message}");
                }
            }
        }

        private void LstLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstLists.SelectedItem is NameList selectedList)
            {
                lstMembers.ItemsSource = selectedList.members;
            }
        }

        private void BtnAddMember_Click(object sender, RoutedEventArgs e)
        {
            if (lstLists.SelectedItem is NameList selectedList)
            {
                var dialog = new InputDialog("请输入新成员姓名：");
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    string newMember = dialog.InputText.Trim();

                    if (string.IsNullOrWhiteSpace(newMember))
                    {
                        MessageBox.Show("成员姓名不能为空");
                        return;
                    }

                    if (selectedList.members.Contains(newMember))
                    {
                        MessageBox.Show("该成员已存在");
                        return;
                    }

                    selectedList.members.Add(newMember);
                    lstMembers.Items.Refresh();
                }
            }
        }
        private void BtnDeleteMember_Click(object sender, RoutedEventArgs e)
        {
            if (lstLists.SelectedItem is NameList selectedList && lstMembers.SelectedItem is string selectedMember)
            {
                selectedList.members.Remove(selectedMember);
                lstMembers.Items.Refresh();
            }
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                File.WriteAllText(_filePath, json);

                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.ReloadLists();
                    MessageBox.Show("保存成功，请重启RandPicker以应用");
                }
            }
            catch
            {
                MessageBox.Show("保存失败，请检查文件是否被占用");
            }
        }
        private void BtnReturn_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.mainFrame.Visibility = Visibility.Collapsed;
                mainWindow.ShowOriginalUI();
            }
        }
    }
}