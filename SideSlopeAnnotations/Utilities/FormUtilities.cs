/*--------------------------------------------------------------------------------------+
|
|  $Copyright: (c) 2022 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/

#region Used References

using System;
using System.Diagnostics;
using System.Windows;
using Bentley.Interop.MicroStationDGN;
using SideSlopeAnnotations.Forms;
using BIM = Bentley.Interop.MicroStationDGN;
using BMI = Bentley.MstnPlatformNET.InteropServices;

#endregion

namespace SideSlopeAnnotations.Utilities
    {
    /// <summary>
    /// General Utility functions used in SideSlopeAnnotations add-in functionality
    /// </summary>
    internal class FormUtilities
    {

        /// <summary>
        /// Object of FrmSideSlopeAnnotation dlg. Used to show/hide dlg.
        /// </summary>
        static public FrmSideSlopeAnnotation m_FrmSideSlopeAnnotation = new FrmSideSlopeAnnotation();

        /// <summary>
        /// Read only validation
        /// </summary>
        /// <returns></returns>
        public static bool IsDGNReadOnly()
        {
            bool bIsReadOnly = false;
            try
            {
                BIM.Application msApp = BMI.Utilities.ComApp;
                DesignFile activeDesignFile = msApp.ActiveDesignFile;
                ModelReference activeModelReference = msApp.ActiveModelReference;
                bIsReadOnly = activeModelReference.IsReadOnly;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return bIsReadOnly;
        }

        #region Form Show Hide Operations

        /// <summary>
        /// Displays SideSlopeAnnotationDlg
        /// </summary>
        public static void ShowSideSlopeAnnotationDlg()
        {
            try
            {
                if (IsDGNReadOnly())
                {
                    MessageBox.Show("SideSlopeAnnotations tool can not run on READ ONLY DGN file", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!m_FrmSideSlopeAnnotation.m_bIsOpened)
                {

                    m_FrmSideSlopeAnnotation = new FrmSideSlopeAnnotation();
                    m_FrmSideSlopeAnnotation.Show();
                }
                else
                {
                    FrmSideSlopeAnnotation.m_sInstance.Activate();
                    FrmSideSlopeAnnotation.m_sInstance.Focus();
                    FrmSideSlopeAnnotation.m_sInstance.BringIntoView();

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Hides SideSlopDlg
        /// </summary>
        public static void HideSideSlopeAnnotationDlg()
        {
            try
            {
                if (m_FrmSideSlopeAnnotation.m_bIsOpened)
                {
                    FrmSideSlopeAnnotation.m_sInstance.m_bIsOpened = false;
                    FrmSideSlopeAnnotation.m_sInstance.Hide();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        #endregion
    }
}
