using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Windows.Threading;
using System.Data;
using System.Collections;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;

//预先存入的wavelengthArray?


namespace maya
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer = new DispatcherTimer();
        private OmniDriver.CCoWrapper wrapper;
        private double[] xCoord = new double[2068];
        private double[] yCoord = new double[2068];
        private EnumerableDataSource<double> xDataSource;
        private EnumerableDataSource<double> datasDataSource;
        private CompositeDataSource compositeDataSource;

        
        private ObservableDataSource<Point> datapoint = new ObservableDataSource<Point>();

        private int waveIndex;   // index
        private double collectDataTime = 0.1;

        protected double[] data;             //light intensity
        protected double[] wavelengthArray;  //real wavelength
        int numberOfSpectrometers;

        private ArrayList datalist = new ArrayList();
       
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void On_MouseMove(object sender, MouseEventArgs e)
        {
            vl.Value = dp.Position.X;
            waveIndex = wavelengthToIndex(dp.Position.X);
            textBox2.Text = wavelengthArray[(int)waveIndex].ToString();
            intensityIndicator.Content = yCoord[(int)waveIndex].ToString();
        } 

        

        private void open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog opendialog = new OpenFileDialog();
            opendialog.Filter = "MAYA光谱文件|*.maya|所有文件|*.*";
            opendialog.DefaultExt = "maya";
           
            opendialog.Title = "打开";
            opendialog.ValidateNames = true;

            opendialog.CheckFileExists = true;

            if (opendialog.ShowDialog().Value)
            {
                FileStream fi = new FileStream(opendialog.FileName, FileMode.Open, FileAccess.Read);
                BinaryFormatter bf = new BinaryFormatter();
                datalist = bf.Deserialize(fi) as ArrayList;
                    

                DataTable dtbl = new DataTable();
                plotSpectrum(((DataUnit)datalist[0]).da);
                waveIndex = 0;
                showLightIntensity_Click(sender, e);
            }
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            stopWork_Click(sender, e);
            SaveFileDialog savedialog = new SaveFileDialog();
            savedialog.Filter = "MAYA光谱文件|*.maya|所有文件|*.*";
            savedialog.DefaultExt = "maya";
            savedialog.AddExtension = true;
            
            savedialog.OverwritePrompt = true;
            savedialog.Title = "保存";
            savedialog.ValidateNames = true;


            if (savedialog.ShowDialog().Value)
            {
                if (datalist == null)
                {
                    MessageBox.Show("no data");
                    return;
                }
                FileStream fo = new FileStream(savedialog.FileName, FileMode.Create, FileAccess.Write); 
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fo,datalist);
                fo.Close();

                MessageBox.Show("数据已保存", "saved" );
            }

        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult key = MessageBox.Show(
               "确定要退出?", "confirm",
               MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            bool cancel;
            cancel = (key == MessageBoxResult.No);
            if (cancel)
                return;
            else
                this.Close();
        }

        private void beginWork_Click(object sender, RoutedEventArgs e)
        {
            timer.Interval = TimeSpan.FromSeconds(collectDataTime);
            timer.Tick += new EventHandler(plotSpectrum);       //可以通过timer来控制显示操作
            timer.IsEnabled = true;   
        }

        private void stopWork_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            dp.Position = new System.Windows.Point(wavelengthArray[0], ((DataUnit)datalist[0]).da[0]);
        }

        private void btn_beginWork_Click(object sender, RoutedEventArgs e)
        {
            beginWork_Click(sender, e);
        }

        private void plotSpectrum(object sender , EventArgs e)
        {
            data = ((double[])wrapper.getSpectrum(0));
            for (int i = 0; i < data.Length; ++i)
            {
               xCoord[i] = wavelengthArray[i];
               yCoord[i] = data[i];
            }

            DataUnit ds = new DataUnit(data);
            datalist.Add(ds);
            datasDataSource.RaiseDataChanged();
        }

        private void plotSpectrum(double[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
               xCoord[i] = wavelengthArray[i];
               yCoord[i] = data[i];
            }
            datasDataSource.RaiseDataChanged();
        }
     
        private void showLightIntensity_Click(object sender, RoutedEventArgs e)  //对应单个波长随时间的变化关系
        {
            if(datalist == null)
            {
                MessageBox.Show("没有数据");
                return;
            }
            
            datapoint.Collection.Clear(); //清除点
            for (int j = 0; j < datalist.Count; j++)
            {
                double x = j;    //da为每隔0.1s(默认，可改）存入datalist一次
                double y = ((DataUnit)datalist[j]).da[(int)waveIndex];
                Point p = new Point(x, y);
                datapoint.AppendAsync(base.Dispatcher, p);    //在datapoint里面新加入一个点
            }          
            plotter2.Viewport.FitToView();
            dp2.Position = new System.Windows.Point(0, ((DataUnit)datalist[0]).da[(int)waveIndex]);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            wrapper = new OmniDriver.CCoWrapper();
            wrapper.setIntegrationTime(0, 100000);
            txt_intergrationTime.Text = "0.1";
            txt_collectDataTime.Text = "0.1";
            numberOfSpectrometers = wrapper.openAllSpectrometers();
            if (numberOfSpectrometers == 0)
            {
                MessageBox.Show("找不到光谱仪");
                textBox1.Text = "0";
                return;
            }
            else
            {
                textBox1.Text = numberOfSpectrometers.ToString();
                wavelengthArray = (double[])wrapper.getWavelengths(0);
            }
            for (int i = 0; i < 2068; ++i)
            {
                xCoord[i] = i;
                yCoord[i] = 0;
            }
            xDataSource = new EnumerableDataSource<double>(xCoord);
            xDataSource.SetXMapping(x => x);

            datasDataSource = new EnumerableDataSource<double>(yCoord);
            datasDataSource.SetYMapping(y => y);

            compositeDataSource = new CompositeDataSource(xDataSource, datasDataSource);

            plotter.AddLineGraph(compositeDataSource, new Pen(Brushes.Blue, 1), new PenDescription("光谱曲线"));
            plotter.Viewport.FitToView();
            plotter2.AddLineGraph(datapoint, new Pen(Brushes.Red, 2), new PenDescription("亮度变化曲线"));
        }

        private int wavelengthToIndex(double wavelength)
        {
            int i = 0;
            for (; i < wavelengthArray.Length-1; i++)
            {
                if (wavelengthArray[i] > dp.Position.X)
                    break;
            }
            return i;
        }

        private void showTable_certainData_Click(object sender, RoutedEventArgs e)
        {

            timer.Stop();
            if (datalist == null)
            {
                MessageBox.Show("没有数据");
                return;
            }

            DataTable tbl = new DataTable();
            tbl.Columns.Add("time");
            tbl.Columns.Add("light Intesity");

            for (int m = 0; m < datalist.Count; m++)
            {
                tbl.Rows.Add((m*collectDataTime).ToString()+"s", ((DataUnit)datalist[m]).da[(int)waveIndex]);
            }

            ShowTable showTbl = new ShowTable(tbl);
            showTbl.Show();

        }

        private void showTable_allData_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            if (datalist == null)
            {
                MessageBox.Show("没有数据");
                return;
            }

   //         int wl;
            DataTable dtbl = new DataTable();
            dtbl.Columns.Add("time");
            for (int i = 0; i < 2068; i++)
            {
                dtbl.Columns.Add(i.ToString() , typeof(double));
                //将i换为String.Format( "{0:F} ",wavelengthArray[i])却不行。。。
            }
                
            for (int j = 0; j < datalist.Count; j++)
            {
                DataRow cRow = dtbl.NewRow();
                object[] rowData = new object[2069];
                rowData[0] = j;
                for (int m = 0; m < 2068; m++)
                {
                    rowData[m+1] = ((DataUnit)datalist[j]).da[m];
                }
                cRow.ItemArray = rowData;
                dtbl.Rows.Add(cRow);
             }

            ShowTable showTbl = new ShowTable(dtbl);
            showTbl.Show();
        }


        private void changeWavelength_Click(object sender, RoutedEventArgs e)
        {
            waveIndex = wavelengthToIndex(double.Parse(textBox2.Text));
            vl.Value = double.Parse(textBox2.Text);
            dp.Position = new Point(vl.Value, ((DataUnit)datalist[0]).da[waveIndex]);
            showLightIntensity_Click(sender, e);
            intensityIndicator.Content = yCoord[(int)waveIndex].ToString();
        }

        private void tblShowAll_Click(object sender, RoutedEventArgs e)
        {
            showTable_allData_Click(sender, e);
        }

        private void dp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            showLightIntensity_Click(sender, e);
        }

        private void dp2_MouseMove(object sender, MouseEventArgs e)
        {
            vl2.Value = dp2.Position.X;
        }

        private void dp2_MouseUp(object sender, MouseButtonEventArgs e)
        {
            vl2.Value = dp2.Position.X;
            if(dp2.Position.X>=0 && dp2.Position.X<datalist.Count)
                plotSpectrum(((DataUnit)datalist[(int)dp2.Position.X]).da);
        }

        private void changeIntergrationTime(double param_intergtationTime)
        {
            wrapper.setIntegrationTime(0,(int)(1000000*param_intergtationTime));
        }

        private void changeCollectDataTime(double param_collectDataTime)
        {
            collectDataTime = param_collectDataTime;
        }

        private void mnu_changeIntergrationTime_Click(object sender, RoutedEventArgs e)
        {
            changeIntergrationTime(double.Parse(txt_intergrationTime.Text));
        }

        private void mnu_changeCollectDataTime_Click(object sender, RoutedEventArgs e)
        {
            changeCollectDataTime(double.Parse(txt_collectDataTime.Text));
        }

        private void txt_collectDataTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            changeCollectDataTime(double.Parse(txt_collectDataTime.Text));
        }

        private void txt_intergrationTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            changeIntergrationTime(double.Parse(txt_intergrationTime.Text));
        }

    }
}

 