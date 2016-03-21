using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DoorBellClient.Models
{
    public class DoorBells 
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "DoorBellID")]
        public string DoorBellID { get; set; }

        [JsonProperty(PropertyName = "PicturesId")]
        public string PicturesId { get; set; }

        [UpdatedAt]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}