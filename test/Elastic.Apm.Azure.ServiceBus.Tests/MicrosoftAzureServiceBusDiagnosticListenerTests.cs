﻿using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using Elastic.Apm.Api;
using Elastic.Apm.Azure.ServiceBus.Tests.Azure;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Azure;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.ServiceBus.Tests
{
	[Collection("AzureServiceBus")]
	public class MicrosoftAzureServiceBusDiagnosticListenerTests : IDisposable
	{
		private readonly AzureServiceBusTestEnvironment _environment;
		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _sender;
		private readonly ServiceBusAdministrationClient _adminClient;

		public MicrosoftAzureServiceBusDiagnosticListenerTests(AzureServiceBusTestEnvironment environment, ITestOutputHelper output)
		{
			_environment = environment;

			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_agent.Subscribe(new MicrosoftAzureServiceBusDiagnosticsSubscriber());
			_adminClient = new ServiceBusAdministrationClient(environment.ServiceBusConnectionString);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Send_To_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);

			await _agent.Tracer.CaptureTransaction("Send AzureServiceBus Message", "message", async () =>
			{
				await sender.SendAsync(new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{ServiceBus.SegmentName} SEND to {scope.QueueName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(ServiceBus.SubType);
			span.Action.Should().Be("send");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be($"sb://{_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace}/");
			destination.Service.Name.Should().Be(ServiceBus.SubType);
			destination.Service.Resource.Should().Be($"{ServiceBus.SubType}/{scope.QueueName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Send_To_Topic()
		{
			await using var scope = await TopicScope.CreateWithTopic(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.TopicName);
			await _agent.Tracer.CaptureTransaction("Send AzureServiceBus Message", "message", async () =>
			{
				await sender.SendAsync(new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{ServiceBus.SegmentName} SEND to {scope.TopicName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(ServiceBus.SubType);
			span.Action.Should().Be("send");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be($"sb://{_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace}/");
			destination.Service.Name.Should().Be(ServiceBus.SubType);
			destination.Service.Resource.Should().Be($"{ServiceBus.SubType}/{scope.TopicName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Schedule_To_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			await _agent.Tracer.CaptureTransaction("Schedule AzureServiceBus Message", "message", async () =>
			{
				await sender.ScheduleMessageAsync(
					new Message(Encoding.UTF8.GetBytes("test message")),
					DateTimeOffset.Now.AddSeconds(10)).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{ServiceBus.SegmentName} SCHEDULE to {scope.QueueName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(ServiceBus.SubType);
			span.Action.Should().Be("schedule");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be($"sb://{_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace}/");
			destination.Service.Name.Should().Be(ServiceBus.SubType);
			destination.Service.Resource.Should().Be($"{ServiceBus.SubType}/{scope.QueueName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Schedule_To_Topic()
		{
			await using var scope = await TopicScope.CreateWithTopic(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.TopicName);
			await _agent.Tracer.CaptureTransaction("Schedule AzureServiceBus Message", "message", async () =>
			{
				await sender.ScheduleMessageAsync(
					new Message(Encoding.UTF8.GetBytes("test message")),
					DateTimeOffset.Now.AddSeconds(10)).ConfigureAwait(false);
			});

			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{ServiceBus.SegmentName} SCHEDULE to {scope.TopicName}");
			span.Type.Should().Be(ApiConstants.TypeMessaging);
			span.Subtype.Should().Be(ServiceBus.SubType);
			span.Action.Should().Be("schedule");
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be($"sb://{_environment.ServiceBusConnectionStringProperties.FullyQualifiedNamespace}/");
			destination.Service.Name.Should().Be(ServiceBus.SubType);
			destination.Service.Resource.Should().Be($"{ServiceBus.SubType}/{scope.TopicName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_Receive_From_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString, scope.QueueName, ReceiveMode.PeekLock);

			await sender.SendAsync(
				new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);

			await receiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2)))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(1);
			var transaction = _sender.FirstTransaction;

			transaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVE from {scope.QueueName}");
			transaction.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_Receive_From_Topic_Subscription()
		{
			await using var scope = await TopicScope.CreateWithTopicAndSubscription(_adminClient);

			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.TopicName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString,
				EntityNameHelper.FormatSubscriptionPath(scope.TopicName, scope.SubscriptionName));

			await sender.SendAsync(
				new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);

			await receiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2)))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(1);
			var transaction = _sender.FirstTransaction;

			transaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVE from {scope.TopicName}/Subscriptions/{scope.SubscriptionName}");
			transaction.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_ReceiveDeferred_From_Queue()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString, scope.QueueName, ReceiveMode.PeekLock);

			await sender.SendAsync(
				new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);

			var message = await receiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
			await receiver.DeferAsync(message.SystemProperties.LockToken).ConfigureAwait(false);

			await receiver.ReceiveDeferredMessageAsync(message.SystemProperties.SequenceNumber).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2), count: 2))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(2);

			var transaction = _sender.FirstTransaction;
			transaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVE from {scope.QueueName}");
			transaction.Type.Should().Be(ApiConstants.TypeMessaging);

			var secondTransaction = _sender.Transactions[1];
			secondTransaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVEDEFERRED from {scope.QueueName}");
			secondTransaction.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Capture_Transaction_When_ReceiveDeferred_From_Topic_Subscription()
		{
			await using var scope = await TopicScope.CreateWithTopicAndSubscription(_adminClient);

			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.TopicName);
			var receiver = new MessageReceiver(_environment.ServiceBusConnectionString,
				EntityNameHelper.FormatSubscriptionPath(scope.TopicName, scope.SubscriptionName));

			await sender.SendAsync(
				new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);

			var message = await receiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
			await receiver.DeferAsync(message.SystemProperties.LockToken).ConfigureAwait(false);

			await receiver.ReceiveDeferredMessageAsync(message.SystemProperties.SequenceNumber).ConfigureAwait(false);

			if (!_sender.WaitForTransactions(TimeSpan.FromMinutes(2), count: 2))
				throw new Exception("No transaction received in timeout");

			_sender.Transactions.Should().HaveCount(2);

			var transaction = _sender.FirstTransaction;
			transaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVE from {scope.TopicName}/Subscriptions/{scope.SubscriptionName}");
			transaction.Type.Should().Be(ApiConstants.TypeMessaging);

			var secondTransaction = _sender.Transactions[1];
			secondTransaction.Name.Should().Be($"{ServiceBus.SegmentName} RECEIVEDEFERRED from {scope.TopicName}/Subscriptions/{scope.SubscriptionName}");
			secondTransaction.Type.Should().Be(ApiConstants.TypeMessaging);
		}

		[AzureCredentialsFact]
		public async Task Does_Not_Capture_Span_When_QueueName_Matches_IgnoreMessageQueues()
		{
			await using var scope = await QueueScope.CreateWithQueue(_adminClient);
			var sender = new MessageSender(_environment.ServiceBusConnectionString, scope.QueueName);
			_agent.ConfigStore.CurrentSnapshot = new MockConfigSnapshot(ignoreMessageQueues: scope.QueueName);

			await _agent.Tracer.CaptureTransaction("Send AzureServiceBus Message", "message", async () =>
			{
				await sender.SendAsync(new Message(Encoding.UTF8.GetBytes("test message"))).ConfigureAwait(false);
			});

			_sender.SignalEndSpans();
			_sender.WaitForSpans();
			_sender.Spans.Should().HaveCount(0);
		}

		public void Dispose() => _agent.Dispose();
	}
}
