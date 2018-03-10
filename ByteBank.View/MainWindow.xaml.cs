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

    /*
     * ASYNC AWAIT
     * A Microsoft, percebendo isto, lançou um recurso novo na versão 5 do C#, que torna esta escrita de aplicações assíncronas mais simples. 
     * O nome deste recurso é AsyncAwait, que facilita a construção de todos estes ornamentos. Para começarmos a usá-lo, precisamos indicar 
     * ao compilador a tarefa ou método a ser executado de forma assíncrona. No caso, será o clique do botão (BtnProcessar_Click()), para 
     * indicar que sua função é assíncrona, adicionando-se um codificador em seu cabeçalho.
     * 
        01 | btnCalcular.IsEnabled = false;
        02 | var A = await CalculaRaiz(100);
        03 | btnCalcular.IsEnabled = true;

        O compilador reescreve o código de forma que a linha 2 seja executada em uma Task e a linha 3 seja executada de 
        forma encadeada, mas no mesmo contexto da linha 1.

        Esta é uma das grandes motivações do async/await. O contexto da linha 1 é preservado e mantido na execução da linha 3
     */

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

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            BtnProcessar.IsEnabled = false;

            //Obtem as contas do usuarios
            var contas = r_Repositorio.GetContaClientes();

            //Atualiza a lista            
            AtualizarView(Enumerable.Empty<string>(), TimeSpan.Zero);
            //Inicio do processamento
            var inicio = DateTime.Now;

            //await
            //O recurso se chama AsyncAwait e não é à toa. A primeira palavra chave é o async, e a segunda é o await. 
            //Podemos usá-lo para consolidar as contas, pois trata-se de uma task, a qual sempre pode ser aguardada.
            // Daqui em diante, voltamos a executar no contexto da thread inicial. Ao utilizarmos ConsolidarContas() 
            //temos uma tarefa que retorna uma lista de string, e nossa preocupação não é com a tarefa, e sim com seu
            //resultado, a lista de string.Usando o await, podemos armazenar apenas o resultado desta tarefa em uma variável.
            //Vamos também comentar a linha var resultado = task.Result; para não haver conflito entre nomes.
            var resultado = await ConsolidarContas(contas);
            /*
                * Nosso resultado não é uma task de lista de string como está definido em nossa função. O resultado é apenas uma lista de 
                * string pois aqui, na verdade, é como se estivessemos dentro de um ContinueWith(). Mas todo o ornamento de criar-se uma 
                * task, entre outros, se dá pelo compilador, deixando o código muito menos indentado, com menos níveis e uma "cara mais natural", 
                * com que estamos acostumados.

                Ao executarmos no contexto original, não precisamos mais nos preocupar em guardar o contexto no início da função, 
                porque o compilador já fará isto. Não precisamos nos preocupar com os encadeamentos referentes ao fim, atualização de 
                view e clique do botão. Sendo assim, vamos removê-los de ConsolidarContas(), deletando-se este também:
             */

            var fim = DateTime.Now;
            AtualizarView(resultado, fim - inicio);
            BtnProcessar.IsEnabled = true;

            this.Cursor = Cursors.Arrow;
        }

        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas)
        {
            /*
             * Parece que algo está estranho, pois estamos retornando uma lista vazia. Até o método ConsolidarContas retornar, 
             * nenhuma tarefa terá o processamento terminado. Na realidade, o que vamos retornar neste método não será uma lista de string, 
             * e sim uma tarefa que retorna uma lista de string. Ainda não vimos uma tarefa com retorno, mas isto é possível, sendo do mesmo 
             * tipo, porém genérico. Vamos utilizá-la como uma lista. Teremos uma task e, entre os sinais de maior e menor, indicamos o 
             * retorno da tarefa correspondente, no caso, uma lista de string.
             */          

            var tasks = contas.Select(conta =>
                Task.Factory.StartNew(() => r_Servico.ConsolidarMovimentacao(conta))
            );

            /*
             *  temos várias tarefas (tasks), todas retornando o mesmo tipo, e com o resultado, teremos uma tarefa que retorna
             *  um array deste tipo. Como podemos utilizar isto? Guardaremos o WhenAll de todas estas tarefas em uma variável. 
             *  Aqui, não queremos uma task, e sim o resultado, portanto podemos usar apenas o await, tendo como resultado um 
             *  array de string
             */
            return await Task.WhenAll(tasks);
        }

        private void AtualizarView(IEnumerable<string> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}
