using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Tests
{
	internal class TestTcContext : ITcContext
	{
		private string _token;

		public Task<NetworkCredential> GetCredentials(CancellationToken ct)
		{
			var credentials = new NetworkCredential("user@domain.tld", "password");

			return Task.FromResult(credentials);
		}

		public async Task SetAuthToken(CancellationToken ct, string token)
		{
			_token = token;
		}

		public async Task<string> GetAuthToken(CancellationToken ct)
		{
			return _token;
		}
	}

	[TestClass]
	public class Given_TcClient : TestBase
	{
		private readonly TestTcContext _context = new TestTcContext();

		[TestMethod]
		public async Task When_GettingUserInfo()
		{
			var sut = new TcClient(_context);
			var userInfo = await sut.GetUserInfo(CancellationToken.None);

			using var _ = new AssertionScope();

			userInfo.Should().NotBeNull();
			userInfo.FirstName.Should().NotBeNullOrWhiteSpace();
			userInfo.LastName.Should().NotBeNullOrWhiteSpace();
			userInfo.Id.Should().NotBe(0);
		}

		[TestMethod]
		public async Task When_GettingAllTenants()
		{
			var client = new TcClient(_context);
			var sut = client.Tenants;

			var all = await sut.GetAll(CancellationToken.None);

			using var _ = new AssertionScope();

			all.Should().NotBeNull();
			all.Length.Should().NotBe(0);
			all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
		}

		[TestMethod]
		public async Task When_GettingMovedInTenants()
		{
			var client = new TcClient(_context);
			var sut = client.Tenants.OnlyMovedIn();

			var all = await sut.GetAll(CancellationToken.None);

			using var _ = new AssertionScope();

			all.Should().NotBeNull();
			all.Length.Should().NotBe(0);
			all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
		}

		[TestMethod]
		public async Task When_GettingMNoLeaseTenants()
		{
			var client = new TcClient(_context);
			var sut = client.Tenants.OnlyNoLease();

			var all = await sut.GetAll(CancellationToken.None);

			using var _ = new AssertionScope();

			all.Should().NotBeNull();
			// Won't check for zero on this one, since it's normal for it to be zero
			all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
		}

		[TestMethod]
		public async Task When_GetProperties()
		{
			var client = new TcClient(_context);
			var sut = client.Properties;

			var all = await sut.GetAll(CancellationToken.None);

			all.Should().NotBeNull();
			all.Length.Should().NotBe(0);
			all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
		}

		[TestMethod]
		public async Task When_GetUnits()
		{
			var client = new TcClient(_context);
			var sut = client.Units;

			var all = await sut.GetAll(CancellationToken.None);

			all.Should().NotBeNull();
			all.Length.Should().NotBe(0);
			all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
		}

		[TestMethod]
		public async Task When_GetTransactionsForTenant()
		{
			var client = new TcClient(_context);
			var firstTenantId = await GetFirstTenantId(client);

			var sut = client.Transactions
				.ForCategory(TcTransactionCategory.Income)
				.ForTenant(firstTenantId);
			var all = await sut.GetAll(CancellationToken.None);

			all.Should().NotBeNull();
			all.Length.Should().NotBe(0);
			all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();

			Console.WriteLine($"There is {all.Length} income transactions for tenant {firstTenantId}.");
		}

		[TestMethod]
		public async Task When_GetTransactionsForUnit()
		{
			var client = new TcClient(_context);
			var firstUnitId = await GetFirstUnitId(client);

			var sut = client.Transactions
				.ForCategory(TcTransactionCategory.Income)
				.ForUnit(firstUnitId);
			var all = await sut.GetAll(CancellationToken.None);

			all.Should().NotBeNull();
			all.Length.Should().NotBe(0);
			all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();

			Console.WriteLine($"There is {all.Length} income transactions for unit {firstUnitId}.");
		}

		[TestMethod]
		public async Task When_GetExpenseTransactions()
		{
			var client = new TcClient(_context);

			var sut = client.Transactions
				.ForCategory(TcTransactionCategory.Expense);
			var all = await sut.GetAll(CancellationToken.None);

			all.Should().NotBeNull();
			all.Length.Should().NotBe(0);
			all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();

			Console.WriteLine($"There is {all.Length} expense transactions.");
		}

		[TestMethod]
		public async Task When_GetBalancePerProperty()
		{
			var client = new TcClient(_context);

			var all = (await client.Transactions
					.ForCategory(TcTransactionCategory.Income)
					.ForStatus(TcTransactionStatus.WithBalance)
					.GetAll(CancellationToken.None))
				.AsEnumerable()
				.Where(t => t.PropertyId != null) // only property-specific income
				.GroupBy(t => (long)t.PropertyId, t => t.Balance) // group them
				.Select(g => (propertyId: g.Key, balance: g.Sum())) // summarize
				.ToArray(); // create final array

			all.Should().NotBeNull();
			all.Length.Should().NotBe(0);
			all.AsEnumerable().Select(x => x.propertyId).Should().OnlyHaveUniqueItems();

		}

		private static async Task<long> GetFirstTenantId(TcClient client)
		{
			var tenants = await client.Tenants.OnlyMovedIn().GetAll(CancellationToken.None, 1);
			var firstTenantId = tenants.Slice(0, 1).ToArray()[0].Id;
			return firstTenantId;
		}

		private static async Task<long> GetFirstUnitId(TcClient client)
		{
			var units = await client.Units.OnlyOccuped().GetAll(CancellationToken.None, 1);
			var firstUnitId = units.Slice(0, 1).ToArray()[0].Id;
			return firstUnitId;
		}
	}
}
