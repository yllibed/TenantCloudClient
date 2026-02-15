using AwesomeAssertions;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Tests;

[TestClass]
public class Given_TcClient : TestBase
{
	private const string TcUsername = "landlord.test.tc@gmail.com";
	private const string TcPassword = "1234Zxcv";
	private readonly InMemoryTcContext _context = new(TcUsername, TcPassword);

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GettingUserInfo()
	{
		var sut = new TcClient(_context);
		var userInfo = await sut.GetUserInfo(CancellationToken.None);

		userInfo.Should().NotBeNull();
		userInfo!.FirstName.Should().NotBeNullOrWhiteSpace();
		userInfo.LastName.Should().NotBeNullOrWhiteSpace();
		userInfo.Id.Should().NotBe(0);
	}

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GettingAllTenants()
	{
		var client = new TcClient(_context);
		var sut = client.Tenants;

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GettingMovedInTenants()
	{
		var client = new TcClient(_context);
		var sut = client.Tenants.OnlyMovedIn();

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GettingMNoLeaseTenants()
	{
		var client = new TcClient(_context);
		var sut = client.Tenants.OnlyNoLease();

		var all = await sut.GetAll(CancellationToken.None);

		// Won't check for zero on this one, since it's normal for it to be zero
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GetProperties()
	{
		var client = new TcClient(_context);
		var sut = client.Properties;

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GetUnits()
	{
		var client = new TcClient(_context);
		var sut = client.Units;

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GetTransactionsForTenant()
	{
		var client = new TcClient(_context);
		var firstTenantId = await GetFirstTenantId(client);

		var sut = client.Transactions
			.ForCategory(TcTransactionCategory.Income)
			.ForTenant(firstTenantId);
		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GetTransactionsForUnit()
	{
		var client = new TcClient(_context);
		var firstUnitId = await GetFirstUnitId(client);

		var sut = client.Transactions
			.ForCategory(TcTransactionCategory.Income)
			.ForUnit(firstUnitId);
		var all = await sut.GetAll(CancellationToken.None);

		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GetExpenseTransactions()
	{
		var client = new TcClient(_context);

		var sut = client.Transactions
			.ForCategory(TcTransactionCategory.Expense);
		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	[Ignore("Requires live TenantCloud API")]
	public async Task When_GetBalancePerProperty()
	{
		var client = new TcClient(_context);

		var all = (await client.Transactions
				.ForCategory(TcTransactionCategory.Income)
				.ForStatus(TcTransactionStatus.WithBalance)
				.GetAll(CancellationToken.None))
			.AsEnumerable()
			.Where(t => t.PropertyId != null)
			.GroupBy(t => (long)t.PropertyId!, t => t.Balance)
			.Select(g => (propertyId: g.Key, balance: g.Sum()))
			.ToArray();

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.propertyId).Should().OnlyHaveUniqueItems();
	}

	private static async Task<long> GetFirstTenantId(TcClient client)
	{
		var tenants = await client.Tenants.OnlyMovedIn().GetAll(CancellationToken.None, 1).ConfigureAwait(false);
		return tenants.AsEnumerable().First().Id;
	}

	private static async Task<long> GetFirstUnitId(TcClient client)
	{
		var units = await client.Units.OnlyOccuped().GetAll(CancellationToken.None, 1).ConfigureAwait(false);
		return units.AsEnumerable().First().Id;
	}
}
