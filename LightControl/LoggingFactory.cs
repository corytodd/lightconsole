namespace LightControl
{
    public class LoggingFactory
    {
        private static ILogger m_logger;

        /// <summary>
        /// @todo this logging doesn't feel right. We should lean on the 
        /// application's config and logger.
        /// </summary>
        public static void InitializeLogFactory()
        {
            m_logger = new Log4netAdapter();
        }

        public static ILogger GetLogger() { return m_logger; }
    }
}
