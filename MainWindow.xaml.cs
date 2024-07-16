using System.IO.Pipes;
using System.IO;
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

namespace WPF_Named_Pipe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listeningTask;

        public MainWindow()
        {
            InitializeComponent();
            StartListeningForMessages();
            TBSendMassege.Focus();
        }

        private void BtnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            string message = TBSendMassege.Text;
            if (!string.IsNullOrWhiteSpace(message))
            {
                SendMessage(message);
            }
        }

        private void SendMessage(string message)
        {
            Task.Run(() =>
            {
                try
                {
                    using (var pipeClient = new NamedPipeClientStream(".", "pipe1", PipeDirection.Out))
                    {
                        pipeClient.Connect();
                        using (var streamWriter = new StreamWriter(pipeClient) { AutoFlush = true })
                        {
                            streamWriter.WriteLine(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error sending message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }

        private void StartListeningForMessages()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _listeningTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using (var pipeServer = new NamedPipeServerStream("pipe2", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                        {
                            await pipeServer.WaitForConnectionAsync(token);
                            using (var streamReader = new StreamReader(pipeServer))
                            {
                                string message = (await streamReader.ReadToEndAsync()).Trim();
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    TBlockReceivedMessage.Text = message;
                                });
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show($"Error listening for messages: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                }
            }, token);
        }
    }


}