using NLog;
using NLog.LayoutRenderers;
using System;
using System.Text;

namespace PSNotes.Logging
{
    [LayoutRenderer("PSNotesJson")]
    public class JsonLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException("logEvent");
            }

            builder.Append(LogEventModelBuilder.Build(logEvent));
        }
    }
}
