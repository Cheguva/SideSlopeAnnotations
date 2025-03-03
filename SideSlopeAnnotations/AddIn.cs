/*--------------------------------------------------------------------------------------+
|
|     $Source: AddIn.cs $
|
+--------------------------------------------------------------------------------------*/

namespace SideSlopeAnnotations
{
    /*=====================================================================================**/
    /* Required | Implementation of Addin Class            
    /*=====================================================================================**/
    [Bentley.MstnPlatformNET.AddInAttribute(MdlTaskID = "SideSlopeAnnotations")]
    public sealed class AddIn : Bentley.MstnPlatformNET.AddIn
    {
        private static AddIn s_ordAddIn = null;

        public AddIn(System.IntPtr mdlDesc)
            : base(mdlDesc)
        {
            s_ordAddIn = this;
        }

        protected override int Run(string[] commandLine)
        {
            return 0;
        }

        internal static AddIn Instance()
        {
            return s_ordAddIn;
        }
        
    }
}
