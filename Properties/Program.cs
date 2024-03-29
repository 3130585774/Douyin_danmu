﻿using CommandLine;
using PuppeteerSharp;
using RestSharp;

namespace DouyinCap
{
    class Program
    {
        static Browser? browser;
        static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<StartupOptions>(args)
                .WithParsed(MainAction);
        }

        static void AppExit(object? sender, EventArgs e)
        {
            browser?.Dispose();
        }

        static void ConsoleAppExit(object? sender, ConsoleCancelEventArgs e)
        {
            browser?.Dispose();
            e.Cancel = true;
        }
        static void Fetcher_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            Console.Write($"\rBrowser downloading... [{e.ProgressPercentage}%] {new string('.', DateTime.Now.Second % 4).PadRight(4)}");
        }

        static void MainAction(StartupOptions options)
        {
            async Task MainActionAsync()
            {
                using var fetcher = new BrowserFetcher();

                if (!fetcher.LocalRevisions().Contains(BrowserFetcher.DefaultChromiumRevision))
                {
                    fetcher.DownloadProgressChanged += Fetcher_DownloadProgressChanged;
                    await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                    Console.WriteLine();   // add a new line
                }

                Console.WriteLine($"Starting browser...");
                using Browser browser = await Puppeteer.LaunchAsync(new LaunchOptions()
                {
                    Headless = !options.ShowBrowser,
                });

                Program.browser = browser;      // 存储到静态变量
                
                AppDomain.CurrentDomain.ProcessExit += AppExit;       // 自动关闭
                Console.CancelKeyPress += ConsoleAppExit;

                RestClient? client = null;
                long roomId = options.RoomId;
                string liveHomeAddr = $"https://live.douyin.com/";
                string liveRoomAddr = $"https://live.douyin.com/{roomId}";

                if (options.PostAddress != null)
                    client = new RestClient(options.PostAddress);

                Console.WriteLine($"Loading page...");
                using Page page = (await browser.PagesAsync())[0];

                await page.GoToAsync(liveHomeAddr);
                await page.GoToAsync(liveRoomAddr);
                string? lastDataId = null;

                while (true)
                {
                    string query = QueryHelper.AllChatMessagesAfter(lastDataId);
                    ElementHandle[]? items = await page.QuerySelectorAllAsync(query);

                    if (items.Length == 0)
                    {
                        string msgQuery = QueryHelper.AllChatMessages();
                        ElementHandle[]? msgs = await page.QuerySelectorAllAsync(msgQuery);

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

                            Console.WriteLine($"{name}: {value}");

                            if (client != null)
                            {
                                var request = new RestRequest()
                                .AddJsonBody(new
                                 {
                                     Name = name,
                                     Value = value,
                                 });
                                try
                                {
                                    _ = await client.PostAsync(request);
                                }
                                catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                            }
                        }
                    }

                    if (items.Length > 0)
                        lastDataId = await items[^1].GetAttributeAsync("data-id");
                }
            }

            try
            {
                MainActionAsync().Wait();
            }
            catch { }
        }

        class StartupOptions
        {
            [Option("show-browser", Default = false, HelpText = "Show browser window.")]
            public bool ShowBrowser { get; set; }


            [Option("post-addr", Default = null, HelpText = "Specify a http server to post chat message data.")]
            public string? PostAddress { get; set; }


            [Value(0, MetaName = "room-id", HelpText = "Live room Id", Required = true)]
            public long RoomId { get; set; }
        }
    }
}