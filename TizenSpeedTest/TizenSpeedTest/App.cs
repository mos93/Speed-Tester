using Newtonsoft.Json;
using NSpeedTest;
using NSpeedTest.Models;
using Plugin.DeviceInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TizenSpeedTest
{

    public class App : Application
    {
        private static SpeedTestClient client;
        private static Settings settings;
        private const string DefaultCountry = "Belarus";
        private static string clientCountry = null;
        private PrintableSpeed printableDownloadSpeed;
        private PrintableSpeed printableUploadSpeed;
        private SpeedTestTab speedTestTab;
        private AboutTab aboutTab;
        private HistoryTab historyTab;
        private Button testBtn;
        private ActivityIndicator testBtnLabel;
        private TapGestureRecognizer aboutBtn;
        private TapGestureRecognizer historyBtn;
        private Label serverInfo;
        private Label indicator;
        private Grid resultsBoard;
        private StackLayout indicators;
        private Image inAppLogo;
        private Image downloadSpeedIcon;
        private Label downloadSpeedValue;
        private Label downloadSpeedLabel;
        private Image uploadSpeedIcon;
        private Label uploadSpeedValue;
        private Label uploadSpeedLabel;
        private TestState myTestState;


        enum TestState
        {
            Start,
            Idle,
            Testing,
            OneDone,
            AllDone
        }

        private struct PrintableSpeed
        {
            public string label;
            public double speed;
            public PrintableSpeed(double speed)
            {
                if (speed > 1024)
                {
                    speed = Math.Round(speed / 1024, 2);
                    this.speed = speed;
                    this.label = "Mbps";
                }
                else
                {
                    speed = Math.Round(speed, 2);
                    this.speed = speed;
                    this.label = "Kbps";
                }

            }
            public string getPrintableSpeed()
            {
                return label + ": " + speed.ToString();
            }
        }


        public App()
        {
            //Initializing the tabs
            speedTestTab = new SpeedTestTab();
            historyTab = new HistoryTab();
            aboutTab = new AboutTab();

            //setting references to the UI views
            testBtn = speedTestTab.FindByName<Button>("TestBtn");
            testBtnLabel = speedTestTab.FindByName<ActivityIndicator>("TestBtnLabel");

            resultsBoard = speedTestTab.FindByName<Grid>("ResultsBoard");
            indicators = speedTestTab.FindByName<StackLayout>("Indicators");
            indicator = speedTestTab.FindByName<Label>("Indicator");
            inAppLogo = speedTestTab.FindByName<Image>("InAppLogo");

            downloadSpeedIcon = speedTestTab.FindByName<Image>("DownloadSpeedIcon");
            downloadSpeedValue = speedTestTab.FindByName<Label>("DownloadSpeedValue");
            downloadSpeedLabel = speedTestTab.FindByName<Label>("DownloadSpeedLabel");

            uploadSpeedIcon = speedTestTab.FindByName<Image>("UploadSpeedIcon");
            uploadSpeedValue = speedTestTab.FindByName<Label>("UploadSpeedValue");
            uploadSpeedLabel = speedTestTab.FindByName<Label>("UploadSpeedLabel");

            historyBtn = speedTestTab.FindByName<TapGestureRecognizer>("TapHistory");
            aboutBtn = speedTestTab.FindByName<TapGestureRecognizer>("TapAbout");
            serverInfo = speedTestTab.FindByName<Label>("ServerInfo");

            //setting the click handelers
            testBtn.Clicked += OnTestBtnClicked;
            historyBtn.Tapped += OnHistoryBtnClicked;
            aboutBtn.Tapped += OnAboutBtnClicked;

            //State zero
            myTestState = TestState.Start;

            //Using a NavigationPage to handle multiple tabs(pages)
            MainPage = new NavigationPage(speedTestTab)
            {
                BarBackgroundColor = Color.FromHex("#141526"),
                BarTextColor = Color.FromHex("#FFFFFF")
            };





        }

        private void OnTestBtnClicked(object sender, EventArgs e)
        {
            UpdateTestState();
            ShowDownloadResults();
            ShowUploadResults();
            HideResults();
            Task.Run(async () =>
            {
                StartSpeedTestAsync();
            }).ConfigureAwait(false);
        }

        private void OnAboutBtnClicked(object sender, EventArgs e)
        {
            MainPage.Navigation.PushAsync(aboutTab);
        }

        private void OnHistoryBtnClicked(object sender, EventArgs e)
        {
            historyTab.UpdateHistoryTable();
            MainPage.Navigation.PushAsync(historyTab);
        }

        private void UpdateServerInfo(Server info)
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                serverInfo.Text = String.Format("Server Hosted by {0} ({1}/{2})", info.Sponsor, info.Name, info.Country);

            });
        }

        private void UpdateDownloadUi(double dnSpeed)
        {
            printableDownloadSpeed = new PrintableSpeed(dnSpeed);
            UpdateTestState();

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                downloadSpeedValue.Text = printableDownloadSpeed.speed.ToString();
                downloadSpeedLabel.Text = printableDownloadSpeed.label;
                ShowDownloadResults();


            });


        }

        private void UpdateUploadUi(double upSpeed)
        {
            printableUploadSpeed = new PrintableSpeed(upSpeed);
            UpdateTestState();

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                uploadSpeedValue.Text = printableUploadSpeed.speed.ToString();
                uploadSpeedLabel.Text = printableUploadSpeed.label;
                ShowUploadResults();



            });


        }

        private void UpdateTestState()
        {
            if (myTestState == TestState.Testing)
            {
                myTestState = TestState.OneDone;
            }
            else if (myTestState == TestState.OneDone)
            {
                myTestState = TestState.AllDone;
            }
            else if (myTestState == TestState.Start)
            {
                myTestState = TestState.Testing;
                inAppLogo.IsVisible = false;
            }
            else
            {
                myTestState = TestState.Testing;
            }
        }

        private void UpdateIndicators(string info = null)
        {
            if (info == null)
            {
                //damn, forgot why @TODO
            }
            else
            {
                indicator.Text = info;
                if (myTestState == TestState.OneDone)
                {
                    indicators.IsVisible = false;
                }
                if (myTestState == TestState.AllDone)
                {
                    testBtn.Text = "Test Again";
                    testBtn.IsEnabled = true;
                    testBtn.IsVisible = true;
                    testBtnLabel.IsVisible = false;
                    UpdateTestsHistory();
                }
            }

        }

        private void UpdateTestsHistory()
        {
            string newTest = string.Format("{0};{1};{2};{3};{4}", DateTime.Now.ToString("MM-dd-yy"), printableDownloadSpeed.speed, printableDownloadSpeed.label,
                    printableUploadSpeed.speed, printableUploadSpeed.label);
            var currentNumberOfTests = HistoryTab.GetNumberOfHistoryEntries();
            currentNumberOfTests++;
            Application.Current.Properties["currentNumberOfTests"] = currentNumberOfTests;
            var newTestKey = "test#" + currentNumberOfTests;
            Application.Current.Properties[newTestKey] = newTest;
        }

        private void HideResults()
        {
            serverInfo.Text = "    ";
            UpdateIndicators("Getting Settings");
            indicators.IsVisible = true;
            downloadSpeedIcon.IsVisible = false;
            downloadSpeedLabel.IsVisible = false;
            downloadSpeedValue.IsVisible = false;
            uploadSpeedIcon.IsVisible = false;
            uploadSpeedLabel.IsVisible = false;
            uploadSpeedValue.IsVisible = false;
            testBtn.IsEnabled = false;
            testBtn.IsVisible = false;
            testBtnLabel.IsVisible = true;
        }

        private void ShowDownloadResults()
        {
            resultsBoard.IsVisible = true;
            downloadSpeedIcon.IsVisible = true;
            downloadSpeedLabel.IsVisible = true;
            downloadSpeedValue.IsVisible = true;
            UpdateIndicators("D");
        }

        private void ShowUploadResults()
        {
            resultsBoard.IsVisible = true;
            uploadSpeedIcon.IsVisible = true;
            uploadSpeedLabel.IsVisible = true;
            uploadSpeedValue.IsVisible = true;
            UpdateIndicators("U");
        }

        protected override void OnStart()
        {
            // Handle when your app starts

        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes

        }

        private static Server SelectBestServer(IEnumerable<Server> servers)
        {
            Debug.WriteLine("__");
            Debug.WriteLine("Best server by latency:");
            var bestServer = servers.OrderBy(x => x.Latency).First();
            PrintServerDetails(bestServer);
            Debug.WriteLine("__");
            return bestServer;
        }

        private static IEnumerable<Server> SelectServers()
        {
            Debug.WriteLine("__");
            Debug.WriteLine("Selecting best server by distance...");
            List<Server> servers;
            if (clientCountry != null)
            {
                servers = settings.Servers.Where(s => s.Country.Equals(clientCountry)).Take(10).ToList();
            }
            else
            {
                servers = settings.Servers.Where(s => s.Country.Equals(DefaultCountry)).Take(10).ToList();
            }

            foreach (var server in servers)
            {
                server.Latency = client.TestServerLatencyAsync(server).GetAwaiter().GetResult();
                PrintServerDetails(server);
            }
            return servers;
        }

        private static void PrintServerDetails(Server server)
        {
            Debug.WriteLine("Hosted by {0} ({1}/{2}), distance: {3}km, latency: {4}ms", server.Sponsor, server.Name,
                server.Country, (int)server.Distance / 1000, server.Latency);
        }

        private static void PrintSpeed(string type, double speed)
        {
            if (speed > 1024)
            {
                Debug.WriteLine("{0} speed: {1} Mbps", type, Math.Round(speed / 1024, 2));
            }
            else
            {
                Debug.WriteLine("{0} speed: {1} Kbps", type, Math.Round(speed, 2));
            }
        }

        private static async System.Threading.Tasks.Task<string> GetClienCountryAsync()
        {
            string url = "http://freegeoip.net/json/";
            var serverResponse = await SpeedTestWebClient.client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            string jsonstring = await serverResponse.Content.ReadAsStringAsync();
            dynamic dynObj = JsonConvert.DeserializeObject(jsonstring);
            return dynObj.country_name;
        }

        public async System.Threading.Tasks.Task<string> StartSpeedTestAsync()
        {

            client = new SpeedTestClient();
            settings = await client.GetSettingsAsync();
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                UpdateIndicators("Getting Settings");

            });

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                UpdateIndicators("Getting Client's Country");

            });
            clientCountry = await GetClienCountryAsync();

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                UpdateIndicators("Selecting Best Server by Distance");

            });
            var servers = SelectServers();
            var bestServer = SelectBestServer(servers);
            UpdateServerInfo(bestServer);

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                UpdateIndicators("Testing Internet Speed");

            });
            var downloadSpeed = client.TestDownloadSpeed(bestServer, settings.Download.ThreadsPerUrl);
            UpdateDownloadUi(downloadSpeed);
            //PrintSpeed("Download", downloadSpeed);

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                UpdateIndicators("Testing Internet Speed");

            });
            var uploadSpeed = client.TestUploadSpeed(bestServer, settings.Upload.ThreadsPerUrl);
            //PrintSpeed("Upload", uploadSpeed);
            UpdateUploadUi(uploadSpeed);
            return "Down: " + Math.Round(downloadSpeed / 1024, 2).ToString() + "Up: " + Math.Round(uploadSpeed / 1024, 2).ToString();
            //Console.WriteLine("Press a key to exit.");
            //Console.ReadKey();

        }
    }
}
