using System;
using System.Threading.Tasks;
using Datadog.Trace;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Datadog.Tracing
{
	[StepDocumentation("Connects distributed span from the sender to the receiver so we have 1 trace across services.")]
	public class RebusLinkTracingHeaderStep : IIncomingStep
	{
		private const int SpanIdLength = 19;

		private readonly ILog _logger; 

		public RebusLinkTracingHeaderStep(IRebusLoggerFactory rebusLoggerFactory)
		{
			_logger = rebusLoggerFactory?.GetLogger<RebusLinkTracingHeaderStep>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
		}

		public async Task Process(IncomingStepContext context, Func<Task> next)
		{
			var message = context.Load<TransportMessage>();

			_logger.Debug($"Fetching incoming headers of message type {message?.GetMessageType()} with message id {message?.GetMessageId()}.");

			if (message == null)
			{
				_logger.Debug($"Message was null. Skipping creating a custom trace for this message");

				await next().ConfigureAwait(false);
				return;
			}

			ulong? traceId = GetTraceId(message);

			if (traceId == null)
			{
				_logger.Debug($"MessageId {message?.GetMessageId()} hasn't TraceId header set. Skipping creating a custom trace for this message");

				await next().ConfigureAwait(false);
				return;
			}

			var spanSettings = GenerateSpanContext(message, traceId);

			using (var scope = Tracer.Instance.StartActive("DatadogTracingWrapperStep.HandlerWrapper", spanSettings))
			{
				_logger.Debug($"MessageId {message?.GetMessageId()} has an incoming traceId header: {traceId} so a custom trace will be created.");

				await next().ConfigureAwait(false);
			}
		}

		private SpanCreationSettings GenerateSpanContext(TransportMessage message, ulong? traceId)
		{
			ulong spanId = GetSpanId(message);
			var samplingPriority = GetSamplingPriority(message);

			return new SpanCreationSettings()
			{
				FinishOnClose = true,
				Parent = new SpanContext(traceId, spanId, samplingPriority),
			};
		}

		private ulong GetSpanId(TransportMessage message)
		{
			bool parentSpanIdExists = message.Headers.TryGetValue(HttpHeaderNames.ParentId, out string parentSpanId);
			if (parentSpanIdExists && ulong.TryParse(parentSpanId, out ulong parentSpanIdLong))
			{
				return parentSpanIdLong;
			}

			_logger.Debug($"MessageId {message?.GetMessageId()} doesn't have a parent Span Id so we will generate one.");

			return GenerateSpanId(SpanIdLength);
		}

		private static ulong? GetTraceId(TransportMessage message)
		{
			bool traceIdExists = message.Headers.TryGetValue(HttpHeaderNames.TraceId, out string traceId);
			if (traceIdExists && ulong.TryParse(traceId, out ulong traceIdLong))
			{
				return traceIdLong;
			}

			return null;
		}

		private static SamplingPriority? GetSamplingPriority(TransportMessage message)
		{
			bool samplingPriorityExists = message.Headers.TryGetValue(HttpHeaderNames.SamplingPriority, out string samplingPriority);
			if (samplingPriorityExists && Enum.TryParse(samplingPriority, out SamplingPriority samplingPriorityType))
			{
				return samplingPriorityType;
			}

			return null;
		}

		private static ulong GenerateSpanId(int length)
		{
			var random = new Random();
			string spanId = string.Empty;
			for (int i = 0; i < length; i++)
				spanId = string.Concat(spanId, random.Next(10).ToString());
			return ulong.Parse(spanId);
		}
	}
}
