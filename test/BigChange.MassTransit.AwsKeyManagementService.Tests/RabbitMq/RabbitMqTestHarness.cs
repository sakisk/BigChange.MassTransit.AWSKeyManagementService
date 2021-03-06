﻿using System;
using System.Linq;
using MassTransit;
using MassTransit.RabbitMqTransport;
using MassTransit.Testing;
using MassTransit.Transports;
using RabbitMQ.Client;

namespace BigChange.MassTransit.AwsKeyManagementService.Tests.RabbitMq
{
    public class RabbitMqTestHarness :
        BusTestHarness
    {
        private Uri _hostAddress;
        private Uri _inputQueueAddress;

        public RabbitMqTestHarness(string inputQueueName = null)
        {
            Username = "guest";
            Password = "guest";

            InputQueueName = inputQueueName ?? "input_queue";

            NameFormatter = new RabbitMqMessageNameFormatter();

            HostAddress = new Uri("rabbitmq://localhost/");
        }

        public Uri HostAddress
        {
            get => _hostAddress;
            set
            {
                _hostAddress = value;
                _inputQueueAddress = new Uri(HostAddress, InputQueueName);
            }
        }

        public string Username { get; set; }
        public string Password { get; set; }
        public string InputQueueName { get; }
        public string NodeHostName { get; set; }
        public IRabbitMqHost Host { get; private set; }
        public IMessageNameFormatter NameFormatter { get; }

        public override Uri InputQueueAddress => _inputQueueAddress;

        public event Action<IRabbitMqBusFactoryConfigurator> OnConfigureRabbitMqBus;
        public event Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> OnConfigureRabbitMqBusHost;
        public event Action<IRabbitMqReceiveEndpointConfigurator> OnConfigureRabbitMqReceiveEndoint;
        public event Action<IRabbitMqHostConfigurator> OnConfigureRabbitMqHost;
        public event Action<IModel> OnCleanupVirtualHost;

        protected virtual void ConfigureRabbitMqBus(IRabbitMqBusFactoryConfigurator configurator)
        {
            OnConfigureRabbitMqBus?.Invoke(configurator);
        }

        protected virtual void ConfigureRabbitMqBusHost(IRabbitMqBusFactoryConfigurator configurator,
            IRabbitMqHost host)
        {
            OnConfigureRabbitMqBusHost?.Invoke(configurator, host);
        }

        protected virtual void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            OnConfigureRabbitMqReceiveEndoint?.Invoke(configurator);
        }

        protected virtual void ConfigureRabbitMqHost(IRabbitMqHostConfigurator configurator)
        {
            OnConfigureRabbitMqHost?.Invoke(configurator);
        }

        protected virtual void CleanupVirtualHost(IModel model)
        {
            OnCleanupVirtualHost?.Invoke(model);
        }

        protected virtual IRabbitMqHost ConfigureHost(IRabbitMqBusFactoryConfigurator configurator)
        {
            return configurator.Host(HostAddress, h =>
            {
                h.Username(Username);
                h.Password(Password);

                if (!string.IsNullOrWhiteSpace(NodeHostName))
                    h.UseCluster(c => c.Node(NodeHostName));

                ConfigureRabbitMqHost(h);
            });
        }

        protected override IBusControl CreateBus()
        {
            return global::MassTransit.Bus.Factory.CreateUsingRabbitMq(x =>
            {
                Host = ConfigureHost(x);

                CleanUpVirtualHost(Host);

                ConfigureBus(x);

                ConfigureRabbitMqBus(x);

                ConfigureRabbitMqBusHost(x, Host);

                x.ReceiveEndpoint(Host, InputQueueName, e =>
                {
                    e.PrefetchCount = 16;
                    e.PurgeOnStartup = true;

                    ConfigureReceiveEndpoint(e);

                    ConfigureRabbitMqReceiveEndpoint(e);

                    _inputQueueAddress = e.InputAddress;
                });
            });
        }

        private void CleanUpVirtualHost(IRabbitMqHost host)
        {
            try
            {
                var connectionFactory = host.Settings.GetConnectionFactory();
                using (
                    var connection = host.Settings.ClusterMembers?.Any() ?? false
                        ? connectionFactory.CreateConnection(host.Settings.ClusterMembers, host.Settings.Host)
                        : connectionFactory.CreateConnection())
                using (var model = connection.CreateModel())
                {
                    model.ExchangeDelete("input_queue");
                    model.QueueDelete("input_queue");

                    model.ExchangeDelete("input_queue_skipped");
                    model.QueueDelete("input_queue_skipped");

                    model.ExchangeDelete("input_queue_error");
                    model.QueueDelete("input_queue_error");

                    model.ExchangeDelete("input_queue_delay");
                    model.QueueDelete("input_queue_delay");

                    model.ExchangeDelete(InputQueueName);
                    model.QueueDelete(InputQueueName);

                    model.ExchangeDelete(InputQueueName + "_skipped");
                    model.QueueDelete(InputQueueName + "_skipped");

                    model.ExchangeDelete(InputQueueName + "_error");
                    model.QueueDelete(InputQueueName + "_error");

                    model.ExchangeDelete(InputQueueName + "_delay");
                    model.QueueDelete(InputQueueName + "_delay");

                    CleanupVirtualHost(model);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}