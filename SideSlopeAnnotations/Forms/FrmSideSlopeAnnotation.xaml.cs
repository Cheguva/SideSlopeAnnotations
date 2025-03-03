/*--------------------------------------------------------------------------------------+
|
|  $Copyright: (c) 2022 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/

#region Used References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.GeometryNET;
using Bentley.Interop.MicroStationDGN;
using Bentley.MstnPlatformNET;
using SideSlopeAnnotations.Classes;
using SideSlopeAnnotations.Utilities;
using BIM = Bentley.Interop.MicroStationDGN;
using BMI = Bentley.MstnPlatformNET.InteropServices;
using Element = Bentley.DgnPlatformNET.Elements.Element;


#endregion


namespace SideSlopeAnnotations.Forms
{
    internal class LevelDetail
    {
        public string Name { get; set; }
        public bool IsUpdate { get; set; }
        public int ElementCount { get; set; }
    }

    public enum SlopeMethod
    {
        SlopeMethodElevation,
        SlopeMethodFill2D,
        SlopeMethodCut2D
    }

    /// <summary>
    /// Interaction logic for FrmSideSlopeAnnotation.xaml
    /// </summary>
    public partial class FrmSideSlopeAnnotation : Window
    {
        #region Class References

        public static FrmSideSlopeAnnotation m_sInstance = null;
        public bool m_bIsOpened = false;
        private bool m_bIsPrimaryFeature = true;
        public static SlopeMethod currentSlopeMethod = SlopeMethod.SlopeMethodElevation;
        public static Element m_PrimaryElement = null;
        public static MSElementType m_ePrimaryElementType = MSElementType.Line;
        public static Element m_SecondaryElement = null;
        public static MSElementType m_eSecondaryElementType = MSElementType.Line;

        private int m_nIntervalBtwMajor = 10;
        private int m_nMinorPerMajor = 1;
        private double m_dMinorLength = 50;
        private double m_dMinMajorLength = 0.1;
        private double m_dSlopeDifference = 0.1;

        public bool UseElevationDifferenceFactorForMinorLine = true;
        public double MaximumElevationDifference = 0.0;
        private double m_nPrimaryFeatureLength = 0;
        private double m_nSecondaryFeatureLength = 0;

        //Define Variable for Direction of Secondary found
        private bool DirectionDecided;
        private string Direction = "";
        private int subElementIndex = 0; //SubElement Counter
        private double Pang1 = 0.0;
        private double Pang2 = 0.0;
        private double Station = 0.0;
        private BIM.ModelReference FeatureModelReference = null;
        /*
         * Currently we haven't supported Cell types for Slope
         * There are two issues with Cell type: Rotation & Scale */
        private List<LevelDetail> m_LevelDetailList = new List<LevelDetail>();

        private ElementId m_PrimaryFeatureId;
        private ElementId m_SecondaryFeatureId;


        [DllImport("stdmdlbltin.dll")] static extern int mdlElmdscr_pointAtDistance(out BIM.Point3d position, out BIM.Point3d tangent, double inputDistance, long edP, double inputTolerance);
        double Tradians = 0.0;
        BIM.Element m_oLine = null;
        BIM.Element m_oLine2 = null;
        BIM.BsplineCurve oSpline;
        BIM.Element eleComponent = null;
        BIM.BsplineCurveElement oSplineElement = null;
        BIM.Point3d[] pOCLMW1 = null;
        BIM.Point3d[] pOCLMW2 = null;
        BIM.Point3d[] pOCLCW1 = null;
        BIM.Point3d[] pOCLCW2 = null;
        BIM.Point3d[] pEarth = null;
        BIM.Point3d[] pFeeder = null;
        long graphicGroup = 0;
        int nMinorCount = 0;
        int nMajorCount = 0;

        #endregion


        public FrmSideSlopeAnnotation()
        {
            m_sInstance = this;
            InitializeComponent();

        }

        /// <summary>
        /// Update Element Details in the Form
        /// </summary>
        /// <param name="element"></param>
        /// <param name="eElementType"></param>
        /// <param name="featureName"></param>
        /// <returns></returns>
        public Boolean UpdateElementDetails(Element element, MSElementType eElementType, string featureName)
        {
            try
            {
                //Initiation
                double nLength = 0.0;

                // FeatureModelReference = BMI.Utilities.ComApp.ActiveDesignFile.Models[(int)element.DgnModel.GetModelId() + 1];
                FeatureModelReference = BMI.Utilities.ComApp.MdlGetModelReferenceFromModelRefP(element.DgnModelRef.GetNative().ToInt64());

                long eleId = element.ElementId;
                if (m_bIsPrimaryFeature)
                {

                    //Set Variables to be used later
                    m_PrimaryElement = element;
                    m_oLine = FeatureModelReference.GetElementByID(eleId);
                    m_ePrimaryElementType = eElementType;
                    m_PrimaryFeatureId = element.ElementId;

                    //Update UI Labels
                    lPrimaryFeatureName.Content = featureName; //Update Featurename

                    //Define temp Vector
                    CurveVector vec;
                    switch (m_ePrimaryElementType)
                    {
                        case MSElementType.Line:
                            vec = (m_PrimaryElement as Bentley.DgnPlatformNET.Elements.LineElement).GetCurveVector();
                            // Check that we can extract a CurveVector
                            if (null != vec) nLength = vec.SumOfLengths();
                            break;
                        case MSElementType.LineString:
                            vec = (m_PrimaryElement as LineStringElement).GetCurveVector();
                            // Check that we can extract a CurveVector
                            if (null != vec) nLength = vec.SumOfLengths();
                            break;
                        case MSElementType.ComplexString:
                            vec = (m_PrimaryElement as Bentley.DgnPlatformNET.Elements.ComplexStringElement).GetCurveVector();
                            // Check that we can extract a CurveVector
                            if (null != vec) nLength = vec.SumOfLengths();
                            break;
                        case MSElementType.ComplexShape:
                            vec = (m_PrimaryElement as Bentley.DgnPlatformNET.Elements.ComplexShapeElement).GetCurveVector();
                            // Check that we can extract a CurveVector
                            if (null != vec) nLength = vec.SumOfLengths();
                            break;
                    }
                    m_nPrimaryFeatureLength = nLength / Session.Instance.GetActiveDgnModel().GetModelInfo().UorPerMaster;
                    lPrimaryFeatureLength.Content = m_nPrimaryFeatureLength.ToString(); //Update Feature Length

                }
                else //Secondary Feature
                {
                    //Update Variables for later use
                    m_SecondaryElement = element;
                    m_oLine2 = FeatureModelReference.GetElementByID(eleId);
                    m_eSecondaryElementType = eElementType;
                    m_SecondaryFeatureId = element.ElementId;

                    //Update UI
                    lSecondaryFeatureName.Content = featureName;
                    //Define temp Vector
                    CurveVector vec;
                    switch (m_eSecondaryElementType)
                    {
                        case MSElementType.Line:
                            vec = (m_SecondaryElement as Bentley.DgnPlatformNET.Elements.LineElement).GetCurveVector();
                            // Check that we can extract a CurveVector
                            if (null != vec) nLength = vec.SumOfLengths();
                            break;
                        case MSElementType.LineString:
                            vec = (m_SecondaryElement as LineStringElement).GetCurveVector();
                            // Check that we can extract a CurveVector
                            if (null != vec) nLength = vec.SumOfLengths();
                            break;
                        case MSElementType.ComplexString:
                            vec = (m_SecondaryElement as Bentley.DgnPlatformNET.Elements.ComplexStringElement).GetCurveVector();
                            // Check that we can extract a CurveVector
                            if (null != vec) nLength = vec.SumOfLengths();
                            break;
                        case MSElementType.ComplexShape:
                            vec = (m_SecondaryElement as Bentley.DgnPlatformNET.Elements.ComplexShapeElement).GetCurveVector();
                            // Check that we can extract a CurveVector
                            if (null != vec) nLength = vec.SumOfLengths();
                            break;
                    }
                    m_nSecondaryFeatureLength = nLength / Session.Instance.GetActiveDgnModel().GetModelInfo().UorPerMaster;
                    lSecondaryFeateureLength.Content = m_nSecondaryFeatureLength.ToString();

                }

                #region --Handle Hilite Elements

                //HILITES both Elements after Clearning previous 2 elements


                try
                {

                    if (ElementHighlighter.GetCount() >= 2)
                        ElementHighlighter.Empty();

                    ElementHighlighter.m_eHighlightCriteria = ElementHighlighter.HighlightCriteria.MultipleHighlight;
                    GeneralUtilities.RunKeyInCommand("set hilite yellow");
                    //Hilit Primary
                    if (m_PrimaryElement != null)
                        ElementHighlighter.AddElement(m_PrimaryElement);
                    //Hilit Secondary
                    if (m_SecondaryElement != null)
                        ElementHighlighter.AddElement(m_SecondaryElement);
                    //Hilite all selected
                    ElementHighlighter.Highlight(true);

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }

                #endregion


                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return false;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            FormUtilities.HideSideSlopeAnnotationDlg();
            this.Close();
        }

        private void TextBoxInterval_GotMouseCapture(object sender, MouseEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void TextBoxLineStyleScale_GotMouseCapture(object sender, MouseEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void TextBoxInterval_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void TextBoxLineStyleScale_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                m_bIsOpened = true;
                ElementHighlighter.Empty(); //Clear Hilite

                string masterUnitLabel = GeneralUtilities.GetMasterUnitLabel();
                lblIntervalBetweenMajor.Content = "Interval between Majors (" + masterUnitLabel + "):";
                lblIMinMajorLength.Content = "Minor or Major Length >= (" + masterUnitLabel + "):";
                lblSlopeDifference.Content = "Draw if slope difference is >= (" + masterUnitLabel + ") :";
                lblPrimaryFeatureLength.Content = "Length (" + masterUnitLabel + ") :";
                lblSecondaryFeatureLength.Content = "Length (" + masterUnitLabel + ") :";
                textBoxInterval.Text = m_nIntervalBtwMajor.ToString();
                textBoxMinorPerMejor.Text = m_nMinorPerMajor.ToString();
                textBoxMinorLength.Text = m_dMinorLength.ToString();
                textBoxMinMajorLength.Text = m_dMinMajorLength.ToString();
                textSlopeDifference.Text = m_dSlopeDifference.ToString();

                GetLevelDetails();//Function to get LeveNames in Combobox
                                  //  PopulateModels("Default");//FUnction to list all Models in the COmbobox
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private Bentley.Interop.MicroStationDGN.ModelReference GetModelByName(string modelName)
        {
            try
            {
                //Validation
                if (modelName.Trim() == "")
                    return null;

                BIM.Application msApp = BMI.Utilities.ComApp;
                List<string> modelNames = new List<string>();

                foreach (ModelReference item in msApp.ActiveDesignFile.Models)
                {
                    if (item != null && !item.IsReadOnly)
                    {
                        if (modelName.Trim().ToLower() == item.Name.Trim().ToLower())
                        {
                            return item;
                        }

                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return null;
            }
        }


        private void GetLevelDetails()
        {
            try
            {
                // Get levels used in active file (incl. from dgnlibs)
                var dgnFile = Session.Instance.GetActiveDgnFile();
                List<string> levelNameList = new List<string>();

                using (FileLevelCache levelCache = dgnFile.GetLevelCache())
                {
                    //  uint numOfLevels = levelCache.GetLevelCount();
                    //  uint numOfLevelsInclLibs = levelCache.GetLevelCount(true);
                    // MessageCenter.Instance.StatusMessage = $"Levels in file: {numOfLevels}, levels including dgnlibs: {numOfLevelsInclLibs}.";

                    LevelHandleCollection levelHandles = levelCache.GetHandles();

                    foreach (LevelHandle level in levelHandles)
                    {
                        if (!levelNameList.Contains(level.Name))
                            levelNameList.Add(level.Name);
                    }
                }

                //Finally Populate Filter combo by unique LevelNameCollections
                cmbLevelDisplayFilter.ItemsSource = null;
                levelNameList.Sort();
                cmbLevelDisplayFilter.ItemsSource = levelNameList;
                cmbLevelDisplayFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                ElementHighlighter.RemoveElement(ElementUtilities.GetElementFromId(m_PrimaryFeatureId));
                ElementHighlighter.RemoveElement(ElementUtilities.GetElementFromId(m_SecondaryFeatureId));
                ElementHighlighter.Empty();

                //Destruct Elements
                m_PrimaryElement = null;
                m_SecondaryElement = null;
                m_bIsPrimaryFeature = true;
                FeatureModelReference = null;

                m_bIsOpened = false; //Close Form
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// Add Slope Main Function Call
        /// </summary>
        private void AddSideSlope()
        {

            //Validation for 2 Selected Lines
            if (m_oLine == null || m_oLine2 == null)
            {
                GeneralUtilities.NotifyMessage("Please select valid Primary & Secondary Feature", OutputMessagePriority.Error);
                return;

            }
            else if (m_oLine.ModelReference.Is3D != m_oLine2.ModelReference.Is3D)
            {
                GeneralUtilities.NotifyMessage("Please select valid Primary & Secondary Feature from Same model", OutputMessagePriority.Error);
                return;
            }

            GeneralUtilities.NotifyMessage("Process Started...Creating Slope Line");
            long nPlaced = PlacePoints(true, ref pOCLMW1, ref pOCLMW2, ref pOCLCW1, ref pOCLCW2, ref pEarth, ref pFeeder);
            GeneralUtilities.NotifyMessage("Process Completed...Created Slope Line");
            string message = nMajorCount.ToString() + " : Major Lines\n" + nMinorCount.ToString() + " : Minor Lines";
            MessageBox.Show("Slope annotation added\n\n" + message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            ElementHighlighter.RemoveElement(ElementUtilities.GetElementFromId(m_PrimaryFeatureId));
            ElementHighlighter.RemoveElement(ElementUtilities.GetElementFromId(m_SecondaryFeatureId));
            ElementHighlighter.Empty();

        }

        private void BtnPlaceCell_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Implement Wait Curor
                using (new WaitCursor())
                {

                    if (m_PrimaryElement != null && m_SecondaryElement != null)
                    {
                        if (m_PrimaryFeatureId == m_SecondaryFeatureId)
                        {
                            MessageBox.Show("Primary & Secondary feature entities can't be same.\nPlease select different enities and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        nMajorCount = 0;
                        nMinorCount = 0;

                        //Major Interval
                        string value = textBoxInterval.Text.ToString();
                        bool bIsValidValue = int.TryParse(value, out m_nIntervalBtwMajor);
                        if (!bIsValidValue)
                        {
                            MessageBox.Show("Please enter valid numeric value for Interval between Majors", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        m_nIntervalBtwMajor = (int)GeneralUtilities.ConvertValueToCurrentUnits((double)m_nIntervalBtwMajor);


                        //Minors per major
                        value = textBoxMinorPerMejor.Text.ToString();
                        bIsValidValue = int.TryParse(value, out m_nMinorPerMajor);
                        if (!bIsValidValue)
                        {
                            MessageBox.Show("Please enter valid numeric value for Minors per Major", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        //Minor Length Percentage
                        value = textBoxMinorLength.Text.ToString();
                        bIsValidValue = double.TryParse(value, out m_dMinorLength);
                        if (!bIsValidValue)
                        {
                            MessageBox.Show("Please enter valid numeric value for Minor length percentage", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        m_dMinorLength = GeneralUtilities.ConvertValueToCurrentUnits(m_dMinorLength);

                        //Minimum Length for Major Minor
                        value = textBoxMinMajorLength.Text.ToString();
                        bIsValidValue = double.TryParse(value, out m_dMinMajorLength);
                        if (!bIsValidValue)
                        {
                            MessageBox.Show("Please enter valid numeric value for Minor or Major Length", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        m_dMinMajorLength = GeneralUtilities.ConvertValueToCurrentUnits(m_dMinMajorLength);

                        //Validation for Slope Diffference
                        value = textSlopeDifference.Text.ToString();
                        bIsValidValue = double.TryParse(value, out m_dSlopeDifference);
                        if (!bIsValidValue)
                        {
                            MessageBox.Show("Please enter valid Slope differece criteria", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        m_dSlopeDifference = GeneralUtilities.ConvertValueToCurrentUnits(m_dSlopeDifference);

                        //Validation for Level
                        if (cmbLevelDisplayFilter.Text.Trim() == "")
                        {
                            MessageBox.Show("To add Slope Annotation, Please select a Level or Type New Level Name to create automatically", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }


                        if (UseElevationDifferenceFactorForMinorLine == true)
                        {
                            //Notify user it might take time
                            GeneralUtilities.NotifyMessage("Process Started..Analysing elevation");
                            MaximumElevationDifference = GetMaximumElevationBetween3DLines(m_oLine, m_oLine2);
                            GeneralUtilities.NotifyMessage("Process Started..Analysed elevation");
                        }

                        //Call Main Function to Draw Slope lines
                        AddSideSlope();
                    }
                    else
                    {
                        MessageBox.Show("Please select Valid Primary and Secondary features and try again", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                }//using (new WaitCursor())
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);

            }
        }

        private void BtnSelectPrimaryFeature_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ElementHighlighter.RemoveElement(ElementUtilities.GetElementFromId(m_PrimaryFeatureId));
                m_bIsPrimaryFeature = true;

                //Reset all Previously Set variable
                lPrimaryFeatureName.Content = "-";
                lPrimaryFeatureLength.Content = "0.0";
                //Update Variables for later use
                m_PrimaryElement = null;
                m_oLine = null; ;

                //Initiate tool
                opSelectLinearEntity.InstallNewInstance();

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void BtnSelectSecondaryFeature_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ElementHighlighter.RemoveElement(ElementUtilities.GetElementFromId(m_SecondaryFeatureId));
                m_bIsPrimaryFeature = false;

                //Reset all Previously Set variable
                lSecondaryFeatureName.Content = "-";
                lSecondaryFeateureLength.Content = "0.0";
                //Update Variables for later use
                m_SecondaryElement = null;
                m_oLine2 = null; ;


                //Initiate Tool
                opSelectLinearEntity.InstallNewInstance();

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        public long PlacePoints(bool createTangents, ref BIM.Point3d[] InsPMW1, ref BIM.Point3d[] InsPMW2, ref BIM.Point3d[] InsPCW1, ref BIM.Point3d[] InsPCW2, ref BIM.Point3d[] InsPEarth, ref BIM.Point3d[] InsPFeeder)
        {
            int PlacePoints = 0;
            try
            {
                BIM.Application msApp = BMI.Utilities.ComApp;
                long lastrow = 0;
                long nPoints;
                nPoints = lastrow;
                clsTransactionWrapper oWrapper = new clsTransactionWrapper();
                oWrapper.StartUndoableTransaction("Place Points at Distance along Line");
                BIM.ComplexStringElement Complesso;
                BIM.ElementEnumerator ee;
                subElementIndex = 0;//Reset Subelement counter
                //Initiate OsPline Element everytime
                oSpline = new BIM.BsplineCurveClass();

                //Assume Direction of the Secondary feature from Primary feature initially
                DirectionDecided = false;
                Direction = "LEFT";

                //Reset Station & Pang1 & Pang2 for all Subelements
                Pang1 = 0.0;
                Pang2 = 0.0;
                Station = 0.0;
                graphicGroup = msApp.UpdateGraphicGroupNumber(); //Add Elements to Graphic Group Only

                //  ======================================================================== 
                if ((m_oLine.Type == BIM.MsdElementType.ComplexString))
                {
                    Complesso = (BIM.ComplexStringElement)m_oLine;
                    ee = Complesso.GetSubElements();

                    while (ee.MoveNext())
                    {
                        //Count Number of Subelement for some conditions to handle in Subsequent function
                        subElementIndex++;


                        eleComponent = ee.Current;
                        if (((eleComponent.Type == BIM.MsdElementType.Line)
                                    || ((eleComponent.Type == BIM.MsdElementType.LineString)
                                    || (eleComponent.Type == BIM.MsdElementType.Arc))))
                        {
                            oSpline.FromElement(eleComponent);
                            oSplineElement = msApp.CreateBsplineCurveElement1(null, oSpline);
                            CreateSlopeLine();
                        }
                    }
                }
                else
                {
                    //  single m_oline
                    subElementIndex = 1; //Hardcoding for this case
                    eleComponent = m_oLine;
                    oSpline.FromElement(eleComponent);
                    oSplineElement = msApp.CreateBsplineCurveElement1(null, oSpline);
                    CreateSlopeLine();
                }

                oSplineElement = null;
                oSpline = null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return PlacePoints;
        }

        /// <summary>
        /// Get a Point Along Linear Element at a Distance
        /// </summary>
        /// <param name="position"></param>
        /// <param name="tangent"></param>
        /// <param name="distance"></param>
        /// <param name="oElement"></param>
        /// <returns></returns>
        private bool PointAtDistanceAlongElement(ref BIM.Point3d position, ref BIM.Point3d tangent, double distance, BIM.Element oElement)
        {
            long res;
            const double Tolerance = 0.0001;
            res = mdlElmdscr_pointAtDistance(out position, out tangent, distance, oElement.MdlElementDescrP(), Tolerance);
            return (0 == res);
        }

        /// <summary>
        /// Create Tangent Line at a Point
        /// </summary>
        /// <param name="oSplineElement"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        BIM.LineElement CreateTangentAtPoint(BIM.BsplineCurveElement oSplineElement, double distance)
        {
            BIM.LineElement CreateTangentAtPoint = null;
            try
            {
                BIM.Application msApp = BMI.Utilities.ComApp;
                BIM.Point3d position = new BIM.Point3d();
                BIM.Point3d tangent = new BIM.Point3d();
                object xdeg = 0.0;
                if (PointAtDistanceAlongElement(ref position, ref tangent, distance, oSplineElement))
                {
                    BIM.Point3d[] points = new BIM.Point3d[2];
                    points[0] = msApp.Point3dSubtract(position, tangent);
                    points[1] = msApp.Point3dAdd(position, tangent);
                    CreateTangentAtPoint = msApp.CreateLineElement1(null, ref points);
                    BIM.Point3d assex = msApp.Point3dFromXYZ((position.X + 100), position.Y, position.Z);
                    BIM.Vector3d vector1 = msApp.Vector3dSubtractPoint3dPoint3d(position, points[0]);
                    BIM.Vector3d vector2 = msApp.Vector3dSubtractPoint3dPoint3d(position, assex);
                    if ((points[1].Y > points[0].Y))
                    {
                        Tradians = (msApp.Vector3dAngleBetweenVectors(vector1, vector2) * -1);
                    }
                    else
                    {
                        Tradians = msApp.Vector3dAngleBetweenVectors(vector1, vector2);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return CreateTangentAtPoint;
        }

        /// <summary>
        /// Get a Point at A Distance of the Linear Element
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="oElement"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private bool PointAtDistance(ref BIM.Point3d origin, BIM.Element oElement, double distance)
        {
            bool valid = true;
            try
            {
                valid = false;
                switch (oElement.Type)
                {
                    case BIM.MsdElementType.ComplexString:
                        origin = (oElement as BIM.ComplexStringElement).PointAtDistance(distance);
                        valid = (distance < (oElement as BIM.ComplexStringElement).Length);
                        break;
                    case BIM.MsdElementType.Line:
                    case BIM.MsdElementType.LineString:
                        origin = (oElement as BIM.LineElement).PointAtDistance(distance);
                        valid = (distance < (oElement as BIM.LineElement).Length);
                        break;
                    case BIM.MsdElementType.Arc:
                        origin = (oElement as BIM.ArcElement).PointAtDistance(distance);
                        valid = (distance < (oElement as BIM.ArcElement).Length);
                        break;
                    case BIM.MsdElementType.BsplineCurve:
                        origin = (oElement as BIM.BsplineCurveElement).PointAtDistance(distance);
                        valid = (distance < (oElement as BIM.BsplineCurveElement).Length);
                        break;
                    //Handle Complex Shape
                    case BIM.MsdElementType.ComplexShape:
                        if (oElement.IsClosedElement())
                        {
                            origin = (oElement as BIM.ComplexShapeElement).PointAtDistance(distance);
                            valid = (distance < (oElement as BIM.ComplexShapeElement).Perimeter());
                        }
                        break;

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return valid;
        }

        /// <summary>
        /// Create Slope Line main Function
        /// </summary>
        /// <returns></returns>
        private Boolean CreateSlopeLine()
        {
            try
            {

                #region --Initiate all Parameters

                //Initialize all variables
                BIM.Application msApp = BMI.Utilities.ComApp;
                double dSlopeLineLength = 0.0; //This stores Length of Slope Line Major or Minor
                int nMinorLines = 0; //Counter for Minor Slope lines
                bool ChkRedraw = true;
                const long TangentColour = 78;
                long n = 0;
                ScanCriteria oCriteria = new ScanCriteria();
                BIM.Level oLevel = null;
                BIM.Matrix3d rotat1 = new BIM.Matrix3d();
                BIM.Transform3d trans1;
                BIM.BsplineCurve bSpl1 = new BIM.BsplineCurveClass();
                double dL1Length = 0.0;
                BIM.Point3d origin = new BIM.Point3d();
                BIM.LineElement oTangent = null;
                BIM.Point3d PntoStart = new BIM.Point3d();
                #endregion

                #region --Handle Target Model Reference to add Slope Lines & Template Element            

                //Handle the model & Slope Line template
                BIM.ModelReference CurrentModelReference = null;
                BIM.Element TemplateElement = null;

                if (chkTargetModelChoice.IsChecked == true) //Write to Active Model
                {
                    CurrentModelReference = msApp.ActiveModelReference;
                    TemplateElement = null;
                }
                else //Write to Selected Feature's Model
                {
                    CurrentModelReference = m_oLine.ModelReference;
                    TemplateElement = m_oLine;
                }

                #endregion

                #region --Handle Level Names


                string levelName = cmbLevelDisplayFilter.Text;

                if (LevelExist(levelName))
                {
                    oLevel = GetLevel(levelName);
                }
                else
                {
                    BIM.DesignFile activeDesignFile = msApp.ActiveDesignFile;
                    oLevel = activeDesignFile.AddNewLevel(levelName);
                    activeDesignFile.RewriteLevels();
                }

                #endregion

                #region --Handle Line Style

                BIM.LineStyle lineStyle = null;
                try
                {
                    //Validation
                    // if (cmbFeatureLineStyle.SelectedIndex < 0) { cmbFeatureLineStyle.SelectedIndex = 0; }

                    //  lineStyle =  lineStyles[cmbFeatureLineStyle.SelectedIndex + 1]; //As Linestyles starts from index 1
                    lineStyle = msApp.ActiveSettings.LineStyle; //TEMP OVERRIDE for TEST
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }

                #endregion

                if (ChkRedraw)
                {
                    oCriteria.SetModelRef(Session.Instance.GetActiveDgnModel());
                    oCriteria.SetModelSections(DgnModelSections.GraphicElements);
                    ScanDelegate scanDelegate = (Element element, DgnModelRef modelRef) =>
                    {
                        //element.DeleteFromModel();
                        return StatusInt.Success;
                    };
                    oCriteria.Scan(scanDelegate);
                }

                //Initial Calculations for all Factors
                dL1Length = oSplineElement.Length;
                Double unitStationDistanceFactor = (dL1Length / ((dL1Length / m_nIntervalBtwMajor + 1) + (m_nMinorPerMajor * (dL1Length / m_nIntervalBtwMajor)) - 1)); //Calculate UNIT Station Factor to Keep Adding Station distances
                int LoopCountAsperMajorMinor = (int)GeneralUtilities.ConvertValueToCurrentUnits((double)((dL1Length / m_nIntervalBtwMajor) + 1) + (m_nMinorPerMajor * (dL1Length / m_nIntervalBtwMajor))); //Counter for how many times loop should run to cover Major Minor settings
                string CurrentlyDrawing = "";
                nMinorLines = m_nMinorPerMajor; //Assign Minor Per major to a temp Variable
                int Loop1Counter = 0;

                //Main Loop
                for (n = 0; n <= (int)LoopCountAsperMajorMinor; n++)
                {

                #region Decide LEFT or RIGHT Side
                Loop1:
                    if (DirectionDecided == false)
                    {
                        if ((Direction == "LEFT"))
                        {
                            Loop1Counter++;
                            Pang1 = (msApp.Pi() / 2);
                            Pang2 = (msApp.Pi() * -1);
                        }
                        else
                        {
                            Loop1Counter++;
                            Pang1 = ((msApp.Pi() / 2) * -1);
                            Pang2 = msApp.Pi();
                        }
                    }
                    #endregion

                    #region Decide if MAJOR or MINOR to draw


                    //Decide if you want to Draw MAJOR or Minor
                    if (n == 0) //Drawing Major Line
                    {
                        CurrentlyDrawing = "MAJOR";
                        Station = 0;
                        nMinorLines = m_nMinorPerMajor;
                    }
                    else if (n > 0 && nMinorLines > 0) //Drawing Minor Line
                    {
                        CurrentlyDrawing = "minor";
                        Station = Station + unitStationDistanceFactor;
                    }
                    else if (n > 0 && nMinorLines == 0) //Drawing Major Line
                    {
                        CurrentlyDrawing = "MAJOR";
                        Station = Station + unitStationDistanceFactor;
                        nMinorLines = m_nMinorPerMajor;
                    }
                    #endregion


                    #region Main Logic to Draw Lines
                    //FIND the POINT on LINE1 at a Given Station
                    if (PointAtDistance(ref origin, eleComponent, Station))
                    {
                        oTangent = CreateTangentAtPoint(oSplineElement, Station);
                        if (!(oTangent == null))
                        {
                            #region Processing on oTangent Line
                            oTangent.ScaleUniform(ref origin, (MarkerSize() / oTangent.Length));
                            oTangent.GraphicGroup = (int)graphicGroup;
                            oTangent.Color = (int)TangentColour;
                            msApp.ActiveModelReference.AddElement(oTangent);
                            long elementId = oTangent.ID;
                            // Definisco il punto della retta su cui inserire la cella
                            //I define the point of the line on which to insert the cell
                            rotat1 = msApp.Matrix3dFromVectorAndRotationAngle(msApp.Point3dFromXYZ(0, 0, 1), Pang1);
                            trans1 = msApp.Transform3dFromMatrix3dAndFixedPoint3d(rotat1, origin);
                            //  rotate 90� with fix startpoint from oL
                            oTangent.Transform(trans1);
                            oTangent.ScaleAll(origin, 100, 100, 100);
                            (oTangent as BIM.LineElement).set_Vertex(1, origin);
                            BIM.Point3d vertex2 = (oTangent as BIM.LineElement).get_Vertex(2);
                            vertex2.Z = origin.Z;
                            (oTangent as BIM.LineElement).set_Vertex(2, vertex2);

                            #endregion

                            //Finding Intersection from oTangent to secondary feature line
                            BIM.Point3d[] Inter = m_oLine2.AsIntersectableElement().GetIntersectionPointsOnIntersector(oTangent, rotat1);

                            //Checking if Intersection Point Array is empty
                            if (Inter.Length > 0 && Inter[0].X != 0)
                            {
                                BIM.LineElement slopeLine; //Define the Slope Line

                                #region DRAW MAJOR LINE

                                if (CurrentlyDrawing == "MAJOR") //For Major Line
                                {

                                    slopeLine = null; //Reset Slope Line first
                                                      //Major line need to be draw in All the possible cases
                                    slopeLine = msApp.CreateLineElement2(TemplateElement, origin, Inter[0]); //populate Major line


                                    if (slopeLine != null)
                                    {
                                        dSlopeLineLength = slopeLine.Length;
                                        //Code for adding Major Slope Lines to Model
                                        slopeLine.Level = oLevel;

                                        slopeLine.GraphicGroup = (int)graphicGroup;
                                        if (dSlopeLineLength >= m_dMinMajorLength) //validation for minimum slopeline length
                                        {
                                            if (lineStyle != null)
                                                slopeLine.LineStyle = lineStyle;



                                            //Add Slope line to 3D Model always or add it to DEFAULT model
                                            CurrentModelReference.AddElement(slopeLine);
                                            slopeLine.Redraw();
                                            elementId = slopeLine.ID;
                                            nMajorCount++;
                                        }

                                        DirectionDecided = true;
                                        msApp.ActiveModelReference.RemoveElement(oTangent);
                                    }
                                }
                                #endregion
                                //else //For Minor Line
                                #region DRAW Minor Line
                                if (CurrentlyDrawing == "minor")
                                {
                                    //Calculate Slope Line same as Major line but trim it as Minor line as per settings
                                    slopeLine = null;
                                    slopeLine = msApp.CreateLineElement2(TemplateElement, origin, Inter[0]); //populate Major line to trim it to Minor line
                                    dSlopeLineLength = slopeLine.Length;
                                    Bentley.GeometryNET.DPoint3d LeftPoint = new Bentley.GeometryNET.DPoint3d(origin.X, origin.Y, origin.Z);
                                    Bentley.GeometryNET.DPoint3d RightPoint = new Bentley.GeometryNET.DPoint3d(Inter[0].X, Inter[0].Y, Inter[0].Z);
                                    Bentley.GeometryNET.DPoint3d MidPoint = new Bentley.GeometryNET.DPoint3d();

                                    //Define a midPoint based on the PERENTAGE or Elevation Difference Factor
                                    if (UseElevationDifferenceFactorForMinorLine && MaximumElevationDifference > 0)
                                    {
                                        //Formula: Current Slope Difference / Maximum Elevation Difference
                                        double ElevDiffFactor = Math.Abs(Math.Round(origin.Z, 5) - Math.Round(Inter[0].Z, 5)) / MaximumElevationDifference;
                                        //Validation for Overshoot condition
                                        if (ElevDiffFactor > 1)
                                        {
                                            //Renew the Maximum Elevation Diff
                                            MaximumElevationDifference = Math.Abs(Math.Round(origin.Z, 5) - Math.Round(Inter[0].Z, 5));
                                            ElevDiffFactor = Math.Abs(Math.Round(origin.Z, 5) - Math.Round(Inter[0].Z, 5)) / MaximumElevationDifference;

                                            //Based on Fill or Cut situation decide the Midpoint to be generated
                                            if (Math.Round(origin.Z, 5) > Math.Round(Inter[0].Z, 5)) //Fill Condition
                                            {
                                                MidPoint = Bentley.GeometryNET.DPoint3d.Interpolate(LeftPoint, ElevDiffFactor, RightPoint);
                                            }
                                            else //Cut Situation
                                            {
                                                MidPoint = Bentley.GeometryNET.DPoint3d.Interpolate(RightPoint, ElevDiffFactor, LeftPoint);
                                            }
                                        }
                                        else //Current Elevation Diff is lesser than Maximum Elevation Diff
                                        {
                                            //Based on Fill or Cut situation decide the Midpoint to be generated
                                            if (Math.Round(origin.Z, 5) > Math.Round(Inter[0].Z, 5)) //Fill Condition
                                            {
                                                MidPoint = Bentley.GeometryNET.DPoint3d.Interpolate(LeftPoint, ElevDiffFactor, RightPoint);
                                            }
                                            else //Cut Situation
                                            {
                                                MidPoint = Bentley.GeometryNET.DPoint3d.Interpolate(RightPoint, ElevDiffFactor, LeftPoint);
                                            }
                                        }
                                    }
                                    else //Use Percentage for Minor Length if IF condition is not true or for 2D lines
                                    {
                                        #region --Handle Fixed Minor Line length Condition for Elevation

                                        if (FrmSideSlopeAnnotation.currentSlopeMethod == SlopeMethod.SlopeMethodElevation)
                                        {
                                            //Based on Fill or Cut situation decide the Midpoint to be generated
                                            if (Math.Round(origin.Z, 5) > Math.Round(Inter[0].Z, 5)) //Fill Condition
                                            {
                                                MidPoint = Bentley.GeometryNET.DPoint3d.Interpolate(LeftPoint, m_dMinorLength / 100, RightPoint);
                                            }
                                            else //Cut Situation
                                            {
                                                MidPoint = Bentley.GeometryNET.DPoint3d.Interpolate(RightPoint, m_dMinorLength / 100, LeftPoint);
                                            }
                                        }
                                        #endregion

                                        #region --Handle Fixed Minor Line condition for FILL 2D
                                        if (FrmSideSlopeAnnotation.currentSlopeMethod == SlopeMethod.SlopeMethodFill2D)
                                        {
                                            MidPoint = Bentley.GeometryNET.DPoint3d.Interpolate(LeftPoint, m_dMinorLength / 100, RightPoint);
                                        }
                                        #endregion

                                        #region --Handle Fixed Minor Line condution for CUT 2D
                                        if (FrmSideSlopeAnnotation.currentSlopeMethod == SlopeMethod.SlopeMethodCut2D)
                                        {
                                            MidPoint = Bentley.GeometryNET.DPoint3d.Interpolate(RightPoint, m_dMinorLength / 100, LeftPoint);
                                        }
                                        #endregion

                                    }

                                    //populate the pntStart
                                    if (null != MidPoint || MidPoint.X != 0)
                                    {
                                        PntoStart.X = MidPoint.X;
                                        PntoStart.Y = MidPoint.Y;
                                        PntoStart.Z = MidPoint.Z;
                                    }
                                    else
                                    {
                                        continue;
                                    }

                                    #region --SlopeMethod = Elevation for Minor Lines

                                    if (FrmSideSlopeAnnotation.currentSlopeMethod == SlopeMethod.SlopeMethodElevation)
                                    {


                                        //THIS LOGIC has to be based on the ELEVATION value of the POINTS which is NOT done Yet
                                        slopeLine = null;

                                        if (Math.Round(origin.Z, 5) > Math.Round(Inter[0].Z, 5) && null != MidPoint) //Fill Condition in Model based on Elevation
                                        {
                                            //Based on Slope Difference parameter Draw or NPT Draw Slope Line
                                            if (Math.Abs((origin.Z - Inter[0].Z)) >= m_dSlopeDifference)
                                            {
                                                slopeLine = msApp.CreateLineElement2(TemplateElement, origin, PntoStart);
                                            }

                                        }
                                        else if (Math.Round(origin.Z, 5) < Math.Round(Inter[0].Z, 5) && null != MidPoint) //Cut Condition in Model based on Elevation
                                        {
                                            //Based on Slope Difference parameter Draw or NPT Draw Slope Line
                                            if (Math.Abs((origin.Z - Inter[0].Z)) >= m_dSlopeDifference)
                                            {
                                                slopeLine = msApp.CreateLineElement2(TemplateElement, Inter[0], PntoStart);
                                            }
                                        }

                                    }

                                    #endregion

                                    #region --SlopeMethod = Fill2D for Minor Lines
                                    if (FrmSideSlopeAnnotation.currentSlopeMethod == SlopeMethod.SlopeMethodFill2D)
                                    {
                                        slopeLine = msApp.CreateLineElement2(TemplateElement, origin, PntoStart);
                                    }
                                    #endregion

                                    #region --SlopeMethod = Cut2D for Minor Lines
                                    if (FrmSideSlopeAnnotation.currentSlopeMethod == SlopeMethod.SlopeMethodCut2D)
                                    {
                                        slopeLine = msApp.CreateLineElement2(TemplateElement, Inter[0], PntoStart);
                                    }
                                    #endregion

                                    #region --Write Minor SlopeLines to Model

                                    //Validation if SLOPE LINE has to be DRAW or NOT
                                    if (slopeLine != null)
                                    {
                                        double dLength = slopeLine.Length;
                                        // Code for adding Minor Slope Lines to Model
                                        slopeLine.Level = oLevel;
                                        slopeLine.GraphicGroup = (int)graphicGroup; //Add Slope Lines to Graphic Group


                                        if (dLength >= m_dMinMajorLength) //Validation
                                        {
                                            if (lineStyle != null)
                                                slopeLine.LineStyle = lineStyle;

                                            //Add Element to Model
                                            CurrentModelReference.AddElement(slopeLine);
                                            slopeLine.Redraw();
                                            elementId = slopeLine.ID;
                                            DirectionDecided = true;
                                            //Increase or Decrease Counter
                                            nMinorLines--;
                                            nMinorCount++;

                                        }// if (dLength >= m_dMinMajorLength)
                                    }//if (slopeLine <> null)

                                    #endregion

                                    //Remove Tangent Line
                                    msApp.ActiveModelReference.RemoveElement(oTangent);


                                } //if (CurrentlyDrawing == "minor") 

                                #endregion

                            } //(Inter.Length > 0 && Inter[0].X != 0)
                            else if (!DirectionDecided) //If no Direction Decided Recalculate the oTangent
                            {
                                msApp.ActiveModelReference.RemoveElement(oTangent);
                                //INcase we tried Left & Right side both at Station 0, we need to increase Station & try again
                                if (Loop1Counter >= 2)
                                {
                                    Station = Station + unitStationDistanceFactor;
                                    n++; //Increase main loop counter too

                                }

                                if ((Direction == "LEFT"))
                                {
                                    Direction = "RIGHT"; //Right Side
                                    goto Loop1;//Only Get called ONCE until we know the direction
                                }
                                else
                                {
                                    Direction = "LEFT"; //Left Side
                                    goto Loop1; //Only Get called ONCE until we know the direction
                                }
                            }
                            else
                            {
                                if (Station == 0 && subElementIndex == 1) //Only reset direciton at the 1st Station until we get direction
                                { DirectionDecided = false; }

                            }

                            msApp.ActiveModelReference.RemoveElement(oTangent);
                        }
                    } //if (PointAtDistance(ref origin, eleComponent, Station))

                    #endregion

                } //for (n = 0; n <= (dL1Length / m_nIntervalBtwMajor + 1) + (m_nMinorPerMajor * (dL1Length / m_nIntervalBtwMajor)); n++)

                msApp.ActiveModelReference.RemoveElement(oTangent); //CleanUp

                //All OK
                return true;

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return false;
            }
        }


        /// <summary>
        /// Calculate Maximum Slope between 2 3DLines
        /// </summary>
        /// <param name="Line1"></param>
        /// <param name="Line2"></param>
        /// <returns></returns>
        private Double GetMaximumElevationBetween3DLines(BIM.Element Line1, BIM.Element Line2)
        {
            try
            {
                #region --Validation

                //Validation
                if (null == Line1 || null == Line2)
                    return default(double);
                #endregion

                #region --Initiate all Parameters

                //Initialize all variables
                BIM.Application msApp = BMI.Utilities.ComApp;
                //Handle the model & Slope Line template
                BIM.ModelReference CurrentModelReference = null;
                BIM.Element TemplateElement = null;
                double dSlopeLineLength = 0.0; //This stores Length of Slope Line Major or Minor
                double MaximumSlope = 0.0;
                const long TangentColour = 78;
                long n = 0;
                ScanCriteria oCriteria = new ScanCriteria();

                BIM.Matrix3d rotat1 = new BIM.Matrix3d();
                BIM.Transform3d trans1;
                BIM.BsplineCurve bSpl1 = new BIM.BsplineCurveClass();
                double dL1Length = 0.0;
                BIM.Point3d origin = new BIM.Point3d();
                BIM.LineElement oTangent = null;

                long lastrow = 0;
                long nPoints;
                nPoints = lastrow;

                BIM.ComplexStringElement Complesso;
                BIM.ElementEnumerator ee;
                subElementIndex = 0;//Reset Subelement counter
                                    //Initiate OsPline Element everytime
                oSpline = new BIM.BsplineCurveClass();

                //Assume Direction of the Secondary feature from Premiry feasture initially
                DirectionDecided = false;
                Direction = "LEFT";

                //Reset Station & Pang1 & Pang2 for all Subelements
                Pang1 = 0.0;
                Pang2 = 0.0;
                Station = 0.0;
                //  ======================================================================== 
                #endregion

                #region --Code to Handle Maximum ELevation Diff for Element with Subelement


                //HANDLE if Line is ComplexString
                if (m_oLine.Type == BIM.MsdElementType.ComplexString || m_oLine.Type == BIM.MsdElementType.ComplexShape || m_oLine.Type == BIM.MsdElementType.MultiLine)
                {

                    Complesso = (BIM.ComplexStringElement)Line1;
                    ee = Complesso.GetSubElements();
                    while (ee.MoveNext())
                    {
                        //Count Number of Subelement for some conditions to handle in Subsequent function
                        subElementIndex++;
                        eleComponent = ee.Current;
                        if (eleComponent.Type == MsdElementType.Line ||
                            eleComponent.Type == MsdElementType.LineString ||
                        eleComponent.Type == MsdElementType.Arc)
                        {

                            oSpline.FromElement(eleComponent);
                            oSplineElement = msApp.CreateBsplineCurveElement1(null, oSpline);


                            //Initial Calculations for all Factors
                            dL1Length = oSplineElement.Length;
                            Double unitStationDistanceFactor = (dL1Length / ((dL1Length / m_nIntervalBtwMajor + 1) + (m_nMinorPerMajor * (dL1Length / m_nIntervalBtwMajor)) - 1)); //Calculate UNIT Station Factor to Keep Adding Station distances
                                                                                                                                                                                   //Double unitStationDistanceFactor = dL1Length / 20;
                            int LoopCountAsperMajorMinor = (int)GeneralUtilities.ConvertValueToCurrentUnits((double)((dL1Length / m_nIntervalBtwMajor) + 1) + (m_nMinorPerMajor * (dL1Length / m_nIntervalBtwMajor))); //Counter for how many times loop should run to cover Major Minor settings
                                                                                                                                                                                                                       // int LoopCountAsperMajorMinor = (int)dL1Length;
                            TemplateElement = Line1;
                            CurrentModelReference = Line1.ModelReference;
                            int Loop1Counter = 0;
                            Station = 0; //Reset Station everytime

                            /////////////////////////////////

                            #region --Code to Calculate Maximum  Elevation Difference for Current Subelement

                            //Main Loop
                            for (n = 0; n <= (int)LoopCountAsperMajorMinor; n++)
                            //for (n = 0; n <= (int)dL1Length; n++)
                            {

                            #region Decide LEFT or RIGHT Side
                            Loop1:
                                if (DirectionDecided == false)
                                {
                                    if ((Direction == "LEFT"))
                                    {
                                        Loop1Counter++;
                                        Pang1 = (msApp.Pi() / 2);
                                        Pang2 = (msApp.Pi() * -1);
                                    }
                                    else
                                    {
                                        Loop1Counter++;
                                        Pang1 = ((msApp.Pi() / 2) * -1);
                                        Pang2 = msApp.Pi();
                                    }
                                }
                                #endregion

                                #region Decide if MAJOR or MINOR to draw

                                Station = Station + unitStationDistanceFactor;
                                // nMinorLines = m_nMinorPerMajor;

                                #endregion


                                #region Main Logic to Draw Lines
                                //FIND the POINT on LINE1 at a Given Station
                                if (PointAtDistance(ref origin, eleComponent, Station))
                                {
                                    oTangent = CreateTangentAtPoint(oSplineElement, Station);
                                    if (!(oTangent == null))
                                    {
                                        #region Processing on oTangent Line
                                        oTangent.ScaleUniform(ref origin, (MarkerSize() / oTangent.Length));
                                        oTangent.GraphicGroup = (int)graphicGroup;
                                        oTangent.Color = (int)TangentColour;
                                        msApp.ActiveModelReference.AddElement(oTangent);
                                        long elementId = oTangent.ID;
                                        // Definisco il punto della retta su cui inserire la cella
                                        //I define the point of the line on which to insert the cell
                                        rotat1 = msApp.Matrix3dFromVectorAndRotationAngle(msApp.Point3dFromXYZ(0, 0, 1), Pang1);
                                        trans1 = msApp.Transform3dFromMatrix3dAndFixedPoint3d(rotat1, origin);
                                        //  rotate 90� with fix startpoint from oL
                                        oTangent.Transform(trans1);
                                        oTangent.ScaleAll(origin, 100, 100, 100);
                                        (oTangent as BIM.LineElement).set_Vertex(1, origin);
                                        BIM.Point3d vertex2 = (oTangent as BIM.LineElement).get_Vertex(2);
                                        vertex2.Z = origin.Z;
                                        (oTangent as BIM.LineElement).set_Vertex(2, vertex2);

                                        #endregion

                                        //Finding Intersection from oTangent to secondary feature line
                                        BIM.Point3d[] Inter = Line2.AsIntersectableElement().GetIntersectionPointsOnIntersector(oTangent, rotat1);

                                        //Checking if Intersection Point Array is empty
                                        if (Inter.Length > 0 && Inter[0].X != 0)
                                        {
                                            BIM.LineElement slopeLine; //Define the Slope Line

                                            //Calculate Slope Line same as Major line but trim it as Minor line as per settings
                                            slopeLine = null;
                                            slopeLine = msApp.CreateLineElement2(TemplateElement, origin, Inter[0]); //populate Major line to trim it to Minor line
                                            dSlopeLineLength = slopeLine.Length;
                                            Bentley.GeometryNET.DPoint3d LeftPoint = new Bentley.GeometryNET.DPoint3d(origin.X, origin.Y, origin.Z);
                                            Bentley.GeometryNET.DPoint3d RightPoint = new Bentley.GeometryNET.DPoint3d(Inter[0].X, Inter[0].Y, Inter[0].Z);

                                            //Calculate MaximumSlope
                                            if (MaximumSlope < (Math.Abs(Math.Round(origin.Z, 5) - Math.Round(Inter[0].Z, 5))))
                                            {
                                                MaximumSlope = Math.Abs(Math.Round(origin.Z, 5) - Math.Round(Inter[0].Z, 5));

                                            }

                                            DirectionDecided = true;
                                            //Remove Tangent
                                            msApp.ActiveModelReference.RemoveElement(oTangent);

                                        } //(Inter.Length > 0 && Inter[0].X != 0)
                                        else if (!DirectionDecided) //If no Direction Decided Recalculate the oTangent
                                        {
                                            msApp.ActiveModelReference.RemoveElement(oTangent);
                                            //INcase we tried Left & Right side both at Station 0, we need to increase Station & try again
                                            if (Loop1Counter >= 2)
                                            {
                                                Station = Station + unitStationDistanceFactor;
                                                n++; //Increase main loop counter too

                                            }

                                            if ((Direction == "LEFT"))
                                            {
                                                Direction = "RIGHT"; //Right Side
                                                goto Loop1;//Only Get called ONCE until we know the direction
                                            }
                                            else
                                            {
                                                Direction = "LEFT"; //Left Side
                                                goto Loop1; //Only Get called ONCE until we know the direction
                                            }
                                        }
                                        else
                                        {
                                            if (Station == 0 && subElementIndex == 1) //Only reset direciton at the 1st Station until we get direction
                                            { DirectionDecided = false; }

                                        }

                                        msApp.ActiveModelReference.RemoveElement(oTangent);
                                    }
                                } //if (PointAtDistance(ref origin, eleComponent, Station))

                                #endregion

                            } //for (n = 0; n <= (dL1Length / m_nIntervalBtwMajor + 1) + (m_nMinorPerMajor * (dL1Length / m_nIntervalBtwMajor)); n++)

                            #endregion
                            ////////////////////////////////////


                        }//if (eleComponent.Type == BIM.MsdElementType.Line || eleComponent.Type == BIM.MsdElementType.LineString || eleComponent.Type == BIM.MsdElementType.Arc)
                    }// while (ee.MoveNext())

                }//if (m_oLine.Type == BIM.MsdElementType.ComplexString)
                #endregion

                #region --Code to Handle Maximum ELevation Diff for Element WITHOUT Subelement

                else //Handle if Line do not have SubElements
                {
                    if (eleComponent.Type == MsdElementType.Line ||
                        eleComponent.Type == MsdElementType.LineString ||
                    eleComponent.Type == MsdElementType.Arc)
                    {

                        oSpline.FromElement(eleComponent);
                        oSplineElement = msApp.CreateBsplineCurveElement1(null, oSpline);

                        //Initial Calculations for all Factors
                        dL1Length = oSplineElement.Length;
                        Double unitStationDistanceFactor = (dL1Length / ((dL1Length / m_nIntervalBtwMajor + 1) + (m_nMinorPerMajor * (dL1Length / m_nIntervalBtwMajor)) - 1)); //Calculate UNIT Station Factor to Keep Adding Station distances
                                                                                                                                                                               //Double unitStationDistanceFactor = dL1Length / 20;
                        int LoopCountAsperMajorMinor = (int)GeneralUtilities.ConvertValueToCurrentUnits((double)((dL1Length / m_nIntervalBtwMajor) + 1) + (m_nMinorPerMajor * (dL1Length / m_nIntervalBtwMajor))); //Counter for how many times loop should run to cover Major Minor settings
                                                                                                                                                                                                                   // int LoopCountAsperMajorMinor = (int)dL1Length;
                        TemplateElement = Line1;
                        CurrentModelReference = Line1.ModelReference;
                        int Loop1Counter = 0;
                        Station = 0; //Reset Station everytime

                        /////////////////////////////////

                        #region --Code to Calculate Maximum  Elevation Difference for Current Subelement

                        //Main Loop
                        for (n = 0; n <= (int)LoopCountAsperMajorMinor; n++)
                        //for (n = 0; n <= (int)dL1Length; n++)
                        {

                        #region Decide LEFT or RIGHT Side
                        Loop1:
                            if (DirectionDecided == false)
                            {
                                if ((Direction == "LEFT"))
                                {
                                    Loop1Counter++;
                                    Pang1 = (msApp.Pi() / 2);
                                    Pang2 = (msApp.Pi() * -1);
                                }
                                else
                                {
                                    Loop1Counter++;
                                    Pang1 = ((msApp.Pi() / 2) * -1);
                                    Pang2 = msApp.Pi();
                                }
                            }
                            #endregion

                            //Increment Stations
                            Station = Station + unitStationDistanceFactor;

                            #region Main Logic to Calculate Elevatio Diff
                            //FIND the POINT on LINE1 at a Given Station
                            if (PointAtDistance(ref origin, eleComponent, Station))
                            {
                                oTangent = CreateTangentAtPoint(oSplineElement, Station);
                                if (!(oTangent == null))
                                {
                                    #region Processing on oTangent Line
                                    oTangent.ScaleUniform(ref origin, (MarkerSize() / oTangent.Length));
                                    oTangent.GraphicGroup = (int)graphicGroup;
                                    oTangent.Color = (int)TangentColour;
                                    msApp.ActiveModelReference.AddElement(oTangent);
                                    long elementId = oTangent.ID;
                                    // Definisco il punto della retta su cui inserire la cella
                                    //I define the point of the line on which to insert the cell
                                    rotat1 = msApp.Matrix3dFromVectorAndRotationAngle(msApp.Point3dFromXYZ(0, 0, 1), Pang1);
                                    trans1 = msApp.Transform3dFromMatrix3dAndFixedPoint3d(rotat1, origin);
                                    //  rotate 90� with fix startpoint from oL
                                    oTangent.Transform(trans1);
                                    oTangent.ScaleAll(origin, 100, 100, 100);
                                    (oTangent as BIM.LineElement).set_Vertex(1, origin);
                                    BIM.Point3d vertex2 = (oTangent as BIM.LineElement).get_Vertex(2);
                                    vertex2.Z = origin.Z;
                                    (oTangent as BIM.LineElement).set_Vertex(2, vertex2);

                                    #endregion

                                    //Finding Intersection from oTangent to secondary feature line
                                    BIM.Point3d[] Inter = Line2.AsIntersectableElement().GetIntersectionPointsOnIntersector(oTangent, rotat1);

                                    //Checking if Intersection Point Array is empty
                                    if (Inter.Length > 0 && Inter[0].X != 0)
                                    {
                                        BIM.LineElement slopeLine; //Define the Slope Line

                                        //Calculate Slope Line same as Major line but trim it as Minor line as per settings
                                        slopeLine = null;
                                        slopeLine = msApp.CreateLineElement2(TemplateElement, origin, Inter[0]); //populate Major line to trim it to Minor line
                                        dSlopeLineLength = slopeLine.Length;
                                        Bentley.GeometryNET.DPoint3d LeftPoint = new Bentley.GeometryNET.DPoint3d(origin.X, origin.Y, origin.Z);
                                        Bentley.GeometryNET.DPoint3d RightPoint = new Bentley.GeometryNET.DPoint3d(Inter[0].X, Inter[0].Y, Inter[0].Z);

                                        //Calculate MaximumSlope
                                        if (MaximumSlope < (Math.Abs(Math.Round(origin.Z, 5) - Math.Round(Inter[0].Z, 5))))
                                        {
                                            MaximumSlope = Math.Abs(Math.Round(origin.Z, 5) - Math.Round(Inter[0].Z, 5));
                                        }

                                        DirectionDecided = true;
                                        //Remove Tangent
                                        msApp.ActiveModelReference.RemoveElement(oTangent);

                                    } //(Inter.Length > 0 && Inter[0].X != 0)
                                    else if (!DirectionDecided) //If no Direction Decided Recalculate the oTangent
                                    {
                                        msApp.ActiveModelReference.RemoveElement(oTangent);
                                        //INcase we tried Left & Right side both at Station 0, we need to increase Station & try again
                                        if (Loop1Counter >= 2)
                                        {
                                            Station = Station + unitStationDistanceFactor;
                                            n++; //Increase main loop counter too

                                        }

                                        if ((Direction == "LEFT"))
                                        {
                                            Direction = "RIGHT"; //Right Side
                                            goto Loop1;//Only Get called ONCE until we know the direction
                                        }
                                        else
                                        {
                                            Direction = "LEFT"; //Left Side
                                            goto Loop1; //Only Get called ONCE until we know the direction
                                        }
                                    }
                                    else
                                    {
                                        if (Station == 0 && subElementIndex == 1) //Only reset direciton at the 1st Station until we get direction
                                        { DirectionDecided = false; }

                                    }

                                    msApp.ActiveModelReference.RemoveElement(oTangent);
                                }
                            } //if (PointAtDistance(ref origin, eleComponent, Station))

                            #endregion

                        } //for (n = 0; n <= (dL1Length / m_nIntervalBtwMajor + 1) + (m_nMinorPerMajor * (dL1Length / m_nIntervalBtwMajor)); n++)

                        #endregion
                        ////////////////////////////////////

                    }//Check Element types

                }

                #endregion

                oSplineElement = null;
                oSpline = null;

                //All OK
                return MaximumSlope;

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return default(double);
            }
        }


        /// <summary>
        /// Check if LevelName Exists in Current DGN
        /// </summary>
        /// <param name="lName"></param>
        /// <returns></returns>
        private bool LevelExist(string lName)
        {
            BIM.Application msApp = BMI.Utilities.ComApp;
            foreach (BIM.Level oLv in msApp.ActiveDesignFile.Levels)
            {
                if ((oLv.Name.ToUpper() == lName.Trim().ToUpper()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get Level Object by Name from Current DGN File
        /// </summary>
        /// <param name="lName"></param>
        /// <returns></returns>
        private BIM.Level GetLevel(string lName)
        {
            BIM.Level level = null;
            BIM.Application msApp = BMI.Utilities.ComApp;
            foreach (BIM.Level oLv in msApp.ActiveDesignFile.Levels)
            {
                if ((oLv.Name.ToUpper() == lName.Trim().ToUpper()))
                {
                    level = oLv;
                    break;
                }
            }
            return level;
        }

        private double MarkerSize()
        {
            string txtMarkerSize = "1.0";
            string size = txtMarkerSize;
            if (string.IsNullOrEmpty(size))
                size = "1.0";
            return Convert.ToDouble(size);
        }


        private void RbElevation_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                currentSlopeMethod = SlopeMethod.SlopeMethodElevation;

                // Enable/Disable relevant parameters
                textSlopeDifference.IsEnabled = true;
                lblSlopeDifference.IsEnabled = true;

                //Enable / Disable other controls for UseElevationDifferenceFactorForMinorLine
                chkUseElevationDifferenceFactorForMinorLine.IsEnabled = true;
                chkUseElevationDifferenceFactorForMinorLine.IsChecked = false;
                textBoxMinorLength.IsEnabled = true;
                lblMinorLength.IsEnabled = true;
                UseElevationDifferenceFactorForMinorLine = false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

        }

        private void RbFill_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                currentSlopeMethod = SlopeMethod.SlopeMethodFill2D;
                // Enable/Disable relevant parameters
                textSlopeDifference.IsEnabled = false;
                lblSlopeDifference.IsEnabled = false;

                //Enable / Disable other controls for UseElevationDifferenceFactorForMinorLine
                chkUseElevationDifferenceFactorForMinorLine.IsEnabled = false;
                chkUseElevationDifferenceFactorForMinorLine.IsChecked = false;
                textBoxMinorLength.IsEnabled = true;
                lblMinorLength.IsEnabled = true;
                UseElevationDifferenceFactorForMinorLine = false;

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void RbCut_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                currentSlopeMethod = SlopeMethod.SlopeMethodCut2D;
                // Enable/Disable relevant parameters
                textSlopeDifference.IsEnabled = false;
                lblSlopeDifference.IsEnabled = false;

                //Enable / Disable other controls for UseElevationDifferenceFactorForMinorLine
                chkUseElevationDifferenceFactorForMinorLine.IsEnabled = false;
                chkUseElevationDifferenceFactorForMinorLine.IsChecked = false;
                textBoxMinorLength.IsEnabled = true;
                lblMinorLength.IsEnabled = true;
                UseElevationDifferenceFactorForMinorLine = false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void TextBoxInterval_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                TextBox currentTextBox = sender as TextBox;
                AcceptIntegers(currentTextBox, e, false, 0, 1000); //Maximum 100 Meters allowed
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

        }

        private void TextBoxMinorPerMejor_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                TextBox currentTextBox = sender as TextBox;
                AcceptIntegers(currentTextBox, e, false, 0, 999); //Maximum 999 minors allwed between majors
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void TextBoxMinorLength_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                TextBox currentTextBox = sender as TextBox;
                AcceptDecimalNumbers(currentTextBox, e, false, 0, 100); //Maximum Percentage of Minor Length as cmopare to Major Line
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void TextBoxMinMajorLength_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                TextBox currentTextBox = sender as TextBox;
                AcceptDecimalNumbers(currentTextBox, e, false, 0, 999); //Maximum 999 meters as Minimum Length of Minor or Major Lines
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void TextSlopeDifference_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                TextBox currentTextBox = sender as TextBox;
                AcceptDecimalNumbers(currentTextBox, e, false, 0, 999); // Maximum 999 meters as Minimum Elevation Difference 
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void AcceptDecimalNumbers(TextBox txtBox, TextCompositionEventArgs e, bool allowSignedValues = true, double minValue = -100, double maxValue = 100, int maxDecimalPrecision = 3)
        {

            if (allowSignedValues)
            {
                if (e.Text.Equals("-") && txtBox.Text.Length.Equals(0))
                    return;
            }

            if (e.Text.Equals(".") && txtBox.Text.Contains("."))
            {
                e.Handled = true;
                return;
            }

            string decimalSeperator = string.Empty;
            if (e.Text.Equals(".") || e.Text.Equals(","))
            {
                decimalSeperator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.ToString();
                txtBox.Text = txtBox.Text + decimalSeperator;
                txtBox.CaretIndex = txtBox.Text.Length;
            }

            if (!int.TryParse(e.Text, out int i))
                e.Handled = true;

            if (!double.TryParse((txtBox.Text + e.Text), out double d))
                e.Handled = true;

            if (allowSignedValues && !(d >= minValue && d <= maxValue))
                e.Handled = true;

            if (!allowSignedValues && !(d >= 0 && d <= maxValue))
                e.Handled = true;

            if (txtBox.Text.Contains("."))
            {
                decimalSeperator = ".";
            }
            else if (txtBox.Text.Contains(","))
            {
                decimalSeperator = ",";
            }

            if (!string.IsNullOrWhiteSpace(decimalSeperator))
            {
                int charIndex = txtBox.Text.IndexOf(decimalSeperator);
                string subString = txtBox.Text.Substring(charIndex);
                int lengthSubString = subString.Length;

                if (lengthSubString > maxDecimalPrecision)
                {
                    e.Handled = true;
                }
            }

        }

        private void AcceptIntegers(TextBox txtBox, TextCompositionEventArgs e, bool allowSignedValues = true, double minValue = -100, double maxValue = 100)
        {
            if (allowSignedValues)
            {
                if (e.Text.Equals("-") && txtBox.Text.Length.Equals(0))
                    return;
            }

            if (!int.TryParse(e.Text, out int i))
            {
                e.Handled = true;
                return;
            }

            int testInt = Convert.ToInt32(txtBox.Text + e.Text);

            if (allowSignedValues && !(testInt >= minValue && testInt <= maxValue))
                e.Handled = true;

            if (!allowSignedValues && !(testInt >= 0 && testInt <= maxValue))
                e.Handled = true;
        }


        private void CmbFeatureLineStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ChkUseElevationDifferenceFactorForMinorLine_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chkUseElevationDifferenceFactorForMinorLine.IsChecked == true)
                {
                    textBoxMinorLength.IsEnabled = false;
                    lblMinorLength.IsEnabled = false;
                    UseElevationDifferenceFactorForMinorLine = true;
                }
                else
                {
                    textBoxMinorLength.IsEnabled = true;
                    lblMinorLength.IsEnabled = true;
                    UseElevationDifferenceFactorForMinorLine = false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void ChkUseElevationDifferenceFactorForMinorLine_Checked(object sender, RoutedEventArgs e)
        {

        }

    } //Class
   
} //Namespace



