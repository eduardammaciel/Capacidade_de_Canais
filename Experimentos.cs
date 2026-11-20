namespace CapacidadeCanais;

public static class Experimentos
{
public static void ExecutarTodos(string pasta)
{
    var rel = new Relatorio(pasta);
    Experimento1(rel);
    Experimento2(rel);
    Experimento3(rel);
    Experimento4(rel);
}

    /*------------------------------------------------------------------
       Experimento 1 – Nyquist: B = 1, 5, 10 kHz; N = 2, 4, 8, 16
      ------------------------------------------------------------------
    */
    public static void Experimento1(Relatorio rel)
    {
        rel.Secao("Experimento 1 – Capacidade de Nyquist  C = 2B·log2(N)");

        double[] bandas = { 1e3, 5e3, 10e3 };
        int[] niveis = { 2, 4, 8, 16 };

        var cab = new List<string> { "N (níveis)", "bits/símbolo" };
        cab.AddRange(bandas.Select(b => $"B = {Fmt.Hz(b)}"));
        var t = new Tabela("Tabela 1 – Capacidade de Nyquist", cab.ToArray());

        foreach (int n in niveis)
        {
            var linha = new List<string> { n.ToString(), Fmt.Num(Math.Log2(n), 0) };
            linha.AddRange(bandas.Select(b => Fmt.Taxa(Canal.Nyquist(b, n))));
            t.Adicionar(linha.ToArray());
        }
        rel.AdicionarTabela(t);

        var g = new GraficoSvg
        {
            Titulo = "Experimento 1 – Capacidade de Nyquist (C × N)",
            Subtitulo = "C = 2B·log2(N), canal ideal sem ruído",
            RotuloX = "Número de níveis de sinalização (N)",
            RotuloY = "Capacidade C (kbit/s)",
            TicksX = niveis.Select(n => (double)n).ToArray()
        };
        for (int i = 0; i < bandas.Length; i++)
        {
            double b = bandas[i];
            g.Series.Add(new Serie
            {
                Nome = $"B = {Fmt.Hz(b)}",
                X = niveis.Select(n => (double)n).ToArray(),
                Y = niveis.Select(n => Canal.Nyquist(b, n) / 1e3).ToArray(),
                Cor = GraficoSvg.Paleta[i],
                Rotulos = true
            });
        }
        rel.Grafico(g, "exp1_nyquist_C_vs_N.svg");
    }

