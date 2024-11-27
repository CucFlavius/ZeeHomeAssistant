using HADotNet.Core;
using HADotNet.Core.Clients;
using HidApi;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ZeeHomeAssistant.Toolset
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseWindowsService() // Enables Windows Service support
                .ConfigureServices(services =>
                {
                    services.AddHostedService<ZeeHomeAssistantToolsetService>();
                })
                .Build();

            await host.RunAsync();
        }
    }
    
    public class ZeeHomeAssistantToolsetService : BackgroundService
    {
        private Timer _timer;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var homeAssistantAddress = "http://192.168.88.40:30027/";
            var apiKey = Environment.GetEnvironmentVariable("HAS_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Error: HAS_API_KEY environment variable is not set.");
                return Task.CompletedTask;
            }

            Console.WriteLine($"Using API Key: {apiKey}"); // Temporary logging for debugging
            ClientFactory.Initialize(homeAssistantAddress, apiKey);

            // Set up a Timer to run every 3 minutes
            _timer = new Timer(async _ => await CheckBatteryAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(3));
            return Task.CompletedTask;
        }

        private static async Task CheckBatteryAsync()
        {
            try
            {
                // Find device
                var deviceInfo = FindDevice();

                if (deviceInfo == null)
                {
                    Console.WriteLine($"{DateTime.Now}: No HyperX Cloud II Wireless found.");
                    return;
                }

                var level = GetBatteryLevel(deviceInfo);
                if (level > 0)
                {
                    HidApi.Hid.Exit(); // Call at the end of your program
                    await SetHeadphonesBatteryPercentage(level);
                    Console.WriteLine($"{DateTime.Now}: Battery level updated: {level}%");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now}: Error: Could not get battery level.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: Error: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }

        private static DeviceInfo FindDevice()
        {
            int usage = 0;
            DeviceInfo deviceInfo = null;
            foreach(var hid in HidApi.Hid.Enumerate())
            {
                using var devicee = hid.ConnectToDevice();
                var iinfo = devicee.GetDeviceInfo();

                if (iinfo.ProductString == "HyperX Cloud II Wireless")
                {
                    if (iinfo.UsagePage > usage)
                    {
                        usage = iinfo.UsagePage;
                        deviceInfo = hid;
                    }

                    Console.WriteLine($"Found device: {iinfo.ProductString} {iinfo.UsagePage}");
                }
            }

            return deviceInfo;
        }

        private static int GetBatteryLevel(DeviceInfo deviceInfo)
        {
            using var device = deviceInfo.ConnectToDevice();
            var info = device.GetDeviceInfo();
            var manufacturer = info.ManufacturerString;
            var productName = info.ProductString;


            const int WRITE_BUFFER_SIZE = 52;
            const int DATA_BUFFER_SIZE = 20;

            byte[] writeBuffer = new byte[WRITE_BUFFER_SIZE];
            // Data buffer is max 20 bytes for the currently supported headsets.

            int batteryByteInt = 7;

            if (manufacturer.Contains("HP"))
            {
                if (productName.Contains("Cloud II Core"))
                {
                    // HP Cloud II Core Wireless data
                    writeBuffer[0] = 0x66;
                    writeBuffer[1] = 0x89;

                    batteryByteInt = 4;
                }
                else if (productName.Contains("Cloud II Wireless"))
                {
                    // HP Cloud II Wireless data
                    writeBuffer[0] = 0x06;
                    writeBuffer[1] = 0xff;
                    writeBuffer[2] = 0xbb;
                    writeBuffer[3] = 0x02;
                }
                else if (productName.Contains("Cloud Alpha Wireless"))
                {
                    // HP Cloud Alpha Wireless data
                    writeBuffer[0] = 0x21;
                    writeBuffer[1] = 0xbb;
                    writeBuffer[2] = 0x0b;

                    batteryByteInt = 3;
                }
            }
            else
            {
                // Kingston Cloud II supported but it requires some input report checking before writes.
                const int INPUT_BUFFER_SIZE = 160;

                ReadOnlySpan<byte> buffer = new byte[INPUT_BUFFER_SIZE];
                buffer = device.GetInputReport(6, INPUT_BUFFER_SIZE);
                //buffer[0] = 6; // Set the first value to the number of the report
                //int ret1 = hid_get_input_report(headsetDevice, buffer, INPUT_BUFFER_SIZE);

                // Kingston Cloud II Wireless data
                writeBuffer[0] = 0x06;
                writeBuffer[2] = 0x02;
                writeBuffer[4] = 0x9a;
                writeBuffer[7] = 0x68;
                writeBuffer[8] = 0x4a;
                writeBuffer[9] = 0x8e;
                writeBuffer[10] = 0x0a;
                writeBuffer[14] = 0xbb;
                writeBuffer[15] = 0x02;
            }

            device.Write(writeBuffer);
            //int ret = hid_write(headsetDevice, writeBuffer, WRITE_BUFFER_SIZE);

            byte[] dataBuffer = new byte[DATA_BUFFER_SIZE];

            device.ReadTimeout(dataBuffer, 1000);
            //hid_read_timeout(headsetDevice, dataBuffer, DATA_BUFFER_SIZE, 1000);

            int level = dataBuffer[batteryByteInt];
            return level;
        }

        static async Task SetHeadphonesBatteryPercentage(float percent)
        {
            var client = ClientFactory.GetClient<StatesClient>();
            var state = await client.SetState("sensor.hyperx_headphone_battery", percent.ToString(),
                new Dictionary<string, object>
                {
                    ["source"] = "HyperX Cloud II Wireless",
                    ["friendly_name"] = "Headphone Battery",
                    ["icon"] = "mdi:battery",
                    ["unit_of_measurement"] = '%'
                });
        }
    }
}