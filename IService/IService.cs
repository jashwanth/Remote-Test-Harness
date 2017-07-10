/////////////////////////////////////////////////////////////////////
//IService.cs     This is an interface that contains the methods   //
//                used by modules which implements this interface. //
//                The methods are a way of providing services like //
//                posting messages and filestream services.Also    //
//                contains a FileTransferMessage with filename and //
//                opened stream information.                       //
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
 * Public Interface :
 * PostMessage(Message) : Post the message remotely to its Blocking
 *                        Queue
 * upLoadFile(FileTransferMessage) : upload the file remotely
 * downLoadFile()                  : download file from remote server
 *                        
 * Maintenance History:
 * ===================
 * ver 1.0 : 20 Nov ,16
 * - first release
 */


using System;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace TestHarness
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract(IsOneWay = true)]
        void PostMessage(Message msg);
        [OperationContract(IsOneWay = true)]
        void upLoadFile(FileTransferMessage msg);
        [OperationContract]
        Stream downLoadFile(string filename);
    }
    [MessageContract]
    public class FileTransferMessage
    {
        [MessageHeader(MustUnderstand = true)]
        public string filename { get; set; }

        [MessageBodyMember(Order = 1)]
        public Stream transferStream { get; set; }
    }
}

