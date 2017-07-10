/////////////////////////////////////////////////////////////////////
// Client2.cs - Client Module which is primarily used to           //
//              demonstrate that concurrent clients can send the   //
//              testrequest to TestHarness. This module is same as //
//              client module except used to demonstrate           //
//              concurrency                                        //
//                                                                 //
//                                                                 //
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
 * - Client2.cs, ITest.cs, BlockingQueue.cs, Messages.cs, IService.cs
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestHarness
{
    public class Client2 : IClient
    {
        public static SWTools.BlockingQueue<Message> inQ_ { get; set; }
        private static ServiceHost clientService = null;
        Thread ClientReadThrd = null;
        private string lastError = "";
        private string ToSendPath = "..\\..\\FilesToSend";
        private string SavePath = "..\\..\\FilesReceived";
        private static HiResTimer hrt = new HiResTimer();

        public static void EnqueueMessagesToClient(Message m)
        {
            inQ_.enQ(m);
        }

        public void uploadFile(string filename, string Uri)
        {
            string fqname = Path.Combine(ToSendPath, filename);
            IService Channel = null;
            Channel = CreateSendChannel(Uri);
            if (Channel == null)
            {
                Console.WriteLine("Failed to establish connection with {0}", Uri);
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
                Console.WriteLine("\n  Uploaded file \"{0}\" .", filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n  can't find \"{0}\" Exception: {1}", fqname, ex.Message);
            }
        }

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
                string rfilename = Path.Combine(SavePath, filename);
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

        public Message getMessage()
        {
            return inQ_.deQ();
        }

        void ThreadProc()
        {
            while (true)
            {
                Message msg = inQ_.deQ();
                if (msg.body == "quit")
                {
                    Close();
                    break;
                }
                Console.Write("\n Client received a new message \n");
                hrt.Stop();
                ulong time = hrt.ElapsedMicroseconds;
                double millitime = time / 1000.0;
                string timetaken = "Total time to get the response in milli seconds is " + millitime.ToString() + " - Req 12";
                timetaken.title();
                msg.show();
            }
        }
        // bool flag to indicate whether to span child thread

        public Client2(bool flag)
        {
            "Creating a new instance of Client2".title();
            if (inQ_ == null)
                inQ_ = new SWTools.BlockingQueue<Message>();
            //  CreateClientRecvChannel(clientUrl);
            // Console.Write("\n Created new client Service to accept http connections");
            if (flag == true)
            { 
              ClientReadThrd = new Thread(ThreadProc);
              ClientReadThrd.IsBackground = true;
              ClientReadThrd.Start();
            }
        }

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
                        Console.Write("Retrying {0} times to establish communication with testharness",
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

        public void Close()
        {
            clientService.Close();
        }

        public void CreateClientRecvChannel(string address)
        {
            // Can't configure SecurityMode other than none with streaming.
            // This is the default for BasicHttpBinding.
            BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
            BasicHttpBinding binding = new BasicHttpBinding(securityMode);
            Uri baseAddress = new Uri(address);
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 50000000;
            clientService = new ServiceHost(typeof(ClientService2), baseAddress);
            clientService.AddServiceEndpoint(typeof(IService), binding, baseAddress);
            clientService.Open();
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

        public void makeQuery(string queryText, string fromUri, string toUri)
        {
            IService Repochannel = CreateSendChannel(toUri);
            if (Repochannel != null)
            {
                Message m = new Message(queryText);
                m.from = fromUri;
                m.to = toUri;
                m.author = "jashwanth";
                "Sending the log Query to Repository using WCF channel - Req#10".title();
                Repochannel.PostMessage(m);
            }
            else
            {
                Console.WriteLine("Failed to post the query request to the Repository");
            }
        }

        public Message buildTestMessage(string FromUri, string ToUri)
        {
            Message msg = new Message();
            msg.to = ToUri;
            msg.from = FromUri;
            msg.author = "Jashwanth";

            testElement te1 = new testElement("test1");
            te1.addDriver("testdriver.dll");
            te1.addCode("testedcode.dll");
            testElement te2 = new testElement("test2");
            te2.addDriver("td1.dll");
            te2.addCode("tc1.dll");
            testElement te3 = new testElement("test3");
            te3.addDriver("anothertestdriver.dll");
            te3.addCode("anothertestedcode.dll");
            testElement tlg = new testElement("loggerTest");
            tlg.addDriver("logger.dll");
            testRequest tr = new testRequest();
            tr.author = "Jashwanth";
            tr.tests.Add(te1);
            tr.tests.Add(te2);
            tr.tests.Add(te3);
            //tr.tests.Add(tlg);
            msg.body = tr.ToString();
            return msg;
        }

        static void Main(string[] args)
        {
            Console.Title = "ConcurrentClient2Demo";
            "Starting the Execution of Client Main".title();
            "Implemented in C# using .Net Framework and Visual Studio 2015 - Req#1".title();

            try
            {
                Client2 myClient2 = new Client2(true);
                string clistr = "http://localhost:8080/Client2IService";
                string Repostr = "http://localhost:8080/RepoIService";
                string Thstr = "http://localhost:8080/TestHarnessIService";
                myClient2.CreateClientRecvChannel(clistr);
                Thread.Sleep(5000);
                /* Send all the code to be tested to the Repository */
                string filepath = Path.GetFullPath(myClient2.ToSendPath);
                "Client is sending the Repository server before sending the Test Request to the Test Harness- Req#6".title();
                Console.WriteLine("Retrieving files from {0}\n", filepath);
                string[] files = Directory.GetFiles(filepath);
                foreach (string file in files)
                {
                    string filename = Path.GetFileName(file);
                    Console.WriteLine("file retrieved is {0} \n", filename);
                    myClient2.uploadFile(filename, Repostr);
                }
                // this sleep ensures that the code to be tested files
                // are sent to the Repository before the Testharness starts
                // copying them to local directory. Removal of this sleep
                // might result in testharness unable to load the dll files
                // and return the result message as file not loaded.
                Thread.Sleep(10000);
                Message msg = myClient2.buildTestMessage(clistr, Thstr);
                "Sending the test Request from concurrent client 2 to TestHarness - Req#4,6".title();
                hrt.Start();
                myClient2.sendTestRequest(msg);
                /* Wait for the TestHarness to complete the execution before
                 sending the query to the repository. This sleep ensures that 
                 TestHarness completes the execution before the client executes
                 Query logs. Removal of this sleep might result in repository
                 returning empty message as it has  not yet stored logs. */
                Thread.Sleep(20000);
                hrt.Start();
                myClient2.makeQuery("test1", clistr, Repostr);
            }
            catch (Exception ex)
            {
                Console.Write("\n\n  {0}\n\n", ex.Message);
            }
            Console.ReadLine();
        }
    }
    // ClientTest to test the functionality of the client
    // This can as well be used by TestHarness to test the client
    // functionality 
    public class ClientTest2 : ITest
    {
        private StringBuilder clientLog;
        public ClientTest2()
        {
            clientLog = new StringBuilder();
        }
        public string getLog()
        {
            return clientLog.ToString();
        }

        public bool test()
        {
            Client2 clientTest = new Client2(false);
            string Clistr = "http://localhost:8080/ClientTestIService";
            string Thstr = "http://localhost:8080/TestHarnessIService";
            string Repostr = "http://localhost:8080/RepoIService";
            clientTest.CreateClientRecvChannel(Clistr);
            // Assuming the Repository has the code to be tested
            clientTest.sendTestRequest(clientTest.buildTestMessage(Clistr, Thstr));
            // Give sufficient time for execution before making the Query
            Thread.Sleep(10000);
            clientTest.makeQuery("test1", Clistr, Repostr);
            while (true)
            {
                Message msg = clientTest.getMessage();
                if (msg.body == "quit")
                {
                    clientTest.Close();
                    clientLog.Append(msg.ToString());
                    break;
                }
                clientLog.Append(msg.ToString());
            }
            return true;
        }
    }
}
