using AquaWPF.Properties;
using HandyControl.Data;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using HC = HandyControl.Controls;

namespace AquaWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Widget> _widgets = new List<Widget>();
        private List<string> _removedWidgetsID = new List<string>();
        private bool exitApp = false;
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
            //this.Hide();
        }

        private void Initialize()
        {
            LoadWidgets(Settings.Default.widgetsDirPath);
            AddFilesToSubMenu(Settings.Default.widgetsDirPath, addWidgetM);

            onStartupItem.IsChecked = Settings.Default.isOnStartup;
        }
        private void AddFilesToSubMenu(string htmlDirPath, MenuItem menuItem)
        {
            menuItem.Items.Clear();

            if (!Directory.Exists(htmlDirPath))
                return;

            string[] htmlFiles = Directory.GetFiles(htmlDirPath);

            if (htmlFiles is null)
                return;

            foreach (string htmlPath in htmlFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(htmlPath);
                var item = new MenuItem
                {
                    Header = fileName,
                    Tag = htmlPath
                };
                item.Click += (s, e) => AddWidgetModern(s, e);

                menuItem.Items.Add(item);
            }
        }

        private void CloseAllWidgets()
        {
            exitApp = true;

            foreach (var widget in _widgets)
            {
                widget.Close();
            }

            exitApp = false;
        }
        private void Info()
        {
            //System.Windows.MessageBox.Show("https://github.com/hdmmytri \n created by Hlib Dmytriiev \n build date: 2025.07.01 21:21");
            Information information = new Information();
            information.ShowDialog();
        }

        private void SetupState(bool enabled)
        {
            if(_widgets is null)
                return;

            foreach (Widget widget in _widgets)
            {
                if (widget is not ISetupModeReceiver receiver)
                    return;

                widget.SetupMode(enabled);
            }
        }

        private void BrowseWidgetsFolder()
        {
            var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok )
            {
                string path = folderDialog.FileName;
                Properties.Settings.Default.widgetsDirPath = path;
                Properties.Settings.Default.Save();
            }

            CloseAllWidgets();
            AddFilesToSubMenu(Settings.Default.widgetsDirPath, addWidgetM);
        }

        private void AddWidgetManually(WidgetProperties widgetProperties)
        {
            if (widgetProperties is null) return;

            var newWidget = new Widget(widgetProperties.tag);

            newWidget.WindowStartupLocation = WindowStartupLocation.Manual;

            newWidget.Tag = widgetProperties.tag;
            newWidget.Top = widgetProperties.top;
            newWidget.Left = widgetProperties.left;
            newWidget.Width = widgetProperties.width;
            newWidget.Height = widgetProperties.height;
            newWidget.Closing += (s, e) => WidgetClosed(s, e);

            newWidget.Show();

            newWidget._id = widgetProperties.guid;
            newWidget._isTransparent = widgetProperties.isTransparent;

            _widgets.Add(newWidget);
        }

        private async Task AddWidgetModern(object sender, EventArgs e)
        {
            if (sender is null) return;

            if (sender is not MenuItem item) return;

            if (item.Tag is null) return;

            string? htmlPath = item.Tag as string;

            if (htmlPath is null) return;

            var newWidget = new Widget(htmlPath);
            newWidget.Show();
            newWidget.Tag = htmlPath;
            newWidget.Closing += (s, e) => WidgetClosed(s, e);

            _widgets.Add(newWidget);

            WaitForWindowRenderedAsync(newWidget);

            SetupForceState(true);
        }

        public async Task WaitForWindowRenderedAsync(Window window)
        {
            var tcs = new TaskCompletionSource<object?>();

            EventHandler handler = null!;
            handler = (s, e) =>
            {
                window.ContentRendered -= handler;
                tcs.SetResult(null);
            };

            window.ContentRendered += handler;

            await tcs.Task;
        }

        private void SetupForceState(bool enabled)
        {
            setupItem.IsChecked = enabled;
            SetupState(enabled);
        }

        private void WidgetClosed(object sender, CancelEventArgs e)
        {
            if (sender is not Widget widget) return;

            if (exitApp) return;

            _removedWidgetsID.Add(widget._id);
            //RemoveJson(widget._id, Settings.Default.widgetsDirPath);
        }

        private void LoadWidgets(string HTMLPath)
        {
            if (string.IsNullOrEmpty(HTMLPath)) return;

            string jsonsPath = Path.Combine(HTMLPath, "jsons");

            if (!Directory.Exists(jsonsPath)) return; //if "jsons" dir doesn't exist return

            string[] jsonFiles = Directory.GetFiles(jsonsPath);

            if (jsonFiles.Length == 0) return;

            foreach (string jsonFile in jsonFiles)
            {
                string output = File.ReadAllText(jsonFile);

                if(output.Length == 0) continue;

                WidgetProperties widgetProperties = JsonConvert.DeserializeObject<WidgetProperties>(output);

                AddWidgetManually(widgetProperties);
            }
        }

        private void SaveWidgets(List<Widget> widgets)
        {

            foreach (Widget widget in widgets)
            {
                if (widget is null) continue;

                if (widget.Tag is null) continue;

                WidgetProperties widgetProperties = new WidgetProperties
                {
                    guid = widget._id,
                    name = widget.Name,
                    tag = widget.Tag as string,
                    top = widget.Top,
                    left = widget.Left,
                    width = widget.Width,
                    height = widget.Height,
                    isTransparent = widget._isTransparent
                };

                if (widgetProperties is null) continue;

                string serializedProperties = JsonConvert.SerializeObject(widgetProperties);

                string rootHTMLDir = Path.GetDirectoryName(widgetProperties.tag);

                string jsonDir = Path.Combine(rootHTMLDir, "jsons");

                if(!Directory.Exists(jsonDir))
                    Directory.CreateDirectory(jsonDir);

                string fileName = widgetProperties.guid + ".json";
                string outputFile = Path.Combine(jsonDir, fileName);

                File.WriteAllText(outputFile, serializedProperties);
            }

            foreach (string widgetID in _removedWidgetsID)
            {
                RemoveJson(widgetID, Settings.Default.widgetsDirPath);
            }

            HC.MessageBox.Show("Succesfully Saved", "Save Widgets", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveJson(string guid, string HTMLPath)
        {
            if (string.IsNullOrEmpty(HTMLPath)) return;

            if(string.IsNullOrEmpty(guid)) return;
            
            string jsonDir = Path.Combine(HTMLPath, "jsons");
            //Debug.WriteLine(jsonDir);
            if (!Directory.Exists(jsonDir)) return;
            
            string[] jsonFiles = Directory.GetFiles(jsonDir);

            foreach(string jsonFile in jsonFiles)
            {
                if(string.IsNullOrEmpty(jsonFile)) continue;

                if(jsonFile.Contains(guid))
                    File.Delete(jsonFile);
            }
            
        }

        void CycleOnStartupState(bool enabled)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (registryKey == null) return;

            if (enabled)
            {
                Debug.WriteLine("added to startup");
                registryKey.SetValue("Aquaview", System.Windows.Forms.Application.ExecutablePath);
            }
            else
            {
                if (registryKey.GetValue("Aquaview") == null) return;
                registryKey.DeleteValue("Aquaview");
            }

            Settings.Default.isOnStartup = onStartupItem.IsChecked;
            Settings.Default.Save();
        }
        private void ExitApp()
        {
            exitApp = true;
            System.Windows.Application.Current.Shutdown();
        }
        private void BrowseWidgets_Click(object sender, RoutedEventArgs e)
        {
            BrowseWidgetsFolder();
        }

        private void SetupWidgets_Click(object sender, RoutedEventArgs e)
        {
            SetupState(setupItem.IsChecked);
        }
        private void SaveWidgets_Click(object sender, RoutedEventArgs e)
        {
            SaveWidgets(_widgets);
        }
        private void Info_Click(object sender, RoutedEventArgs e)
        {
            Info();
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            ExitApp();
        }
        private void OnStartUp_Click(object sender, RoutedEventArgs e)
        {
            CycleOnStartupState(onStartupItem.IsChecked);
        }
    }
}