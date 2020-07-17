using PSNotes.Models.Logging;
using NLog;
using System;
using System.Linq;

namespace PSNotes.Logging
{
    public static class LogEventModelBuilder
    {
        public static LogEventModel Build(LogEventInfo logEvent)
        {
            LogEventModel model = new LogEventModel()
            {
                Logger = logEvent.LoggerName,
                Level = logEvent.Level.ToString(),
                Message = logEvent.FormattedMessage,
                MachineName = Environment.MachineName
            };

            if (logEvent.Exception != null)
            {
                model.Exception = ProcessException(logEvent.Exception);
            }

            return model;
        }

        private static LogExceptionModel ProcessException(Exception exception)
        {
            if (exception == null)
                return null;

            LogExceptionModel model = new LogExceptionModel()
            {
                Source = exception.Source,
                Message = exception.Message,
                StackTrace = exception.StackTrace?.Split('\n').Select(s => s.Trim()).ToArray()
            };

            if (exception.InnerException != null)
            {
                model.InnerException = ProcessException(exception.InnerException);
            }

            return model;
        }
    }
}
