using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace AquaWPF
{
    /// <summary>
    /// Interaction logic for Widget.xaml
    /// </summary>
    public partial class Widget : Window, ISetupModeReceiver
    {
        public string _id = Guid.NewGuid().ToString();
        public bool _isTransparent = false;

        public Widget(string htmlPath = "")
        {
            InitializeComponent();
            InitializeHTML(htmlPath);
            SetupMode(false);
        }

        private void InitializeHTML(string htmlPath)
        {
            string htmlName = Path.GetFileNameWithoutExtension(htmlPath);
            this.Name = htmlName;
            
            Uri uri = new Uri($"file:///{htmlPath.Replace("\\", "/")}");
            webView2.Source = uri;
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        public void SetupMode(bool enabled)
        {
            var visibility = enabled ? Visibility.Visible : Visibility.Hidden;
            ResizeBox.Visibility = visibility;
            //FrameBox.Visibility = visibility;
            TopBarBox.Visibility = visibility;
            FrameBoxBorder.Visibility = visibility;

            var resizeMode = enabled ? ResizeMode.CanResizeWithGrip : ResizeMode.CanMinimize;
            this.ResizeMode = resizeMode;

            var opacity = enabled ? 0.4 : 0.01;
            FrameBox.Opacity = opacity;

            if (this._isTransparent) FrameBox.Opacity = 0;
        }

        public void InitializeTransparent()
        {
            if (this._isTransparent) FrameBox.Opacity = 0;

            if (this._isTransparent) clickThroughItem.IsChecked = true;
        }

        private void webView2_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView2.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            webView2.CoreWebView2.Settings.IsWebMessageEnabled = false;
            webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

            InitializeTransparent();
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void clickThroughItem_Click(object sender, RoutedEventArgs e)
        {
            this._isTransparent = clickThroughItem.IsChecked;
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            webView2.Visibility = Visibility.Hidden;
            var anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.35));
            anim.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, anim);
        }
    }
}
