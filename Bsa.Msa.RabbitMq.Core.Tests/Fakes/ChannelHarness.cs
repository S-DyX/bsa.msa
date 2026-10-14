using System;
using System.Collections.Generic;
using System.Text;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes
{
	/// <summary>
	/// Mocked connection and channel that record what the bus does.
	/// </summary>
	public sealed class ChannelHarness
	{
		/// <summary>
		/// Consumer tag returned by BasicConsume.
		/// </summary>
		public const string ConsumerTag = "consumer-tag";

		/// <summary>
		/// Creates the mocks.
		/// </summary>
		public ChannelHarness()
		{
			SetupModel();
			SetupConnection();
		}

		/// <summary>
		/// Mocked channel.
		/// </summary>
		public Mock<IModel> Model { get; } = new Mock<IModel>();

		/// <summary>
		/// Mocked connection.
		/// </summary>
		public Mock<ISimpleConnection> Connection { get; } = new Mock<ISimpleConnection>();

		/// <summary>
		/// Published messages.
		/// </summary>
		public List<PublishedMessage> Published { get; } = new List<PublishedMessage>();

		/// <summary>
		/// Declared queues with their arguments.
		/// </summary>
		public Dictionary<string, IDictionary<string, object>> DeclaredQueues { get; } =
			new Dictionary<string, IDictionary<string, object>>();

		/// <summary>
		/// Acknowledged delivery tags.
		/// </summary>
		public List<ulong> Acks { get; } = new List<ulong>();

		/// <summary>
		/// The last consumer passed to BasicConsume.
		/// </summary>
		public EventingBasicConsumer Consumer { get; private set; }

		/// <summary>
		/// Delivers a message to the consumer.
		/// </summary>
		/// <param name="deliveryTag">Delivery tag.</param>
		/// <param name="body">Message body.</param>
		/// <param name="configure">Sets the message properties.</param>
		public void Deliver(
			ulong deliveryTag,
			string body,
			Action<IBasicProperties> configure = null)
		{
			var properties = CreateProperties();
			configure?.Invoke(properties);
			Consumer.HandleBasicDeliver(
				ConsumerTag,
				deliveryTag,
				false,
				string.Empty,
				string.Empty,
				properties,
				Encoding.UTF8.GetBytes(body));
		}

		private static IBasicProperties CreateProperties()
		{
			var properties = new Mock<IBasicProperties>();
			properties.SetupAllProperties();
			return properties.Object;
		}

		private void SetupModel()
		{
			Model.Setup(x => x.IsOpen).Returns(true);
			Model.Setup(x => x.IsClosed).Returns(false);
			Model.Setup(x => x.CreateBasicProperties()).Returns(() => CreateProperties());
			Model
				.Setup(x => x.BasicPublish(
					It.IsAny<string>(),
					It.IsAny<string>(),
					It.IsAny<bool>(),
					It.IsAny<IBasicProperties>(),
					It.IsAny<ReadOnlyMemory<byte>>()))
				.Callback<string, string, bool, IBasicProperties, ReadOnlyMemory<byte>>(
					(exchange, routingKey, mandatory, properties, body) => Published.Add(
						new PublishedMessage(exchange, routingKey, properties, Encoding.UTF8.GetString(body.ToArray()))));
			Model
				.Setup(x => x.QueueDeclare(
					It.IsAny<string>(),
					It.IsAny<bool>(),
					It.IsAny<bool>(),
					It.IsAny<bool>(),
					It.IsAny<IDictionary<string, object>>()))
				.Callback<string, bool, bool, bool, IDictionary<string, object>>(
					(queue, durable, exclusive, autoDelete, arguments) => DeclaredQueues[queue] = arguments);
			Model
				.Setup(x => x.BasicConsume(
					It.IsAny<string>(),
					It.IsAny<bool>(),
					It.IsAny<string>(),
					It.IsAny<bool>(),
					It.IsAny<bool>(),
					It.IsAny<IDictionary<string, object>>(),
					It.IsAny<IBasicConsumer>()))
				.Callback<string, bool, string, bool, bool, IDictionary<string, object>, IBasicConsumer>(
					(queue, autoAck, tag, noLocal, exclusive, arguments, consumer) =>
						Consumer = (EventingBasicConsumer)consumer)
				.Returns(ConsumerTag);
			Model
				.Setup(x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()))
				.Callback<ulong, bool>((tag, multiple) => Acks.Add(tag));
		}

		private void SetupConnection()
		{
			Connection.Setup(x => x.IsConnected).Returns(true);
			Connection.Setup(x => x.CreateModel(It.IsAny<string>())).Returns(Model.Object);
			Connection
				.Setup(x => x.Execute(It.IsAny<Action<Func<IModel>>>(), It.IsAny<string>()))
				.Callback<Action<Func<IModel>>, string>((action, name) => action(() => Model.Object));
			Connection
				.Setup(x => x.Configure(It.IsAny<string>(), It.IsAny<Action<Func<IModel>>>(), It.IsAny<bool>()))
				.Callback<string, Action<Func<IModel>>, bool>((name, action, ignore) => action(() => Model.Object));
		}
	}
}
