/////////////////////////////////////////////////////////////////////
// Client.cs - Client Module which is primarily used by the user   //
//             to send the Requests to the TestHarness remotely,   //
//             Query logs to the Repository Remotely and displays  // 
//             the results on the Console application              //
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
 * - Client.cs, ITest.cs, BlockingQueue.cs, Messages.cs, IService.cs
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
    public class Client : IClient
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
                // Quit message so exit the thread and close the service.
                if (msg.body == "quit")
                {
                    Close();
                    break;
                }
                Console.Write("\n Client Child Thread Dequeued a new message \n");
                hrt.Stop();
                ulong time = hrt.ElapsedMicroseconds;
                double millitime = time / 1000.0;
                string timetaken = "Total time to get the response in milli seconds is " + millitime.ToString() + " - Req 12";
                timetaken.title();
                if (msg.from.Contains("Test"))
                {
                    Console.WriteLine("\nDisplaying the results of Test Request\n");
                }
                else
                {
                    Console.WriteLine("\nDisplaying the results of Query Logs\n");
                }

                msg.show();
                if (msg.from.Contains("Repo"))
                { 
                  "Demonstarted that all Requirements from 2-10 are met - Req #13".title();
                }
            }
        }
        // bool flag to indicate whether to span child thread
        // or carry out processing by the main thread itself.
        public Client(bool flag)
        {
            "Creating a new instance of Client".title();
            if (inQ_ == null)
                inQ_ = new SWTools.BlockingQueue<Message>();
            //  CreateClientRecvChannel(clientUrl);
            // call the above function only after creating an object
            // of client as the child thread might throw an exception.
            // Console.Write("\n Created new client Service to accept http connections");

            // Create a child thread , run in background for processing Messages
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
            clientService = new ServiceHost(typeof(ClientService), baseAddress);
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
                Console.WriteLine("Failed to post the request to the Test Harness");
            }
        }

        public void makeQuery(string queryText, string fromUri, string ToUri)
        {
            IService Repochannel = CreateSendChannel(ToUri);
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
        //public void getQuery()
        //{
        //   Console.Write("\n  Results of client query for \"" + "test1" + "\"");
        // //  if (repo_ == null)
        // //     return;
        //   List<string> files = repo_.queryLogs(queryText);
        //   Console.Write("\n  first 10 reponses to query \"" + queryText + "\"");
        //   for (int i = 0; i < 10; ++i)
        //   {
        //      if (i == files.Count())
        //         break;
        //      Console.Write("\n  " + files[i]);
        //    }
        //  }

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
            tr.author = "Jashwanth Reddy";
            tr.tests.Add(te1);
            tr.tests.Add(te2);
            tr.tests.Add(te3);
            //tr.tests.Add(tlg);
            msg.body = tr.ToString();
            return msg;
        }

        static void Main(string[] args)
        {
            Console.Title = "ConcurrentClient1Demo";
            "Starting the Execution of Client Main".title();
            "Implemented in C# using .Net Framework and Visual Studio 2015 - Req#1".title();
            try
            {
                Client myClient1 = new Client(true);
                string clistr = "http://localhost:8080/ClientIService";
                string Repostr = "http://localhost:8080/RepoIService";
                string Thstr = "http://localhost:8080/TestHarnessIService";
                myClient1.CreateClientRecvChannel(clistr);
                Thread.Sleep(5000);
                /* Send all the code to be tested to the Repository */
                string filepath = Path.GetFullPath(myClient1.ToSendPath);
                Console.WriteLine("\nRetrieving files from {0}\n", filepath);
                string[] files = Directory.GetFiles(filepath);
                "Client is sending the Repository server before sending the Test Request to the Test Harness- Req#6".title();
                foreach (string file in files)
                {
                    string filename = Path.GetFileName(file);
                 //   Console.WriteLine("file retrieved is {0} \n", filename);
                    string str = "Sending the retireved test library " + filename + "to Repository using filestream service - Req#6";
                    str.title();
                    myClient1.uploadFile(filename, Repostr);
                }
                // this sleep ensures that the code to be tested files
                // are sent to the Repository before the Testharness starts
                // copying them to local directory. Removal of this sleep
                // might result in testharness unable to load the dll files
                // and return the result message as file not loaded.
                Thread.Sleep(10000);
                Message msg = myClient1.buildTestMessage(clistr, Thstr);
                "Sending the test Request from concurrent client 2 to TestHarness - Req#4,6".title();
                hrt.Start();
                /* start the timer */
                myClient1.sendTestRequest(msg);
                /* Wait for the TestHarness to complete the execution before
                 sending the query to the repository. This sleep ensures that 
                 TestHarness completes the execution before the client executes
                 Query logs. Removal of this sleep might result in repository
                 returning empty message as it has  not yet stored logs. */
                Thread.Sleep(20000);
                hrt.Start();
                myClient1.makeQuery("test1", clistr, Repostr);
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
    public class ClientTest : ITest
    {
        private StringBuilder clientLog;
        public ClientTest()
        {
            clientLog = new StringBuilder();
        }
        public string getLog()
        {
            return clientLog.ToString();
        }

        public bool test()
        {
            Client clientTest = new Client(false);
            string Clistr = "http://localhost:8080/ClientTestIService";
            string Thstr = "http://localhost:8080/TestHarnessIService";
            string Repostr = "http://localhost:8080/RepoIService";
            clientTest.CreateClientRecvChannel(Clistr);
            // Assuming the Repository has the code to be tested
            clientTest.sendTestRequest(clientTest.buildTestMessage(Clistr, Thstr));
            // Give sufficient time for execution before making the Query
            Thread.Sleep(10000);
            clientTest.makeQuery("test1", Clistr, Repostr);
            while(true)
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
