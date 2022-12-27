﻿using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Douyin_danmu
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Browser browser;
        static bool showb;
        static bool discon;

        public static Browser Browser { get => browser; set => browser = value; }

        public MainWindow()
        {
            InitializeComponent();
        }
        public static void Exit()
        {
            Browser?.Dispose();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string url = live_url.Text;
            showb = false;
            discon = false;
            string roomid = Get_Roomid(url);
            //string roomid = "859943315379";
            Debug.WriteLine(roomid);
            _ = StartAsync(showb, roomid);
        }
        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            discon = true;
            Exit();
        }
        private String Get_Roomid(String burl)
        {
            Uri url = new Uri(burl);
            string id = url.AbsolutePath;
            id = id.Remove(0, 1);
            return id;
        }

        private void Danmu_TextChanged(object sender, TextChangedEventArgs e)
        {
            danmu.ScrollToEnd();
        }
        async Task StartAsync(bool show,string roomId)
        {
            using var fetcher = new BrowserFetcher();

            if (!fetcher.LocalRevisions().Contains(BrowserFetcher.DefaultChromiumRevision))
            {
                danmu.AppendText("Downling...\n");
                await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                //Debug.WriteLine();   // add a new line
            }
            danmu.AppendText("Starting browser\n");
            Debug.WriteLine($"Starting browser...");
            using Browser browser = (Browser)await Puppeteer.LaunchAsync(new LaunchOptions()
            {
                Headless = !show,
            });
            MainWindow.browser = browser;
            string liveHomeAddr = $"https://live.douyin.com/";
            string liveRoomAddr = $"https://live.douyin.com/{roomId}";
            danmu.AppendText("Loading page...\n");
            Debug.WriteLine($"Loading page...");
            using PuppeteerSharp.Page page = (PuppeteerSharp.Page)(await browser.PagesAsync())[0];
            await page.GoToAsync(liveHomeAddr);
            await page.GoToAsync(liveRoomAddr);
            string lastDataId = null;
            while (!discon)
            {
                string query = QueryHelper.AllChatMessagesAfter(lastDataId);
                ElementHandle[] items = await page.QuerySelectorAllAsync(query);

                
                if (items.Length == 0)
                {
                    string msgQuery = QueryHelper.AllChatMessages();
                    ElementHandle[] msgs = await page.QuerySelectorAllAsync(msgQuery);

                    if (msgs.Length == 0)
                        lastDataId = null;

                    continue;
                }

                foreach (var item in items)
                {
                    //var nameNode = await item.QuerySelectorAsync(".tfObciRM");
                    //var valueNode = await item.QuerySelectorAsync(".Wz8LGswb");
                    var nameNode = await item.QuerySelectorAsync(".LU6dHmmD");
                    var valueNode = await item.QuerySelectorAsync(".JqBinbea");

                    if (nameNode != null && valueNode != null)
                    {
                        string name = await nameNode.GetInnerTextAsync();
                        string value = await valueNode.GetInnerTextAsync();

                        name = name.TrimEnd(':', '：');
                        Debug.WriteLine($"{name}: {value}");
                        danmu.AppendText($"{name}: {value}\n");

                    }
                }

                if (items.Length > 0)
                    lastDataId = await items[items.Length - 1].GetAttributeAsync("data-id");
            }
        }

        

    }
}