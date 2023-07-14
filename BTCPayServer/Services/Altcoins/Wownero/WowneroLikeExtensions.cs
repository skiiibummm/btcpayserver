#if ALTCOINS
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Configuration;
using BTCPayServer.Payments;
using BTCPayServer.Services.Altcoins.Wownero.Configuration;
using BTCPayServer.Services.Altcoins.Wownero.Payments;
using BTCPayServer.Services.Altcoins.Wownero.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Services.Altcoins.Wownero
{
    public static class WowneroLikeExtensions
    {
        public static IServiceCollection AddWowneroLike(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(provider =>
                provider.ConfigureWowneroLikeConfiguration());
            serviceCollection.AddHttpClient("WOWclient")
                .ConfigurePrimaryHttpMessageHandler(provider =>
                {
                    var configuration = provider.GetRequiredService<WowneroLikeConfiguration>();
                    if(!configuration.WowneroLikeConfigurationItems.TryGetValue("WOW", out var wowConfig) || wowConfig.Username is null || wowConfig.Password is null){
                        return new HttpClientHandler();
                    }
                    return new HttpClientHandler
                    {
                        Credentials = new NetworkCredential(wowConfig.Username, wowConfig.Password),
                        PreAuthenticate = true
                    };
                });
            serviceCollection.AddSingleton<WowneroRPCProvider>();
            serviceCollection.AddHostedService<WowneroLikeSummaryUpdaterHostedService>();
            serviceCollection.AddHostedService<WowneroListener>();
            serviceCollection.AddSingleton<WowneroLikePaymentMethodHandler>();
            serviceCollection.AddSingleton<IPaymentMethodHandler>(provider => provider.GetService<WowneroLikePaymentMethodHandler>());
            serviceCollection.AddSingleton<IUIExtension>(new UIExtension("Wownero/StoreNavWowneroExtension",  "store-nav"));
            serviceCollection.AddSingleton<IUIExtension>(new UIExtension("Wownero/StoreWalletsNavWowneroExtension",  "store-wallets-nav"));
            serviceCollection.AddSingleton<ISyncSummaryProvider, WowneroSyncSummaryProvider>();

            return serviceCollection;
        }

        private static WowneroLikeConfiguration ConfigureWowneroLikeConfiguration(this IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var btcPayNetworkProvider = serviceProvider.GetService<BTCPayNetworkProvider>();
            var result = new WowneroLikeConfiguration();

            var supportedChains = configuration.GetOrDefault<string>("chains", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToUpperInvariant());

            var supportedNetworks = btcPayNetworkProvider.Filter(supportedChains.ToArray()).GetAll()
                .OfType<WowneroLikeSpecificBtcPayNetwork>();

            foreach (var wowneroLikeSpecificBtcPayNetwork in supportedNetworks)
            {
                var daemonUri =
                    configuration.GetOrDefault<Uri>($"{wowneroLikeSpecificBtcPayNetwork.CryptoCode}_daemon_uri",
                        null);
                var walletDaemonUri =
                    configuration.GetOrDefault<Uri>(
                        $"{wowneroLikeSpecificBtcPayNetwork.CryptoCode}_wallet_daemon_uri", null);
                var walletDaemonWalletDirectory =
                    configuration.GetOrDefault<string>(
                        $"{wowneroLikeSpecificBtcPayNetwork.CryptoCode}_wallet_daemon_walletdir", null);
                var daemonUsername =
                    configuration.GetOrDefault<string>(
                        $"{wowneroLikeSpecificBtcPayNetwork.CryptoCode}_daemon_username", null);
                var daemonPassword =
                    configuration.GetOrDefault<string>(
                        $"{wowneroLikeSpecificBtcPayNetwork.CryptoCode}_daemon_password", null);
                if (daemonUri == null || walletDaemonUri == null)
                {
                    throw new ConfigException($"{wowneroLikeSpecificBtcPayNetwork.CryptoCode} is misconfigured");
                }

                result.WowneroLikeConfigurationItems.Add(wowneroLikeSpecificBtcPayNetwork.CryptoCode, new WowneroLikeConfigurationItem()
                {
                    DaemonRpcUri = daemonUri,
                    Username = daemonUsername,
                    Password = daemonPassword,
                    InternalWalletRpcUri = walletDaemonUri,
                    WalletDirectory = walletDaemonWalletDirectory
                });
            }
            return result;
        }
    }
}
#endif
