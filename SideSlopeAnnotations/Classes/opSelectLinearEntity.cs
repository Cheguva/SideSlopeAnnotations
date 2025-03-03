/*--------------------------------------------------------------------------------------+
|
|  $Copyright: (c) 2022 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Bentley.CifNET.GeometryModel.SDK;
using Bentley.CifNET.SDK.Edit;
using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.MstnPlatformNET;
using SideSlopeAnnotations.Forms;
using SideSlopeAnnotations.Utilities;


namespace SideSlopeAnnotations.Classes
{
    /// <summary>
    /// Operator class which allows user to select alignment for drawing Slope lines
    /// </summary>
    public class opSelectLinearEntity : DgnElementSetTool
    {
        /// <summary>
        /// Alignment used in operator
        /// </summary>
        private static Element m_Element = null;
        /// <summary>
        /// MSElementType
        /// </summary>
        private static MSElementType m_ElementType = MSElementType.Line;
        /// <summary>
        /// Feature name
        /// </summary>
        public static string m_FeatureName = "";

        [DllImport("ustation.dll", EntryPoint = "?mdlLocate_allowAllModels@@YAX_N@Z", CharSet = CharSet.Unicode)]
        public static extern void mdlLocate_allowAllModels(bool allModels);
        protected override void SetLocateCriteria()
        {
            base.SetLocateCriteria();
            mdlLocate_allowAllModels(true);
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void OnPostInstall()
        {
            try
            {
                base.BeginPickElements();
                AccuSnap.LocateEnabled = true;
                AccuSnap.SnapEnabled = true;
                Settings.SnapMode = SnapMode.Nearest;
                base.OnPostInstall();
                m_Element = null;
                GeneralUtilities.NotifyMessage("Select Linear element to calculate Side Slope");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cantAcceptReason"></param>
        /// <returns></returns>
        protected override bool OnPostLocate(HitPath path, out string cantAcceptReason)
        {
            cantAcceptReason = "";
            try
            {
                m_Element = null;

                if (path == null)
                {
                    cantAcceptReason = "HitPath is null.";
                    return false;
                }

                ConsensusConnectionEdit consensusConnectionEdit = ConsensusConnectionEdit.GetActive();
                GeometricModel geometricModel = consensusConnectionEdit.GetActiveGeometricModel();
                if (geometricModel == null)
                {
                    cantAcceptReason = "Geometric Model not found.";
                    return false;
                }


                m_Element = path.GetCursorElement();

                if (m_Element == null)
                {
                    cantAcceptReason = "There is no element at cursor.";
                    return false;
                }

                if (path.GetCursorElement().DgnModel.IsReadOnly)
                {
                    cantAcceptReason = "Element with Read-Only Model is not acceptable";
                    return false;
                }



                //Validation suggested by Daryl to allow only 3D elements
                if (FrmSideSlopeAnnotation.currentSlopeMethod == SlopeMethod.SlopeMethodElevation)
                {
                    if (!path.GetCursorElement().DgnModel.Is3d)
                    {
                        cantAcceptReason = "Select valid 3D Linear element.";
                        return false;
                    }
                }

                cantAcceptReason = String.Empty;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dgnButtonEvent"></param>
        /// <returns></returns>
        protected override bool OnDataButton(DgnButtonEvent dgnButtonEvent)
        {
            try
            {
                //Initiate
                //  string cantAcceptReason = string.Empty;

                HitPath hitPath = DoLocate(dgnButtonEvent, true, 0);
                if (hitPath == null)
                {
                    return false;
                }

                m_Element = hitPath.GetCursorElement();

                if (m_Element == null)
                {
                    return false;
                }

                //Handle ElementTYpe

                m_ElementType = m_Element.ElementType;


                if (m_Element.ElementType != MSElementType.Line &&
                    m_Element.ElementType != MSElementType.LineString &&
                    m_Element.ElementType != MSElementType.ComplexString &&
                    m_Element.ElementType != MSElementType.ComplexShape &&
                    m_Element.ElementType != MSElementType.Multiline
                  )
                {
                    GeneralUtilities.NotifyMessage("Please select a Linear element only");
                    return false;
                }

                //Validation suggested by Daryl to allow only 3D elements
                if (FrmSideSlopeAnnotation.currentSlopeMethod == SlopeMethod.SlopeMethodElevation)
                { //Check if Selected Element has FeatureName
                    string elementFeatureName = ElementUtilities.GetFeatureName(m_Element).Trim();

                    //Validation
                    if (string.IsNullOrWhiteSpace(elementFeatureName.Trim()))
                        elementFeatureName = "Unknown";
                    FormUtilities.m_FrmSideSlopeAnnotation.UpdateElementDetails(m_Element, m_ElementType, elementFeatureName);
                    FormUtilities.ShowSideSlopeAnnotationDlg(); //Activate form
                    setForground(FrmSideSlopeAnnotation.m_sInstance);
                    GeneralUtilities.NotifyMessage("Linear element selected successfully");
                }
                else //Condition for MicroStation Lines without FeatureName
                {
                    FormUtilities.m_FrmSideSlopeAnnotation.UpdateElementDetails(m_Element, m_ElementType, "Not Applicable");
                    FormUtilities.ShowSideSlopeAnnotationDlg();//Activate form
                    setForground(FrmSideSlopeAnnotation.m_sInstance);
                    GeneralUtilities.NotifyMessage("Linear element selected successfully");
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return true;
        }

        private void setForground(FrmSideSlopeAnnotation frm)
        {

            try
            {
                frm.Topmost = true;//show form in top
                frm.BringIntoView();
                frm.Topmost = false;//make default window behavior

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public override StatusInt OnElementModify(Element element)
        {
            return StatusInt.Error;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnRestartTool()
        {
            InstallNewInstance();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void InstallNewInstance()
        {
            try
            {
                opSelectLinearEntity opSelectLineElement = new opSelectLinearEntity();
                opSelectLineElement.InstallTool();
                m_Element = null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void EndTool()
        {
            //Workaround for issue: Curve Number dlg not in front
            //Once Selection operator ends Curve Dialog is displayed and then ElementSelection tool is registered which causes dlg to hide again
            //So now, Selection operator is not quite after selecting points. Key in to set ElementSelection tool is trigerre here which would internally quite Selection tool
            GeneralUtilities.RunKeyInCommand("CHOOSE ELEMENT");
        }
    }
    }
