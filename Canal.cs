using System.Globalization;

namespace CapacidadeCanais;

/*
 Modelos de capacidade de canal (Nyquist e Shannon).
 Todas as larguras de banda em Hz e todas as capacidades em bit/s.
*/
public static class Canal
{
    //Nyquist (canal ideal, sem ruído): C = 2B log2(N).
    public static double Nyquist(double larguraBandaHz, int niveis)
    {
        if (larguraBandaHz <= 0)
            throw new ArgumentOutOfRangeException(nameof(larguraBandaHz), "A largura de banda B deve ser maior que zero.");
        if (niveis < 2)
            throw new ArgumentOutOfRangeException(nameof(niveis), "O número de níveis N deve ser pelo menos 2.");

        return 2.0 * larguraBandaHz * Math.Log2(niveis);
    }

    //Shannon (canal com ruído AWGN): C = B log2(1 + SNR), SNR em escala linear
    public static double Shannon(double larguraBandaHz, double snrLinear)
    {
        if (larguraBandaHz <= 0)
            throw new ArgumentOutOfRangeException(nameof(larguraBandaHz), "A largura de banda B deve ser maior que zero.");
        if (snrLinear < 0)
            throw new ArgumentOutOfRangeException(nameof(snrLinear), "A SNR linear não pode ser negativa.");

        return larguraBandaHz * Math.Log2(1.0 + snrLinear);
    }

    //SNR = 10^(SNR_dB / 10)
    public static double DbParaLinear(double snrDb) => Math.Pow(10.0, snrDb / 10.0);

    //SNR_dB = 10 log10(SNR)
    public static double LinearParaDb(double snrLinear) => 10.0 * Math.Log10(snrLinear);

    //Maior N que ainda respeita Shannon: 2B log2(N) &lt;= B log2(1+SNR)  =>  N &lt;= sqrt(1+SNR)
    public static double NiveisMaximosPorShannon(double snrLinear) => Math.Sqrt(1.0 + snrLinear);
}

//Formatação de valores com unidades
public static class Fmt
{
    public static readonly CultureInfo Br = new("pt-BR");

    public static string Num(double v, int casas = 2) => v.ToString("N" + casas, Br);

    public static string Taxa(double bps)
    {
        if (Math.Abs(bps) >= 1e6) return $"{(bps / 1e6).ToString("N3", Br)} Mbit/s";
        if (Math.Abs(bps) >= 1e3) return $"{(bps / 1e3).ToString("N2", Br)} kbit/s";
        return $"{bps.ToString("N2", Br)} bit/s";
    }

    public static string Hz(double hz)
    {
        if (hz >= 1e6) return $"{(hz / 1e6).ToString("0.###", Br)} MHz";
        if (hz >= 1e3) return $"{(hz / 1e3).ToString("0.###", Br)} kHz";
        return $"{hz.ToString("0.###", Br)} Hz";
    }

    //Lê números aceitando vírgula ou ponto decimal e sufixos k/M.
    public static double LerNumero(string texto)
    {
        string t = texto.Trim().Replace(',', '.');
        double mult = 1;
        if (t.EndsWith("k", StringComparison.OrdinalIgnoreCase)) { mult = 1e3; t = t[..^1]; }
        else if (t.EndsWith("M")) { mult = 1e6; t = t[..^1]; }

        if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
            throw new FormatException($"Valor numérico inválido: \"{texto}\".");
        return v * mult;
    }
}
