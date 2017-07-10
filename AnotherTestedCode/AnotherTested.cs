/////////////////////////////////////////////////////////////////////
//  AnotherTested.cs -    Module to be tested by TestHarness       //
//  ver 1.0                                                        //
//  Language:      Visual C#  2015                                 //
//  Platform:      Mac, Windows 7                                  //
//  Application:   TestHarness , FL16                              //
//  Author:        Jashwanth Reddy, Syracuse University            //
//                 (315) 949-8857, jgangula@syr.edu                //
/////////////////////////////////////////////////////////////////////

/*
Module Operations:
==================
This package contains all the code that to be tested by testharess


Public Interface:
=================
public:
------
myWackyFunction() - Tests the code and returns the bool status

Build Process:
==============
Required files

Maintainance History
====================
ver 1.0 : 20 November 2016
    - first release 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
  public class AnotherTested
  {
    public bool myWackyFunction()
    {
      return false;
    }
#if (TEST_TESTED)
    static void Main(string[] args)
    {
    }
#endif
  }
}
