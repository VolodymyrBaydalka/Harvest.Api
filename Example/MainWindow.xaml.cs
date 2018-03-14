using Harvest.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HarvestClient client;

        public MainWindow()
        {
            InitializeComponent();


            client = new HarvestClient("HavestApiClient")
            {
                ClientId = "<ClientId>",
                ClientSecret = "<ClientSecret>",
                RedirectUri = new Uri("http://redirect/url"),
            };

            webBrowser.Source = client.BuildAuthorizationUrl();
            webBrowser.Navigating += async (s, e) =>
            {
                if (client.IsRedirectUri(e.Uri))
                {
                    e.Cancel = true;

                    try
                    {
                        await client.AuthorizeAsync(e.Uri);
                        await ApiSample(client);
                    }
                    catch
                    {
                        webBrowser.Source = client.BuildAuthorizationUrl();
                    }
                }
            };
        }

        public async System.Threading.Tasks.Task ApiSample(HarvestClient harvestClient) {

            await harvestClient.RefreshTokenAsync();

            var proj = await harvestClient.GetProjectAssignmentsAsync();

            Debugger.Break();
        }
    }
}
