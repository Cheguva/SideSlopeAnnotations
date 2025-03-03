/*--------------------------------------------------------------------------------------+
|
|  $Copyright: (c) 2022 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/

using Bentley.Interop.MicroStationDGN;
using BMI = Bentley.MstnPlatformNET.InteropServices;

namespace SideSlopeAnnotations.Classes
{
    class clsTransactionWrapper
    {
        public void StartUndoableTransaction(string name)
        {
            Application msApp = BMI.Utilities.ComApp;
            msApp.CommandState.CommandName = name;
        }
        private void Class_Terminate()
        {
            Application msApp = BMI.Utilities.ComApp;
            msApp.CommandState.StartDefaultCommand();
        }
    }
}
