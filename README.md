# Rebus.Datadog.Tracing

## Description

A .Net Standard library containing two Rebus Steps that are used to be added in the Rebus pipeline as middlewares.

The two steps are:
- `RebusSetTracingHeadersStep` is an outgoing step that is used in the message producers to set the outgoing headers of the Rebus message. The headers are the 3 important headers used by Datadog <br/>
 
 TraceId | SpanId (ParentId) | SamplingPriority
| :---: | :---: | :---:
x-datadog-trace-id  | x-datadog-parent-id | x-datadog-sampling-priority


- `RebusLinkTracingHeaderStep` is an incoming step used to fetch the headers set by the first step (`RebusSetTracingHeadersStep`) and creates a new trace using the 3 headers. If the TraceId is missing, the trace will no be created, if the other 2 headers are missing, the trace will still be created with the existing TraceId and a newly generated spanId, while SamplingPriority is set to null.


⚠️ The current library only sets up tracing using `Assembly Datadog.Trace` version `2.2.0`. Note that when you use it, the Datadog agent installed for your machine/container has to match this version.
If this library ends up to be useful, we will create one version for each coresponding Datadog agent version and package.

## Usage example

### Producer

```
.Options(o => o.Decorate<IPipeline>(c =>
			{
				var pipeline = c.Get<IPipeline>();
				var step = new RebusSetTracingHeadersStep(c.Get<IRebusLoggerFactory>());
				return new PipelineStepInjector(pipeline)
					.OnSend(step, PipelineRelativePosition.After, typeof(AssignDefaultHeadersStep));
			});
```

### Consumer
```
.Options(o => o..Decorate<IPipeline>(c =>
			{
				var pipeline = c.Get<IPipeline>();
				var step = new RebusLinkTracingHeaderStep(c.Get<IRebusLoggerFactory>());
				return new PipelineStepInjector(pipeline)
					.OnReceive(step, PipelineRelativePosition.Before, typeof(DispatchIncomingMessageStep));
			});
```
