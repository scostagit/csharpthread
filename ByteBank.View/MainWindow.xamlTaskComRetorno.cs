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
     * TASK TIPADA
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

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
                
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();
            BtnProcessar.IsEnabled = false;

            //Obtem as contas do usuarios
            var contas = r_Repositorio.GetContaClientes();
            // a porção para cada thread processar
            var contasQuantidadePorThread = contas.Count() / 4;
           
            //Atualiza a lista 
            AtualizarView(new List<string>(), TimeSpan.Zero);
            //Inicio do processamento
            var inicio = DateTime.Now;

            ConsolidarContas(contas)
              .ContinueWith(task =>
              {
                  var fim = DateTime.Now;
                  var resultado = task.Result; //A propriedade Rseulta armaza o valor de retorno da nossa task retornada no metodo ConsolidarContas
                  AtualizarView(resultado, fim - inicio);
                  BtnProcessar.IsEnabled = true;

              }, taskSchedulerUI); //estamos encadeando outras task que é a UI (principal)  

            this.Cursor = Cursors.Arrow;
        }

        private Task<List<string>> ConsolidarContas(IEnumerable<ContaCliente> contas)
        {
            /*
             * Parece que algo está estranho, pois estamos retornando uma lista vazia. Até o método ConsolidarContas retornar, 
             * nenhuma tarefa terá o processamento terminado. Na realidade, o que vamos retornar neste método não será uma lista de string, 
             * e sim uma tarefa que retorna uma lista de string. Ainda não vimos uma tarefa com retorno, mas isto é possível, sendo do mesmo 
             * tipo, porém genérico. Vamos utilizá-la como uma lista. Teremos uma task e, entre os sinais de maior e menor, indicamos o 
             * retorno da tarefa correspondente, no caso, uma lista de string.
             */
            var resultado = new List<string>();

            var tasks = contas.Select(conta => {
                //estou criando uma task por conta cliente.
                return Task.Factory.StartNew(() => {
                    /*
                     *  Sempre que temos uma tarefa encadeando outra, por parâmetro, recebemos a tarefa anterior, finalizada. 
                     */
                    var contaResulado = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(contaResulado);
                });
            });


            /* SOBRECARGA continueWith: a primeira devolve uma task, a segunda um tipo generico
                                     * Como retornaremos uma tarefa que por sua vez retorna uma lista e que, na verdade, precisa esperar todas as outras tarefas? 
                                     * O WhenAll já vai esperar a execução de todas as tarefas. E ainda temos o método ContinueWith, o qual possibilita duas 
                                     * sobrecargas: uma normal, que retorna uma task, e outra genérica, que retorna uma task com retorno de tipo genérico. Vamos 
                                     * usar esta construção, e ela receberá um delegate, uma ação, que irá retornar o resultado. Como estamos executando esta tarefa de 
                                     * forma encadeada em outra (aquela que espera todas as demais serem executadas), neste momento sabemos que o resultado está populado, 
                                     * bastando retorná-lo.
                                     */

            //Quando todas as minhas tasks forem executados retornamos a lista de resultados.
            return Task.WhenAll(tasks) // O WhenAll vai esperar a execução de todas as tarefas.
                .ContinueWith(t => { //temos o método ContinueWith, o qual possibilita duas sobrecargas: uma normal, que retorna uma task  e outra genérica, que retorna uma task com retorno de tipo genérico

                    /* Sempre que temos uma tarefa encadeando outra, por parâmetro, recebemos a tarefa anterior, finalizada. 
                     * 
                     * Agora veremos por que isto é importante. Não se trata de uma tarefa qualquer, e sim com retorno de lista de string. 
                     * 
                     * Teremos portanto uma propriedade disponível chamada task.Result, pois as tarefas que possuem retorno são de classe genérica.
                     */
                    return resultado;
            });           
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
