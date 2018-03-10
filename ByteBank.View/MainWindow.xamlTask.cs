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
    * TASK x THRAD
        * Thread é algo mais próximo do concreto. Toda implementação no .Net segue basicamente o que o sistema operacional oferece. 
        * Você usa quando precisa lidar especificamente com thread. Note que é comum algumas pessoas pensarem em thread quando ela só 
        * quer paralelismo ou mesmo assincronicidade. Thread é uma forma de atingir isso, mas não a única. Podemos chamá-lo de mecanismo.

        Task é algo mais abstrato. É algo que foi criado no .Net para o programador não ter que lidar com os detalhes do paralelismo ou assincronicidade.
        Quando se usa uma tarefa está dizendo que precisa de algo pronto em algum momento futuro. Como isso será realizado é algo que o framework pode decidir
        como fazer melhor. Em geral ele é capaz de fazer isso. Podemos considerá-lo de regra do negócio (executar assincronamente).

        Tem uma chance razoável da tarefa usar uma ou mais threads internas para alcançar o objetivo, seja criando threads ou usando existentes. 
        De qualquer forma, mesmo quando se fala em criação, muito provavelmente será feito através de um pool gerenciado pelo framework.

        Um exemplo de diferença de como a tarefa escolhe o melhor caminho: o Thread.Sleep() consome processamento para esperar um tempo, 
        o Task.Delay() cria uma interrupção no processador (através do OS) para o código ser invocado.

        Task é mais poderosa
        Há uma série de ferramentas na API de Task para usar os recursos de forma mais fácil, correta e eficaz. O controle e a comunicação
        entre as tarefas é muito melhor. Tudo o que precisaria ser feito com threads para o bom uso já está pronto e foi realizado por uma 
        equipe que entende do assunto e teve condições de testar adequadamente.

        É comum usar tarefas associadas com async.

        Como será usado em alguma aplicação específica ou qual é melhor é sempre difícil dizer com certeza, principalmente sem muitos detalhes 
        sobre os requisitos. Mas o que se recomenda hoje no .Net é usar task por padrão e somente se ela não fornecer tudo o que se espera passar 
        para thread ou outra forma mais concreta de obter um resultado futuro que não bloqueie a aplicação.

        TASK x THREAD
        Task é mais performática, porque passamos a responsabilidade de provisionamento e gerenciamento das Threads disponíveis para o TaskScheduler default 
        usado pela Task.Factory, que possui uma inteligência 
        O uso da Task.Factory e o default TaskScheduler é uma boa prática pois o gerenciamento das threads por eles é feito de forma muito inteligente
    */
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        //Criação da lista para armazenar os resultados
        List<string> resultados = new List<string>();

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
            this.Cursor = Cursors.Wait;

            /*
             * O erro ocorreu porque estávamos na thread principal, atualizando a lista de resultados (LstResultados) e o texto de resumo (TxtTempo.Text = mensagem) 
             * na mesma thread. Colocamos os códigos criados agora em uma diferente, sem saber qual, cuja responsabilidade não é nossa. Uma thread diferente não pode 
             * acessar o controle da interface gráfica. O .NET protege isto, e quando uma tentativa de acesso a um objeto da interface gráfica ocorre por uma 
             * thread diferente, a aplicação é travada, lançando-se a exceção.

            É necessário informar ao .NET que o código recém criado será executado na interface gráfica, sem ser necessário esperar todas as outras,
            mantendo-se a interface gráfica travada até que a execução do código. Lembra que tínhamos um intermediário responsável por delegar quem 
            executaria qual tarefa, delegando assegurando que todas as threads fossem executadas da forma mais otimizada possível?

            O  TaskScheduler também existe na thread principal, sendo obtido a partir de uma que estiver em execução, com o método estático 
            FromCurrentSynchronizationContext(), que retorna o TaskScheduler atuante no momento.

            Assim, informamos ao .NET nosso desejo de que a tarefa referente à finalização e atualização de visualização seja feita de acordo com a demanda do 
            TaskScheduler, podendo ser enviado no método ContinueWith, como um parâmetro.
             */

            //Agora esta no contexto da thread principal, se eu mudado para dentro de uma thread lá sera o novo contexto.
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


            /*=================================
             * -- TaskScheduler ---------------
             * ================================
             * É gestor de tarefas do .Net. Agora nao precisamos mais nos preocuprar a performance das Threads,
             * ou quais sao as boas praticas. O task e excutado em um alto niveil. Thread baixo nivel. OS.
             * 
             * Se há 10 contas sendo executadas simultaneamente, e 8 núcleos, sendo que um deles está parado, o TaskScheduler coloca-o para trabalhar.
             * Não precisamos mais nos preocupar em dividir tarefas para cada thread, pois ele faz isto. Quando usamos a Factory, propriedade estática 
             * da classe Task, utilizamos o TaskScheduler default que o .NET nos fornece.
             */
            var contasTarefa = contas.Select(conta => {

                // Task ("tarefa" em inglês). Ele possui uma propriedade estática, Factory, que constrói tarefas. Nossa nova tarefa é constituída
                //por outra expressão lambda, a qual não recebe nenhum parâmetro, em que faremos a consolidação desta conta.
                return Task.Factory.StartNew (() => {
                    var resultado = r_Servico.ConsolidarMovimentacao(conta);
                    resultados.Add(resultado);          
                });
            }).ToArray();

            /*
             * To Array
             * Relembremos que Select é uma função do LINQ, que age de forma mais "preguiçosa" possível. Neste momento, na execução da próxima 
             * linha: var fim = DateTime.Now;. Nenhuma tarefa foi criada, pois o LINQ executa queries somente quando necessário. Se não usamos 
             * a variável contasTarefas, o código que vem a seguir não é executado. Vamos forçar todas as tarefas a serem criadas, gerando um 
             * array (.ToArray();) e obrigando o LINQ a executá-las:
             */


            /* WaitAll: Nao faça nada até que as todas as threads tenham sido executadas:
             * Por isso, criaremos um laço de repetição while para verificar se a thread IsAlive... Não nos preocupamos mais com threads, 
             * tampouco com esta propriedade. Faremos isto de um jeito diferente: o Task possui um método estático chamado WaitAll que não faz
             * nada até que todas as outras tarefas sejam finalizadas. Este método recebe uma lista de tarefas como parâmetro, travando a 
             * execução deste até que todas as tarefas terminem.
             */
            //Task.WaitAll(contasTarefa);

            /*
             No momento em que se alcança o código Task.WaitAll(contasTarefas), a aplicação é pausada, e as linhas seguintes - de finalização e atualização da tela 
             - só serão executadas quando terminarmos todas as tarefas existentes. Verificaremos seu funcionamento apertando "Start" mais uma vez e acompanhando
             o uso das CPUs pelo Gerenciador de Tarefas.*/

            //com o whenAll conseguimos encadear outras tarefas.
            Task.WhenAll(contasTarefa)
                .ContinueWith(task => {

                    //Parametro task:dentro do corpo da função temos disponível a task que originou a execução.. Ou seja, a task será a tarefa que espera todas as outras (Task.WhenAll()) 
                    //Termino do processamento
                    var fim = DateTime.Now;
                    AtualizarView(resultados, fim - inicio);

                    //Não podemos alterar um objeto da interface gráfica a partir de outras Threads por isso eu preciso do contexto da thread UI(princiual)
                    //que esta no objecto taskSchedulerUI;
                    BtnProcessar.IsEnabled = true;

                }, taskSchedulerUI);


            this.Cursor = Cursors.Arrow;
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
;