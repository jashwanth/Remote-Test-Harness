/////////////////////////////////////////////////////////////////////
// RepoService.cs - Service class used by remote channels to       //
//                  create proxy objects and communicate with      // 
//                  TestHarness                                    //
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

/* Required Files:
* - TestHarnessService.cs, IService.cs, Messages.cs
*
* Public Interface :
* PostMessage(Message ) : post messages to TestHarness Blocking Queue
* downLoadFile()        : returns the stream object and sent across
*                         the channel to be downloaded remotely. 
* upLoadFile()          : upload the file to the TestHarness when called
*                         remotely 
* Maintenance History:
 --------------------
* ver 1.0 : 20 November 2016
* - first release
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    class TestHarnessService : IService
    {
        public void PostMessage(Message msg)
        {
            "TestHarness received a new message:".title();
            msg.show();
            TestHarness.EnqueueMessagesToTestHarness(msg);
        }
        public void upLoadFile(FileTransferMessage msg)
        {
            string dir = TestHarness.returnCurTempDir();
            int totalBytes = 0;
            int BlockSize = 1024;
            byte[] block = new byte[BlockSize];
            string filename = msg.filename;
            string rfilename = Path.Combine(dir, filename);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            using (var outputStream = new FileStream(rfilename, FileMode.Create))
            {
                while (true)
                {
                    int bytesRead = msg.transferStream.Read(block, 0, BlockSize);
                    totalBytes += bytesRead;
                    if (bytesRead > 0)
                        outputStream.Write(block, 0, bytesRead);
                    else
                        break;
                }
            }
            string str =  "Received file " + filename + " of " + totalBytes + "bytes .";
            str.title();
            return;
        }
        /* Not used in the current project 4
         However the functionality will just be same as any other service
         methods used in this project */
        public Stream downLoadFile(string filename)
        {
            return null;
        }
    }
}
