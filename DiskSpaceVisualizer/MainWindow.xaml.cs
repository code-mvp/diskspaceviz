using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Collections;
using System.Windows.Controls.DataVisualization.Charting;
using Visifire.Charts;
using System.Windows.Threading;

namespace DiskSpaceVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TreeViewItem dummyNode = new TreeViewItem();
        bool isAnalyzing = false;
        
        public MainWindow()
        {
            InitializeComponent();
            PopulateTree();
            InitGraph();

            cboChartType.SelectedIndex = 0;
        }

        void InitGraph()
        {
            DataSeries ds = new DataSeries();

            ds.ShowInLegend = true;
            ds.RenderAs = RenderAs.Pie;
            ds.DataSource = "{Binding}";

            DataMapping m1 = new DataMapping();
            m1.MemberName = "AxisXLabel";
            m1.Path = "Key";

            ds.DataMappings = new DataMappingCollection();
            ds.DataMappings.Add(m1);


            DataMapping m2 = new DataMapping();
            m2.MemberName = "YValue";
            m2.Path = "Value";

            ds.DataMappings.Add(m2);

            



            chart.Series.Add(ds);
            
        }

        void PopulateTree()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = drive.ToString();
                
                item.Tag = drive;

                item.Expanded += new RoutedEventHandler(Node_Expanded);
                
                item.Items.Add("*");
                
                
                tvwExplorer.Items.Add(item);

            }

        }

        void Node_Expanded(object sender, RoutedEventArgs e)
        {
            isAnalyzing = true;

            TreeViewItem item = (TreeViewItem)e.OriginalSource;
            //item.Items.Clear();

            if (item.Items.Count == 1 && item.Items[0].ToString() == "*")
            {
                item.Items.Clear();
            }

            DirectoryInfo dir;
            
            if (item.Tag is DriveInfo)
            {
                DriveInfo drive = (DriveInfo)item.Tag;
                if (!drive.IsReady)
                    return;

                dir = drive.RootDirectory;
                //tbSize.Text = drive.TotalSize.ToString();
            }
            else
            {
                dir = (DirectoryInfo)item.Tag;
                //tbSize.Text = DirSize(dir).ToString();
            }

            try
            {
                ArrayList data = new ArrayList();
            
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    if (!isAnalyzing)
                        return;

                    if (subDir.Attributes == (FileAttributes.System | FileAttributes.Hidden | FileAttributes.Directory))
                    {
                        continue;
                    }
                
                    TreeViewItem newItem = new TreeViewItem();
                    newItem.Tag = subDir;
                    newItem.Header = subDir.ToString();
                    newItem.Items.Add("*");
                    item.Items.Add(newItem);

                    KeyValuePair<string, long> graph = new KeyValuePair<string, long>(subDir.ToString(), DirSize(subDir));
                    data.Add(graph);
                }

                DrawChart(data);
                tbSize.Text = "Status : Analyzing Completed";
            }
            catch(Exception ex)
            {
                tbSize.Text = ex.Message;
                // An exception could be thrown in this code if you don't
                // have sufficient security permissions for a file or directory.
                // You can catch and then ignore this exception.
            }

        }

        private void DrawChart(ArrayList chartData)
        {
            //((PieSeries)chart.Series[0]).ItemsSource = chartData.ToArray();
            //chart.DataContext = chartData.ToArray();

            DrawPieChart(chartData);
        }

        private void DrawPieChart(ArrayList chartData)
        {
            
            chart.Series[0].RenderAs = RenderAs.Pie;
            chart.Series[0].DataSource = chartData.ToArray();
            chart.Series[0].ToolTipText = "Size: #YValue";     
        }

        public  long DirSize(DirectoryInfo d)
        {
            long Size = 0;
            // Add file sizes.
            try
            {
                if (d.Attributes != FileAttributes.System)
                {
                    FileInfo[] fis = d.GetFiles();
                    foreach (FileInfo fi in fis)
                    {
                        Size += fi.Length;
                        
                    }
                    // Add subdirectory sizes.
                    DirectoryInfo[] dis = d.GetDirectories();
                    foreach (DirectoryInfo di in dis)
                    {
                        if (!isAnalyzing)
                            break;

                        tbSize.Text = "Analyzing {" + di.FullName + "}";
                        tbSize.UpdateLayout();
                        Dispatcher.Invoke(DispatcherPriority.Background, new Action( delegate {}));

                        Size += DirSize(di);
                    }
                }
            }
            catch { }
            return (Size);
        }

        private void cboChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((cboChartType.SelectedItem as ComboBoxItem).Content.ToString())
            {
                case "Pie":
                    chart.Series[0].RenderAs = RenderAs.Pie;
                    break;
                case "Column":
                    chart.Series[0].RenderAs = RenderAs.Column;
                    
                    break;
                case "Bar":
                    chart.Series[0].RenderAs = RenderAs.Bar;
                    break;
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            isAnalyzing = false;            
        }

        private void btnShowDayStats_Click(object sender, RoutedEventArgs e)
        {
            
            if (string.IsNullOrEmpty(txtPath.Text))
            {
                MessageBox.Show("Please enter the path");
                return;
            }

            int lastNDays = Convert.ToInt16(txtLastNDays.Text);

            var files = GetDirectories(txtPath.Text, txtLastNDays.Text);

            if (null == files)
            {
                MessageBox.Show("No files found.");
                return;
            }

            var enumerator = files.GetEnumerator();

            ArrayList datas = new ArrayList();
            while (enumerator.MoveNext())
            {
                DiskItem item = enumerator.Current;
                
                KeyValuePair<string, long> graph = new KeyValuePair<string, long>(item.Name, item.Size);
                datas.Add(graph);    
   
            }
            DrawChart(datas);
        }

        static DateTime GetExplorerFileDate(string fileFullPath)
        {
            TimeSpan localOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            return (new FileInfo(fileFullPath)).LastWriteTimeUtc + localOffset;
        }

        static DateTime GetExplorerFileDate(FileInfo fi)
        {
            TimeSpan localOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            return fi.LastWriteTimeUtc + localOffset;
        }

        static DateTime GetExplorerFolderDate(DirectoryInfo di)
        {
            TimeSpan localOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            return di.LastWriteTimeUtc + localOffset;
        }



        private static IEnumerable<DiskItem> GetFilesFromDirectory(string dirPath)
        {
            //List<FileItem> files = new List<FileItem>();

            try
            {
                DirectoryInfo Dir = new DirectoryInfo(dirPath);
                FileInfo[] FileList = Dir.GetFiles("*.*", SearchOption.AllDirectories);

                var files = from FI in FileList
                        where GetExplorerFileDate(FI).Date == DateTime.Now.Date.Subtract(TimeSpan.FromDays(10))
                        select new DiskItem { Name = FI.FullName, LastWriteDateTime = FI.LastWriteTime, Size = FI.Length };

                return files;
                        
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }


        private IEnumerable<DiskItem> GetDirectories(string dirPath, string ndays)
        {
            //List<FileItem> files = new List<FileItem>();

            int fromDays = 10;

            if (!string.IsNullOrEmpty(ndays))
                fromDays = Convert.ToInt16(ndays);

            try
            {
                DirectoryInfo Dir = new DirectoryInfo(dirPath);
                DirectoryInfo[] dirs = Dir.GetDirectories();


                var dirList = from di in dirs
                            where GetExplorerFolderDate(di).Date >= DateTime.Now.Date.Subtract(TimeSpan.FromDays(fromDays))
                            select new DiskItem { Name = di.FullName, LastWriteDateTime = di.LastWriteTime, Size = DirSize(di) };

                return dirList;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }
    }

    class DiskItem
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime LastWriteDateTime { get; set; }
    }

}
