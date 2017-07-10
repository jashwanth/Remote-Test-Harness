/////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - Client WPF Module which is primarily used  //
//                      by the user to send the Requests to the    //
//                      TestHarness remotely, Query logs to the    //
//                      Repository Remotely and displays           //
//                      the results on the WPF application and     //
//                      execution time summary                     //
//  ver 1.0                                                        //
//  Language:      Visual C#  2015                                 //
//  Platform:      Mac, Windows 7                                  //
//  Application:   TestHarness - Project4                          //
//                 CSE681 - Software Modeling and Analysis,        //
//                 Fall 2016                                       //
//  Author:        Jashwanth Reddy, Syracuse University            //
//                 (315) 949-8857, jgangula@syr.edu                //
//                                                                 //
//  Source:        Jim Fawcett                                     //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * The module basically creates a ServiceHost to accept the remote connections
 * and uses a child thread to process those messages enqueued into a Blocking
 * Queue. The main thread is repsonsible for sending the Test Requests to the
 * TestHarness and Query logs to the Repository remotely by using channels to
 * connect to them and posting the messages to their Blocking Queues. The module
 * also filestream services provided by ClientService.cs to send the entire code
 * to be tested to the Repository before beginning of its operation.
 * 
 * Required Files:
 * - MainWindow.xaml.cs, ITest.cs, BlockingQueue.cs, Messages.cs, IService.cs
 * 
 * Public Interface :
 *  EnqueueMessagesToClient(Message): Used by ClientService to Enqueue messages
 *  uploadFile(filename, uri)       : Used to upload the file to the remote uri
 *  download(filename, uri)         : Though not used in this application, client
 *                                    can use this utility to download log files 
 *                                    from the Repository.
 *  Client()                        : Constructor
 *  CreateSendChannel(uri)          : Generic method to remotely establish a 
 *                                    channel with the specified Uri.
 *  Close()                         : Closes the ServiceHost which stops further
 *                                    new connection establishment with Client.
 *  CreateClientRecvChannel()       : Creates a new Http Binding and adds it to the
 *                                    service endpoint , exposes its services.
 *  sendTestRequest(Message)        : Post the message to the test harness remotely.
 *  makeQuery(queryText, Uri)       : Query the Repository for specified string .
 *  buildTestMessage(from, to)      : Build a test message that contains the xml formatted
 *                                    list of test drivers and codes to be tested.
 *  
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 20 November 2016
 * - first release
 */
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.Xml.Linq;
using TestHarness;
using System.Threading;
using System.ServiceModel;

