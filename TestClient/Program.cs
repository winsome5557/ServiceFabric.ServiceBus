﻿using System;
using System.Threading;
using Microsoft.Azure;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using ServiceFabric.ServiceBus.Clients;

namespace TestClient
{
	internal class Program
	{
		private static readonly string ConnectionStringForManaging;

		private static readonly string QueueNameStateless = "TestQueueStateless";
		private static readonly string TopicNameStateless = "TestTopicStateless";
		private static readonly string SubscriptionNameStateless = "TestSubscriptionStateless";

		private static readonly string QueueNameStateful = "TestQueueStateful";
		private static readonly string TopicNameStateful = "TestTopicStateful";
		private static readonly string SubscriptionNameStateful = "TestSubscriptionStateful";

		static Program()
		{
			//Get a Service Bus connection string that has rights to manage the Service Bus namespace, to be able to create queues and topics.
			//this is not needed in production situations, unless you want to create them on the fly using your own code.
			ConnectionStringForManaging = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.Manage");
		}

		// ReSharper disable once UnusedParameter.Local
		private static void Main(string[] args)
		{
			try
			{
				ProcessInput();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				Console.WriteLine("Hit any key to exit");
				Console.ReadKey(true);
			}
		}

		private static void ProcessInput()
		{
			while (true)
			{
				Console.WriteLine("Choose an option:");

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Manage Azure Service Bus namespace:");
				Console.WriteLine("1: Create the demo service bus queues");
				Console.WriteLine("2: Create the demo service bus topics");
				Console.WriteLine("3: Create subscriptions for the topics created with option 2");

				Console.ResetColor();
				Console.WriteLine();
				Console.WriteLine("Send Messages to Reliable Services:");

				Console.WriteLine("4: Send a message to SampleQueueListeningStatefulService");
				Console.WriteLine("5: Send a message to SampleQueueListeningStatelessService");

				Console.WriteLine("6: Send a message to SampleSubscriptionListeningStatefulService.");
				Console.WriteLine("7: Send a message to SampleSubscriptionListeningStatelessService.");

				Console.WriteLine();
				Console.WriteLine("Other: exit");


				var key = Console.ReadKey(true);
				switch (key.Key)
				{
					case ConsoleKey.D1:
					case ConsoleKey.NumPad1:
						CreateServiceBusQueue(QueueNameStateless);
						CreateServiceBusQueue(QueueNameStateful);
						break;

					case ConsoleKey.D2:
					case ConsoleKey.NumPad2:
						CreateServiceBusTopic(TopicNameStateless);
						CreateServiceBusTopic(TopicNameStateful);
						break;

					case ConsoleKey.D3:
					case ConsoleKey.NumPad3:
						CreateTopicSubscription(TopicNameStateless, SubscriptionNameStateless);
						CreateTopicSubscription(TopicNameStateful, SubscriptionNameStateful);
						break;

					case ConsoleKey.D4:
					case ConsoleKey.NumPad4:
						SendTestMessageToQueue(new Uri("fabric:/MyServiceFabricApp/SampleQueueListeningStatefulService"), QueueNameStateful, true);
						break;

					case ConsoleKey.D5:
					case ConsoleKey.NumPad5:
						SendTestMessageToQueue(new Uri("fabric:/MyServiceFabricApp/SampleQueueListeningStatelessService"), QueueNameStateless, false);
						break;

					case ConsoleKey.D6:
					case ConsoleKey.NumPad6:
						SendTestMessageToTopic(new Uri("fabric:/MyServiceFabricApp/SampleSubscriptionListeningStatefulService"), TopicNameStateful, true);
						break;

					case ConsoleKey.D7:
					case ConsoleKey.NumPad7:
						SendTestMessageToTopic(new Uri("fabric:/MyServiceFabricApp/SampleSubscriptionListeningStatelessService"), TopicNameStateless, false);
						break;

					default:
						return;
				}

				Thread.Sleep(200);
				Console.Clear();
			}
		}

		private static void CreateServiceBusTopic(string topicName)
		{
			var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionStringForManaging);

			if (namespaceManager.TopicExists(topicName))
			{
				Console.WriteLine($"Topic '{topicName}' exists.");
				return;
			}

			var td = new TopicDescription(topicName);
			var sendKey = SharedAccessAuthorizationRule.GenerateRandomKey();
			var receiveKey = SharedAccessAuthorizationRule.GenerateRandomKey();

			td.Authorization.Add(new SharedAccessAuthorizationRule("SendKey", sendKey, new[] { AccessRights.Send }));
			td.Authorization.Add(new SharedAccessAuthorizationRule("ReceiveKey", receiveKey, new[] { AccessRights.Listen }));

			namespaceManager.CreateTopic(td);
			Console.WriteLine($"Created Topic '{topicName}'.");

