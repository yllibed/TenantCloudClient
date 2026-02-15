using Microsoft.Extensions.DependencyInjection;

namespace Yllibed.TenantCloudClient.Cdp;

/// <summary>
/// Extension methods for registering CDP-based TenantCloud services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class CdpServiceCollectionExtensions
{
	/// <summary>
	/// Registers <see cref="ITcAuthTokenProvider"/> using the CDP-based <see cref="CdpTokenProvider"/>
	/// that extracts tokens from a running Chromium browser session.
	/// If an <see cref="ITcTokenStore"/> is registered, it will be used for token persistence automatically.
	/// </summary>
	public static IServiceCollection AddCdpTokenProvider(
		this IServiceCollection services,
		Action<CdpTokenProviderOptions>? configure = null)
	{
		services.AddSingleton<ITcAuthTokenProvider>(sp =>
		{
			var options = new CdpTokenProviderOptions
			{
				TokenStore = sp.GetService<ITcTokenStore>(),
			};
			configure?.Invoke(options);
			return new CdpTokenProvider(options);
		});
		return services;
	}
}
