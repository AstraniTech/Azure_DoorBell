using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.WindowsAzure.Mobile.Service;
using Newtonsoft.Json;
using Microsoft.WindowsAzure;
using Microsoft.ServiceBus.Messaging;

namespace iotpocService.Controllers
{
    public class MessageQueueController : ApiController
    {
        public ApiServices Services { get; set; }

        
        // GET api/MessageQueue
        //public string Get(string jsonObj)
        //{
        //    Services.Log.Info("Hello from custom controller!");
        //    return jsonObj;
        //}

        public string Post(string jsonObj)
        {
            
            Services.Log.Info("doorbellInfo Json String: " + jsonObj);
            var doorbellNotification = JsonConvert.DeserializeObject<DoorBellNotification>(jsonObj);
            string SuccessCode = "Failed";
            Services.Log.Info("Sending notification to service bus queue");
            try
            {
                if(doorbellNotification!=null)
                {
                    string connectionString = CloudConfigurationManager.GetSetting("MS_ServiceBusConnectionString");

                    QueueClient Client =
                        QueueClient.CreateFromConnectionString(connectionString, "smartdoordemo");


                    string body = JsonConvert.SerializeObject(new DoorBellNotification()
                    {
                        doorBellID = doorbellNotification.doorBellID,
                        imageUrl = doorbellNotification.imageUrl
                    });

                    var brokeredMsg = new BrokeredMessage(body);
                    brokeredMsg.Properties["doorBellID"] = doorbellNotification.doorBellID;
                    brokeredMsg.Properties["imageUrl"] = doorbellNotification.imageUrl;

                    // Send message to the queue.
                    Client.Send(brokeredMsg);
                    SuccessCode = "Success";
                    Services.Log.Info("Message Sent Successfully!");
                }
               
            }
            catch (Exception ex)
            {
                Services.Log.Info("Message Failed to Send: " + ex.Message);
                SuccessCode = "Failed";
            }

            return SuccessCode;
            
        }


        public class DoorBellNotification
        {
            public string doorBellID { get; set; }
            public string imageUrl { get; set; }
        }
    }
}
