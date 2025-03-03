/*--------------------------------------------------------------------------------------+
|
|     $Source: Keyin.cs $
|
+--------------------------------------------------------------------------------------*/
using SideSlopeAnnotations.Utilities;
namespace SideSlopeAnnotations
{
    /*=====================================================================================**/
    /* Required | Keyin Class            
    /*=====================================================================================**/
    // Interface between CommandTable.xml and AddIn.cs
    public sealed class Keyin
    {
        /*------------------------------------------------------------------------------------**/
        /* SideSlopeAnnotations -> Annotate_Side_Slope
        /*------------------------------------------------------------------------------------**/
        public static void AnnotateSideSlope(string unparsed)
        {
            FormUtilities.ShowSideSlopeAnnotationDlg();
        }
    }
}

