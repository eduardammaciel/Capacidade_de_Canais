using System.Globalization;
using System.Text;

namespace CapacidadeCanais;
public class Serie
{
    public required string Nome { get; init; }
    public required double[] X { get; init; }
    public required double[] Y { get; init; }
    public string Cor { get; init; } = "#1f77b4";
    public bool Tracejada { get; init; }
    public bool Marcadores { get; init; } = true;
    public bool Rotulos { get; init; }
    /// <summary>Permite pintar pontos individuais (ex.: destacar pontos acima do limite de Shannon).</summary>
    public Func<int, string>? CorDoPonto { get; init; }
}

public class GraficoSvg
{
    public required string Titulo { get; init; }
    public string? Subtitulo { get; init; }
    public required string RotuloX { get; init; }
    public required string RotuloY { get; init; }
    public List<Serie> Series { get; } = new();
    public double[]? TicksX { get; init; }
    public bool EscalaLog2X { get; init; }

    private const int W = 900, H = 540;
    private const int ML = 80, MR = 220, MT = 72, MB = 72;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static readonly string[] Paleta = { "#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b" };

    public string Gerar()
    {
        if (Series.Count == 0) throw new InvalidOperationException("O gráfico não tem séries.");

        double Tx(double x) => EscalaLog2X ? Math.Log2(x) : x;

        var todosX = Series.SelectMany(s => s.X).ToArray();
        double xMin = TicksX?.Min() ?? todosX.Min();
        double xMax = TicksX?.Max() ?? todosX.Max();
        double yMaxDados = Series.SelectMany(s => s.Y).Max();
        double passoY = PassoBonito(yMaxDados, 6);
        double yMax = Math.Ceiling(yMaxDados * 1.08 / passoY) * passoY;

        int pw = W - ML - MR, ph = H - MT - MB;
        double pad = pw * 0.04; // folga lateral para os rótulos não encostarem nos eixos
        double PX(double x) => ML + pad + (Tx(x) - Tx(xMin)) / (Tx(xMax) - Tx(xMin)) * (pw - 2 * pad);
        double PY(double y) => MT + ph - y / yMax * ph;
        string F(double v) => v.ToString("0.##", Inv);

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {W} {H}\" width=\"{W}\" height=\"{H}\" font-family=\"Segoe UI, Arial, Helvetica, sans-serif\">");
        sb.AppendLine($"<rect width=\"{W}\" height=\"{H}\" fill=\"white\"/>");

        sb.AppendLine($"<text x=\"{F(ML + pw / 2.0)}\" y=\"30\" text-anchor=\"middle\" font-size=\"19\" font-weight=\"bold\" fill=\"#222\">{Esc(Titulo)}</text>");
        if (Subtitulo is not null)
            sb.AppendLine($"<text x=\"{F(ML + pw / 2.0)}\" y=\"52\" text-anchor=\"middle\" font-size=\"13\" fill=\"#666\">{Esc(Subtitulo)}</text>");

