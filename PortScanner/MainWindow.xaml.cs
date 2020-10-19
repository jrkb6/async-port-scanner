using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PortScanner.Model;

namespace PortScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
 
    public partial class MainWindow : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private int numberOfTasks;
        private ObservableCollection<OpenPort> items;
        ScannerExecutor scannerExecutor;
        private bool scanning = false;
        private const int maxNumberOfTasks = 4001;

        public MainWindow()
        {
            InitializeComponent();
            items = new ObservableCollection<OpenPort>();
            openPortListView.ItemsSource = items;
            logger.Info("App started..");
            stopButton.IsEnabled = false;
            clearButton.IsEnabled = false;
            numberOfTaskSlider.Maximum = maxNumberOfTasks;

        }


        private void numberOfTaskInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(numberOfTaskInput.Text, out numberOfTasks))
            {
                // If not int clear textbox text
                numberOfTaskInput.Clear();
            }
            else
            {
                if (numberOfTasks <= 0 || numberOfTasks >= maxNumberOfTasks)
                {
                    numberOfTasks = 1;
                }

                numberOfTaskInput.Text = numberOfTasks.ToString();
                numberOfTaskSlider.Value = numberOfTasks;
            }
        }

        private void UpdateButtons()
        {
            scanButton.IsEnabled = !scanning;
            stopButton.IsEnabled = scanning;
            clearButton.IsEnabled = !scanning;
        }

        private void scanButton_Click(object sender, RoutedEventArgs e)
        {
            
            DoScan(false);
        }
        private void quickScanButton_Click(object sender, RoutedEventArgs e)
        {
            
            DoScan(true);
        }




        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                scannerExecutor.StopAllTasks();
                Task.WhenAll(scannerExecutor.GetRunningTasks().ToArray());
                scanning = false;
                UpdateButtons();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            items = new ObservableCollection<OpenPort>();
            openPortListView.ItemsSource = items;
            numberOfTaskSlider.Value = 1;
            IPRangeInput.Text = "";

        }


        private void numberOfTaskSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            // Get slider Value.
            int value = (int) slider.Value;

            // ... Set Input text to changed value
            if (numberOfTaskInput != null)
            {
                numberOfTaskInput.Text = value.ToString();
            }
        }

        /// <summary>
        /// Scanner operation on ui 
        /// </summary>
        /// <param name="quickScan"></param> true if only common described ports to be scanned.
        private void DoScan(bool quickScan)
        {
            scanning = true;
            UpdateButtons();
            if (items.Count > 0)
            {
                items = new ObservableCollection<OpenPort>();
                openPortListView.ItemsSource = items;
            }

            string ipstr = IPRangeInput.Text;
            IPRange ipRange;
            try
            {
                ipRange = new IPRange(ipstr);
            }
            catch (ArgumentNullException ex)
            {
                OnInputError(ex);
                return;
            }
            catch (ArgumentException ex)
            {
                OnInputError(ex);
                return;
            }


            IEnumerable<IPAddress> enumerable = ipRange.GetAllIP();
            scannerExecutor = new ScannerExecutor(numberOfTasks);
            scannerExecutor.BuildExecutor(enumerable, UpdateGui, quickScan);
            stopButton.IsEnabled = true;
            Task.WhenAll(scannerExecutor.GetRunningTasks().ToArray());
            logger.Trace("ALL FINISHED");
        }

        public void UpdateGui(OpenPort port)
        {
            logger.Trace("Before dispatcher for port{}", port.Port);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                logger.Debug("Adding port inside dispatcher");
                this.items.Add(port);
            }
            ));
        }

        private void OnInputError(Exception ex)
        {
            logger.Error(ex);
            IPRangeInput.Focus();
            IPRangeInput.BorderBrush = System.Windows.Media.Brushes.Red;
            scanning = false;
            UpdateButtons();
        }
    }
}