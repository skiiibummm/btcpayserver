using NBitcoin;

namespace BTCPayServer
{
    public partial class BTCPayNetworkProvider
    {
        public void InitWownero()
        {
            Add(new WowneroLikeSpecificBtcPayNetwork()
            {
                CryptoCode = "WOW",
                DisplayName = "Wownero",
                Divisibility = 12,
                BlockExplorerLink =
                    NetworkType == ChainName.Mainnet
                        ? "https://muchwow.lol/tx?id={0}"
                DefaultRateRules = new[]
                {
                    "WOW_X = WOW_BTC * BTC_X",
                    "WOW_BTC = CoinGecko(WOW_BTC)"
                },
                CryptoImagePath = "/imlegacy/wownero.svg",
                UriScheme = "wownero"
            });
        }
    }
}
