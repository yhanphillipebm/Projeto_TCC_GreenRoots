using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using AppGreenRoots.Views;

namespace GreenRootsApp
{
    public partial class PassaporteCompletoWindow : Window
    {
        private ProcessoProducao _processo;

        public PassaporteCompletoWindow(ProcessoProducao processo)
        {
            InitializeComponent();
            _processo = processo;
            DataContext = _processo;
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de impressão será implementada em breve.", "Imprimir", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Arquivo Texto|*.txt|PDF|*.pdf",
                FileName = $"Passaporte_{_processo.Codigo}_{_processo.TimestampProducao:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    string conteudo = GerarConteudoPassaporte();
                    File.WriteAllText(saveDialog.FileName, conteudo);
                    
                    MessageBox.Show("Passaporte salvo com sucesso!", "Sucesso", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnEnviar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de envio será implementada em breve.", "Enviar", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnVoltar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private string GerarConteudoPassaporte()
        {
            return $"PASSAPORTE DIGITAL GREEN ROOTS\n" +
                   $"================================\n\n" +
                   $"INFORMAÇÕES BÁSICAS:\n" +
                   $"Processo: {_processo.Nome}\n" +
                   $"Código: {_processo.Codigo}\n" +
                   $"Data/Hora: {_processo.TimestampProducao:dd/MM/yyyy HH:mm:ss}\n" +
                   $"Status: {_processo.Status}\n\n" +
                   $"MATERIAIS UTILIZADOS:\n" +
                   $"{string.Join("\n", _processo.Materiais.Select(m => $"• {m}"))}\n\n" +
                   $"MÉTRICAS DE SUSTENTABILIDADE:\n" +
                   $"• Tempo de Produção: {_processo.TempoProducaoEstimado} minutos\n" +
                   $"• Emissão de CO₂: {_processo.EmissaoCO2Estimada} kg\n" +
                   $"• Energia Consumida: {_processo.EnergiaEstimada} kWh\n\n" +
                   $"SENSORES ATIVADOS:\n" +
                   $"{string.Join("\n", _processo.SensoresRequeridos.Select(s => $"• {s}"))}\n\n" +
                   $"EVENTOS REGISTRADOS:\n" +
                   $"{string.Join("\n", _processo.EventosAssociados?.Select(e => $"{e.Timestamp:HH:mm:ss} - {e.TipoSensor}: {e.Evento} ({e.Valor} {e.Unidade})") ?? new List<string>())}\n\n" +
                   $"CÓDIGO DE VERIFICAÇÃO:\n" +
                   $"{_processo.CodigoVerificacaoCompleto}\n\n" +
                   $"================================\n" +
                   $"Green Roots - Sistema de Rastreabilidade Sustentável";
        }
    }
}