/*--------------------------------------------------------------------------------------+
|
|  $Copyright: (c) 2022 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/

using Bentley.DgnPlatformNET.Elements;
using Bentley.MstnPlatformNET;
using System;
using System.Diagnostics;

namespace SideSlopeAnnotations.Classes
{
    /// <summary>
    /// ElementHighlighter exists to manage an element agenda.
    /// </summary>
    class ElementHighlighter : IDisposable
    {
        /// <summary>
        /// An element agenda that stores the element whose ID is selected once we've scanned
        /// the active DGN model for tagged elements.
        /// We use this agenda to hilight elements selected in the ListView control.
        /// </summary>
        static Bentley.DgnPlatformNET.ElementAgendaDisplayable m_ElementAgendaDisplayable = new Bentley.DgnPlatformNET.ElementAgendaDisplayable();


        /// <summary>
        /// Highlight Criteria
        /// </summary>
        public enum HighlightCriteria
        {
            SingleHighlight,
            MultipleHighlight,
        };

        /// <summary>
        /// Current Highlight Criteria
        /// </summary>
        static public HighlightCriteria m_eHighlightCriteria = HighlightCriteria.SingleHighlight;

        /// <summary>
        /// Constructor
        /// </summary>
        public ElementHighlighter()
        {
        }

        /// <summary>
        /// Get or set the highlight criteria.
        /// You can select a single element or multiple elements for highlighting. 
        /// </summary>
        public static HighlightCriteria Criteria
        {
            get
            {
                return m_eHighlightCriteria;
            }
            set
            {
                m_eHighlightCriteria = value;
            }
        }

        /// <summary>
        /// Highlight element
        /// </summary>
        /// <param name="bHighlight"></param>
        static public void Highlight(bool bHighlight)
        {
            try
            {
                if (bHighlight)
                    m_ElementAgendaDisplayable.Hilite();
                else
                    m_ElementAgendaDisplayable.ClearHilite();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// Gets Count of Elements in ElementAgendaDisplayable
        /// </summary>
        static public int GetCount()
        {
            try
            {
                if (null == m_ElementAgendaDisplayable)
                    return 0;

                return (int)m_ElementAgendaDisplayable.GetCount();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return 0;
            }
        }


        /// <summary>
        /// Clears ElementAgendaDisplayable
        /// </summary>
        static public void Empty()
        {
            try
            {
                if (null == m_ElementAgendaDisplayable)
                    return;

                m_ElementAgendaDisplayable.ClearHilite();
                m_ElementAgendaDisplayable.Empty(true);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Ensure that we clear the currently-highlighted element when we finish with this object
        /// </summary>
        public void Dispose()
        {
            try
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary> 
        /// Ensure that we clear the currently-highlighted element when we finish with this object
        /// </summary>
        protected virtual void Dispose(bool bDisposing)
        {
            try
            {
                if (bDisposing)
                {
                    if (m_ElementAgendaDisplayable != null)
                    {
                        Empty();
                        m_ElementAgendaDisplayable.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Add an element to our agenda.
        /// </summary>
        static public void AddElement(Element element)
        {
            try
            {

                if (HighlightCriteria.SingleHighlight == m_eHighlightCriteria)
                {
                    m_ElementAgendaDisplayable.Empty(true);
                }
                var targetModel = element.DgnModelRef;
                foreach (var attachment in Session.Instance.GetActiveDgnModel().GetDgnAttachments())
                {
                    if (attachment.GetDgnModel() == targetModel.GetDgnModel())
                    {
                        targetModel = attachment;
                        break;
                    }
                }
                Element elm = m_ElementAgendaDisplayable.Find(element, targetModel, 0, m_ElementAgendaDisplayable.GetCount() - 1);
                //If Element is not added to Element Agenda then Add now
                if (elm == null)
                {
                    m_ElementAgendaDisplayable.Insert(element, targetModel, true);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Remove an element from our agenda so that it is no longer highlighted.
        /// </summary> 
        static public void RemoveElement(Bentley.DgnPlatformNET.Elements.Element element)
        {
            try
            {
                //vaidation
                if (null == element)
                    return;

                element.Invalidate();
                m_ElementAgendaDisplayable.DropInvalidEntries();

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
    }
}
