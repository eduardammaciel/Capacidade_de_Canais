using System.Text;
using CapacidadeCanais;

Console.OutputEncoding = Encoding.UTF8;

// Executa os 4 experimentos: imprime as tabelas no console
// e grava os gráficos (SVG) na pasta "saida/graficos".
string pasta = Path.GetFullPath("saida");
Experimentos.ExecutarTodos(pasta);

Console.WriteLine();
Console.WriteLine($"Gráficos (SVG): {Path.Combine(pasta, "graficos")}");