/////////////////////////////////////////////////////////////////////
// AnotherTestDriver.cs - define testing process                   //
//                                                                 //                
// Application: CSE681 - Software Modelling and Analysis,          //
// Test Harness Project-4                                          //
// Author:      Jashwanth Reddy, Syracuse University,              //
//              jgangula@syr.edu, (315) 949-8857                   //
// Source:      Jim Fawcett                                        //
/////////////////////////////////////////////////////////////////////

/* Module Operation:
 * ================
 * This is test driver code which tests the functionality of AnotherTestedCode
 * module
 * 
 * Public Interface 
 * ================
 * bool test()                //Create an instance of AnotherTestedCode and return the test execution status.
 * bool getLog()              //To give extra log information of the tested code and helpful to user to further debug.
 *
 * Build Process
 * =============
 * - Required Files: AnotherTestDriver.cs, AnotherTested.cs, ITest.cs
 * - Compiler Command: csc AnotherTestDriver.cs, AnotherTested.cs
 *
 * Maintainance History
 * ====================
 * ver 1.0 : 20 November 2016
 *     - first release 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHarness;

namespace TestHarness
{
  public class AnotherTestDriver : ITest
  {
    public bool test()
    {
      TestHarness.AnotherTested tested = new TestHarness.AnotherTested();
      return tested.myWackyFunction();
    }
    public string getLog()
    {
      return "demo test that always fails";
    }
#if (TEST_ANOTHERTESTDRIVER)
    static void Main(string[] args)
    {
    }
#endif
  }
}