    /*------------------------------------------------------------------
       Experimento 2 – Shannon: SNR = 1000, 5000, 10000, 50000; B = 1..10 kHz
      ------------------------------------------------------------------
    */
    public static void Experimento2(Relatorio rel)
    {
        rel.Secao("Experimento 2 – Capacidade de Shannon  C = B·log2(1 + SNR)");

        double[] snrs = { 1_000, 5_000, 10_000, 50_000 };
        double[] bandas = Enumerable.Range(1, 10).Select(k => k * 1e3).ToArray();

        var cab = new List<string> { "B" };
        cab.AddRange(snrs.Select(s => $"SNR = {Fmt.Num(s, 0)} ({Fmt.Num(Canal.LinearParaDb(s), 1)} dB)"));
        var t = new Tabela("Tabela 2 – Capacidade de Shannon", cab.ToArray());

        double cMax = double.MinValue, cMin = double.MaxValue;
        (double b, double s) argMax = default, argMin = default;

        foreach (double b in bandas)
        {
            var linha = new List<string> { Fmt.Hz(b) };
            foreach (double s in snrs)
            {
                double c = Canal.Shannon(b, s);
                linha.Add(Fmt.Taxa(c));
                if (c > cMax) { cMax = c; argMax = (b, s); }
                if (c < cMin) { cMin = c; argMin = (b, s); }
            }
            t.Adicionar(linha.ToArray());
        }
        rel.AdicionarTabela(t);

        var ext = new Tabela("Tabela 3 – Maior e menor capacidade obtidas", "", "B", "SNR (linear)", "C");
        ext.Adicionar("Maior", Fmt.Hz(argMax.b), Fmt.Num(argMax.s, 0), Fmt.Taxa(cMax));
        ext.Adicionar("Menor", Fmt.Hz(argMin.b), Fmt.Num(argMin.s, 0), Fmt.Taxa(cMin));
        rel.AdicionarTabela(ext);

        // Multiplicar a SNR por 5 multiplica C por 5?  (razão independe de B)
        var r = new Tabela("Tabela 4 – Efeito de multiplicar a SNR por 5 (qualquer B)",
            "Comparação", "log2(1+SNR) antes", "log2(1+SNR) depois", "C_depois / C_antes");
        foreach (var (a, d) in new[] { (1_000.0, 5_000.0), (10_000.0, 50_000.0) })
        {
            double la = Math.Log2(1 + a), ld = Math.Log2(1 + d);
            r.Adicionar($"SNR {Fmt.Num(a, 0)} → {Fmt.Num(d, 0)}", Fmt.Num(la, 3), Fmt.Num(ld, 3), $"{Fmt.Num(ld / la, 3)}×");
        }
        rel.AdicionarTabela(r);

        var g = new GraficoSvg
        {
            Titulo = "Experimento 2 – Capacidade de Shannon (C × B)",
            Subtitulo = "C = B·log2(1 + SNR), SNR em escala linear",
            RotuloX = "Largura de banda B (kHz)",
            RotuloY = "Capacidade C (kbit/s)",
            TicksX = bandas.Select(b => b / 1e3).ToArray()
        };
        for (int i = 0; i < snrs.Length; i++)
        {
            double s = snrs[i];
            g.Series.Add(new Serie
            {
                Nome = $"SNR = {Fmt.Num(s, 0)}",
                X = bandas.Select(b => b / 1e3).ToArray(),
                Y = bandas.Select(b => Canal.Shannon(b, s) / 1e3).ToArray(),
                Cor = GraficoSvg.Paleta[i]
            });
        }
        rel.Grafico(g, "exp2_shannon_C_vs_B.svg");
    }

    /*------------------------------------------------------------------
       Experimento 3 – Influência do ruído: B = 5 kHz; SNR = 0..40 dB (passo 5)
      ------------------------------------------------------------------
    */
    public static void Experimento3(Relatorio rel)
    {
        rel.Secao("Experimento 3 – Influência do ruído (B = 5 kHz)");

        const double B = 5e3;
        double[] dbs = Enumerable.Range(0, 9).Select(k => k * 5.0).ToArray();

        var t = new Tabela("Tabela 5 – Capacidade de Shannon × SNR (B = 5 kHz)",
            "SNR (dB)", "SNR (linear)", "log2(1+SNR)", "C");
        foreach (double db in dbs)
        {
            double lin = Canal.DbParaLinear(db);
            t.Adicionar(Fmt.Num(db, 0), Fmt.Num(lin, 2), Fmt.Num(Math.Log2(1 + lin), 3), Fmt.Taxa(Canal.Shannon(B, lin)));
        }
        rel.AdicionarTabela(t);

        var cmp = new Tabela("Tabela 6 – Comparação em 10, 20, 30 e 40 dB",
            "SNR (dB)", "SNR (linear)", "C", "Ganho vs. 10 dB abaixo", "C / C(10 dB)");
        double c10 = Canal.Shannon(B, Canal.DbParaLinear(10));
        double? anterior = null;
        foreach (double db in new[] { 10.0, 20, 30, 40 })
        {
            double c = Canal.Shannon(B, Canal.DbParaLinear(db));
            string ganho = anterior is null ? "—" : "+" + Fmt.Taxa(c - anterior.Value);
            cmp.Adicionar(Fmt.Num(db, 0), Fmt.Num(Canal.DbParaLinear(db), 0), Fmt.Taxa(c), ganho, $"{Fmt.Num(c / c10, 2)}×");
            anterior = c;
        }
        rel.AdicionarTabela(cmp);
        rel.Nota($"Ganho assintótico por +10 dB: B·log2(10) = {Fmt.Taxa(B * Math.Log2(10))}.");

        var g = new GraficoSvg
        {
            Titulo = "Experimento 3 – Capacidade × SNR (B = 5 kHz)",
            Subtitulo = "SNR = 10^(SNR_dB/10);  C = B·log2(1 + SNR)",
            RotuloX = "SNR (dB)",
            RotuloY = "Capacidade C (kbit/s)",
            TicksX = dbs
        };
        g.Series.Add(new Serie
        {
            Nome = "B = 5 kHz",
            X = dbs,
            Y = dbs.Select(db => Canal.Shannon(B, Canal.DbParaLinear(db)) / 1e3).ToArray(),
            Cor = GraficoSvg.Paleta[0],
            Rotulos = true
        });
        rel.Grafico(g, "exp3_shannon_C_vs_SNRdB.svg");
    }