namespace TestHarness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ClientWPF : Window, IClient
    {
        private string XMLMessage = "";
        private static int count = 10;
        private static string repochannel = "";
        public static SWTools.BlockingQueue<Message> inQ_ { get; set; }
        private ServiceHost clientService = null;
        private Thread ClientReadThrd = null;
        private string lastError = "";
        string ToSendPath = "..\\..\\FilesToSend";
        string SavePath = "..\\..\\FilesReceived";
        private delegate void NewMessage(Message msg);
        private event NewMessage OnNewMessage;
        private static HiResTimer hrt = new HiResTimer();

        public ClientWPF()
        {
            Console.Title = "DemoWPF";
            InitializeComponent();
            OnNewMessage += new NewMessage(OnNewMessageHandler);

            if (inQ_ == null)
                inQ_ = new SWTools.BlockingQueue<Message>();

            ClientReadThrd = new Thread(ThreadProc);
            ClientReadThrd.IsBackground = true;
            ClientReadThrd.Start();
        //    "Created a new instance of ClientWPF - Req#11".title();
        }
        public void OnNewMessageHandler(Message msg)
        {
            "Inside Message Handler".title();
            msg.show();

            if (msg.from.Contains ("Test"))
            {
               "Received a Message from TestHarness".title();
                textBoxResult.Text = msg.body;
            }

            if (msg.from.Contains("Repo"))
            {
                "Received a message from Repository".title();
                textBoxRepo.Text = msg.body;
                "Repo:The message is loaded into textbox".title();
              //  textBoxRepo.Text.title();
            }
        }

        public void uploadFile(string filename, string Uri)
        {
            string fqname = System.IO.Path.Combine(ToSendPath, filename);
            IService Channel = null;
            Channel = CreateSendChannel(Uri);
            if (Channel == null)
            {
                string toPrint = "Failed to establish connection with " + Uri;
                toPrint.title();
                return;
            }
            try
            {
                using (var inputStream = new FileStream(fqname, FileMode.Open))
                {
                    FileTransferMessage msg = new FileTransferMessage();
                    msg.filename = filename;
                    msg.transferStream = inputStream;
                    Channel.upLoadFile(msg);
                }
                string printfile = "Uploaded the file " + filename + "to Repository";
                printfile.title();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n  can't find \"{0}\" Exception: {1}", fqname, ex.Message);
            }
        }

        // Though this method is not used here it can be used by 
        // client to download the file remotely to local folders
        public void download(string filename, string Uri)
        {
            IService Channel = null;
            int BlockSize = 1024;
            byte[] block = new byte[BlockSize];
            Channel = CreateSendChannel(Uri);
            if (Channel == null)
            {
                Console.WriteLine("Failed to download files from {0} ", Uri);
            }
            else
            {
                Channel = CreateSendChannel("http://localhost:8080/TestHarnessIService");
                if (Channel == null)
                    Console.WriteLine("Failed to download files from TestHarness");
            }
            int totalBytes = 0;
            try
            {
                Stream strm = Channel.downLoadFile(filename);
                string rfilename = System.IO.Path.Combine(SavePath, filename);
                if (!Directory.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);
                using (var outputStream = new FileStream(rfilename, FileMode.Create))
                {
                    while (true)
                    {
                        int bytesRead = strm.Read(block, 0, BlockSize);
                        totalBytes += bytesRead;
                        if (bytesRead > 0)
                            outputStream.Write(block, 0, bytesRead);
                        else
                            break;
                    }
                }
                Console.Write("\n  Received file \"{0}\" of {1} bytes.", filename, totalBytes);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
            }
        }

        public static void EnqueueMessagesToClient(Message m)
        {
            inQ_.enQ(m);
        }
        void ThreadProc()
        {
            while (true)
            {
                Message msg = inQ_.deQ();
               
                "Child Thread dequeued a new request".title();
                msg.show();
                if (msg.body == "quit")
                {
                    Close();
                    break;
                }

                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,OnNewMessage,msg);
            }
        }


        // Create proxy to another Peer's Communicator
        public IService CreateSendChannel(string address)
        {
            int tryCount = 0;
            int MaxCount = 10;
            ChannelFactory<IService> factory = null;
            while (true)
            {
                try
                {
                    BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
                    EndpointAddress baseAddress = new EndpointAddress(address);
                    BasicHttpBinding binding = new BasicHttpBinding(securityMode);
                    binding.TransferMode = TransferMode.Streamed;
                    binding.MaxReceivedMessageSize = 500000000;
                    factory = new ChannelFactory<IService>(binding, address);
                    tryCount = 0;
                    break;
                }
                catch (Exception ex)
                {
                    if (++tryCount < MaxCount)
                    {
                        Thread.Sleep(100);
                        Console.Write("Retrying {0} times to establish communication with Test Harness",
                                       tryCount);
                    }
                    else
                    {
                        lastError = ex.Message;
                        break;
                    }
                }
            }
            if (factory != null)
            {
                //    Console.Write("\n Creating proxy object to communicate with Test Harness");
                return factory.CreateChannel();
            }
            else
            {
                Console.Write("\n Failed to create proxy object to communicate with Test Harness");
                return null;
            }
        }

        public void CreateClientRecvChannel(string address)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            Uri baseAddress = new Uri(address);
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 50000000;
            clientService = new ServiceHost(typeof(ClientService), baseAddress);
            clientService.AddServiceEndpoint(typeof(IService), binding, baseAddress);
            clientService.Open();
        }

        // override WPF method here for Close 
        public new void Close()
        {
            clientService.Close();
        }

        public void sendTestRequest(Message testRequest)
        {
            IService Thchannel = CreateSendChannel("http://localhost:8080/TestHarnessIService");
            if (Thchannel != null)
            {
                "Sending the test request to Test Harness using WCF channel - Req#10".title();
                Thchannel.PostMessage(testRequest);
            }
            else
            {
                Console.WriteLine("Failed to post th request to the Test Harness");
            }
        }
        public void sendResults(Message results)
        {
            //RLog.write("\n  Client received results message:");
            //RLog.write("\n  " + results.ToString());
            //RLog.putLine();
            Console.Write("\n  Client received results message:");
            Console.Write("\n  " + results.ToString());
            Console.WriteLine();
        }
        public void makeQuery(string queryText, string fromUri, string toUri)
        {
            IService Repochannel = CreateSendChannel(toUri);
            if (Repochannel != null)
            {
                Message m = new Message(queryText);
                m.from = fromUri;
                m.to = "http://localhost:8080/RepoIService";
                m.author = "jashwanth";
                "Sending the log Query to Repository using WCF channel - Req#10".title();
                Repochannel.PostMessage(m);
            }
            else
            {
                Console.WriteLine("Failed to post the query request to the Repository");
            }
        }
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
        }
        private void SubmitToTH(object sender, RoutedEventArgs e)
        {
           XMLMessage = txtEditor.Text;
           BuildMessage(XMLMessage);
        }
        public void BuildMessage(string messagebody)
        {
            Console.WriteLine("The Message submitted to TestHarness is: ");
            Console.WriteLine(XMLMessage);

            ClientWPF cl1 = new ClientWPF();
            string Repostr = "http://localhost:8080/RepoIService";


            
            Message msg = new Message();
            msg.body = messagebody;

            Console.WriteLine("The Message body which is being sent to TestHarness: ");
            Console.WriteLine(msg.body);
            string frommessage = "http://localhost:8080/ClientIService" + count++;
            //count++;
            msg.author = "Jashwanth";
            msg.to = "TH";
            msg.from = frommessage;
            cl1.CreateClientRecvChannel(msg.from);

            /* Send all the code to be tested to the Repository */
            string filepath = System.IO.Path.GetFullPath(cl1.ToSendPath);
            Console.WriteLine("\n  retrieving files from\n {0}\n", filepath);
            string[] files = Directory.GetFiles(filepath);
            hrt.Start();
            foreach (string file in files)
            {
                string filename = System.IO.Path.GetFileName(file);
                Console.WriteLine("\nfile retrieved is {0} \n", filename);
                cl1.uploadFile(filename, Repostr);
            }
            /* this sleep ensures that client copies code before  sending the request*/
            Thread.Sleep(10000);
            cl1.sendTestRequest(msg);
            hrt.Stop();
            ulong time = hrt.ElapsedMicroseconds;
            double millisecond = time / 1000.0;
            TimerTextBox.Text = millisecond.ToString() + "ms";
            string timetaken = "Total time to get the response in milli seconds is " + millisecond.ToString() + " - Req 12";
            timetaken.title();
            Thread.Sleep(20000);
        }
        public void QListner(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Inside QueryListner button");
            ClientWPF c2 = new ClientWPF();
            repochannel = "http://localhost:8080/ClientIService" + count++;
            string Repostr = "http://localhost:8080/RepoIService";
            c2.CreateClientRecvChannel(repochannel);
            string queryToBeQueried = RemotePortTextBox.Text;
            c2.makeQuery(queryToBeQueried, repochannel, Repostr);
        }

        private void ResultBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void textBoxRepo_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
