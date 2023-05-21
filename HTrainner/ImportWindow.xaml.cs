using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using MahApps.Metro.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using static HsrAITrainner.MainWindow;
using Utill;
using OpenCvSharp;
using MahApps.Metro.Controls.Dialogs;

namespace HsrAITrainner
{
    /// <summary>
    /// ImportWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ImportWindow : MetroWindow, INotifyPropertyChanged
    {
        private IniFile m_Config = null;

        private string path = "";
        private GridLength oldLength = new GridLength(500, GridUnitType.Pixel);

        private ObservableCollection<StructResult> m_LabelList = null;

        private bool m_IsUnlockAllSelect = false;

        public bool ShowDialogResult { get { return m_ShowDialogResult; } set { m_ShowDialogResult = value; } }
        private bool m_ShowDialogResult = false;

        public int Rotation { get { return m_Config.GetInt32("IMPORT", "Rotation", 0); } set { m_Config.WriteValue("IMPORT", "Rotation", 0); NotifyPropertyChanged("Rotation"); } }

        public ImportWindow(ObservableCollection<StructResult> list)
        {
            InitializeComponent();
            this.m_LabelList = list;
            cb.Checked += delegate
            {
                if (m_IsUnlockAllSelect)
                {
                    m_IsUnlockAllSelect = false;
                }

                if (Items.Count > 0)
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        Items[i].IsUse = true;
                    }
                }

                CollectionViewSource.GetDefaultView(dg.ItemsSource).Refresh();
            };
            cb.Unchecked += delegate
            {
                if (!m_IsUnlockAllSelect)
                {
                    if (Items.Count > 0)
                    {
                        for (int i = 0; i < Items.Count; i++)
                        {
                            Items[i].IsUse = false;
                        }
                    }

                    CollectionViewSource.GetDefaultView(dg.ItemsSource).Refresh();
                }
            };

