using log4net;
using log4net.Config;

namespace LightControl
{
    public class Log4NetAdapter : ILogger
    {
        private readonly ILog _mLog;

        public Log4NetAdapter()
        {
            XmlConfigurator.Configure();
            _mLog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Log(string message, params object[] args)
        {
            _mLog.Info(string.Format(message, args));
        }
    }
}
