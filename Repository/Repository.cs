/////////////////////////////////////////////////////////////////////
// Repository.cs - This module stores all the codes and result logs//
//                 useful for other modules                        //
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
 * Will download the code to be tested from client
 * and will upload the code to be tested to Testharness
 * Will accept Queries for Logs and Libraries.
 * 
 * Required Files:
 * - Repository.cs, ITest.cs, Message.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 20 Nov 2016
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace TestHarness
{
  public class Repository : IRepository
  {
    private string repoStoragePath = "..\\..\\..\\Repository\\RepositoryStorage\\";
    public static SWTools.BlockingQueue<Message> inQ_ { get; set; }
    static ServiceHost RepoService = null;
    private Thread RepoThrd = null;
    private string lastError = "";

    public static void EnqueueMessagesToRepo(Message m)
    {
       inQ_.enQ(m);
    }

    public Message getMessage()
    {
       return inQ_.deQ();
    }

    // Create proxy to another Peer's Communicator
    public IService createSendChannel(string address)
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
         //    Console.Write("\n Created proxy object to communicate with Test Harness");
             return factory.CreateChannel();
         }
         else
            return null;     
         
    }

    //Create Reposiotry serviceHost to accept new connections
    public void CreateRepoRecvChannel(string address)
    {
       // Can't configure SecurityMode other than none with streaming.
       // This is the default for BasicHttpBinding.
        BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
        BasicHttpBinding binding = new BasicHttpBinding(securityMode);
        Uri baseAddress = new Uri(address);
        binding.TransferMode = TransferMode.Streamed;
        binding.MaxReceivedMessageSize = 50000000;
        RepoService = new ServiceHost(typeof(RepoService), baseAddress);
        RepoService.AddServiceEndpoint(typeof(IService), binding, baseAddress);
        RepoService.Open();
    }
    public void Close()
    {
        RepoService.Close();
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
     //   Console.Write("\n Repository received a new message \n");
     //   msg.show();
        if (msg.from.Contains("Client"))
        {
           Message newm = new Message("");
           newm.from = "http://localhost:8080/RepoIService";
           newm.to = msg.from;
           string toPrint = "Received a client query to search for string " + msg.body + "returns logs with that string - Req#9";
           toPrint.title();
           List<string> result = queryLogs(msg.body);
           foreach (string str in result)
           {
               newm.body += str + "\n";         
           }
           IService Clientchannel = createSendChannel(msg.from);
           if (Clientchannel != null)
           {
             "Sending query log to the client".title();
             "Sending the below message".title();
             Console.WriteLine("Message body is {0} ", newm.body);
             Clientchannel.PostMessage(newm);
           }
           else
           {
              "Client channel is not created hence cannot send query log".title();
           }
         }
         else if (msg.from.Contains("Test"))
         {
         //   Console.WriteLine("Repository received a message from TestHarness");
            msg.show();
            if (getFiles(msg.from, msg.body) != true)
            {
               "Failed to process the code request from the TestHarness".title();
            }
          }
      }
    }

    public Repository(bool flag)
        {
            //   Console.Write("\n Creating instance of Repository");
            if (inQ_ == null)
                inQ_ = new SWTools.BlockingQueue<Message>();
            //   CreateRepoRecvChannel(RepoUrl);
            //   Console.Write("\n Created new Repository Service to accept http connections");
            if (flag == true)
            {
                RepoThrd = new Thread(ThreadProc);
                RepoThrd.IsBackground = true;
                RepoThrd.Start();
            }
        }
        
   //----< search for text in log files >---------------------------
   /*
    * This function should return a message.  I'll do that when I
    * get a chance.
    */
    public List<string> queryLogs(string queryText)
    {
      List<string> queryResults = new List<string>();
      string path = System.IO.Path.GetFullPath(repoStoragePath);
      string[] files = System.IO.Directory.GetFiles(repoStoragePath, "*.txt");
      foreach(string file in files)
      {
        string contents = File.ReadAllText(file);
        if (contents.Contains(queryText))
        {
          string name = System.IO.Path.GetFileName(file);
          queryResults.Add(name);
        }
      }
      queryResults.Sort();
      queryResults.Reverse();
      return queryResults;
    }
    public void uploadFile(string filename, string Uri)
        {
            string fqname = Path.Combine(repoStoragePath, filename);
            IService Channel = null;
            /* Send to TestHarness if flag is 1 */

            Channel = createSendChannel(Uri);
            if (Channel == null)
            {
                Console.WriteLine("Repository Failed to establish connection with {0}", Uri);
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

    public void download(string filename, string Uri)
    {
         IService Channel = null;
         int BlockSize = 1024;
         byte[] block = new byte[BlockSize];
         Channel = createSendChannel(Uri);
         if (Channel == null)
         {
              Console.WriteLine("Failed to download files from {0} ", Uri);
         }
         else
         {
            Channel = createSendChannel("http://localhost:8080/TestHarnessIService");
            if (Channel == null)
                Console.WriteLine("Failed to download files from TestHarness");
         }
         int totalBytes = 0;
         try
         {
            Stream strm = Channel.downLoadFile(filename);
            string rfilename = Path.Combine(repoStoragePath, filename);
            if (!Directory.Exists(repoStoragePath))
                 Directory.CreateDirectory(repoStoragePath);
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
  
    //----< send files with names on fileList >----------------------   
    public bool getFiles(string uri, string fileList)
        {
            string[] files = fileList.Split(new char[] { ',' });
            //IService Channel = createSendChannel(uri);
            string repoStoragePath = "..\\..\\RepositoryStorage\\";
            foreach (string file in files)
            {
                string str = "Sending the file requested " + file + " to TestHarness using filestream - Req#6";
                str.title();
                string fqSrcFile = repoStoragePath + file;
               // string fqDstFile = "";
                try
                {
                    uploadFile(file, uri);
               //     fqDstFile = path + "\\" + file;
               //     File.Copy(fqSrcFile, fqDstFile);
                }
                catch(Exception ex)
                {
                    Console.Write("\n  could not Upload file: {0] Exception message: {1}", file, ex.Message);
                    return false;
                }
            }
            return true;
        }
    
    static void Main(string[] args)
    {
            Console.Title = "RepositoryDemo";
            "Begin of Execution of Repository Main".title();
         try
         { 
           Repository myrepo = new Repository(true);
           myrepo.CreateRepoRecvChannel("http://localhost:8080/RepoIService");
         }
         catch (Exception ex)
         {
            Console.Write("\n\n  {0}\n\n", ex.Message);
         }
         Console.ReadLine();
    }
   }
  public class RepoTest : ITest
    {
        private StringBuilder RepoLog;
        public RepoTest()
        {
            RepoLog = new StringBuilder();
        }
        public string getLog()
        {
            return RepoLog.ToString();
        }

        public bool test()
        {
            Repository myrepo = new Repository(false);
            string repostr = "http://localhost:8080/RepoIService";
            myrepo.CreateRepoRecvChannel(repostr);
            while (true)
            {
                Message msg = myrepo.getMessage();
                if (msg.body == "quit")
                {
                    myrepo.Close();
                    break;
                }
           
                if (msg.from.Contains("Client"))
                {
                    Message newm = new Message("");
                    newm.from = repostr;
                    newm.to = msg.from;
                    List<string> result = myrepo.queryLogs(msg.body);
                    foreach (string str in result)
                    {
                        newm.body += str + "\n";
                    }
                    IService Clientchannel = myrepo.createSendChannel(msg.from);
                    if (Clientchannel != null)
                    {
                        "Sending query log to the client".title();
                        "Sending the below message to client".title();
                        Console.WriteLine("Message body is {0} ", newm.body);
                        Clientchannel.PostMessage(newm);
                    }
                    else
                    {
                        "Client channel is not created hence cannot send query log".title();
                    }
                }
            }
            return true;
        }
    }
}
