using Repl;
using Yllibed.TenantCloudClient.Mcp.Tools;

namespace Yllibed.TenantCloudClient.Mcp;

internal sealed class TenantCloudModule(ITcClient client, EntityCache cache) : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("get", get => get.Context("user", user =>
			user.Map("info", new UserTools(client).GetUserInfo)
				.WithDescription("Get the signed-in TenantCloud user.").ReadOnly()));
		map.Context("list", list =>
		{
			list.Map("contacts", new ContactTools(client).ListContacts)
				.WithDescription("List contacts, optionally filtered by role.").ReadOnly();
			list.Map("properties", new PropertyTools(client).ListProperties)
				.WithDescription("List rental properties.").ReadOnly();
			list.Map("units", new UnitTools(client, cache).ListUnits)
				.WithDescription("List units by property or occupancy.").ReadOnly();
			list.Map("transactions", new TransactionTools(client, cache).ListTransactions)
				.WithDescription("List transactions by tenant, property, unit, status, or category.").ReadOnly();
			list.Map("leases", new LeaseTools(client, cache).ListLeases)
				.WithDescription("List leases by property, unit, or status.").ReadOnly();
		});
	}
}
