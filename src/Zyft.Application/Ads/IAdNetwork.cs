namespace Zyft.Application.Ads;

/// <summary>
/// Rede de anúncios que preenche os espaços sem anúncio direto (ADR-0021). Hoje é o Google AdSense <b>simulado</b>:
/// nenhuma chamada sai para o Google; a interface desenha um bloco no formato do AdSense.
/// </summary>
public interface IAdNetwork
{
    /// <summary>Bloco da rede para o espaço, ou null quando a rede está desligada ou não atende o espaço.</summary>
    AdNetworkSlot? SlotFor(string placement);

    /// <summary>Espaços em que a rede vem antes do anúncio direto (banners padrão de rede).</summary>
    bool Prefers(string placement);
}

/// <summary>Dados que o cliente usa para montar o <c>&lt;ins class="adsbygoogle"&gt;</c>. <c>Mock</c> = não carregar o script do Google.</summary>
public sealed record AdNetworkSlot(string Provider, string Client, string Slot, string AdFormat, bool Mock)
{
    /// <summary>Id usado em "Ocultar"; não corresponde a nenhum anúncio do catálogo.</summary>
    public string AdId => $"adsense-{Slot}";
}