        for (double y = 0; y <= yMax + passoY / 2; y += passoY)
        {
            double py = PY(y);
            sb.AppendLine($"<line x1=\"{ML}\" y1=\"{F(py)}\" x2=\"{ML + pw}\" y2=\"{F(py)}\" stroke=\"#e3e3e3\"/>");
            sb.AppendLine($"<text x=\"{ML - 8}\" y=\"{F(py + 4)}\" text-anchor=\"end\" font-size=\"12\" fill=\"#444\">{y.ToString("0.##", Fmt.Br)}</text>");
        }

        double[] ticksX = TicksX ?? TicksLineares(xMin, xMax);
        foreach (double x in ticksX)
        {
            double px = PX(x);
            sb.AppendLine($"<line x1=\"{F(px)}\" y1=\"{MT}\" x2=\"{F(px)}\" y2=\"{MT + ph}\" stroke=\"#f0f0f0\"/>");
            sb.AppendLine($"<text x=\"{F(px)}\" y=\"{MT + ph + 20}\" text-anchor=\"middle\" font-size=\"12\" fill=\"#444\">{x.ToString("0.##", Fmt.Br)}</text>");
        }

        sb.AppendLine($"<line x1=\"{ML}\" y1=\"{MT + ph}\" x2=\"{ML + pw}\" y2=\"{MT + ph}\" stroke=\"#333\" stroke-width=\"1.5\"/>");
        sb.AppendLine($"<line x1=\"{ML}\" y1=\"{MT}\" x2=\"{ML}\" y2=\"{MT + ph}\" stroke=\"#333\" stroke-width=\"1.5\"/>");
        sb.AppendLine($"<text x=\"{F(ML + pw / 2.0)}\" y=\"{H - 22}\" text-anchor=\"middle\" font-size=\"14\" fill=\"#222\">{Esc(RotuloX)}</text>");
        sb.AppendLine($"<text x=\"22\" y=\"{F(MT + ph / 2.0)}\" text-anchor=\"middle\" font-size=\"14\" fill=\"#222\" transform=\"rotate(-90 22 {F(MT + ph / 2.0)})\">{Esc(RotuloY)}</text>");

        foreach (var s in Series)
        {
            string pontos = string.Join(" ", s.X.Select((x, i) => $"{F(PX(x))},{F(PY(s.Y[i]))}"));
            string traco = s.Tracejada ? " stroke-dasharray=\"8 5\"" : "";
            sb.AppendLine($"<polyline points=\"{pontos}\" fill=\"none\" stroke=\"{s.Cor}\" stroke-width=\"2.5\"{traco}/>");

            for (int i = 0; i < s.X.Length; i++)
            {
                double px = PX(s.X[i]), py = PY(s.Y[i]);
                string cor = s.CorDoPonto?.Invoke(i) ?? s.Cor;
                if (s.Marcadores)
                    sb.AppendLine($"<circle cx=\"{F(px)}\" cy=\"{F(py)}\" r=\"4.5\" fill=\"{cor}\" stroke=\"white\" stroke-width=\"1.2\"/>");
                if (s.Rotulos)
                    sb.AppendLine($"<text x=\"{F(px)}\" y=\"{F(py - 10)}\" text-anchor=\"middle\" font-size=\"11\" fill=\"{cor}\">{s.Y[i].ToString("0.#", Fmt.Br)}</text>");
            }
        }

        int lx = W - MR + 20, ly = MT + 10;
        for (int i = 0; i < Series.Count; i++)
        {
            var s = Series[i];
            int y = ly + i * 26;
            string traco = s.Tracejada ? " stroke-dasharray=\"8 5\"" : "";
            sb.AppendLine($"<line x1=\"{lx}\" y1=\"{y}\" x2=\"{lx + 30}\" y2=\"{y}\" stroke=\"{s.Cor}\" stroke-width=\"2.5\"{traco}/>");
            if (s.Marcadores) sb.AppendLine($"<circle cx=\"{lx + 15}\" cy=\"{y}\" r=\"4\" fill=\"{s.Cor}\"/>");
            sb.AppendLine($"<text x=\"{lx + 38}\" y=\"{y + 4}\" font-size=\"13\" fill=\"#222\">{Esc(s.Nome)}</text>");
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static double PassoBonito(double faixa, int alvo)
    {
        double bruto = faixa / alvo;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(bruto)));
        double r = bruto / mag;
        double n = r <= 1 ? 1 : r <= 2 ? 2 : r <= 2.5 ? 2.5 : r <= 5 ? 5 : 10;
        return n * mag;
    }

    private static double[] TicksLineares(double min, double max)
    {
        double passo = PassoBonito(max - min, 8);
        var t = new List<double>();
        for (double v = Math.Ceiling(min / passo) * passo; v <= max + passo / 1000; v += passo) t.Add(v);
        return t.ToArray();
    }

    private static string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
