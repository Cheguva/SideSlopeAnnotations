/*--------------------------------------------------------------------------------------+
|
|  $Copyright: (c) 2022 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/

#region Used References

using System;
using System.Diagnostics;
using Bentley.DgnPlatformNET;
using Bentley.MstnPlatformNET;
using NotificationManager = Bentley.DgnPlatformNET.NotificationManager;

#endregion

namespace SideSlopeAnnotations.Utilities
{
    /// <summary>
    /// General Utility functions used in SideSlopeAnnotations add-in functionality
    /// </summary>
    internal class GeneralUtilities
    {
        /// <summary>
        /// Inch to Meter conversion factor
        /// </summary>
        public static readonly double m_dInchInMeter = 0.0254;
        /// <summary>
        /// Feet to Meter conversion factor
        /// </summary>
        public static readonly double m_dFootInMeter = 0.3048;
        /// <summary>
        /// Yard to Meter conversion factor
        /// </summary>
        public static readonly double m_dYardInMeter = 0.9144;
        /// <summary>
        /// mile to Meter conversion factor
        /// </summary>
        public static readonly double m_dMileInMeter = 1609.344;

        //#region KeyIn

        /// <summary>
        /// Execute KeyIn Command passed to this function
        /// </summary>
        public static void RunKeyInCommand(string keyIn)
        {
            try
            {
                Session.Instance.Keyin(keyIn);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        //#endregion 

        //#region Error And Message Reporting

        /// <summary>
        /// Displays notifications
        /// </summary>
        /// <param name="message"></param>
        public static void NotifyMessage(string message, OutputMessagePriority msgType = OutputMessagePriority.Information)
        {
            try
            {
                NotifyMessageDetails messageDetails;
                messageDetails = new NotifyMessageDetails(msgType, message, message, NotifyTextAttributes.AutoHideDecoration, OutputMessageAlert.Balloon);
                NotificationManager.OutputMessage(messageDetails);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        //#endregion

        //#region Units

        /// <summary>
        /// Gets lable of Master units
        /// </summary>
        /// <returns>Lable of Master units</returns>
        public static string GetMasterUnitLabel()
        {
            string label = "";
            try
            {
                ModelInfo modelInfo = Session.Instance.GetActiveDgnModel().GetModelInfo();
                UnitDefinition masterUnits = modelInfo.GetMasterUnit();
                label = masterUnits.Label;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return label;
        }

        
        /// <summary>
        /// Converts given value to current data units
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ConvertValueToCurrentUnits(double value)
        {
            double convertedValue = value;
            try
            {
                ModelInfo modelInfo = Session.Instance.GetActiveDgnModel().GetModelInfo();
                StandardUnit standardUnit = modelInfo.GetMasterUnit().IsStandardUnit;
                switch (standardUnit)
                {
                    case StandardUnit.EnglishMiles:
                        convertedValue = value * m_dMileInMeter;
                        break;
                    case StandardUnit.EnglishYards:
                        convertedValue = value * m_dYardInMeter;
                        break;
                    case StandardUnit.EnglishFeet:
                        convertedValue = value * m_dFootInMeter;
                        break;
                    case StandardUnit.EnglishInches:
                        convertedValue = value * m_dInchInMeter;
                        break;

                    case StandardUnit.EnglishSurveyMiles:
                        convertedValue = value * 1609.347219;
                        break;
                    case StandardUnit.EnglishSurveyFeet:
                        convertedValue = value / 3.2808333333465;
                        break;
                    case StandardUnit.EnglishSurveyInches:
                        convertedValue = value / 39.37;
                        break;

                    case StandardUnit.MetricKilometers:
                        convertedValue = value * 1000;
                        break;
                    case StandardUnit.MetricMeters:
                        convertedValue = value * 1;
                        break;
                    case StandardUnit.MetricCentimeters:
                        convertedValue = value / 100;
                        break;
                    case StandardUnit.MetricMillimeters:
                        convertedValue = value / 1000;
                        break;
                    case StandardUnit.MetricMicrometers:
                        convertedValue = value / 1000000;
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return convertedValue;
        }
    }
}
