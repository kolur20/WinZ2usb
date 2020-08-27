
using Resto.CashServer.Plugins;
using Resto.CashServer.Z2SlideEmulator.Readers;
using Resto.Framework.Common;
using Resto.Framework.Common.CardProcessor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace Resto.CashServer.CardSlideEmulator
{
    public sealed class CardSlideEmulatorPluginContext : ICashServerPluginContext, IDisposable
    {
        private readonly List<Window> handledWindows = new List<Window>();
        private const int MaxCardTrackLength = 255;
        private Reader reader = null;
        private bool isMainControllerExists;
        [STAThread]
        public void InitContext(PluginEnvironment pluginEnvironment)
        {
            this.isMainControllerExists = PluginEnvironmentHelper.IsMainControllerExists(pluginEnvironment);
            reader = new Reader();
            reader.InitializaeZ2();
            reader.StartNotifyTask();
        }
        [STAThread]
        public void AfterServerStarted()
        {
        }
        [STAThread]
        public void AfterModelsCreated(IServiceProvider serviceProvider)
        {
            if (!this.isMainControllerExists)
                return;
            CardSlideEmulatorPluginContext.MainWindow.KeyDown += new KeyEventHandler(this.OnWindowKeyDown);
            CardSlideEmulatorPluginContext.MainWindow.LostKeyboardFocus += new KeyboardFocusChangedEventHandler(this.RefreshChildWindows);
            
        }
        [STAThread]
        public void Dispose()
        {
            this.handledWindows.ForEach((Action<Window>)(w => w.KeyDown -= new KeyEventHandler(this.OnWindowKeyDown)));
        }
        [STAThread]
        private void RefreshChildWindows(
          object sender,
          KeyboardFocusChangedEventArgs keyboardFocusChangedEventArgs)
        {
            this.handledWindows.Where<Window>((Func<Window, bool>)(w => !CardSlideEmulatorPluginContext.MainWindow.OwnedWindows.Cast<Window>().Contains<Window>(w))).ToList<Window>().ForEach((Action<Window>)(w =>
            {
                w.KeyDown -= new KeyEventHandler(this.OnWindowKeyDown);
                this.handledWindows.Remove(w);
            }));
            CardSlideEmulatorPluginContext.MainWindow.OwnedWindows.Cast<Window>().Where<Window>((Func<Window, bool>)(w => !this.handledWindows.Contains(w))).ToList<Window>().ForEach((Action<Window>)(w =>
            {
                w.KeyDown += new KeyEventHandler(this.OnWindowKeyDown);
                this.handledWindows.Add(w);
            }));
        }
        [STAThread]
        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F6)
                UiDispatcher.ExecuteAsync((Action)(() =>
                {
                    reader.StopNotifyTask();
                    reader.InitializaeZ2();
                    reader.StartNotifyTask();
                }));
            if (e.Key != Key.F4)
                return;
            TextBox textBox = new TextBox();
            textBox.BorderBrush = (Brush)new SolidColorBrush(Colors.Black);
            textBox.BorderThickness = new Thickness(5.0);
            textBox.FontSize = 50.0;
            textBox.FontWeight = FontWeights.Bold;
            textBox.TextAlignment = TextAlignment.Center;
            textBox.MaxLength = (int)byte.MaxValue;
            this.TextBox = textBox;
            Window window = new Window();
            window.Content = (object)this.TextBox;
            window.Height = 80.0;
            window.HorizontalContentAlignment = HorizontalAlignment.Center;
            window.ResizeMode = ResizeMode.NoResize;
            window.ShowInTaskbar = false;
            window.Topmost = true;
            window.VerticalContentAlignment = VerticalAlignment.Center;
            window.Width = 400.0;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.WindowStyle = WindowStyle.None;
            window.Owner = CardSlideEmulatorPluginContext.MainWindow;
            this.Dialog = window;
            this.TextBox.Focus();
            UiDispatcher.ExecuteAsync((Action)(() =>
            {
                this.Dialog.KeyDown += new KeyEventHandler(this.OnSubWindowKeyDown);
                this.Dialog.Closing += new CancelEventHandler(this.OnSubWindowClosing);
                this.Dialog.ShowDialog();
            }));
            e.Handled = true;
        }
        [STAThread]
        private void OnSubWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
        [STAThread]
        private void OnSubWindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Return:
                    this.SubmitMe();
                    e.Handled = true;
                    break;
                case Key.Escape:
                case Key.F4:
                    this.EscapeMe();
                    e.Handled = true;
                    break;
            }
        }
        [STAThread]
        private void SubmitMe()
        {
            string track = this.TextBox.Text;
            this.EscapeMe();
            if (track.Length == 0)
                return;
            UiDispatcher.ExecuteWithMessagePumping((Action)(() => ((Resto.Front.Common.Core.CardProcessor.CardProcessor)Resto.Front.Common.Core.CardProcessor.CardProcessor.Instance).ImitateCardRolled(new MagnetTrackData(string.Empty, track, string.Empty))));
        }
        [STAThread]
        private void EscapeMe()
        {
            this.Dialog.Closing -= new CancelEventHandler(this.OnSubWindowClosing);
            this.Dialog.KeyDown -= new KeyEventHandler(this.OnSubWindowKeyDown);
            this.Dialog.Close();
            this.Dialog.Owner = (Window)null;
            this.Dialog = (Window)null;
        }

        private static Window MainWindow
        {
            get
            {
                return Application.Current.MainWindow;
            }
        }

        private Window Dialog { get; set; }

        private TextBox TextBox { get; set; }
        
    }
}
