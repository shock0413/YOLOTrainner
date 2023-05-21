using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using MahApps.Metro.Controls;

namespace HsrAITrainner
{
    /// <summary>
    /// Window1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProjectManager : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string ProjectName { get { return m_projectName; } set { m_projectName = value; NotifyPropertyChanged("ProjectName"); } }
        private string m_projectName = "";

        public string ProjectPath { get { return m_projectPath; } set { m_projectPath = value; NotifyPropertyChanged("ProjectPath"); } }
        private string m_projectPath = "";

        public ProjectManager(string path)
        {
            InitializeComponent();

            ProjectPath = path;

            Init(ProjectPath);
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            tbProjectPath.Text = ProjectPath;
        }

        private string temp = "";

        public void Init(string m_path)
        {
            // 프로젝트 폴더의 프로젝트 리스트 로딩
            List<string> ProjectList = new List<string>();

            // m_defaultProjectPaht = defaultProjectPath;

            string[] fullPath = null;

            try
            {
                fullPath = Directory.GetDirectories(m_path);
            }
            catch
            {
                m_path = m_path.Split('\\')[0] + "\\";
                fullPath = Directory.GetDirectories("C:\\");
            }

            foreach(string path in fullPath)
            {
                string temp = path.Split('\\')[path.Split('\\').Length - 1];

                ProjectList.Add(temp);
            }

            lbProjectList.ItemsSource = ProjectList;

            temp = m_path;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ProjectName = tbProjectName.Text;

            if (Directory.Exists(ProjectPath + "\\" + ProjectName) == false)
            {
                Directory.CreateDirectory(temp + "\\" + ProjectName);
            }
            else
            {
                MessageBox.Show("이미 존재하는 프로젝트입니다.");
            }


            // 프로젝트 폴더의 프로젝트 리스트 로딩
            List<string> ProjectList = new List<string>();

            // m_defaultProjectPaht = defaultProjectPath;

            string[] fullPath = null;

            try
            {
                fullPath = Directory.GetDirectories(temp);
            }
            catch
            {
                temp = temp.Split('\\')[0] + "\\";
                fullPath = Directory.GetDirectories(temp);
            }

            foreach (string path in fullPath)
            {
                string temp = path.Split('\\')[path.Split('\\').Length - 1];

                ProjectList.Add(temp);
            }

            lbProjectList.ItemsSource = ProjectList;
            tbProjectName.Text = "";
        }

        private void lbProjectList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(lbProjectList.SelectedItem != null)
                tbProjectName.Text = lbProjectList.SelectedItem.ToString();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ProjectName = tbProjectName.Text;

            if (Directory.Exists(ProjectPath + "\\" + ProjectName) == false)
            {
                MessageBox.Show(this, "없는 프로젝트입니다.", "확인");
            }
            else if (ProjectName == "")
            {
                MessageBox.Show(this, "프로젝트를 선택하여 주십시오.", "확인");
            }
            else
            {
                ProjectName = tbProjectName.Text;
                tbProjectName.Text = ProjectName;
                this.DialogResult = true;
            }
        }

        private void LbProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                ProjectPath = dialog.FileName;

                tbProjectPath.Text = ProjectPath;

                Init(ProjectPath);

                tbProjectName.Text = "";
            }

            dialog.Dispose();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {

        }
    }
}
