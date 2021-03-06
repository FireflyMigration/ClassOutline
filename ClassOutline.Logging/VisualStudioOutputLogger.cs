﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

using log4net.Appender;
using log4net.Core;
using Newtonsoft.Json;

namespace ClassOutline.Logging
{
    
    public class VisualStudioOutputLogger : AppenderSkeleton
    {
      
        private const string OUTPUTPANENAME = "FireflyCommunity";
        public static EnvDTE.DTE DTE { get; set; }

        public VisualStudioOutputLogger()
        {
            
        }
        protected override void Append(LoggingEvent loggingEvent)
        {
            Debug.WriteLine("ClassOutline.Logging:" + loggingEvent.RenderedMessage);
  
            dynamic tmp = null;

            if (loggingEvent.ExceptionObject != null)
            {
                tmp = new
                {
                    Message = loggingEvent.RenderedMessage,

                };
            }
            else
            {
                tmp = new
                {
                    Message = loggingEvent.RenderedMessage,

                    Exception = loggingEvent.ExceptionObject
                };
            }

            var errorMessage = JsonConvert.SerializeObject(tmp);
            ReportError(errorMessage);
        }

        private void ReportError(string errorInfo)
        {
            var dte = DTE;
            if (dte == null) return;

            var w = dte.Windows.Item(Constants.vsWindowKindOutput);
            var outp = w.Object as OutputWindow;
            OutputWindowPane pane = null;

            try
            {
                pane = outp.OutputWindowPanes.Item(OUTPUTPANENAME);
            }
            catch
            {
                pane = outp.OutputWindowPanes.Add(OUTPUTPANENAME);
            }


            pane.Activate();
            pane.OutputString(errorInfo);
            pane.OutputString("\n");
        }
    }
}
