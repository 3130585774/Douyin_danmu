using Douyin_danmu;
using Panuon.WPF.UI;
using PuppeteerSharp;
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
        public static Browser browser;
        public static Browser Browser { get => browser; set => browser = value; }
        public static bool discon;
        public static bool showb;
        public MainWindow()
        {
            InitializeComponent();
        }
     
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string url = live_url.Text;
            showb = false;
            discon = false;
            string roomid =Get_Roomid(url);
            Debug.WriteLine(roomid);
            _ = StartAsync(showb, roomid);
        }
        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            discon = true;
            Exit();
        }
        private void Danmu_TextChanged(object sender, TextChangedEventArgs e)
        {
            danmu.ScrollToEnd();
        }
        public void appendDanmuText(string msg)
        {
            danmu.AppendText(msg);
        }
        public static void Exit()
        {
            Browser?.Dispose();
        }
        public String Get_Roomid(String burl)
        {
            Uri url = new Uri(burl);
            string id = url.AbsolutePath;
            id = id.Remove(0, 1);
            return id;
        }
        public async Task StartAsync(bool show, string roomId)
        {
            using var fetcher = new BrowserFetcher();

            if (!fetcher.LocalRevisions().Contains(BrowserFetcher.DefaultChromiumRevision))
            {
                danmu.AppendText($"下载浏览器...\n");
                await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            }
            danmu.AppendText($"启动服务...\n");
            using Browser browser = (Browser)await Puppeteer.LaunchAsync(new LaunchOptions()
            {
                Headless = !show,
            });
            MainWindow.browser = browser;
            string liveHomeAddr = $"https://live.douyin.com/";
            string liveRoomAddr = $"https://live.douyin.com/{roomId}";
            danmu.AppendText($"加载中...\n");
            using PuppeteerSharp.Page page = (PuppeteerSharp.Page)(await browser.PagesAsync())[0];
            await page.GoToAsync(liveHomeAddr);
            await page.GoToAsync(liveRoomAddr); //加载到直播页面
            string lastDataId = null;
            while (!discon)
            {
                string query = QueryHelper.AllChatMessagesAfter(lastDataId);
                ElementHandle[] items = await page.QuerySelectorAllAsync(query);
                string livenumberq = QueryHelper.LiveNumber();
                string gitlistq = QueryHelper.GiftList();
                ElementHandle livenumber = await page.QuerySelectorAsync(livenumberq);
                ElementHandle[] gitlist = await page.QuerySelectorAllAsync(gitlistq);
                string number = await livenumber.GetInnerTextAsync();
                Debug.WriteLine($"{number}");

                if (items.Length == 0)
                {
                    string msgQuery = QueryHelper.AllChatMessages();
                    ElementHandle[] msgs = await page.QuerySelectorAllAsync(msgQuery);

                    if (msgs.Length == 0)
                        lastDataId = null;

                    continue;
                }

                foreach (var item in items) // 弹幕
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
                foreach ( var item in gitlist)
                {
                    var nameNode = await item.QuerySelectorAsync("IgN8mayw");
                    string name = await nameNode.GetInnerTextAsync();
                    Debug.WriteLine($"{name}");
                }
                if (items.Length > 0)
                    lastDataId = await items[items.Length - 1].GetAttributeAsync("data-id");
            }
        }
    }
}