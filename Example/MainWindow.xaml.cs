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


            var tester = new RawApiTester {
                AccessToken = "2083951.pt.LfsgnAnsun51GpHFEYCTM9GTLT3VXocQ09R17sWCj5oopzdY0qNp74HGqbVmlLrpA1YTtFqE5z2hGpmofc1JAg",
                AccountId = 1176282
            };

            tester.Show();

            var testClient = HarvestClient.FromAccessToken("TestClient", "2083951.pt.LfsgnAnsun51GpHFEYCTM9GTLT3VXocQ09R17sWCj5oopzdY0qNp74HGqbVmlLrpA1YTtFqE5z2hGpmofc1JAg");

            testClient.DefaultAccountId = 1176282;


            System.Threading.Tasks.Task.Run(async () => {
                try
                {
                    var m = await testClient.GetMe();

                    var pa = await testClient.GetUserAssignmentsAsync();
                    var a2 = await testClient.GetUserAssignmentsAsync(pa.UserAssignments[0].Project.Id);

                    var a = await testClient.GetUserAssignmentAsync(pa.UserAssignments[0].Project.Id, pa.UserAssignments[0].Id);

                    //var ts = await testClient.GetTasksAsync();
                    //var p = await testClient.GetProjectsAsync();
                    var i = await testClient.GetInvoicesAsync();
                    var c = await testClient.GetInvoiceItemCategoriesAsync();
                    var c1 = await testClient.GetInvoiceItemCategoryAsync(21934841);

                    Debugger.Break();
                }
                catch (HttpHarvestException e)
                {
                    try
                    {
                    }
                    catch {
                        Debugger.Break();
                    }
                    Debugger.Break();
                }
            });


            //client = new HarvestClient("HavestApiClient")
            //{
            //    ClientId = "nLlrEcPQ-qdYTZLefr0fDbdP",
            //    ClientSecret = "Ks3PuzGzs6BIm09tCtVC6wahW46Qy23ajFTqfO_OyklCGkFtTQUp3JoX-uqPhKOQbaRDB73XM5Wg0Z7G00ThTg",
            //    RedirectUri = new Uri("http://arrowdigital.com/timetracker"),
            //};

            //webBrowser.Source = client.BuildAuthorizationUrl();
            //webBrowser.Navigating += async (s, e) =>
            //{
            //    if (client.IsRedirectUri(e.Uri))
            //    {
            //        e.Cancel = true;

            //        try
            //        {
            //            await client.AuthorizeAsync(e.Uri);
            //            await ApiSample(client);
            //        }
            //        catch
            //        {
            //            webBrowser.Source = client.BuildAuthorizationUrl();
            //        }
            //    }
            //};
        }

        public async System.Threading.Tasks.Task ApiSample(HarvestClient harvestClient) {

            //await harvestClient.RefreshTokenAsync();
            //var c = await harvestClient.GetCompanyAsync();
            var i = await harvestClient.GetInvoicesAsync();
            //var proj = await harvestClient.GetProjectAssignmentsAsync();
            //var t = await harvestClient.GetTaskAssignmentsAsync(proj.ProjectAssignments[0].Project.Id);

            Debugger.Break();
        }
    }
}
