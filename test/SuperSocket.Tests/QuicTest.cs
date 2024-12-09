using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SuperSocket.Connection;
using SuperSocket.ProtoBase;
using SuperSocket.Quic;
using SuperSocket.Server.Host;
using Xunit;
using Xunit.Abstractions;
using QuicListener = System.Net.Quic.QuicListener;

namespace SuperSocket.Tests
{
    [Trait("Category", "Quic")]
    public class QuicTest : TestClassBase
    {
        public QuicTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void TestQuicSupport()
        {
#pragma warning disable CA2252,CA1416
            Assert.True(QuicListener.IsSupported, "QUIC is not supported.");
#pragma warning restore CA2252,CA1416
        }

        [Theory]
        [Trait("Category", "Quic.TestEcho")]
        [InlineData(typeof(QuicHostConfigurator), false)]
        public async Task TestEcho(Type hostConfiguratorType, bool clientReadAsDemand)
        {
#pragma warning disable CA2252,CA1416
            Assert.True(QuicListener.IsSupported, "QUIC is not supported.");
#pragma warning restore CA2252,CA1416

            var serverSessionEvent = new AutoResetEvent(false);

            var hostConfigurator = CreateObject<IHostConfigurator>(hostConfiguratorType);
            using (var server = CreateSocketServerBuilder<TextPackageInfo, LinePipelineFilter>(hostConfigurator)
                       .UsePackageHandler(async (s, p) =>
                       {
                           await s.SendAsync(Utf8Encoding.GetBytes(p.Text + "\r\n"));
                       })
                       .UseSessionHandler(
                           onConnected: (s) =>
                           {
                               serverSessionEvent.Set();
                               return ValueTask.CompletedTask;
                           },
                           onClosed: (s, e) =>
                           {
                               serverSessionEvent.Set();
                               return ValueTask.CompletedTask;
                           }).UseQuic()
                       .BuildAsServer())
            {
                Assert.Equal("TestServer", server.Name);

                Assert.True(await server.StartAsync());
                OutputHelper.WriteLine("Server started.");

                var options = new ConnectionOptions
                {
                    Logger = NullLogger.Instance,
                    ReadAsDemand = clientReadAsDemand
                };

                var client = hostConfigurator.ConfigureEasyClient(new LinePipelineFilter(), options);

                var connected =
                    await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, hostConfigurator.Listener.Port));

                Assert.True(connected);

                for (var i = 0; i < 100; i++)
                {
                    var msg = Guid.NewGuid().ToString();
                    await client.SendAsync(Utf8Encoding.GetBytes(msg + "\r\n"));

                    if (i == 0)
                    {
                        Assert.True(serverSessionEvent.WaitOne(1000));
                    }
                    
                    var package = await client.ReceiveAsync();
                    Assert.NotNull(package);
                    Assert.Equal(msg, package.Text);
                }

                await client.CloseAsync();
                Assert.True(serverSessionEvent.WaitOne(1000));
                await server.StopAsync();
            }
        }
    }
}