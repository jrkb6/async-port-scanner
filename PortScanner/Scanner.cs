using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using PortScanner.Model;

namespace PortScanner
{
    /// <summary>
    /// Scanner is responsible for the connection to the remote host with all port numbers or defined common ports.
    /// it takes a range of ip as a list, tries connection for each ip and port tuple
    /// </summary>
    public class Scanner
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private const int MinPort = 1;
        private const int MaxPort = 65535;
        private int numberOfPortTasks = 0;
        readonly List<IPAddress> ips;


        private int[] ports = new[]
        {
            20,
            21,
            22,
            23,
            25,
            53,
            80,
            110,
            143,
            161,
            443,
            445,
            3389,
            8080,
            8090
        };

        public Scanner(List<IPAddress> ips)
        {
            logger.Info("Scanner initialized..");
            this.ips = ips;
        }
        public Scanner(IEnumerable<IPAddress> addresses)
        {
            logger.Info("Scanner initialized..");
            this.ips = addresses.ToList();
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="scanOnlyCommonPorts"></param>
        /// <param name="token"></param>
        public void ScanAsync(Action<OpenPort> callback, bool scanOnlyCommonPorts, CancellationToken token)
        {
            logger.Info(("ThreadId {} -- Scan is starting..", Thread.CurrentThread.ManagedThreadId));
            if (scanOnlyCommonPorts)
            {
                Parallel.ForEach(ips,
                    (ip, state) =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            logger.Debug("Shutdown");
                            state.Break();
                        }

                        Parallel.ForEach(ports, (port, state) =>
                        {
                            if (token.IsCancellationRequested)
                            {
                                logger.Debug("Shutdown");
                                state.Break();
                            }

                            ScanPortsAsync(ip, port, callback, token);
                        });
                    });
            }
            else
            {
                foreach (var ip in ips)
                {
                    if (token.IsCancellationRequested)
                    {
                        logger.Debug("Shutdownn");
                        break;
                    }

                    for (var port = MinPort; port < MaxPort; port++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            logger.Debug("Shutdown");
                            break;
                        }

                        ScanPortsAsync(ip, port, callback, token);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="scanOnlyCommonPorts"></param>
        /// <param name="token"></param>
        public void ScanAsyncOnPorts(Action<OpenPort> callback, bool scanOnlyCommonPorts, CancellationToken token,List<int> ports)
        {
            logger.Info(("ThreadId {} -- Scan is starting..", Thread.CurrentThread.ManagedThreadId));
            if (scanOnlyCommonPorts)
            {
               //to be implemented..
            }
            else
            {
                foreach (var port in ports)
                {
                    if (token.IsCancellationRequested)
                    {
                        logger.Debug("Shutdownn");
                        break;
                    }

                    foreach (var ip in ips)
                    {
                        if (token.IsCancellationRequested)
                        {
                            logger.Debug("Shutdown");
                            break;
                        }
                        ScanPortsAsync(ip, port, callback, token);
                    }
                        
                }
            }
        }

        /// <summary>
        /// Request to given host in a given port. If req. is replied in timeout, then add port to observ. items via callback.
        /// </summary>
        /// <param name="address"></param> ip address from given range
        /// <param name="port"></param> port
        /// <param name="callback"></param> ui thread
        /// <returns></returns>
        private async Task ScanPortsAsync(IPAddress address, int port, Action<OpenPort> callback,
            CancellationToken mainTaskToken)
        {
            var timeOut = TimeSpan.FromSeconds(5);
            var cancellationCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                using (var cts = new CancellationTokenSource(timeOut))
                {
                    using (var client = new TcpClient())
                    {
                        if (mainTaskToken.IsCancellationRequested)
                        {
                            logger.Trace("Main thread closed. Stopping checking for port >= {} ", port);
                            return;
                        }
                        logger.Trace("Connection requesting for {}:{}", address.ToString(), port);
                        var task = client.ConnectAsync(address, port);
                        using (cts.Token.Register(() => cancellationCompletionSource.TrySetResult(true)))
                        {
                            if (task != await Task.WhenAny(task, cancellationCompletionSource.Task))
                            {
                                //logger.Trace("Exception on port {} from host {}", port, address.ToString());
                                throw new TaskCanceledException(task);
                            }

                            if (task.IsCompletedSuccessfully && !mainTaskToken.IsCancellationRequested)
                            {
                                logger.Trace("Open port {} from host {}", port, address.ToString());
                                callback(new OpenPort() {Host = address.ToString(), Port = port});
                            }
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                logger.Trace("Port skipped {}", port);
            }
        }

        private void ScanPort(IPAddress address, int port, Action<OpenPort> callback,
            CancellationToken mainTaskToken)
        {
            if (mainTaskToken.IsCancellationRequested)
            {
                logger.Trace("Port skipped {}", port);
                return;
            }

            using (var client = new TcpClient())
            {
                try
                {
                    logger.Trace("Connection requesting for {}:{}",  address.ToString(), port);
                    client.Connect(address, port);
                    logger.Trace("Open port {} from host {}", port, address.ToString());
                    callback(new OpenPort() {Host = address.ToString(), Port = port});
                }
                catch (Exception)
                {
                    logger.Trace("Port skipped {}", port);
                }
            }
        }
    }
}