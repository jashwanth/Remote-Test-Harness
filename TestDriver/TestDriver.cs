/////////////////////////////////////////////////////////////////////
// TestDriver.cs - define testing process                          //
//                                                                 //                
// Application: CSE681 - Software Modelling and Analysis,          //
// Test Harness Project-4                                          //
// Author:      Jashwanth Reddy, Syracuse University,              //
//              jgangula@syr.edu, (315) 949-8857                   //
// Source:      Jim Fawcett                                        //
/////////////////////////////////////////////////////////////////////
/* Module Operation:
 * ================
 * This is test driver code which tests the functionality of TestedCode module
 *
 * Public Interface
 * ================
 * bool test()   :  Create an instance of TestedCode and return the test execution status.
 * bool getLog() :  To give extra log information of the tested code 
 *                  and helpful to user to further debug.
 *
 * Build Process
 * =============
 * - Required Files: TestDriver.cs, Tested.cs, ITest.cs
 * - Compiler Command: csc TestDriver.cs, Tested.cs
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
 public  class TestDriver : ITest
  {
    public bool test()
    {
      TestHarness.Tested tested = new TestHarness.Tested();
      return tested.myWackyFunction();
    }
    public string getLog()
    {
      return "demo test that always passes";
    }
#if (TEST_TESTDRIVER)
    static void Main(string[] args)
    {
    }
#endif
  }
}