    /* ------------------------------------------------------------------
       Experimento 4 – Nyquist × Shannon: B = 3 kHz, SNR = 30 dB, N = 2..64
       ------------------------------------------------------------------
    */
    public static void Experimento4(Relatorio rel)
    {
        rel.Secao("Experimento 4 – Nyquist × Shannon (B = 3 kHz, SNR = 30 dB)");

        const double B = 3e3, snrDb = 30;
        double snr = Canal.DbParaLinear(snrDb);
        double cShannon = Canal.Shannon(B, snr);
        int[] niveis = { 2, 4, 8, 16, 32, 64 };

        var t = new Tabela("Tabela 7 – Taxa de Nyquist × limite de Shannon",
            "N", "bits/símbolo", "C Nyquist", "C Shannon", "Nyquist / Shannon", "Ultrapassa Shannon?");
        foreach (int n in niveis)
        {
            double cN = Canal.Nyquist(B, n);
            t.Adicionar(n.ToString(), Fmt.Num(Math.Log2(n), 0), Fmt.Taxa(cN), Fmt.Taxa(cShannon),
                $"{Fmt.Num(cN / cShannon * 100, 1)} %", cN > cShannon ? "SIM" : "não");
        }
        rel.AdicionarTabela(t);

        double nMax = Canal.NiveisMaximosPorShannon(snr);
        rel.Nota($"SNR linear = {Fmt.Num(snr, 0)}; limite de Shannon = {Fmt.Taxa(cShannon)}.");
        rel.Nota($"Igualando 2B·log2(N) = B·log2(1+SNR) → N_max = √(1+SNR) = {Fmt.Num(nMax, 2)} " +
                 $"(≈ {Fmt.Num(Math.Log2(nMax), 2)} bits/símbolo). Qualquer N > {Fmt.Num(nMax, 2)} gera taxa de Nyquist acima do limite.");

        var xs = niveis.Select(n => (double)n).ToArray();
        var g = new GraficoSvg
        {
            Titulo = "Experimento 4 – Nyquist × Shannon (B = 3 kHz, SNR = 30 dB)",
            Subtitulo = "Pontos vermelhos: taxa de Nyquist acima do limite de Shannon (eixo N em escala log2)",
            RotuloX = "Número de níveis de sinalização (N)",
            RotuloY = "Capacidade C (kbit/s)",
            TicksX = xs,
            EscalaLog2X = true
        };
        g.Series.Add(new Serie
        {
            Nome = "Nyquist 2B·log2(N)",
            X = xs,
            Y = niveis.Select(n => Canal.Nyquist(B, n) / 1e3).ToArray(),
            Cor = GraficoSvg.Paleta[0],
            Rotulos = true,
            CorDoPonto = i => Canal.Nyquist(B, niveis[i]) > cShannon ? "#d62728" : GraficoSvg.Paleta[0]
        });
        g.Series.Add(new Serie
        {
            Nome = $"Shannon ({Fmt.Num(cShannon / 1e3, 1)} kbit/s)",
            X = new[] { xs.First(), xs.Last() },
            Y = new[] { cShannon / 1e3, cShannon / 1e3 },
            Cor = GraficoSvg.Paleta[3],
            Tracejada = true,
            Marcadores = false
        });
        rel.Grafico(g, "exp4_nyquist_vs_shannon.svg");
    }
}
