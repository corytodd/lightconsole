namespace LightControl
{
    partial class TCPConnected
    {
        static readonly string RequestUrlEncodeStr = "cmd={0}&data={1}&fmt=xml";
        static readonly string GetStateTemplate = "<gwrcmds><gwrcmd><gcmd>RoomGetCarousel</gcmd><gdata><gip><version>1</version><token>{0}" +
                                            "</token><fields>name,control,power,product,class,realtype,status</fields></gip></gdata></gwrcmd></gwrcmds>";

        static readonly string RoomSendTemplate = "<gip><version>1</version><token>{0}</token><rid>{1}</rid><value>{2}</value></gip>";
        static readonly string RoomSendLevelTemplate = "<gip><version>1</version><token>{0}</token><rid>{1}</rid><value>{2}</value><type>level</type></gip>";

        static readonly string DeviceSendTemplate = "<gip><version>1</version><token>{0}</token><did>{1}</did><value>{2}</value></gip>";
        static readonly string DeviceSendLevelTempalte = "<gip><version>1</version><token>{0}</token><did>{1}</did><value>{2}</value><type>level</type></gip>";

        static readonly string LogInTemplate = "<gip><version>1</version><email>{0}</email><password>{1}</password></gip>";

        /// <summary>
        /// Returned by gateway when we have an invalid token
        /// </summary>
        static readonly string PermissedDeniedStr = "<gip><version>1</version><rc>401</rc></gip>";

        /// <summary>
        /// Returned by gateway when we attempt to sync while not in sync mode
        /// </summary>
        static readonly string NotInSyncModeStr = "<gip><version>1</version><rc>404</rc></gip>";

        /// <summary>
        /// @todo move to user app data or embedded settings
        /// </summary>
        static readonly string ConfigPath = "config.json";
    }
}
