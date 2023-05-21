using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;

namespace HsrAITrainner
{
    /// <summary>
    /// TrainDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TrainDialog : CustomDialog
    {
        public event EventHandler Canceled;
        public event EventHandler Settinged;
        public bool IsCanceled { get { return m_IsCanceled; } }
        private bool m_IsCanceled = false;

        public TrainDialog()
        {
            InitializeComponent();
        }

        public void SetTitle(string title)
        {
            Dispatcher.Invoke(() =>
            {
                Title.Text = title;
            });
        }

        public void SetMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                Message.Text = message;
            });
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            m_IsCanceled = true;
            Canceled(sender, e);
            CancelBtn.IsEnabled = false;
            SetEnableSettingButton(false);
        }

        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {
            Settinged(sender, e);
        }

        public void SetEnableSettingButton(bool enable)
        {
            Dispatcher.Invoke(() =>
            {
                SettingBtn.IsEnabled = enable;
            });
        }

        public void SetEnableCancelButton(bool enable)
        {
            Dispatcher.Invoke(() =>
            {
                CancelBtn.IsEnabled = enable;
            });
        }
    }
}
