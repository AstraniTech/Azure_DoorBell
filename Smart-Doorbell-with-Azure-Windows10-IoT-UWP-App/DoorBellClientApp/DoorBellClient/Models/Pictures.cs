using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;

namespace DoorBellClient.Models
{
    public class Pictures
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "DoorBellID")]
        public string DoorBellID { get; set; }

        [UpdatedAt]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}