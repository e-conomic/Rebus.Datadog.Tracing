using System.Collections.Generic;
using Datadog.Trace;
using Moq;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Xunit;

namespace Rebus.Datadog.Tracing.Tests
{
	public class RebusLinkTracingHeaderStepTests : TestsBase
	{
		public Mock<ILog> _logger { get; set; }
		const string messageId = "messageId";
		const string traceId = "1234567890987654321";
		const string spanId = "9234567890987654321";
		public RebusLinkTracingHeaderStep _underTest { get; set; }

		public RebusLinkTracingHeaderStepTests()
		{
			var rebusLoggerFactory = new Mock<IRebusLoggerFactory>();
			_logger = new Mock<ILog>();
			rebusLoggerFactory.Setup(o => o.GetLogger<RebusLinkTracingHeaderStep>()).Returns(_logger.Object);
			_underTest = new RebusLinkTracingHeaderStep(rebusLoggerFactory.Object);

			_nbrOfCalls = 0;
		}

		[Fact]
		public async void ShouldNotLinkHeaders_WhenTraceIdIsNull()
		{
			var headers = new Dictionary<string, string>()
			{
				{"rbs2-msg-id", messageId }
			};
			var transportMessage = new TransportMessage(headers, new byte[10]);
			var incomingStepContext = new IncomingStepContext(transportMessage, CreateTransactionContext());

			await _underTest.Process(incomingStepContext, Next);

			Assert.Equal(1, _nbrOfCalls);
			_logger.Verify(o => o.Debug($"MessageId {messageId} hasn't TraceId header set. Skipping creating a custom trace for this message"), Times.Once);
		}

		[Fact]
		public async void ShouldLinkHeaders_WhenTraceIdIsNotNullButSpanIdIsNull()
		{
			var headers = new Dictionary<string, string>()
			{
				{"rbs2-msg-id", messageId },
				{ HttpHeaderNames.TraceId, traceId }
			};
			var transportMessage = new TransportMessage(headers, new byte[10]);
			var incomingStepContext = new IncomingStepContext(transportMessage, CreateTransactionContext());

			await _underTest.Process(incomingStepContext, Next);

			Assert.Equal(1, _nbrOfCalls);
			_logger.Verify(o => o.Debug($"MessageId {messageId} hasn't TraceId header set. Skipping creating a custom trace for this message"), Times.Never);
			_logger.Verify(o => o.Debug($"MessageId {messageId} has an incoming traceId header: {traceId} so a custom trace will be created."), Times.Once);
			_logger.Verify(o => o.Debug($"MessageId {messageId} doesn't have a parent Span Id so we will generate one."), Times.Once);
		}

		[Fact]
		public async void ShouldLinkHeaders_WhenTraceIdAndSpanIdIsSet()
		{
			var headers = new Dictionary<string, string>()
			{
				{"rbs2-msg-id", messageId },
				{ HttpHeaderNames.TraceId, traceId },
				{ HttpHeaderNames.ParentId, spanId }
			};
			var transportMessage = new TransportMessage(headers, new byte[10]);
			var incomingStepContext = new IncomingStepContext(transportMessage, CreateTransactionContext());

			await _underTest.Process(incomingStepContext, Next);

			Assert.Equal(1, _nbrOfCalls);
			_logger.Verify(o => o.Debug($"MessageId {messageId} hasn't TraceId header set. Skipping creating a custom trace for this message"), Times.Never);
			_logger.Verify(o => o.Debug($"MessageId {messageId} has an incoming traceId header: {traceId} so a custom trace will be created."), Times.Once);
			_logger.Verify(o => o.Debug($"MessageId {messageId} doesn't have a parent Span Id so we will generate one."), Times.Never);
		}
	}
}
