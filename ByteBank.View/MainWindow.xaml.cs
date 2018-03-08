using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;

        public MainWindow()
        {
            InitializeComponent();
            //Criação do Repositorio
            r_Repositorio = new ContaClienteRepository();
            //Criação to serviço de calculo.
            r_Servico = new ContaClienteService();
        }
        /// Clicaremos em "Fazer Processamento" no ByteBank. Lembrem-se que no vídeo anterior só havia um núcleo em funcionamento (CPU 0), e agora temos
        /// mais um. Ou seja, provavelmente o primeiro que estava sendo utilizado está executando uma thread da aplicação, e o outro (CPU 1), a outra thread. 
        /// Desta forma, teoricamente, a aplicação rodará mais rapidamente - mas isto não é uma regra. Muitos fatores influenciam a velocidade de execução de 
        /// uma aplicação: o sistema operacional pode estar aguardando respostas da rede, tentando ler o disco rígido, entre outros.        

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            //Obtem as contas do usuarios
            var contas = r_Repositorio.GetContaClientes();
            // a porção para cada thread processar
            var contasQuantidadePorThread = contas.Count() / 4;   
            //vamos dividir o resultado da thread
            //Take(), que recebe, por parâmetro, um número inteiro que representa os n primeiros elementos a serem armazenados
            var contas_parte1 = contas.Take(contasQuantidadePorThread);
            //Skip(), que recebe por parâmetro um número inteiro que representa os n primeiros elementos a serem pulados da lista.
            var contas_parte2 = contas.Skip(contasQuantidadePorThread).Take(contasQuantidadePorThread);
            var contas_parte3 = contas.Skip(contasQuantidadePorThread * 2).Take(contasQuantidadePorThread);
            var contas_parte4 = contas.Skip(contasQuantidadePorThread * 3);

            //Criação da lista para armazenar os resultados
            var resultado = new List<string>();
            //Atualiza a lista 
            AtualizarView(new List<string>(), TimeSpan.Zero);
            //Inicio do processamento
            var inicio = DateTime.Now;

            //Thread:"linha de execução", que na realidade é a tradução de "thread", termo técnico bastante comum na computação quando falamos sobre Paralelismo.
            Thread thread_parte1 = new Thread(()=> {
                foreach (var conta in contas_parte1)
                {
                    var resultadoConta = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoConta);
                }
            });

            Thread thread_parte2 = new Thread(() => {
                foreach (var conta in contas_parte2)
                {
                    var resultadoConta = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoConta);
                }
            });

            Thread thread_parte3 = new Thread(() =>
            {
                foreach (var conta in contas_parte3)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });

            Thread thread_parte4 = new Thread(() =>
            {
                foreach (var conta in contas_parte4)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });


            //Aqui estou executando de fato as threads.
            thread_parte1.Start();
            thread_parte2.Start();
            thread_parte3.Start();
            thread_parte4.Start();

            //O compilador nao vai esperar o processador processasr as thread acima. Precisamo pedir
            //gentilemente para ele esperar. Como fazemos isso? com a proprieadde Alive:
            //================================================================================================
            // IsAlive ---------------------------------------------------------------------------------------
            //================================================================================================
            // A classe Thread possui uma propriedade denominada IsAlive, que retorna "verdadeiro" quando ela está em execução, 
            //e "falso" ao fim de seu processamento. Vamos utilizá-la para ficarmos presos a este método até que as threads terminem.
            while (thread_parte1.IsAlive || thread_parte2.IsAlive || thread_parte3.IsAlive || thread_parte4.IsAlive)
            {
                //Aquele comentário que colocamos, //Não vou fazer nada, não é totalmente verdadeiro. Momento após momento, 
                //a app verifica o IsAlive da thread_parte1 e, depois, da thread_parte2. É um trabalho, esta execução incessante
                //do laço de repetição, causando uso de CPU.

                // Entre uma pergunta e outra, podemos, de fato, não fazer nada. Para isto, ou seja, para não usarmos a CPU,
                //pode -se usar um método estático da classe Thread chamado Sleep:
                //Não vou fazer nada
                Thread.Sleep(250);
                //======================================
                //Sleep -------------------------------
                //======================================
                //O Sleep é um método que, literalmente, coloca a Thread para dormir. Ela recebe por parâmetro um número inteiro 
                //que representa o número de milisegundos durante os quais a Thread ficará sem fazer nada. 

                //O processamento foi finalizado... Se antes demorava-se mais de 40s, a linha de código que acabamos de acrescentar 
                //para fazer a Thread principal parar de ficar perguntando incessantemente se as threads estão trabalhando, nos fez diminuir
                //este tempo consideravelmente, passando a levar 30s para consolidar a informação de todos os clientes de desenvolvimento.
            }

            //Termino do processamento
            var fim = DateTime.Now;

            AtualizarView(resultado, fim - inicio);
        }

        private void AtualizarView(List<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}
