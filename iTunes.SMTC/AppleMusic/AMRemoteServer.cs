using Makaretu.Dns;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace iTunes.SMTC.AppleMusic
{
    internal class AMRemoteServer
    {
        private static readonly Lazy<AMRemoteServer> _instance = new(() =>
        {
            return new AMRemoteServer();
        }, true);

        public static AMRemoteServer Instance => _instance.Value;

        private IDisposable serverHandle = null;
        private ServiceDiscovery mDnsDiscovery;

        public void Start(Action<IServiceCollection> configureServices = null)
        {
            if (serverHandle == null)
            {
                var host = Host.CreateDefaultBuilder()
#if DEBUG || UNPACKAGEDDEBUG
                    .UseEnvironment(Environments.Development)
#endif
                    .ConfigureWebHost(builder =>
                    {
                        builder
                        .UseKestrel((context, serverOptions) =>
                        {
                            serverOptions.Listen(IPAddress.Loopback, 0);
                            serverOptions.ListenAnyIP(0, listenOptions =>
                            {
                                listenOptions.Protocols = HttpProtocols.Http1;
                            });
                            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
                        })
                        .ConfigureLogging((context, builder) =>
                        {
                            builder.AddConsole();

                            if (context.HostingEnvironment.IsDevelopment())
                            {
                                builder.AddDebug();
                            }
                        })
                        .UseShutdownTimeout(TimeSpan.FromSeconds(5))
                        .ConfigureServices((context, serviceCollection) =>
                        {
                            serviceCollection
                                .AddControllers()
                                .AddJsonOptions(options =>
                                {
                                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                                });

                            if (context.HostingEnvironment.IsDevelopment())
                            {
                                serviceCollection.AddEndpointsApiExplorer();
                                serviceCollection.AddSwaggerGen();
                            }

                            configureServices?.Invoke(serviceCollection);
                        })
                        .Configure((context, app) =>
                        {
                            app.UsePathBase("/api")
                               .UseRouting()
                               .UseEndpoints(ep =>
                                {
                                    ep.MapControllers();
                                    ep.MapDefaultControllerRoute();
                                });

                            if (context.HostingEnvironment.IsDevelopment())
                            {
                                app.UseSwagger();
                                app.UseSwaggerUI();
                            }
                        });
                    });

                serverHandle = host.Start();

                // Local server info
                var ipAddr = GetLocalIPAddress();
                var port = GetServerPort(serverHandle as IHost);

                if (mDnsDiscovery != null)
                {
                    mDnsDiscovery.Unadvertise();
                }
                else
                {
                    mDnsDiscovery = new ServiceDiscovery();
                }

                // Advertise service
                var service = new ServiceProfile("am-remote", "_itunes-smtc._tcp", (ushort)port, ipAddr != null ? ImmutableList.Create(ipAddr) : null);
                service.AddProperty("hostname", ipAddr.ToString());
                service.AddProperty("port", port.ToString());
                service.AddProperty("uri", "/api/am-remote");
                mDnsDiscovery.Advertise(service);
            }
            else
            {
                throw new InvalidOperationException("Server already started...");
            }
        }

        public void Stop()
        {
            mDnsDiscovery?.Unadvertise();
            serverHandle?.Dispose();
            serverHandle = null;
        }

        private static IPAddress GetLocalIPAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Unspecified);
                socket.Connect("8.8.8.8", 65530);
                var endpoint = socket.LocalEndPoint as IPEndPoint;
                return endpoint.Address;
            }
            catch
            {
                return null;
            }
        }

        private static int GetServerPort(IHost host)
        {
            var serverSvc = host.Services.GetService<IServer>();
            var localAddress = serverSvc?.Features?.Get<IServerAddressesFeature>()?.Addresses?.FirstOrDefault(addr =>
            {
                return addr.StartsWith("http://[::]:");
            });

            if (localAddress != null)
            {
                var portStr = localAddress.Split("http://[::]:").LastOrDefault();

                if (int.TryParse(portStr, out var port))
                {
                    return port;
                }
            }

            return 0;
        }
    }
}