			Console.WriteLine($"Now manually update the App.config in the Subscription Listening-Service with the Send and Receive connection strings for this Topic:'{topicName}'.");
			Console.WriteLine( "Send Key - SharedAccessKeyName:'SendKey'");
			Console.WriteLine($"Send Key - SharedAccessKey:'{sendKey}'");
			Console.WriteLine();

			Console.WriteLine($"Receive Key - SharedAccessKeyName:'ReceiveKey'");
			Console.WriteLine($"Receive Key - SharedAccessKey:'{receiveKey}'");

			Console.WriteLine("Hit any key to continue...");
			Console.ReadKey(true);
		}

		private static void CreateTopicSubscription(string topicName, string subscriptionName)
		{
			var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionStringForManaging);
			if (namespaceManager.SubscriptionExists(topicName, subscriptionName))
			{
				Console.WriteLine($"Subscription '{subscriptionName}' for Topic '{topicName}' exists.");
				return;
			}
			namespaceManager.CreateSubscription(topicName, subscriptionName);
			Console.WriteLine($"Created Subscription '{subscriptionName}' for Topic '{topicName}'.");
		}

		private static void CreateServiceBusQueue(string queueName)
		{
			var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionStringForManaging);

			if (namespaceManager.QueueExists(queueName))
			{
				Console.WriteLine($"Queue '{queueName}' exists.");
				return;
			}

			var qd = new QueueDescription(queueName);
			var sendKey = SharedAccessAuthorizationRule.GenerateRandomKey();
			var receiveKey = SharedAccessAuthorizationRule.GenerateRandomKey();
			qd.Authorization.Add(new SharedAccessAuthorizationRule("SendKey", sendKey, new[] { AccessRights.Send }));
			qd.Authorization.Add(new SharedAccessAuthorizationRule("ReceiveKey", receiveKey, new[] { AccessRights.Listen }));

			namespaceManager.CreateQueue(qd);
			Console.WriteLine($"Created queue '{queueName}'.");

			Console.WriteLine($"Now manually update the App.config in the Queue Listening-Service with the Send and Receive connection strings for this Queue:'{queueName}'.");
			Console.WriteLine("Send Key - SharedAccessKeyName:'SendKey'");
			Console.WriteLine($"Send Key - SharedAccessKey:'{sendKey}'");
			Console.WriteLine();

			Console.WriteLine($"Receive Key - SharedAccessKeyName:'ReceiveKey'");
			Console.WriteLine($"Receive Key - SharedAccessKey:'{receiveKey}'");

			Console.WriteLine("Hit any key to continue...");
			Console.ReadKey(true);
		}

		
		private static void SendTestMessageToTopic(Uri uri, string topicName, bool serviceSupportsPartitions)
		{
			//the name of your application and the name of the Service, the default partition resolver and the topic name
			//to create a communication client factory:
			var factory = new ServiceBusTopicCommunicationClientFactory(ServicePartitionResolver.GetDefault(), topicName);
			
			ServicePartitionClient<ServiceBusTopicCommunicationClient> servicePartitionClient;

			if (serviceSupportsPartitions)
			{
				//determine the partition and create a communication proxy
				long partitionKey = 0L;
				servicePartitionClient = new ServicePartitionClient<ServiceBusTopicCommunicationClient>(factory, uri, partitionKey);
			}
			else
			{
				servicePartitionClient = new ServicePartitionClient<ServiceBusTopicCommunicationClient>(factory, uri);
			}
			
			//use the proxy to send a message to the Service
			servicePartitionClient.InvokeWithRetry(c => c.SendMessage(CreateMessage()));

			Console.WriteLine("Message sent to topic");
		}
		
		private static void SendTestMessageToQueue(Uri uri, string queueName, bool serviceSupportsPartitions)
		{
			//the name of your application and the name of the Service, the default partition resolver and the topic name
			//to create a communication client factory:
			var factory = new ServiceBusQueueCommunicationClientFactory(ServicePartitionResolver.GetDefault(), queueName);

			ServicePartitionClient<ServiceBusQueueCommunicationClient> servicePartitionClient;

			if (serviceSupportsPartitions)
			{
				//determine the partition and create a communication proxy
				long partitionKey = 0L;
				servicePartitionClient = new ServicePartitionClient<ServiceBusQueueCommunicationClient>(factory, uri, partitionKey);
			}
			else
			{
				servicePartitionClient = new ServicePartitionClient<ServiceBusQueueCommunicationClient>(factory, uri);
			}

			//use the proxy to send a message to the Service
			servicePartitionClient.InvokeWithRetry(c => c.SendMessage(CreateMessage()));

			Console.WriteLine("Message sent to queue");
		}

		private static BrokeredMessage CreateMessage()
		{
			return new BrokeredMessage()
			{
				Properties =
				{
					{ "TestKey", "TestValue" }
				}
			};
		}
	}
}