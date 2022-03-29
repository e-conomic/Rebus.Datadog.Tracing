using System.Threading.Tasks;
using Rebus.Transport;

namespace Rebus.Datadog.Tracing.Tests
{
	public class TestsBase
	{
		public int _nbrOfCalls { get; set; }

		protected static ITransactionContext CreateTransactionContext()
		{
			var scope = new RebusTransactionScope();

			var transactionContext = scope.TransactionContext;
			return transactionContext;
		}

		protected async Task Next()
		{
			_nbrOfCalls++;
			await Task.CompletedTask;
		}
	}
}
