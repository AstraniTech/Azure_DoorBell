using DoorBellClient.Models;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DoorBellClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IMobileServiceTable<Pictures> picturesTbl = App.MobileService.GetTable<Pictures>();
        private Timer timer;
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (App.ConnectedToInternet())
            {
                await RegisterForNotificationsAsync();
                loadPics();
                timer = new Timer(new TimerCallback(timer_Tick), timer, 15000, 15000);
            }
            else
            {
                await new MessageDialog("Please check your Internet Connection").ShowAsync();
            }
        }

       

        private void timer_Tick(object state)
        {
            loadPics();
        }

        private async void loadPics()
        {
           await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
           {
               progressBar.Visibility = Visibility.Visible;
               var pictures = await picturesTbl.Where(x => x.DoorBellID == "123456").OrderByDescending(x => x.UpdatedAt).ToCollectionAsync();
               itemListView.ItemsSource = pictures;
               progressBar.Visibility = Visibility.Collapsed;
           });
            
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            loadPics();
        }

        private async Task RegisterForNotificationsAsync()
        {
            try
            {


                var channel = await Windows.Networking.PushNotifications.PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

                var tags = new List<string>();
                tags.Add("123456");
                await App.MobileService.GetPush().RegisterNativeAsync(channel.Uri, tags);

            }
            catch (Exception ex)
            {

            }


        }
    }
}
