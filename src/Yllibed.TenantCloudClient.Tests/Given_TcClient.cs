using AwesomeAssertions;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Tests;

[TestClass]
public class Given_TcClient : TestBase
{
	[TestMethod]
	public async Task When_GettingUserInfo()
	{
		var sut = new TcClient(TokenProvider);
		var userInfo = await sut.GetUserInfo(CancellationToken.None);

		userInfo.Should().NotBeNull();
		userInfo!.FirstName.Should().NotBeNullOrWhiteSpace();
		userInfo.LastName.Should().NotBeNullOrWhiteSpace();
		userInfo.Id.Should().NotBe(0);
	}

	[TestMethod]
	public async Task When_GettingAllContacts()
	{
		var client = new TcClient(TokenProvider);
		var sut = client.Contacts;

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	public async Task When_GettingMovedInContacts()
	{
		var client = new TcClient(TokenProvider);
		var sut = client.Contacts.OnlyMovedIn();

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	public async Task When_GetProperties()
	{
		var client = new TcClient(TokenProvider);
		var sut = client.Properties;

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	public async Task When_GetUnits()
	{
		var client = new TcClient(TokenProvider);
		var sut = client.Units;

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	public async Task When_GetTransactionsForTenant()
	{
		var client = new TcClient(TokenProvider);
		var firstContactId = await GetFirstContactId(client);

		var sut = client.Transactions
			.ForCategory(TcTransactionCategory.Income)
			.ForTenant(firstContactId);
		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	public async Task When_GetTransactionsForUnit()
	{
		var client = new TcClient(TokenProvider);
		var firstUnitId = await GetFirstUnitId(client);

		var sut = client.Transactions
			.ForCategory(TcTransactionCategory.Income)
			.ForUnit(firstUnitId);
		var all = await sut.GetAll(CancellationToken.None);

		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	public async Task When_GetExpenseTransactions()
	{
		var client = new TcClient(TokenProvider);

		var sut = client.Transactions
			.ForCategory(TcTransactionCategory.Expense);
		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	public async Task When_GetBalancePerProperty()
	{
		var client = new TcClient(TokenProvider);

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

	[TestMethod]
	public async Task When_GetLeases()
	{
		var client = new TcClient(TokenProvider);
		var sut = client.Leases;

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	public async Task When_GetActiveLeases()
	{
		var client = new TcClient(TokenProvider);
		var sut = client.Leases.OnlyActive();

		var all = await sut.GetAll(CancellationToken.None);

		all.Length.Should().NotBe(0);
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	[TestMethod]
	public async Task When_GetLeasesForProperty()
	{
		var client = new TcClient(TokenProvider);
		var firstPropertyId = await GetFirstPropertyId(client);

		var sut = client.Leases.ForProperty(firstPropertyId);

		var all = await sut.GetAll(CancellationToken.None);

		// May be empty if no leases for that property, just verify unique IDs
		all.AsEnumerable().Select(x => x.Id).Should().OnlyHaveUniqueItems();
	}

	private static async Task<long> GetFirstContactId(TcClient client)
	{
		var contacts = await client.Contacts.OnlyMovedIn().GetAll(CancellationToken.None, 1).ConfigureAwait(false);
		return contacts.AsEnumerable().First().Id;
	}

	private static async Task<long> GetFirstUnitId(TcClient client)
	{
		var units = await client.Units.OnlyOccuped().GetAll(CancellationToken.None, 1).ConfigureAwait(false);
		return units.AsEnumerable().First().Id;
	}

	private static async Task<long> GetFirstPropertyId(TcClient client)
	{
		var properties = await client.Properties.GetAll(CancellationToken.None, 1).ConfigureAwait(false);
		return properties.AsEnumerable().First().Id;
	}
}
