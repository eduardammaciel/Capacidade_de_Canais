using System.Text;

namespace CapacidadeCanais;

/// <summary>Tabela simples impressa no console.</summary>
public class Tabela
{
    public string Titulo { get; }
    public string[] Cabecalho { get; }
    public List<string[]> Linhas { get; } = new();

    public Tabela(string titulo, params string[] cabecalho)
    {
        Titulo = titulo;
        Cabecalho = cabecalho;
    }

    public void Adicionar(params string[] linha) => Linhas.Add(linha);

    public void Imprimir()
    {
        int[] larg = Cabecalho.Select((c, i) =>
            Math.Max(c.Length, Linhas.Count == 0 ? 0 : Linhas.Max(l => l[i].Length))).ToArray();

        string Linha(string[] cel) =>
            "| " + string.Join(" | ", cel.Select((c, i) => i == 0 ? c.PadRight(larg[i]) : c.PadLeft(larg[i]))) + " |";
        string sep = "+" + string.Join("+", larg.Select(l => new string('-', l + 2))) + "+";

        Console.WriteLine();
        Console.WriteLine(Titulo);
        Console.WriteLine(sep);
        Console.WriteLine(Linha(Cabecalho));
        Console.WriteLine(sep);
        foreach (var l in Linhas) Console.WriteLine(Linha(l));
        Console.WriteLine(sep);
    }
}

/// <summary>
/// Organiza a saída dos experimentos: imprime as tabelas no console
/// e salva cada gráfico como .svg na pasta de saída.
/// </summary>
public class Relatorio
{
    private readonly string _pasta;

    public Relatorio(string pasta)
    {
        _pasta = pasta;
        Directory.CreateDirectory(Path.Combine(pasta, "graficos"));
    }

    public void Secao(string titulo)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine(titulo);
        Console.WriteLine(new string('=', 78));
    }

    public void AdicionarTabela(Tabela t) => t.Imprimir();

    public void Grafico(GraficoSvg g, string nomeArquivo)
    {
        string caminho = Path.Combine(_pasta, "graficos", nomeArquivo);
        File.WriteAllText(caminho, g.Gerar(), new UTF8Encoding(false));
        Console.WriteLine($"\n[gráfico salvo em {Path.GetFullPath(caminho)}]");
    }

    public void Nota(string texto) => Console.WriteLine(texto);
}