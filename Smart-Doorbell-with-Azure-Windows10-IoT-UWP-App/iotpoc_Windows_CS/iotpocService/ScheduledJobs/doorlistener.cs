using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.WindowsAzure.Mobile.Service;
using Microsoft.ServiceBus;
using Microsoft.WindowsAzure;
using System;
using Microsoft.ServiceBus.Messaging;
using iotpocService.Models;
using System.Collections.Generic;
using iotpocService.DataObjects;
using System.Linq;
using System.Data.Entity;
using Microsoft.WindowsAzure.Mobile.Service.ScheduledJobs;
using System.Threading;

namespace iotpocService
{
    // A simple scheduled job which can be invoked manually by submitting an HTTP
    // POST request to the path "/jobs/sample".

    public class doorlistener : ScheduledJob
    {
        
        string connectionString =
    CloudConfigurationManager.GetSetting("MS_ServiceBusConnectionString");
        iotpocContext DBcontext;

        protected override void Initialize(ScheduledJobDescriptor scheduledJobDescriptor,
            CancellationToken cancellationToken)
        {
            base.Initialize(scheduledJobDescriptor, cancellationToken);

            // Create a new context with the supplied schema name.
            DBcontext = new iotpocContext();
        }
        public override Task ExecuteAsync()
        {
           
            // Create the queue if it does not exist already.           

            var namespaceManager =
            NamespaceManager.CreateFromConnectionString(connectionString);

            if (!namespaceManager.QueueExists("smartdoordemo"))
            {
                namespaceManager.CreateQueue("smartdoordemo");
                Services.Log.Info("smartdoordemo Queue created successfully!");
            }
            else
            {
                Services.Log.Info("smartdoordemo Queue Already Exist!");

            }

            try
            {
                listenForMessages();
            }
            catch (Exception ex)
            {
                Services.Log.Info("Exception :"+ex.Message);
            }
            


            Services.Log.Info("doorlistener scheduled job Done!");
            return Task.FromResult(true);
        }

        private void listenForMessages()
        {
            Services.Log.Info("listenForMessages");
            // Long poll the service bus for seconds
            QueueClient Client =
              QueueClient.CreateFromConnectionString(connectionString, "smartdoordemo");

            // Configure the callback options.
            OnMessageOptions options = new OnMessageOptions();
            options.AutoComplete = false;
            options.AutoRenewTimeout = TimeSpan.FromMinutes(1);

            
            // Callback to handle received messages.
            Client.OnMessageAsync(async (message) =>
            {
                try
                {
                    Services.Log.Info("Callback to handle received messages");
                    // Process message from queue.
                    Services.Log.Info("Body: " + message.GetBody<string>());
                    Services.Log.Info("MessageID: " + message.MessageId);
                    Services.Log.Info("doorBellID: " +
                    message.Properties["doorBellID"]);
                    Services.Log.Info("imageUrl: " +
                    message.Properties["imageUrl"]);

                    var doorBellId = message.Properties["doorBellID"];
                    var imgURL = message.Properties["imageUrl"];

                    Services.Log.Info("Query the database");
                    var doorbell = await (from x in DBcontext.DoorBells
                                          where x.DoorBellID == doorBellId.ToString()
                                          select x).ToListAsync();

                    Services.Log.Info("Query completed");

                    if (doorbell.Count > 0)
                    {
                        Services.Log.Info("doorBell Already Found");
                        var newPicture = new DataObjects.Pictures()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Url = imgURL.ToString(),
                            DoorBellID = doorBellId.ToString()
                        };

                        DBcontext.Pictures.Add(newPicture);

                        Services.Log.Info("Picture Guid : "+ newPicture.Id);

                        await DBcontext.SaveChangesAsync();

                        SendNotificationToClient(newPicture);
                    }
                    else
                    {
                        Services.Log.Info("doorBell Not Found, So creating new one");
                        var newDoorBell = new DataObjects.DoorBells()
                        {
                            Id = Guid.NewGuid().ToString(),
                            DoorBellID = doorBellId.ToString()
                        };
                        DBcontext.DoorBells.Add(newDoorBell);                        

                        var newPicture = new DataObjects.Pictures()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Url = imgURL.ToString(),
                            DoorBellID = doorBellId.ToString()
                        };

                        DBcontext.Pictures.Add(newPicture);                      

                        await DBcontext.SaveChangesAsync();
                        

                        Services.Log.Info("NewdoorBell Guid : " + newDoorBell.Id);
                        Services.Log.Info("Picture Guid : " + newPicture.Id);
                        SendNotificationToClient(newPicture);
                    }
                    // Remove message from queue.
                    message.Complete();

                    
                }
                catch (Exception ex)
                {
                    // Indicates a problem, unlock message in queue.
                    message.Abandon();
                    Services.Log.Info("Exception :" + ex.Message);
                }
            }, options);
           

