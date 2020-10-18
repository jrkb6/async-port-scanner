using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PortScanner.Model;

namespace PortScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    /*
     TODO

    Complete ScannerExecutor create a scan method in executor that calls all Scanner scans in async and puts to global port data.
     
     */
    public partial class MainWindow : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private int numberOfTasks;
        private ObservableCollection<OpenPort> items;
        ScannerExecutor scannerExecutor;
        private readonly object _collectionOfObjectsSync = new object();
        private bool scanning = false;

        public MainWindow()
        {
            InitializeComponent();
            items = new ObservableCollection<OpenPort>();
            openPortListView.ItemsSource = items;
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                BindingOperations.EnableCollectionSynchronization(items, _collectionOfObjectsSync);
            }));
            logger.Info("App started..");
            stopButton.IsEnabled = false;
            clearButton.IsEnabled = false;
            
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
                if (numberOfTasks <= 0 || numberOfTasks >= 10)
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
                onInputError(ex);
                return;
            }
            catch (ArgumentException ex)
            {
                onInputError(ex);
                return;
            }


            IEnumerable<IPAddress> enumerable = ipRange.GetAllIP();
            scannerExecutor = new ScannerExecutor(numberOfTasks);
            scannerExecutor.BuildExecutor(enumerable, (openPort) => updateGui(openPort), quickScan);
            stopButton.IsEnabled = true;
            Task.WhenAll(scannerExecutor.GetRunningTasks().ToArray());
            logger.Trace("ALL FINISHED");
        }

        public void updateGui(OpenPort port)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => this.items.Add(port)));
        }

        private void onInputError(Exception ex)
        {
            logger.Error(ex);
            IPRangeInput.Focus();
            IPRangeInput.BorderBrush = System.Windows.Media.Brushes.Red;
            scanning = false;
            UpdateButtons();
        }
    }
}