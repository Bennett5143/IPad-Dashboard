using Dashboard.Domain.Common;

namespace Dashboard.Domain.Crypto;

/// <summary>Beobachtbarer Zwischenspeicher der zuletzt abgerufenen Krypto-Watchlist.</summary>
public sealed class CryptoState : ObservableState<CryptoSnapshot>;
