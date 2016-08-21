using log4net;
using log4net.Config;

namespace LightControl
{
    public class Log4netAdapter : ILogger
    {
        private readonly ILog m_log;

        public Log4netAdapter()
        {
            XmlConfigurator.Configure();
            m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Log(string message, params object[] args)
        {
            m_log.Info(string.Format(message, args));
        }
    }
}
