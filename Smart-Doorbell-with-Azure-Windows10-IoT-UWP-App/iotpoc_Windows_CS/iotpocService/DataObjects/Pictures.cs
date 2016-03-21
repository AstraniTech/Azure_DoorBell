using Microsoft.WindowsAzure.Mobile.Service;

namespace iotpocService.DataObjects
{
    public class Pictures : EntityData
    {
        public string Url { get; set; }
        public string TimeStamp { get; set; }
        public string DoorBellID { get; set; }
    }
}