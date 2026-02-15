using Microsoft.Extensions.DependencyInjection;

namespace Yllibed.TenantCloudClient;

/// <summary>
/// Extension methods for registering TenantCloud services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class TenantCloudServiceCollectionExtensions
{
	/// <summary>
	/// Registers <see cref="ITcClient"/> (implemented by <see cref="TcClient"/>).
	/// An <see cref="ITcAuthTokenProvider"/> must already be registered in the container.
	/// </summary>
	public static IServiceCollection AddTenantCloudClient(this IServiceCollection services)
	{
		services.AddSingleton<ITcClient, TcClient>();
		return services;
	}

	/// <summary>
	/// Registers <see cref="ITcTokenStore"/> using the OS-native <see cref="SecureTokenStore"/>
	/// (DPAPI on Windows, Keychain on macOS, Secret Service on Linux).
	/// </summary>
	public static IServiceCollection AddSecureTokenStore(
		this IServiceCollection services,
		Action<SecureTokenStoreOptions>? configure = null)
	{
		services.AddSingleton<ITcTokenStore>(sp =>
		{
			var options = new SecureTokenStoreOptions();
			configure?.Invoke(options);
			return new SecureTokenStore(options);
		});
		return services;
	}
}
