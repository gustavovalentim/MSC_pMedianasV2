using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using Gurobi;

namespace pMedianasGustavo
{
    class ProblemaPMedianas
    {
        public GRBEnv Ambiente;
        public GRBModel Modelo;
        public int NumeroItens;
        public int NumeroMedianas;
        public double DistanciaTotal;
        public Item[] Itens;
        public Mediana[] Medianas;
        public double[,] MatrizDistancias;
        public void CriarResolverModeloExato()
        {
            Ambiente = new GRBEnv();
            Modelo = new GRBModel(Ambiente);
            Modelo.ModelSense = GRB.MINIMIZE;
            GRBVar[] Y = new GRBVar[NumeroItens];
            GRBVar[,] X = new GRBVar[NumeroItens, NumeroItens];
            //Função objetivo (1) e Conjuntos de Restrições (5) e (6)
            for (int j = 0; j < NumeroItens; j++)
            {
                Y[j] = Modelo.AddVar(0, 1, 0, GRB.BINARY, "y_" + j.ToString());
            }
            for (int i = 0; i < NumeroItens; i++)
            {
                for (int j = 0; j < NumeroItens; j++)
                {
                    X[i, j] = Modelo.AddVar(0, 1, Itens[i].Demanda * MatrizDistancias[i, j], GRB.BINARY, "x_" + i.ToString() + "_" + j.ToString());
                }
            }

            //Restrição (2)
            GRBLinExpr expr = new GRBLinExpr();
            //expr=
            //j=0: expr=1 Y[0]
            //j=1: expr=1 Y[0] + 1 Y[1]
            //...
            //j=19: expr=1 Y[0] + 1 Y[1] + ... + 1 Y[18] + 1 Y[19]
            for (int j = 0; j < NumeroItens; j++)
            {
                expr.AddTerm(1, Y[j]);
            }
            Modelo.AddConstr(expr == NumeroMedianas, "R2");

            //Conjunto de Restrições (3)
            expr.Clear();
            //expr=
            for (int i = 0; i < NumeroItens; i++)
            {
                expr.Clear();
                for (int j = 0; j < NumeroItens; j++)
                {
                    expr.AddTerm(1, X[i, j]);
                }
                Modelo.AddConstr(expr == 1, "R3" + "_" + i.ToString());
            }

            //Conjunto de Restrições (4)
            for (int j = 0; j < NumeroItens; j++)
            {
                expr.Clear();
                for (int i = 0; i < NumeroItens; i++)
                {
                    expr.AddTerm(Itens[i].Demanda, X[i, j]);
                }
                Modelo.AddConstr(expr <= Itens[j].Capacidade * Y[j], "R4_" + j.ToString());
            }

            //conjunto de restrições (GGG)
            //for (int i = 0; i < NumeroItens; i++)
            //{
            //    for (int j = 0; j < NumeroItens; j++)
            //    {
            //        Modelo.AddConstr(MatrizDistancias[i, j] * X[i, j] <= 300, "RGGG_" + i.ToString() + "_" + j.ToString());
            //    }
            //}

            //Escrever Modelo .lp
            Modelo.Write(@"C:\Teste\ModeloPmedianas.lp");

            //Otimizar
            Modelo.Optimize();
            DistanciaTotal = Modelo.ObjVal;
            //"Ler" resultado
            int NumeroMedianasJaDefinidas = 0;
            Dictionary<int, int> DicionarioMedianas = new Dictionary<int, int>();
            for (int j = 0; j < NumeroItens; j++)
            {
                if (Y[j].X > 0.9)
                {
                    Medianas[NumeroMedianasJaDefinidas].ItemMediana = j;
                    Medianas[NumeroMedianasJaDefinidas].ItensAlocados = new List<int>();
                    Medianas[NumeroMedianasJaDefinidas].DistanciaItensMediana = 0;
                    DicionarioMedianas.Add(j, NumeroMedianasJaDefinidas);
                    NumeroMedianasJaDefinidas++;
                }
            }
            for (int i = 0; i < NumeroItens; i++)
            {
                for (int j = 0; j < NumeroItens; j++)
                {
                    if (X[i, j].X > 0.9)
                    {
                        Itens[i].MedianaAlocada = DicionarioMedianas[j];
                        Medianas[DicionarioMedianas[j]].ItensAlocados.Add(i);
                        Medianas[DicionarioMedianas[j]].DistanciaItensMediana += MatrizDistancias[i, j];
                    }
                }
            }
        }
        public void LerInstancia(string _caminhoArquivo)
        {
            string[] ArquivoTodo = File.ReadAllLines(_caminhoArquivo);
            NumeroItens = ArquivoTodo.Length - 1;
            Itens = new Item[NumeroItens];
            for(int i=0;i<NumeroItens;i++)
            {
                string[] LinhaAtual = ArquivoTodo[i + 1].Split('\t');
                Itens[i] = new Item();
                Itens[i].X = int.Parse(LinhaAtual[0]);
                Itens[i].Y = int.Parse(LinhaAtual[1]);
                Itens[i].Demanda = int.Parse(LinhaAtual[2]);
                Itens[i].Capacidade = int.Parse(LinhaAtual[3]);
                Itens[i].CustoInstalacao = int.Parse(LinhaAtual[4]);
            }
        }
        public void CriarCoresAleatoriasParaMedianas()
        {
            Random Aleatorio = new Random();
            for(int j=0;j<NumeroMedianas;j++)
            {
                int A = Aleatorio.Next(100,181);
                int R = Aleatorio.Next(0, 256);
                int G = Aleatorio.Next(0, 256);
                int B = Aleatorio.Next(0, 256);
                Medianas[j].Cor = Color.FromArgb(A, R, G, B);
            }
        }
        public void CalcularDistanciasEntreItens()
        {
            MatrizDistancias = new double[NumeroItens, NumeroItens];
            for (int i = 0; i < NumeroItens; i++)
            {
                for (int j = 0; j < NumeroItens; j++)
                {
                    MatrizDistancias[i, j] = Distancia2Pontos(Itens[i].X, Itens[j].X, Itens[i].Y, Itens[j].Y);
                }
            }
        }
        public double Distancia2Pontos(int x1, int x2, int y1, int y2)
        {
            double DeltaX = (double)x2 - (double)x1;
            double DeltaY = (double)y2 - (double)y1;
            return Math.Sqrt(DeltaX * DeltaX + DeltaY * DeltaY);
        }
        public Bitmap Desenhar()
        {
            Bitmap Desenho = new Bitmap(600, 600);
            Graphics g = Graphics.FromImage(Desenho);
            g.FillRectangle(Brushes.White, 0, 0, 600, 600);
            int RaioMediana = 8;
            int RaioItem = 5;
            Font drawFont = new Font("Arial", 7);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            for (int j = 0; j < NumeroMedianas; j++)
            {
                int MedianaDesenhar = Medianas[j].ItemMediana;
                Brush Pincel = new SolidBrush(Medianas[j].Cor);
                g.FillEllipse(Pincel, Itens[MedianaDesenhar].X - RaioMediana, Itens[MedianaDesenhar].Y - RaioMediana, 2 * RaioMediana, 2 * RaioMediana);
                for (int i = 0; i < Medianas[j].ItensAlocados.Count; i++)
                {
                    int ItemDesenhar = Medianas[j].ItensAlocados[i];
                    g.FillEllipse(Pincel, Itens[ItemDesenhar].X - RaioItem, Itens[ItemDesenhar].Y - RaioItem, 2 * RaioItem, 2 * RaioItem);
                    string st = ItemDesenhar + ": (" + Itens[ItemDesenhar].X + "," + Itens[ItemDesenhar].Y + ")";
                    g.DrawString(st, drawFont, drawBrush, Itens[ItemDesenhar].X - 6 * RaioItem, Itens[ItemDesenhar].Y - 3 * RaioItem);
                }
            }
            return Desenho;
        }
        public void CalcularDistanciaTotal()
        {
            DistanciaTotal = 0;
            for (int j = 0; j < NumeroMedianas; j++)
            {
                DistanciaTotal += Medianas[j].DistanciaItensMediana;
            }
        }
        public void InicializaMedianas()
        {
            Medianas = new Mediana[NumeroMedianas];
            for (int j = 0; j < NumeroMedianas; j++)
            {
                Medianas[j] = new Mediana();
            }
        }
    }
    class Mediana
    {
        public double DistanciaItensMediana;
        public int ItemMediana;
        public Color Cor;
        public List<int> ItensAlocados;
    }
    class Item
    {
        public int X;
        public int Y;
        public int Demanda;
        public int Capacidade;
        public int MedianaAlocada;
        public int CustoInstalacao;
    }
}

