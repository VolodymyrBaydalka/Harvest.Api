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
        public MainWindow()
        {
            InitializeComponent();

            var auth = new HarvestAuthentication
            {
                RedirectUri = new Uri("<RedirectUri>"),
                ClientId = "<ClientId>", // TODO
                ClientSecret = "<ClientSecret>", // TODO
                UserAgent = "HavestApiClient" // TODO
            };

            webBrowser.Source = auth.BuildUrl();
            webBrowser.Navigating += async (s, e) =>
            {
                if (auth.IsRedirectUri(e.Uri))
                {
                    e.Cancel = true;

                    try
                    {
                        var client = await auth.HandleCallback(e.Uri);
                        await ApiSample(client);
                    }
                    catch
                    {
                        webBrowser.Source = auth.BuildUrl();
                    }
                }
            };
        }

        public async System.Threading.Tasks.Task ApiSample(HarvestClient harvestClient) {
            var proj = await harvestClient.GetProjectAssignmentsAsync();

            Debugger.Break();
        }
    }
}
