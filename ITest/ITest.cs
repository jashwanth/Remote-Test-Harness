/////////////////////////////////////////////////////////////////////
// ITest.cs - Abstarct interfaces that need to be implemented by   //
//            respective classes so that certain functionality is  //
//            mandated on them.                                    //
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
 * ITest.cs provides interfaces:
 * - ICallback      used by child AppDomain to send messages to TestHarness
 * - IRequestInfo   used by TestHarness
 * - ITestInfo      used by TestHarness
 * - ILoadAndTest   used by TestHarness
 * - ITest          used by LoadAndTest
 * - IRepository    used by Client and TestHarness
 * - IClient        used by TestExec and TestHarness
 *
 * Required files:
 * ---------------
 * - ITest.cs
 * 
 * Maintanence History:
 * --------------------
 * ver 1.1 : 11 Nov 2016
 * - added loadPath function to ILoadAndTest
 * ver 1.0 : 16 Oct 2016
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
    // Extension Method for display of formatted results
    static public class BasicExtensions  // the first argument type "this string" defines the target type
    {
        static public void title(this string astring, char underline = '=')
        {
            Console.Write("\n  {0}", astring);
            Console.Write("\n {0}", new string(underline, astring.Length + 2));
        }
    }
    /////////////////////////////////////////////////////////////
    // used by child AppDomain to send messages to TestHarness

  public interface ICallback
  {
    void sendCallbackMessage(Message msg);
  }

  /////////////////////////////////////////////////////////////
  // used by child AppDomain to invoke test driver's test()

  public interface ITest
  {
    bool test();
    string getLog();
  }
  /////////////////////////////////////////////////////////////
  // used by child AppDomain to communicate with Repository
  // via TestHarness Comm

  /// <summary>
  /// used by repository to get the files requested by TestHarness
  /// and send query logs to the client
  /// </summary>
  public interface IRepository
  {
    bool getFiles(string path,string fileList);  // fileList is comma separated list of files
    List<string> queryLogs(string queryText);
  }

  /////////////////////////////////////////////////////////////
  // used by client to send requests to TestHarness and Repo
  // via TestHarness Comm
  public interface IClient
  {
    void sendTestRequest(Message msg);
    void makeQuery(string queryText, string fromUri, string ToUri);
  }
  /////////////////////////////////////////////////////////////
  // used by TestHarness to communicate with child AppDomain

  public interface ILoadAndTest
  {
    ITestResults test(IRequestInfo requestInfo);
    void setCallback(ICallback cb);
    void loadPath(string path);
  }
  public interface ITestInfo
  {
    string testName { get; set; }
    List<string> files { get; set; }
  }
  public interface IRequestInfo
  {
    string tempDirName { get; set; }
    List<ITestInfo> requestInfo { get; set; }
  }
  public interface ITestResult
  {
    string testName { get; set; }
    string testResult { get; set; }
    string testLog { get; set; }
  }
  public interface ITestResults
  {
    string testKey { get; set; }
    DateTime dateTime { get; set; }
    List<ITestResult> testResults { get; set; }
  }
}