            m_Config = new IniFile("Config.ini");
        }

        public ObservableCollection<StructItem> Items = new ObservableCollection<StructItem>();
        public ObservableCollection<bool> Checks = new ObservableCollection<bool>();

        public ImportWindow()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public async void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;

            Items.Clear();

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                path = dialog.FileName;
                m_Path.Text = path;

                string[] files = Directory.GetFiles(path);

                ProgressDialogController controller = await this.ShowProgressAsync("불러오는 중 ...", "불러오는 중입니다 ...", true);
                controller.SetCancelable(true);
                controller.Minimum = 0;
                controller.Maximum = files.Length;
                int progress = 0;

                int cnt = 0;

                await Task.Run(() =>
                {
                    files.ToList().ForEach(x =>
                    {
                        if (controller.IsCanceled)
                        {
                            return;
                        }

                        if (x.EndsWith(".jpg") || x.EndsWith("jpeg") || x.EndsWith(".bmp") || x.EndsWith(".png"))
                        {
                            switch (cnt)
                            {
                                case 0:
                                    controller.SetMessage("불러오는 중입니다 " + Environment.NewLine + x);
                                    cnt++;
                                    break;
                                case 1:
                                    controller.SetMessage("불러오는 중입니다 ." + Environment.NewLine + x);
                                    cnt++;
                                    break;
                                case 2:
                                    controller.SetMessage("불러오는 중입니다 .." + Environment.NewLine + x);
                                    cnt++;
                                    break;
                                case 3:
                                    controller.SetMessage("불러오는 중입니다 ..." + Environment.NewLine + x);
                                    cnt = 0;
                                    break;
                            }

                            string m_FileName = Path.GetFileNameWithoutExtension(x);

                            files.ToList().ForEach(y =>
                            {
                                if (y.EndsWith(".data"))
                                {
                                    string filename = Path.GetFileNameWithoutExtension(y);

                                    if (m_FileName == filename.Split('_')[0])
                                    {
                                        Mat mat = Cv2.ImRead(x);
                                        int w = mat.Width;
                                        int h = mat.Height;
                                        mat.Dispose();
                                        mat = null;

                                        ImageInfo imageInfo = new ImageInfo()
                                        {
                                            FileName = x,
                                            Width = w,
                                            Height = h,
                                            DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                            CharList = new List<CharInfo>(),
                                            IsVisble = true
                                        };

                                        StreamReader sr = new StreamReader(y);

                                        string line = "";

                                        while ((line = sr.ReadLine()) != null)
                                        {
                                            string[] _s = line.Split(',');

                                            int idx = Convert.ToInt32(_s[0]);
                                            double labeled_x = Convert.ToDouble(_s[1]);
                                            double labeled_y = Convert.ToDouble(_s[2]);
                                            double labeled_width = Convert.ToDouble(_s[3]);
                                            double labeled_height = Convert.ToDouble(_s[4]);

                                            CharInfo charInfo = new CharInfo()
                                            {
                                                Labeled_Name = m_LabelList[idx].LabelName,
                                                Labeled_X = labeled_x / w,
                                                Labeled_Y = labeled_y / h,
                                                Labeled_Width = labeled_width / w,
                                                Labeled_Height = labeled_height / h,
                                                Is_Labeled = true
                                            };

                                            if (Rotation == 0)
                                            {
                                                labeled_x = (charInfo.Labeled_X * w + (charInfo.Labeled_Width * w / 2)) / w;
                                                labeled_y = (charInfo.Labeled_Y * h + (charInfo.Labeled_Height * h / 2)) / h;
                                                labeled_width = charInfo.Labeled_Width;
                                                labeled_height = charInfo.Labeled_Height;
                                            }
                                            if (Rotation == -90)
                                            {
                                                labeled_x = (charInfo.Labeled_Y * h + charInfo.Labeled_Height * h / 2) / w;
                                                labeled_y = (h - charInfo.Labeled_X * w - (charInfo.Labeled_Width * w / 2)) / h;
                                                labeled_width = (charInfo.Labeled_Height * h) / w;
                                                labeled_height = (charInfo.Labeled_Width * w) / h;
                                            }
                                            else if (Rotation == -180)
                                            {
                                                labeled_x = (w - charInfo.Labeled_X * w - (charInfo.Labeled_Width * w / 2)) / w;
                                                labeled_y = (h - charInfo.Labeled_Y * h - (charInfo.Labeled_Height * h / 2)) / h;
                                                labeled_width = charInfo.Labeled_Width;
                                                labeled_height = charInfo.Labeled_Height;
                                            }
                                            else if (Rotation == -270)
                                            {
                                                labeled_x = (w - charInfo.Labeled_Y * h - charInfo.Labeled_Height * h / 2) / w;
                                                labeled_y = (charInfo.Labeled_X * w + (charInfo.Labeled_Width * w / 2)) / h;
                                                labeled_width = (charInfo.Labeled_Height * h) / w;
                                                labeled_height = (charInfo.Labeled_Width * w) / h;
                                            }
                                            else if (Rotation == 90)
                                            {
                                                labeled_x = (w - charInfo.Labeled_Y * h - charInfo.Labeled_Height * h / 2) / w;
                                                labeled_y = (charInfo.Labeled_X * w + (charInfo.Labeled_Width * w / 2)) / h;
                                                labeled_width = (charInfo.Labeled_Height * h) / w;
                                                labeled_height = (charInfo.Labeled_Width * w) / h;
                                            }
                                            else if (Rotation == 180)
                                            {
                                                labeled_x = (w - charInfo.Labeled_X * w - (charInfo.Labeled_Width * w / 2)) / w;
                                                labeled_y = (h - charInfo.Labeled_Y * h - (charInfo.Labeled_Height * h / 2)) / h;
                                                labeled_width = charInfo.Labeled_Width;
                                                labeled_height = charInfo.Labeled_Height;
                                            }
                                            else if (Rotation == 270)
                                            {
                                                labeled_x = (charInfo.Labeled_Y * h + charInfo.Labeled_Height * h / 2) / w;
                                                labeled_y = (h - charInfo.Labeled_X * w - (charInfo.Labeled_Width * w / 2)) / h;
                                                labeled_width = (charInfo.Labeled_Height * h) / w;
                                                labeled_height = (charInfo.Labeled_Width * w) / h;
                                            }

                                            if (labeled_width > 1)
                                            {
                                                labeled_width = 1;
                                            }

                                            if (labeled_height > 1)
                                            {
                                                labeled_height = 1;
                                            }

                                            if ((labeled_x - (labeled_width / 2) + labeled_width) > 1)
                                            {
                                                labeled_x = labeled_x - Math.Abs((labeled_x - (labeled_width / 2) + labeled_width) - 1);
                                            }

                                            if ((labeled_y - (labeled_height / 2) + labeled_height) > 1)
                                            {
                                                labeled_y = labeled_y - Math.Abs((labeled_y - (labeled_height / 2) + labeled_height) - 1);
                                            }

                                            if (labeled_x - (labeled_width / 2) < 0)
                                            {
                                                labeled_x = labeled_x + Math.Abs(labeled_x - (labeled_width / 2));
                                            }

                                            if (labeled_y - (labeled_height / 2) < 0)
                                            {
                                                labeled_y = labeled_y + Math.Abs(labeled_y - (labeled_height / 2));
                                            }

                                            charInfo.Labeled_X = labeled_x;
                                            charInfo.Labeled_Y = labeled_y;
                                            charInfo.Labeled_Width = labeled_width;
                                            charInfo.Labeled_Height = labeled_height;

                                            imageInfo.CharList.Add(charInfo);
                                        }

                                        sr.Close();
                                        sr.Dispose();
                                        sr = null;

                                        StructItem item = new StructItem()
                                        {
                                            ImageInfo = imageInfo
                                        };

                                        Items.Add(item);
                                    }
                                }
                            });
                        }

                        progress++;
                        controller.SetProgress(progress);
                    });
                });

                await controller.CloseAsync();

                dg.ItemsSource = Items;
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StructItem item = (StructItem)dg.SelectedItem;
            ImageInfo imageInfo = item.ImageInfo;
            BitmapImage bi = new BitmapImage();
            Mat mat = Cv2.ImRead(imageInfo.FileName);
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            // bi.UriSource = new Uri(imageInfo.FileName);
            bi.StreamSource = mat.ToMemoryStream();
            bi.EndInit();
            mat.Dispose();
            mat = null;

            dp.BitmapImage = bi;

            int w = imageInfo.Width;
            int h = imageInfo.Height;

            imageInfo.CharList.ForEach(x =>
            {
                if (x.Labeled_Name == "Blank")
                {

                }

                double labeled_width = x.Labeled_Width * w;
                double labeled_height = x.Labeled_Height * h;
                double labeled_x = x.Labeled_X * w - (labeled_width / 2);
                double labeled_y = x.Labeled_Y * h - (labeled_height / 2);

                dp.DrawRectangle(x.Labeled_Name, Brushes.Transparent, new Pen(Brushes.Red, 1), labeled_x, labeled_y, labeled_width, labeled_height);
            });

            if (!expander.IsExpanded)
            {
                expander.IsExpanded = true;
            }
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            RowDefinition r = this.m_Grid.RowDefinitions[2];
            r.Height = oldLength;
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            RowDefinition r = this.m_Grid.RowDefinitions[2];
            oldLength = r.Height;
            r.Height = new GridLength(0, GridUnitType.Auto);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (cb.IsChecked == true)
            {
                m_IsUnlockAllSelect = true;
                cb.IsChecked = false;
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            ShowDialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ShowDialogResult = false;
            this.Close();
        }
    }

    public class StructItem
    {
        public ImageInfo ImageInfo { get; set; }
        public bool IsUse { get; set; }
    }
}
