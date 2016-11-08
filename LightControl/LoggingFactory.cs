namespace LightControl
{
    public class LoggingFactory
    {
        private static ILogger _mLogger;

        /// <summary>
        /// @todo this logging doesn't feel right. We should lean on the 
        /// application's config and logger.
        /// </summary>
        public static void InitializeLogFactory()
        {
            _mLogger = new Log4NetAdapter();
        }

        public static ILogger GetLogger() { return _mLogger; }
    }
}
