/////////////////////////////////////////////////////////////////////
// ClientServiceWPF.cs - Service class used by remote channels to  //
//                       create proxy objects and communicate with // 
//                       client                                    //
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
* - ClientServiceWPF.cs, IService.cs, Messages.cs
*
* Public Interface :
* PostMessage(Message ) : post messages to Client Blocking Queue
* downLoadFile()        : returns the stream object and sent across
*                         the channel to be downloaded remotely. 
* upLoadFile()          : upload the file to the client when called
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
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
                    IncludeExceptionDetailInFaults = true)]
    class ClientService : IService
    {
        static string ToSendPath = "..\\..\\FilesToSend";
        static string SavePath = "..\\..\\FilesReceived";
        int BlockSize = 1024;
        byte[] block = null;

        public ClientService()
        {
            block = new byte[BlockSize];
        }
        public void PostMessage(Message msg)
        {
            "Remote channel posted a new message to client".title();
            "The message is".title();
            msg.show();
            ClientWPF.EnqueueMessagesToClient(msg);
        }

        public Stream downLoadFile(string filename)
        {
            string sfilename = Path.Combine(ToSendPath, filename);
            FileStream outStream = null;
            if (File.Exists(sfilename))
            {
                outStream = new FileStream(sfilename, FileMode.Open);
            }
            else
                throw new Exception("open failed for \"" + filename + "\"");
            string toPrint = "Sent a outstream for " + filename ;
            toPrint.title();
            return outStream;
        }

        public void upLoadFile(FileTransferMessage msg)
        {
            int totalBytes = 0;
            string filename = msg.filename;
            string rfilename = Path.Combine(SavePath, filename);
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);
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
            string toPrint = "Received file " + filename + "of bytes: " + totalBytes;
            toPrint.title();
        }
    }
}
