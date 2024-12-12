using System;
using System.Drawing;
using System.Text;
using MemeHashDriver_v3_github;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static System.Net.Mime.MediaTypeNames;

var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

IConfiguration config = builder.Build();

var porofiles = config.GetSection("AccountsData").Get<List<AccountData>>();
var authToken = config.GetSection("AuthToken").Get<string>();

var httpClient = new HttpClient();
var authBody = $"{{\"token\": \"{authToken}\"}}";

while (true)
{
    foreach (var profile in porofiles)
    {
        Console.WriteLine($"Start work. Profle name: {profile.name}");

        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:3001/v1.0/browser_profiles/{profile.id}/start?automation=1"))
        {
            requestMessage.Headers.Add("Accept", "application/json");
            requestMessage.Content = new StringContent(authBody, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            var port = (JObject.Parse(content))["automation"]?["port"]?.ToObject<string>();

            Console.WriteLine("4 min pause");
            Thread.Sleep(4 * 60 * 1000);
            Console.WriteLine("4 min pause ended");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to start profile {profile.name}. Status: {response.StatusCode}");
            }
            else
            {
                StartWorkWithProfile(port, profile);
            }
        }
    }
}

static void StartWorkWithProfile(string port, AccountData profile)
{
    var chromeOptions = new ChromeOptions();
    chromeOptions.DebuggerAddress = $"127.0.0.1:{port}";
    Console.WriteLine($"POOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOORT  {port}");
    var driver = new ChromeDriver(chromeOptions);

    var status = OpenMemeHashUrl(driver, profile.MemHashUrl);

    if (status)
    {
        var action = new Actions(driver);
        action.MoveByOffset(177, 400).Click().Perform();
        Console.WriteLine("farm started (I hope)");

        while (true)
        {
            Thread.Sleep(10000);
            var rgb = GetMiningStatusColor(driver).ToString();


            if ((rgb == "Rgba32(255, 108, 101, 255)") | (rgb == "Rgba32(255, 254, 179, 255)"))
            {
                Console.WriteLine(DateTime.Now);

                if (rgb == "Rgba32(255, 108, 101, 255)")
                    Console.WriteLine("farm in STOPED!!!!");
                if (rgb == "Rgba32(255, 254, 179, 255)")
                    Console.WriteLine("pidor zavis");


                var windowHandles = driver.WindowHandles;

                foreach (var handle in windowHandles)
                {
                    driver.SwitchTo().Window(handle);  // Переходим на вкладку
                    driver.Close();  // Закрываем вкладку
                }
                driver.Dispose();
                break;
            }
            Console.WriteLine(DateTime.Now);
        }
    }
    else
    {
        Console.WriteLine($"Account was skipped: {profile.name}");

        var windowHandles = driver.WindowHandles;

        foreach (var handle in windowHandles)
        {
            driver.SwitchTo().Window(handle);  // Переходим на вкладку
            driver.Close();  // Закрываем вкладку
        }
        driver.Dispose();
    }
}

static bool OpenMemeHashUrl(ChromeDriver driver, string url)
{
    for (int i = 0; i < 5; i++)
    {
        driver.Navigate().GoToUrl("https://www.google.com/");
        driver.Navigate().GoToUrl(url);

        Console.WriteLine("Thread sleep 1 min: start");
        Thread.Sleep(1000 * 60);
        Console.WriteLine("Thread sleep 1 min: stop");
        Console.WriteLine($"Current URL: {driver.Url}");
        Console.WriteLine();

        var rgba = GetMiningStatusColor(driver).ToString();

        if (rgba == "Rgba32(255, 254, 179, 255)")
        {
            return true;
        }
    }

    return false;
}

static Rgba32 GetMiningStatusColor(ChromeDriver driver)
{
    GetMemHashScreenshot(driver);

    using (Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>("screenshot.png"))
    {
        return image[160, 289];
    }
}

static void GetMemHashScreenshot(ChromeDriver driver)
{
    Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();

    // Сохраняем скриншот во временный файл
    screenshot.SaveAsFile("screenshot.png");
}