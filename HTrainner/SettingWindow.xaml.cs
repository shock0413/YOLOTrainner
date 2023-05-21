using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Utill;

namespace HsrAITrainner
{
    /// <summary>
    /// SettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingWindow : MetroWindow, INotifyPropertyChanged
    {
        private IniFile config;
        private IniFile ini;

        public event EventHandler Confirmed;
        public event EventHandler Canceled;

        public int Score { get { return score; } set { score = value; NotifyPropertyChanged("Score"); } }
        private int score = 0;

        public string ReserveStartTimeStr { get { return reserveStartTimeStr; } set { reserveStartTimeStr = value; } }
        private string reserveStartTimeStr = "";

        public string ReserveEndTimeStr { get { return reserveEndTimeStr; } set { reserveEndTimeStr = value; } }
        private string reserveEndTimeStr = "";

        public int ReserveStartHour { get { return reserveStartHour; } set { reserveStartHour = value; NotifyPropertyChanged("ReserveStartHour"); } }
        private int reserveStartHour;
        public int ReserveStartMinute { get { return reserveStartMinute; } set { reserveStartMinute = value; NotifyPropertyChanged("ReserveStartMinute"); } }
        private int reserveStartMinute;

        public int ReserveEndHour { get { return reserveEndHour; } set { reserveEndHour = value; NotifyPropertyChanged("ReserveEndHour"); } }
        private int reserveEndHour;

        public int ReserveEndMinute { get { return reserveEndMinute; } set { reserveEndMinute = value; NotifyPropertyChanged("ReserveEndMinute"); } }
        private int reserveEndMinute;

        public bool EnableReserveEnd { get { return enableReserveEnd; } set { enableReserveEnd = value; NotifyPropertyChanged("EnableReserveEnd"); } }
        private bool enableReserveEnd = false;

        public string ReserveStartDateStr { get { return reserveStartDateStr; } set { reserveStartDateStr = value; NotifyPropertyChanged("ReserveStartDateStr"); } }
        private string reserveStartDateStr = "";

        public Visibility ReserveStartDateVisibility { get { return reserveStartDateVisibility; } set { reserveStartDateVisibility = value; NotifyPropertyChanged("ReserveStartDateVisibility"); } }
        private Visibility reserveStartDateVisibility = Visibility.Collapsed;

        public Visibility ReserveEndDateVisibility { get { return reserveEndDateVisibility; } set { reserveEndDateVisibility = value; NotifyPropertyChanged("ReserveEndDateVisibility"); } }
        private Visibility reserveEndDateVisibility = Visibility.Collapsed;

        public DateTime ReserveStartDateTime { get; set; }

        public DateTime ReserveEndDateTime { get; set; }

        public string ReserveEndDateStr { get { return reserveEndDateStr; } set { reserveEndDateStr = value; NotifyPropertyChanged("ReserveEndDateStr"); } }
        private string reserveEndDateStr = "";

        public bool EnableReserveDate
        {
            get
            {
                return enableReserveDate;
            }
            set
            {
                enableReserveDate = value;

                if (value)
                {
                    ReserveStartDateVisibility = Visibility.Visible;
                    ReserveEndDateVisibility = Visibility.Visible;
                }
                else
                {
                    ReserveStartDateVisibility = Visibility.Collapsed;
                    ReserveEndDateVisibility = Visibility.Collapsed;
                }

                NotifyPropertyChanged("EnableReserveDate");
            }
        }
        private bool enableReserveDate = false;

        public SettingWindow()
        {
            config = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "Detector\\Config.ini");
            ini = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "Config.ini");

            Score = config.GetInt32("Info", "Score", 50);

            EnableReserveEnd = ini.GetBoolian("Train", "EnableReserveEnd", false);

            EnableReserveDate = ini.GetBoolian("Train", "EnableReserveDate", false);

            ReserveStartTimeStr = ini.GetString("Train", "ReserveStartTime", "01:00");
            ReserveEndTimeStr = ini.GetString("Train", "ReserveEndTime", "06:00");

            string[] reserveTimeSpt = ReserveStartTimeStr.Split(':');

            ReserveStartHour = Convert.ToInt32(reserveTimeSpt[0]);
            ReserveStartMinute = Convert.ToInt32(reserveTimeSpt[1]);

            string[] reserveEndTimeSpt = ReserveEndTimeStr.Split(':');

            ReserveEndHour = Convert.ToInt32(reserveEndTimeSpt[0]);
            ReserveEndMinute = Convert.ToInt32(reserveEndTimeSpt[1]);

            ReserveStartDateTime = DateTime.Parse(ini.GetString("Train", "ReserveStartDate", "2021-12-02"));
            ReserveEndDateTime = DateTime.Parse(ini.GetString("Train", "ReserveEndDate", "2021-12-02"));

            ReserveStartDateStr = ReserveStartDateTime.ToString("yyyy-MM-dd");
            ReserveEndDateStr = ReserveEndDateTime.ToString("yyyy-MM-dd");

            InitializeComponent();

            this.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            config.WriteValue("Info", "Score", Score);

            ini.WriteValue("Train", "EnableReserveEnd", EnableReserveEnd);

            ini.WriteValue("Train", "EnableReserveDate", EnableReserveDate);

            ReserveStartTimeStr = String.Format("{0:00}:{1:00}", ReserveStartHour, ReserveStartMinute);

            ini.WriteValue("Train", "ReserveStartTime", ReserveStartTimeStr);

            ReserveEndTimeStr = String.Format("{0:00}:{1:00}", ReserveEndHour, ReserveEndMinute);

            ini.WriteValue("Train", "ReserveEndTime", ReserveEndTimeStr);

            ReserveStartDateStr = ReserveStartDateTime.ToString("yyyy-MM-dd");
            ReserveEndDateStr = ReserveEndDateTime.ToString("yyyy-MM-dd");

            ini.WriteValue("Train", "ReserveStartDate", ReserveStartDateStr);
            ini.WriteValue("Train", "ReserveEndDate", ReserveEndDateStr);

            this.DialogResult = true;
            this.Close();
            
        }

        public void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
