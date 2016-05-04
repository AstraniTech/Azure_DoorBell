using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PhotoSamplePOC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        
        //A GPIO pin for the pushbutton
        GpioPin buttonPin;
        //The GPIO pin number we want to use to control the pushbutton
        int gpioPin = 4;

        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        
        private readonly string PHOTO_FILE_NAME = "photo.jpg";

       
        private bool isPreviewing;
        private bool isRecording;

        public MainPage()
        {
            this.InitializeComponent();

            isRecording = false;
            isPreviewing = false;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            try
            {             

                //Initialize the GPIO pin for the pushbutton
                InitializeGpio();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        //This method is used to initialize a GPIO pin
        private void InitializeGpio()
        {
            //Create a default GPIO controller
            GpioController gpioController = GpioController.GetDefault();
            //Use the controller to open the gpio pin of given number
            buttonPin = gpioController.OpenPin(gpioPin);
            //Debounce the pin to prevent unwanted button pressed events
            buttonPin.DebounceTimeout = new TimeSpan(1000);
            //Set the pin for input
            buttonPin.SetDriveMode(GpioPinDriveMode.Input);
            //Set a function callback in the event of a value change
            buttonPin.ValueChanged += buttonPin_ValueChanged;
        }
        //This method will be called everytime there is a change in the GPIO pin value
        private async void buttonPin_ValueChanged(object sender, GpioPinValueChangedEventArgs e)
        {
            //Only read the sensor value when the button is released
            if (e.Edge == GpioPinEdge.RisingEdge)
            {
               
                await initVideo();
                await takePhoto_Click();
              
            }
        }
        
        private async void loadPhoto()
        {
            try
            {
                await initVideo();
                await takePhoto_Click();
            }
            catch (Exception ex)
            {

            }
        }

        private async void Cleanup()
        {
            if (mediaCapture != null)
            {
                // Cleanup MediaCapture object
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                   
                    isPreviewing = false;
                }
                if (isRecording)
                {
                    await mediaCapture.StopRecordAsync();
                    isRecording = false;
                  
                }
                mediaCapture.Dispose();
                mediaCapture = null;
            }
            
        }

        private async Task initVideo()
        {
            
            try
            {
                if (mediaCapture != null)
                {
                    // Cleanup MediaCapture object
                    if (isPreviewing)
                    {
                        await mediaCapture.StopPreviewAsync();
                       
                        isPreviewing = false;
                    }
                    if (isRecording)
                    {
                        await mediaCapture.StopRecordAsync();
                        isRecording = false;
                       
                    }
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

              
                // Use default initialization
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
               
                mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);
                mediaCapture.RecordLimitationExceeded += new Windows.Media.Capture.RecordLimitationExceededEventHandler(mediaCapture_RecordLimitExceeded);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
               {
                   previewElement.Source = mediaCapture;
               });
                // Start Preview                

                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
               

            }
            catch (Exception ex)
            {
               
            }
        }
        
       
        string path = "";
        private async Task takePhoto_Click()
        {
            try
            {
               
                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
                //takePhoto.IsEnabled = true;
                //status.Text = "Take Photo succeeded: " + photoFile.Path;
                path = photoFile.Path;
                IRandomAccessStream photoStream = await photoFile.OpenReadAsync();

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.SetSource(photoStream);
                    captureImage.Source = bitmap;
                });

                TakeAndSendPicture(photoFile);
            }
            catch (Exception ex)
            {
                
                Cleanup();
            }
            finally
            {
               
            }
        }

        async void TakeAndSendPicture(StorageFile photoFile)
        {

            PhotoResponse photoResp = null;            

            WebRequest photoRequest = WebRequest.Create("https://iotpoc.azure-mobile.net/api/RequestSAS");
            
            photoRequest.Method = "GET"; 
           
            //IotPoc
            photoRequest.Headers["X-ZUMO-APPLICATION"] = "[Your Key]";

            HttpWebResponse response = await photoRequest.GetResponseAsync() as HttpWebResponse;
            using (var sbPhotoResponseStream = response.GetResponseStream())
            {
                StreamReader sr = new StreamReader(sbPhotoResponseStream);

                string data = sr.ReadToEnd();

                photoResp = JsonConvert.DeserializeObject<PhotoResponse>(data);
            }

            //We've gotten the Shared Access Signature for the blob in URL form.
            //This URL points directly to the blob and we are now authorized to
            //upload the picture to this url with a PUT request
            Debug.WriteLine("Pushing photo to SAS Url: " + photoResp.sasUrl);

            // If we have a returned SAS, then upload the blob.
            if (!string.IsNullOrEmpty(photoResp.sasUrl))
            {
                // Get the URI generated that contains the SAS 
                // and extract the storage credentials.
                StorageCredentials cred = new StorageCredentials(photoResp.sasUrl);
                var imageUri = new Uri(photoResp.photoId);

                // Instantiate a Blob store container based on the info in the returned item.
                CloudBlobContainer container = new CloudBlobContainer(
                    new Uri(string.Format("https://{0}/{1}",
                        imageUri.Host, photoResp.ContainerName)), cred);

                    // Upload the new image as a BLOB from the stream.
                    CloudBlockBlob blobFromSASCredential =
                        container.GetBlockBlobReference(photoResp.ResourceName);
                try
                {
                    await blobFromSASCredential.UploadFromFileAsync(photoFile);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Failed to upload file");
                }

            }

            
            StopPreview();

            try
            {
                Debug.WriteLine("Sending notification to service bus queue");
                var MobileService = new MobileServiceClient("https://iotpoc.azure-mobile.net/", "[Your Key]");
                
                var dictionary = new Dictionary<string, string>();
                var doorbellObject = JsonConvert.SerializeObject(new DoorBellNotification()
                {
                    doorBellID = "123456",
                    imageUrl = photoResp.photoId
                });

                dictionary.Add("jsonObj", doorbellObject);
                //dictionary.Add("imageUrl", photoResp.photoId);
                
                var result = await MobileService
         .InvokeApiAsync<string>("MessageQueue",
         System.Net.Http.HttpMethod.Post, dictionary);
                if (result == "Success")
                {
                    Debug.WriteLine("Sending notification to service bus queue Success");
                }
                else
                {
                    Debug.WriteLine("Sending notification to service bus queue Failed");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Sending notification to service bus queue Failed: "+ex.Message);
            }
            
        }
    

        private async void StopPreview()
        {
            if (mediaCapture != null)
            {
                // Cleanup MediaCapture object
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                   
                    isPreviewing = false;
                }
                if (isRecording)
                {
                    await mediaCapture.StopRecordAsync();
                    isRecording = false;
                   
                }
                mediaCapture.Dispose();
                mediaCapture = null;
            }

            Debug.WriteLine("Camera Stopped");
        }

     
        /// <summary>
        /// Callback function for any failures in MediaCapture operations
        /// </summary>
        /// <param name="currentCaptureObject"></param>
        /// <param name="currentFailure"></param>
        private async void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    //status.Text = "MediaCaptureFailed: " + currentFailure.Message;

                    if (isRecording)
                    {
                        await mediaCapture.StopRecordAsync();
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                   
                }
            });
        }

        /// <summary>
        /// Callback function if Recording Limit Exceeded
        /// </summary>
        /// <param name="currentCaptureObject"></param>
        public async void mediaCapture_RecordLimitExceeded(Windows.Media.Capture.MediaCapture currentCaptureObject)
        {
            try
            {
                if (isRecording)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        try
                        {
                           
                            await mediaCapture.StopRecordAsync();
                            isRecording = false;
                           
                            if (mediaCapture.MediaCaptureSettings.StreamingCaptureMode == StreamingCaptureMode.Audio)
                            {
                                
                            }
                            else
                            {
                               
                            }
                        }
                        catch (Exception e)
                        {
                           
                        }
                    });
                }
            }
            catch (Exception e)
            {
                
            }
        }

    }
    public class DoorBellNotification
    {
        public string doorBellID { get; set; }
        public string imageUrl { get; set; }
    }
    public class PhotoResponse
    {
        public string sasUrl { get; set; }
        public string photoId { get; set; }
        public string expiry { get; set; }
        public string ResourceName { get; set; }
        public string ContainerName { get; set; }
    }
}

