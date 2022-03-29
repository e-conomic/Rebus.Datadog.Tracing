using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Datadog.Trace;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Datadog.Tracing
{
	[StepDocumentation("Adds datadog headers to outgoing messages")]
	public class RebusSetTracingHeadersStep : IOutgoingStep
	{
		private readonly ILog _logger;

		public RebusSetTracingHeadersStep(IRebusLoggerFactory rebusLoggerFactory)
		{
			_logger = rebusLoggerFactory?.GetLogger<RebusSetTracingHeadersStep>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
		}

		public async Task Process(OutgoingStepContext context, Func<Task> next)
		{
			using (var scope = Tracer.Instance.StartActive("RebusTracingHeaderStep.Process"))
			{
				Message message = context?.Load<Message>();
				var headers = message?.Headers;
				var activeSpan = scope.Span;

				_logger.Debug($"Populating outgoing headers of message type {message?.GetMessageType()} with id {message?.GetMessageId()}");

				if (headers != null)
				{
					SetTraceId(headers, activeSpan);
					headers[HttpHeaderNames.ParentId] = activeSpan.SpanId.ToString();
					headers[HttpHeaderNames.SamplingPriority] = activeSpan.GetTag(Tags.SamplingPriority);

					_logger.Debug($"MessageId {message?.GetMessageId()} has traceId set to {activeSpan.TraceId}");
				}

				if (next != null)
				{
					await next().ConfigureAwait(false);
				}
			}
		}

		private static void SetTraceId(Dictionary<string, string> headers, ISpan activeSpan)
		{
			bool traceIdExists = headers.TryGetValue(HttpHeaderNames.TraceId, out string existingTraceId);

			if (!traceIdExists || string.IsNullOrWhiteSpace(existingTraceId))
			{
				headers[HttpHeaderNames.TraceId] = activeSpan.TraceId.ToString();
			}
		}
	}
}
