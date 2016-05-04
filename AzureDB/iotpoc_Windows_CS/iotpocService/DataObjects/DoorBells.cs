using Microsoft.WindowsAzure.Mobile.Service;


namespace iotpocService.DataObjects
{
    public class DoorBells : EntityData
    {
        public string DoorBellID { get; set; }
        public string PicturesId { get; set; }
    }
}