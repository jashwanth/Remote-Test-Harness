/////////////////////////////////////////////////////////////////////
// RepoService.cs - Service class used by remote channels to       //
//                  create proxy objects and communicate with      // 
//                  Repository                                     //
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
* - RepoService.cs, IService.cs, Messages.cs
*
* Public Interface :
* PostMessage(Message ) : post messages to Repository Blocking Queue
* downLoadFile()        : returns the stream object and sent across
*                         the channel to be downloaded remotely. 
* upLoadFile()          : upload the file to the repository when called
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
    class RepoService : IService
    {
        int BlockSize = 1024;
        byte[] block = null;
        static string repoStoragePath = "..\\..\\..\\Repository\\RepositoryStorage\\";
        public Stream downLoadFile(string filename)
        {
            string sfilename = Path.Combine(repoStoragePath, filename);
            FileStream outStream = null;
            if (File.Exists(sfilename))
            {
                outStream = new FileStream(sfilename, FileMode.Open);
            }
            else
            {
                string mystr = "open failed for " + filename;
                mystr.title();
                return null;
            }
            string str = "Sent the file: " + filename;
            str.title();
            return outStream;
        }

        public RepoService()
        {
            block = new byte[BlockSize];
        }
        public void PostMessage(Message msg)
        {
           "Repository received a new message:".title();
           //   msg.show();
           Repository.EnqueueMessagesToRepo(msg);
        }

        public void upLoadFile(FileTransferMessage msg)
        {
            int totalBytes = 0;
            string filename = msg.filename;
            string rfilename = Path.Combine(repoStoragePath, filename);
            if (!Directory.Exists(repoStoragePath))
                Directory.CreateDirectory(repoStoragePath);
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
            string str = "Received file " + filename + " of " + totalBytes + "bytes ";
            str.title();
        }
    }
}
