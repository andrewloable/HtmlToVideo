using PuppeteerSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HtmlToVideo
{
    class Program
    {
        private static string url;
        private static string filename;
        private static int width;
        private static int height;
        private static int seconds;
        private static string? chromeExePath;

        static void Main(string[] args)
        {
            ShowHelp(args);
            if (GetParameters(args))
            {
                Environment.Exit(0);
            }

            Task.Run(async () =>
            {
                await Capture();
            }).GetAwaiter().GetResult();
        }

        private static async Task Capture()
        {
            var options = new LaunchOptions
            {
                Headless = false,
                Args = new string[] {
                    "--enable-usermedia-screen-capturing",
                    "--allow-http-screen-capture",
                    "--auto-select-desktop-capture-source=screencap",
                    "--load-extension=" + Path.Combine(Directory.GetCurrentDirectory(), "ext"),
                    "--disable-extensions-except=" + Path.Combine(Directory.GetCurrentDirectory(), "ext"),
                    "--disable-infobars",
                    "--force-device-scale-factor=1",
                    $"--window-size={width},{height}"
                }
            };

            if (string.IsNullOrEmpty(chromeExePath))
            {
                Console.WriteLine("Downloading Chromium...");
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            }
            else
                options.ExecutablePath = chromeExePath;

            Console.WriteLine("Opening Url...");
            var browser = await Puppeteer.LaunchAsync(options);
            var page = await browser.NewPageAsync();
            await page.Client.SendAsync("Emulation.clearDeviceMetricsOverride");
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = width,
                Height = height,
                DeviceScaleFactor = 1
            });
            await page.GoToAsync(url);
            await page.SetBypassCSPAsync(true);
            await page.EvaluateExpressionAsync($"window.postMessage({{type: 'SET_EXPORT_PATH', filename: '{filename}'}}, '*')");
            Console.WriteLine("Recording...");
            await page.WaitForTimeoutAsync(seconds * 1000);
            await page.EvaluateExpressionAsync("window.postMessage({type: 'REC_STOP'}, '*')");
            await page.WaitForTimeoutAsync(2000);
            await browser.CloseAsync();
            Console.WriteLine("You video should be in your Downloads folder.");
        }

        private static void ShowHelp(string[] args)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var attrib = Assembly.GetExecutingAssembly().GetCustomAttributes().FirstOrDefault(r => r.GetType() == typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute;
            Console.WriteLine($"HtmlToVideo {version}");
            Console.WriteLine(attrib.Copyright);
            Console.WriteLine("Usage: HtmlToVideo <url> <filename.webm> <screen width> <screen height> <record time in seconds> <optional: chrome path>");
            Console.WriteLine("Example: HtmlToVideo https://loable.tech test.webm 1920 1080 10");

            if (args == null || args.Length == 0 || args[0].Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(0);
            }
        }

        private static bool GetParameters(string[] args)
        {
            if (args == null || (args.Length < 5 && args.Length > 6))
            {
                Console.WriteLine("Invalid number of parameters passed.");
                Environment.Exit(0);
            }

            var hasError = false;

            // check url
            if (string.IsNullOrWhiteSpace(args[0].Trim()))
            {
                Console.WriteLine("Please provide a URL to access.");
                hasError = true;
            }
            else
                url = args[0].Trim();
            // check filename
            if (string.IsNullOrWhiteSpace(args[1].Trim())){
                Console.WriteLine("Please provide a filename.");
                hasError = true;
            } 
            else
            {
                var ext = Path.GetExtension(args[1].Trim());
                if (ext.Equals(".webm", StringComparison.OrdinalIgnoreCase))
                    filename = args[1].Trim();
                else
                    filename = args[1].Trim() + ".webm";
            }

            // check width
            if (string.IsNullOrWhiteSpace(args[2].Trim()))
            {
                Console.WriteLine("Please provide a screen width.");
                hasError = true;
            }
            else
            {
                if (!int.TryParse(args[2].Trim(), out width))
                {
                    Console.WriteLine("Screen width is not a number.");
                    hasError = true;
                }
            }

            // check height
            if (string.IsNullOrWhiteSpace(args[3].Trim()))
            {
                Console.WriteLine("Please provide a screen height.");
                hasError = true;
            }
            else
            {
                if (!int.TryParse(args[3].Trim(), out height))
                {
                    Console.WriteLine("Screen height is not a number.");
                    hasError = true;
                }
            }

            // check seconds
            if (string.IsNullOrWhiteSpace(args[4].Trim()))
            {
                Console.WriteLine("Please provide a time to record in seconds.");
                hasError = true;
            }
            else
            {
                if (!int.TryParse(args[4].Trim(), out seconds))
                {
                    Console.WriteLine("Time to record is not a number.");
                    hasError = true;
                }
            }

            // check chrome path
            if (args.Length == 6 && !string.IsNullOrWhiteSpace(args[5].Trim()))
            {
                chromeExePath = args[5].Trim();
            }

            return hasError;
        }
    }
}
