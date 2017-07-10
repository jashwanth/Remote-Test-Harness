/////////////////////////////////////////////////////////////////////
// TestHarness.cs - TestHarness Engine: creates child domains      //
// ver 2.0                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * TestHarness package provides integration testing services.  It:
 * - receives structured test requests
 * - retrieves cited files from a repository
 * - executes tests on all code that implements an ITest interface,
 *   e.g., test drivers.
 * - reports pass or fail status for each test in a test request
 * - stores test logs in the repository
 * It contains classes:
 * - TestHarness that runs all tests in child AppDomains
 * - Callback to support sending messages from a child AppDomain to
 *   the TestHarness primary AppDomain.
 * - Test and RequestInfo to support transferring test information
 *   from TestHarness to child AppDomain
 * 
 * Required Files:
 * ---------------
 * - TestHarness.cs, BlockingQueue.cs
 * - ITest.cs, IService.cs
 * - LoadAndTest.cs,  Messages.cs
 *
 * Maintanence History:
 * --------------------
 * ver 1.0 : 20 Nov 2016
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Policy;    // defines evidence needed for AppDomain construction
using System.Runtime.Remoting;   // provides remote communication between AppDomains
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.ServiceModel;
using System.IO;

namespace TestHarness
{
    ///////////////////////////////////////////////////////////////////
    // Callback class is used to receive messages from child AppDomain
    //
    public class Callback : MarshalByRefObject, ICallback
    {
        public void sendCallbackMessage(Message message)
        {
            Console.Write("\n  received msg from childDomain: \"" + message.body + "\"");
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Test and RequestInfo are used to pass test request information
    // to child AppDomain
    //
    [Serializable]
    class Test : ITestInfo
    {
        public string testName { get; set; }
        public List<string> files { get; set; } = new List<string>();
    }
    [Serializable]
    class RequestInfo : IRequestInfo
    {
        public string tempDirName { get; set; }
        public List<ITestInfo> requestInfo { get; set; } = new List<ITestInfo>();
    }
    ///////////////////////////////////////////////////////////////////
    // class TestHarness
    
    public class TestHarness
    {
        public static SWTools.BlockingQueue<Message> inQ_ { get; set; } = new SWTools.BlockingQueue<Message>();
        private ICallback cb_;
        //    private IRepository repo_;
        //    private IClient client_;
        private string repoPath_ = "../../../Repository/RepositoryStorage/";
        private static string filePath_;
        object sync_ = new object();
        // List<Thread> threads_ = new List<Thread>();
        Thread thProcess = null;
        private ServiceHost ThService = null;
        private int tryCount = 0;
        private int MaxCount = 10;
        private string lastError = "";
        static string curDir { get; set; } = null;

        public static string returnCurTempDir()
        {
            return curDir;
        }
        

        public void uploadFile(string filename, string Uri, string dir)
        {
            string fqname = Path.Combine(dir, filename);
            IService Channel = null;
            Channel = createSendChannel(Uri);
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
                Console.Write("\n  Uploaded file \"{0}\" .", filename);
            }
            catch
            {
                Console.Write("\n  can't find \"{0}\"", fqname);
            }
        }
        
        public static void EnqueueMessagesToTestHarness(Message m)
        {
            Console.Write("\n  TestHarness received a testRequest - Req #2");
            inQ_.enQ(m);
        }
        public void CreateThRecvChannel(string address)
        {
           // Can't configure SecurityMode other than none with streaming.
           // This is the default for BasicHttpBinding.
           BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
           BasicHttpBinding binding = new BasicHttpBinding(securityMode);
           Uri baseAddress = new Uri(address);
           binding.TransferMode = TransferMode.Streamed;
           binding.MaxReceivedMessageSize = 50000000;
           ThService = new ServiceHost(typeof(TestHarnessService), baseAddress);
           ThService.AddServiceEndpoint(typeof(IService), binding, baseAddress);
           ThService.Open();
        }
        public IService createSendChannel(string address)
        {
            tryCount = 0;
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
                        Console.Write("Retrying {0} times to establish communication with client",
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
                //     Console.Write("\n Created proxy object to communicate with Client ");
                return factory.CreateChannel();
            }
            else
                return null;
        }

        public void ThreadProc()
        {
            " Creating Child thread".title();
            while (true)
            {
                Message testRequest = inQ_.deQ();
                if (testRequest.body == "quit")
                {
                    inQ_.enQ(testRequest);
                    return;
                }
              
                "Test Harness Server Dequeued Test Request Message- Req#2".title();
                testRequest.show();
                "Each message has author name , test drivers, test libraries  in the form of DLL's to be tested - Req#2".title();
                /* Sleep for sometime before the client sends the code base
                 to Repository so that Testharness wont return file load failed result */
                Thread.Sleep(5000);
                ITestResults testResults = runTests(testRequest);
                lock (sync_)
                {
                    // client_.sendResults(makeTestResultsMessage(testResults));
                    Message resultmsg = makeTestResultsMessage(testResults, testRequest.from);
                    resultmsg.show();
                    IService ClientChannel = createSendChannel(testRequest.from);
                    resultmsg.from = "http://localhost:8080/TestHarnessIService";
                    resultmsg.to = testRequest.from;
                    if (ClientChannel != null)
                    {
                        "Sending the test results to the requested client - Req#7".title();
                        "Using WCF channel to post to the client Blocking Queue- Req#10".title();
                        ClientChannel.PostMessage(resultmsg);
                    }
                    else
                    {
                       "Test Harness failed to send the test results to the client".title();
                    }
                }
            }
        }
        //  public TestHarness()
        public TestHarness()
        {
            //    Console.Write("\n  creating instance of TestHarness");
            //    repo_ = repo;
            repoPath_ = System.IO.Path.GetFullPath(repoPath_);
            cb_ = new Callback();
            thProcess = new Thread(ThreadProc);
            thProcess.IsBackground = true;
            thProcess.Start();
            //    CreateThRecvChannel(address);
        }
       
        //----< make path name from author and time >--------------------

        string makeKey(string author)
        {
            DateTime now = DateTime.Now;
            string nowDateStr = now.Date.ToString("d");
            string[] dateParts = nowDateStr.Split('/');
            string key = "";
            foreach (string part in dateParts)
                key += part.Trim() + '_';
            string nowTimeStr = now.TimeOfDay.ToString();
            string[] timeParts = nowTimeStr.Split(':');
            for (int i = 0; i < timeParts.Count() - 1; ++i)
                key += timeParts[i].Trim() + '_';
            key += timeParts[timeParts.Count() - 1];
            key = author + "_" + key + "_" + "ThreadID" + Thread.CurrentThread.ManagedThreadId;
            string str = "Created a key " + key + "with author, timestamp, threadID - Req#8";
            str.title();
            return key;
        }
        //----< retrieve test information from testRequest >-------------

        List<ITestInfo> extractTests(Message testRequest)
        {
            Console.Write("\n  parsing test request");
            List<ITestInfo> tests = new List<ITestInfo>();
            XDocument doc = XDocument.Parse(testRequest.body);
            foreach (XElement testElem in doc.Descendants("test"))
            {
                Test test = new Test();
                string testDriverName = testElem.Element("testDriver").Value;
                test.testName = testElem.Attribute("name").Value;
                test.files.Add(testDriverName);
                foreach (XElement lib in testElem.Elements("library"))
                {
                    test.files.Add(lib.Value);
                }
                tests.Add(test);
            }
            return tests;
        }
        //----< retrieve test code from testRequest >--------------------

        List<string> extractCode(List<ITestInfo> testInfos)
        {
            Console.Write("\n  retrieving code files from testInfo data structure");
            List<string> codes = new List<string>();
            foreach (ITestInfo testInfo in testInfos)
                codes.AddRange(testInfo.files);
            return codes;
        }
        //----< create local directory and load from Repository >--------

        RequestInfo processRequestAndLoadFiles(Message testRequest)
        {
            string localDir_ = "";
            RequestInfo rqi = new RequestInfo();
            rqi.requestInfo = extractTests(testRequest);
            List<string> files = extractCode(rqi.requestInfo);

            localDir_ = makeKey(testRequest.author);            // name of temporary dir to hold test files
            rqi.tempDirName = localDir_;
            curDir = localDir_;
            filePath_ = System.IO.Path.GetFullPath(localDir_);  // LoadAndTest will use this path
            string fstr = "filePath_ is changed: " + filePath_;
            fstr.title();
            string ldir = "Creating local test directory " + localDir_;
            ldir.title();
            System.IO.Directory.CreateDirectory(localDir_);

            //   Console.Write("\n  loading code from Repository");
            IService RepoChannel = createSendChannel("http://localhost:8080/RepoIService");
            if (RepoChannel == null)
            {
                "Failed to connect to Repository to load the code to be tested".title();
                return null;
            }
            string fileList = null;

            foreach (string file in files)
            {
                fileList += file + ",";
            }
            Message repomsg = new Message();
            repomsg.from = "http://localhost:8080/TestHarnessIService";
            repomsg.to = "http://localhost:8080/RepoIService";
            repomsg.author = "jashwanth";
            repomsg.body = fileList;
            //Console.WriteLine("Message body which contains List of files requested TH->REPO {0}", fileList);
            //repomsg.show();
            RepoChannel.PostMessage(repomsg);
            Console.WriteLine();
            return rqi;
        }
        //----< save results and logs in Repository >--------------------

        bool saveResultsAndLogs(ITestResults testResults, string dir)
        {
            string logName = testResults.testKey + ".txt";
            System.IO.StreamWriter sr = null;
            try
            {
                sr = new System.IO.StreamWriter(System.IO.Path.Combine(dir, logName));
                sr.WriteLine(logName);
                foreach (ITestResult test in testResults.testResults)
                {
                    sr.WriteLine("-----------------------------");
                    sr.WriteLine(test.testName);
                    sr.WriteLine(test.testResult);
                    sr.WriteLine(test.testLog);
                }
                sr.WriteLine("-----------------------------");
            }
            catch
            {
                sr.Close();
                return false;
            }
            sr.Close();
            string torepo = "sending the log file" + logName + "to Repo using filestream service - Req#7";
            torepo.title();
            uploadFile(logName, "http://localhost:8080/RepoIService", dir);
            return true;
        }
        //----< run tests >----------------------------------------------
        /*
         * In Project #4 this function is run by the thread proc for
         * each child AppDomain thread.
         */
        ITestResults runTests(Message testRequest)
        {
            AppDomain ad = null;
            ILoadAndTest ldandtst = null;
            RequestInfo rqi = null;
            ITestResults tr = null;

            try
            {
                lock (sync_)
                {
                    rqi = processRequestAndLoadFiles(testRequest);
                    Thread.Sleep(15000);
                    ad = createChildAppDomain();
                    "Created a Child AppDomain to run the test Request - Req#4".title();
                    ldandtst = installLoader(ad);
                }
                if (ldandtst != null)
                {
                    tr = ldandtst.test(rqi);
                }
                // unloading ChildDomain, and so unloading the library
                Console.WriteLine("Before Saving the log to local dir");
                Thread.Sleep(1000);
                saveResultsAndLogs(tr, rqi.tempDirName);

                lock (sync_)
                {
                    Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": unloading: \"" + ad.FriendlyName + "\"\n");
                    AppDomain.Unload(ad);
                    try
                    {
                  //      System.IO.Directory.Delete(rqi.tempDirName, true);
                        Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": removed directory " + rqi.tempDirName);
                    }
                    catch (Exception ex)
                    {
                        Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": could not remove directory " + rqi.tempDirName);
                        Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": " + ex.Message);
                    }
                }
                return tr;
            }
            catch (Exception ex)
            {
                Console.Write("\n\n---- {0}\n\n", ex.Message);
                return tr;
            }
        }
        //----< make TestResults Message >-------------------------------

        Message makeTestResultsMessage(ITestResults tr, string toUri)
        {
            Message trMsg = new Message();
            trMsg.author = "Jashwanth";
            trMsg.to = toUri;
            trMsg.from = "http://localhost:8080/TestHarnessIService";
            XDocument doc = new XDocument();
            XElement root = new XElement("testResultsMsg");
            doc.Add(root);
            XElement testKey = new XElement("testKey");
            testKey.Value = tr.testKey;
            root.Add(testKey);
            XElement timeStamp = new XElement("timeStamp");
            timeStamp.Value = tr.dateTime.ToString();
            root.Add(timeStamp);
            XElement testResults = new XElement("testResults");
            root.Add(testResults);
            foreach (ITestResult test in tr.testResults)
            {
                XElement testResult = new XElement("testResult");
                testResults.Add(testResult);
                XElement testName = new XElement("testName");
                testName.Value = test.testName;
                testResult.Add(testName);
                XElement result = new XElement("result");
                result.Value = test.testResult;
                testResult.Add(result);
                XElement log = new XElement("log");
                log.Value = test.testLog;
                testResult.Add(log);
            }
            trMsg.body = doc.ToString();
            return trMsg;
        }
        //----< wait for all threads to finish >-------------------------

        public void wait()
        {
            if (thProcess != null)
                thProcess.Join();
        }
      
        // used for debugging 
        void showAssemblies(AppDomain ad)
        {
            Assembly[] arrayOfAssems = ad.GetAssemblies();
            foreach (Assembly assem in arrayOfAssems)
                Console.Write("\n  " + assem.ToString());
        }

        //----< create child AppDomain >---------------------------------
        public AppDomain createChildAppDomain()
        {
            try
            {
                Console.Write("\n  creating child AppDomain - Req #5");

                AppDomainSetup domaininfo = new AppDomainSetup();
                domaininfo.ApplicationBase
                  = "file:///" + System.Environment.CurrentDirectory;  // defines search path for LoadAndTest library

                //Create evidence for the new AppDomain from evidence of current

                Evidence adevidence = AppDomain.CurrentDomain.Evidence;

                // Create Child AppDomain

                AppDomain ad
                  = AppDomain.CreateDomain("ChildDomain", adevidence, domaininfo);

                Console.Write("\n  created AppDomain \"" + ad.FriendlyName + "\"");
                return ad;
            }
            catch (Exception except)
            {
                Console.Write("\n  " + except.Message + "\n\n");
            }
            return null;
        }

        //----< Load and Test is responsible for testing >---------------
        ILoadAndTest installLoader(AppDomain ad)
        {
            ad.Load("LoadAndTest");
            //showAssemblies(ad);
            //Console.WriteLine();

            // create proxy for LoadAndTest object in child AppDomain

            ObjectHandle oh
              = ad.CreateInstance("LoadAndTest", "TestHarness.LoadAndTest");
            object ob = oh.Unwrap();    // unwrap creates proxy to ChildDomain
                                        // Console.Write("\n  {0}", ob);

            // set reference to LoadAndTest object in child

            ILoadAndTest landt = (ILoadAndTest)ob;

            // create Callback object in parent domain and pass reference
            // to LoadAndTest object in child

            landt.setCallback(cb_);
            landt.loadPath(filePath_);  // send file path to LoadAndTest
            return landt;
        }

        static void Main(string[] args)
        {
            Console.Title = "TestHarnessDemo";
            "Begin of Testharess Main Execution".title();
            try
            {
                TestHarness myth_ = new TestHarness();
                myth_.CreateThRecvChannel("http://localhost:8080/TestHarnessIService");
                myth_.wait();
            }
            catch (Exception ex)
            {
                Console.Write("\n\n  {0}\n\n", ex.Message);
            }
            Console.ReadLine();
        }
    }
}
