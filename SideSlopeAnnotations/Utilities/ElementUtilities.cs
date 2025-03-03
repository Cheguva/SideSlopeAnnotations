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
using Bentley.CifNET.SDK;
using Bentley.CifNET.GeometryModel.SDK;
using Element = Bentley.DgnPlatformNET.Elements.Element;
#endregion

namespace SideSlopeAnnotations.Utilities
{
    /// <summary>
    /// Element Utility functions used in SideSlopeAnnotations add-in functionality
    /// </summary>
    internal class ElementUtilities
    {

        /// <summary>
        /// Gets element form ElementId
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static Element GetElementFromId(ElementId Id)
        {
            try
            {
                DgnModelRef dgnModelRef = Session.Instance.GetActiveDgnModelRef();
                DgnFile dgnFile = dgnModelRef.GetDgnFile();
                ModelIndexCollection modelIndices = dgnFile.GetModelIndexCollection();
                foreach (ModelIndex modelIndex in modelIndices)
                {
                    DgnModel dgnModel = dgnFile.LoadRootModelById(out StatusInt status, modelIndex.Id);
                    ModelElementsCollection elements = dgnModel.GetGraphicElements();
                    foreach (Element element in elements)
                    {
                        if (!element.IsDeleted && !element.IsInvisible)
                        {
                            if (element.ElementId == Id)
                            {
                                return element;
                            }
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static string GetFeatureName(Element element)
        {
            string featureName = "";
            try
            {
                DgnModel activeModel = Session.Instance.GetActiveDgnModel();

                using (ConsensusConnection consensusConnection = new ConsensusConnection(activeModel))
                {
                    FeaturizedModelEntity featurizedModelEntity = FeaturizedModelEntity.CreateFromElement(consensusConnection, element);
                    var alignment = featurizedModelEntity as Alignment;
                    if (null != alignment)
                        featurizedModelEntity = alignment.ActiveLinearEntity3d;
                    if (featurizedModelEntity is null)
                        return "";
                    featureName = featurizedModelEntity.Name;

                    //Validation
                    if (featureName.Trim() == "")
                    {
                        //Take name from Corridor
                        featureName = (featurizedModelEntity as LinearEntity3d).GeometryPresentation.Name;
                        featureName = featureName.Substring(featureName.LastIndexOf("\\") + 1);
                    }

                    // featureName = featureName.Substring(featureName.LastIndexOf("\\") + 1);
                    //Return if Populated
                    if (featureName.Trim() != "") { return featureName; }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return featureName;
        }

    }
}
