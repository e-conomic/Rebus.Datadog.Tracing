using System.Collections.Generic;
using Datadog.Trace;
using Moq;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Xunit;

namespace Rebus.Datadog.Tracing.Tests
{
	public class RebusSetTracingHeadersStepTests : TestsBase
	{
		public Mock<ILog> _logger { get; set; }
		const string messageId = "messageId";
		readonly DestinationAddresses _destinationAddresses;
		public RebusSetTracingHeadersStep _underTest { get; set; }

		public RebusSetTracingHeadersStepTests()
		{
			var rebusLoggerFactory = new Mock<IRebusLoggerFactory>();
			_logger = new Mock<ILog>();
			rebusLoggerFactory.Setup(o => o.GetLogger<RebusSetTracingHeadersStep>()).Returns(_logger.Object);
			_underTest = new RebusSetTracingHeadersStep(rebusLoggerFactory.Object);

			_nbrOfCalls = 0;
			_destinationAddresses = new DestinationAddresses(new List<string>());
		}

		[Fact]
		public async void ShouldPopulateTraceIdWhenItDoesNotExist_WhenCallingProcess()
		{
			var headers = new Dictionary<string, string>()
			{
				{"rbs2-msg-id", messageId }
			};
			var transportMessage = new Message(headers, new byte[10]);
			var incomingStepContext = new OutgoingStepContext(transportMessage, CreateTransactionContext(), _destinationAddresses);

			await _underTest.Process(incomingStepContext, Next);

			Assert.Equal(1, _nbrOfCalls);
			Assert.NotNull(headers[HttpHeaderNames.TraceId]);
			Assert.NotNull(headers[HttpHeaderNames.ParentId]);
		}

		[Fact]
		public async void ShouldNotOverrideTraceIdWhenItExists_WhenCallingProcess()
		{
			var traceId = "testTraceId";

			var headers = new Dictionary<string, string>()
			{
				{"rbs2-msg-id", messageId },
				{"x-datadog-trace-id", traceId }
			};
			var transportMessage = new Message(headers, new byte[10]);
			var incomingStepContext = new OutgoingStepContext(transportMessage, CreateTransactionContext(), _destinationAddresses);

			await _underTest.Process(incomingStepContext, Next);

			Assert.Equal(1, _nbrOfCalls);
			Assert.NotNull(headers[HttpHeaderNames.TraceId]);
			Assert.NotNull(headers[HttpHeaderNames.ParentId]);
			Assert.Equal(traceId, headers[HttpHeaderNames.TraceId]);
		}
	}
}