            Services.Log.Info("listenForMessages End");
        }

        private async void SendNotificationToClient(Pictures item)
        {
            Services.Log.Info("Sending notification to Client App");

            WindowsPushMessage message = new WindowsPushMessage();
            var tags = new List<string>();

            tags.Add(item.DoorBellID);

            // Define the XML paylod for a WNS native toast notification 
            // that contains the text of the inserted item.
            message.XmlPayload = "<toast scenario=\'reminder\' launch =\'deals\'>" +
                "<visual>" +
                "<binding template =\'ToastGeneric\'>" +
                "<text>Door Bell Id</text>" +
                "<text>Someone ringing your Door bell with Id: " + item.DoorBellID + "</text>" +
                "<image placement=\'inline\' src=\'" + item.Url + "\'/>" +
                "</binding></visual>" +
                "</toast>";

            
            try
            {

                var result = await Services.Push.SendAsync(message, tags);
                Services.Log.Info(result.State.ToString());
            }
            catch (System.Exception ex)
            {
                Services.Log.Error(ex.Message, null, "Push.SendAsync Error");
            }

            message.XmlPayload =
                                 @"<tile version ='3'>
                                    <visual branding = 'nameAndLogo'>
                                     <binding template = 'TileWide'>
                                      <image src='" + item.Url + @"' placement='background'  hint-overlay='60'/>
                                      <image src='" + item.Url + @"' placement='peek'/>
                                      <group>                                       
                                       <subgroup>
                                        <text hint-style ='body'>Door Bell Id</text>            
                                        <text hint-style ='captionSubtle' hint-wrap = 'true'>" + item.DoorBellID + @"</text>                       
                                       </subgroup>                       
                                      </group>
                                     </binding>
                                    <binding template = 'TileLarge'>
                                      <image src='" + item.Url + @"' placement='background'  hint-overlay='60'/>
                                      <image src='" + item.Url + @"' placement='peek'/>
                                      <group>                                       
                                      <subgroup>
                                       <text hint-style ='body'>Door Bell Id</text>            
                                       <text hint-style ='captionSubtle' hint-wrap = 'true'>" + item.DoorBellID + @"</text>                       
                                      </subgroup>                       
                                     </group>
                                    </binding>
                                    <binding template = 'TileMedium'>
                                      <image src='" + item.Url + @"' placement='background'  hint-overlay='60'/>
                                      <image src='" + item.Url + @"' placement='peek'/>
                                      <group>
                                      <subgroup>
                                       <text hint-style ='body'>Door Bell Id</text>            
                                       <text hint-style ='captionSubtle' hint-wrap = 'true'>" + item.DoorBellID + @"</text>                       
                                      </subgroup>                       
                                     </group>
                                    </binding>
                                  </visual>
                                 </tile>";


            try
            {
                await Services.Push.SendAsync(message, tags);

            }
            catch (System.Exception ex)
            {
                // Write the failure result to the logs.
                Services.Log.Error(ex.Message, null, "Push.SendAsync Error");
            }

        }

        private Int64 GetTime(DateTime date)
        {
            Int64 retval = 0;
            var st = new DateTime(1970, 1, 1);
            TimeSpan t = (DateTime.Now.ToUniversalTime() - st);
            retval = (Int64)(t.TotalMilliseconds + 0.5);
            return retval;
        }

    }
}