using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private int _numberOfTasks;
        private ObservableCollection<OpenPort> _items;
        ScannerExecutor _scannerExecutor;
        private bool _scanning = false;
        private const int MaxNumberOfTasks = 4001;
        private int _timeOutDuration;
        private const int MaxTimeOutDuration = 8000; //ms
        private const int MinTimeOutDuration = 100;
        private const int DefaultTimeOutDuration = 5000;
        private int MaxNumberOfConnections = 5000;

        public MainWindow()
        {
            InitializeComponent();
            _items = new ObservableCollection<OpenPort>();
            openPortListView.ItemsSource = _items;
            Logger.Info("App started..");
            stopButton.IsEnabled = false;
            clearButton.IsEnabled = false;
            numberOfTaskSlider.Maximum = MaxNumberOfTasks;
            _timeOutDuration = DefaultTimeOutDuration;
            timeoutInput.Text = _timeOutDuration.ToString();
        }


        private void numberOfTaskInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(numberOfTaskInput.Text, out _numberOfTasks))
            {
                // If not int clear textbox text
                numberOfTaskInput.Clear();
            }
            else
            {
                if (_numberOfTasks <= 0 || _numberOfTasks >= MaxNumberOfTasks)
                {
                    _numberOfTasks = 1;
                }

                numberOfTaskInput.Text = _numberOfTasks.ToString();
                numberOfTaskSlider.Value = _numberOfTasks;
            }
        }

        private void UpdateButtons()
        {
            scanButton.IsEnabled = !_scanning;
            stopButton.IsEnabled = _scanning;
            clearButton.IsEnabled = !_scanning;
            saveButton.Visibility = _scanning ? Visibility.Visible : Visibility.Hidden;
        }

        private void scanButton_Click(object sender, RoutedEventArgs e)
        {
            DoScan(false);
        }


        private void quickScanButton_Click(object sender, RoutedEventArgs e)
        {
            DoScan(true);
        }

        private async void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Trace("Save button called");
            stopTasks();
            await WriteOnFile();
            Logger.Trace("Tasks stopped and saved.");
            UpdateButtons();
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                stopTasks();
                UpdateButtons();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }


        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            _items = new ObservableCollection<OpenPort>();
            openPortListView.ItemsSource = _items;
            numberOfTaskSlider.Value = 1;
            IPRangeInput.Text = "";
            numberOfConnectionInput.Text = "";
            timeoutInput.Text = "";
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
        private async void DoScan(bool quickScan)
        {
            if (_items.Count > 0)
            {
                _items = new ObservableCollection<OpenPort>();
                openPortListView.ItemsSource = _items;
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
                ShowErrorMessage("Empty ip information given..", "IP information mismatch");
                return;
            }
            catch (ArgumentException ex)
            {
                OnInputError(ex);
                ShowErrorMessage("Either use 127.0.0.1/24 or 127.0.0.1-255 format for ip input",
                    "IP information mismatch");
                return;
            }

            if (!ValidateTimeOutInput())
            {
                OnInputError(new Exception("Invalid timeout"));
                return;
            }

            if (!ValidateNumberOfConnectionInput())
            {
                OnInputError(new Exception(" Invalid Number Of connections"));
                return;
            }

            _scanning = true;
            UpdateButtons();
            IEnumerable<IPAddress> enumerable = ipRange.GetAllIP();
            _scannerExecutor = new ScannerExecutor(_numberOfTasks);
            _scannerExecutor.BuildExecutor(enumerable, UpdateGui, quickScan, _timeOutDuration, MaxNumberOfConnections);


            stopButton.IsEnabled = true;
            bool isCancelled = false;
            try
            {
                await Task.WhenAll(_scannerExecutor.GetRunningTasks().ToArray());
            }
            catch (TaskCanceledException ex)
            {
                isCancelled = true;
            }
            finally
            {
                Logger.Trace("ALL FINISHED");
                _scanning = false;
                UpdateButtons();
            }

            //if is cancelled save button is clicked no need to write results again.
            if (!isCancelled)
            {
                await Task.WhenAll(WriteOnFile());
            }
        }

        /// <summary>
        /// Write Results to a text file to save data.
        /// </summary>
        /// <returns></returns>
        private async Task WriteOnFile()
        {
            var portData = _items.Select(openPort => openPort.ToString()).ToList();
            Logger.Debug("Writing results to report..");
            var basedir = AppDomain.CurrentDomain.BaseDirectory;
            var datetime = DateTime.Now.ToString("MM/dd/yyyy H-mm");
            var path = @$"{basedir}/result_{datetime}.txt";
            await File.WriteAllLinesAsync(path, portData, CancellationToken.None);
            Logger.Debug("Report created");
            MessageBox.Show($"Scan finished... Report created at: {path}", "Finished", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void stopTasks()
        {
            _scannerExecutor.StopAllTasks();
            _scanning = false;
            try
            {
                await Task.WhenAll(_scannerExecutor.GetRunningTasks().ToArray());
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug("Tasks are cancelled from user input.. ");
            }
           
        }

        public void UpdateGui(OpenPort port)
        {
            Logger.Trace("Before dispatcher for port{}", port.Port);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Logger.Debug("Adding port inside dispatcher");
                    this._items.Add(port);
                }
            ));
        }

        private void OnInputError(Exception ex)
        {
            Logger.Error(ex);
            IPRangeInput.Focus();
            IPRangeInput.BorderBrush = System.Windows.Media.Brushes.Red;
            _scanning = false;
            UpdateButtons();
        }


        private void ShowErrorMessage(string message, String caption)
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        //TODO move validators to InputValidator
        private bool ValidateTimeOutInput()
        {
            if (!int.TryParse(timeoutInput.Text, out _timeOutDuration))
            {
                // If not int clear textbox text
                timeoutInput.Clear();
                ShowErrorMessage("Write numbers only in range 2000 - 8000 ms", "Invalid Timeout Settings");
            }
            else
            {
                if (_timeOutDuration < MinTimeOutDuration || _timeOutDuration > MaxTimeOutDuration)
                {
                    _timeOutDuration = DefaultTimeOutDuration;
                    ShowErrorMessage("Write numbers only in range 2000 - 8000 ms", "Invalid Timeout Settings");
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private bool ValidateNumberOfConnectionInput()
        {
            if (!int.TryParse(numberOfConnectionInput.Text, out MaxNumberOfConnections))
            {
                // If not int clear textbox text
                numberOfConnectionInput.Clear();
                ShowErrorMessage("Available only for 20000 active connection.",
                    "Invalid Number Of Connection Settings");
            }
            else
            {
                if (MaxNumberOfConnections < 0 || MaxNumberOfConnections > 20000)
                {
                    MaxNumberOfConnections = 10000;
                    ShowErrorMessage("Available only for 20000 active connection.",
                        "Invalid Number Of Connection Settings");
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
    }
}