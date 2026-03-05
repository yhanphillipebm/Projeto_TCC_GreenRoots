using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using GreenRootsApp;
using Microsoft.Win32;

namespace AppGreenRoots.Views;

public partial class MainWindow : Window
    {
        private SerialPort? serialPort;
        private bool isConnected = false;
        private ObservableCollection<PassaporteDigital> passaportes = new ObservableCollection<PassaporteDigital>();
        private ObservableCollection<ProcessoProducao> processos = new ObservableCollection<ProcessoProducao>();

        public ObservableCollection<PassaporteDigital> Passaportes => passaportes;
        public ObservableCollection<ProcessoProducao> Processos => processos;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LvDadosSensores.ItemsSource = Passaportes;
            LvPassaportes.ItemsSource = Processos;

            // Carregar processos de exemplo
            CarregarProcessosExemplo();
        }

        private void CarregarProcessosExemplo()
        {
            processos.Add(new ProcessoProducao
            {
                Nome = "Produção de Eixo Traseiro",
                Codigo = "ET-001",
                SensoresRequeridos = new string[] { "FSR", "IR", "Ultrassônico" },
                Materiais = new string[] { "Aço 1020", "Lubrificante", "Parafusos" },
                TempoProducaoEstimado = 120,
                EmissaoCO2Estimada = 45.7,
                EnergiaEstimada = 32.5
            });

            processos.Add(new ProcessoProducao
            {
                Nome = "Montagem de Cabine",
                Codigo = "CB-002",
                SensoresRequeridos = new string[] { "FSR", "IR" },
                Materiais = new string[] { "Plástico ABS", "Vidro", "Metal" },
                TempoProducaoEstimado = 180,
                EmissaoCO2Estimada = 78.3,
                EnergiaEstimada = 45.2
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigurarSerial();
            TabControl.SelectedIndex = 0; // Selecionar a aba de monitoramento por padrão
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DesconectarSerial();
        }

        #region Configuração Serial

        private void ConfigurarSerial()
        {
            CarregarPortasSeriais();
        }

        private void CarregarPortasSeriais()
        {
            CmbPortasSerial.Items.Clear();
            var portas = SerialPort.GetPortNames();

            foreach (var porta in portas)
            {
                CmbPortasSerial.Items.Add(porta);
            }

            if (CmbPortasSerial.Items.Count > 0)
            {
                CmbPortasSerial.SelectedIndex = 0;
            }
        }

        private void ConectarSerial()
        {
            if (CmbPortasSerial.SelectedItem == null)
            {
                MessageBox.Show("Selecione uma porta serial válida.", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string porta = CmbPortasSerial.SelectedItem.ToString()!;

                serialPort = new SerialPort(porta, 115200, Parity.None, 8, StopBits.One);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();

                isConnected = true;
                AtualizarInterfaceConexao();

                TxtStatus.Text = "Status: Conectado";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
                TxtEstatisticas.Text = "Conectado. Aguardando dados dos sensores...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar: {ex.Message}", "Erro de Conexão",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DesconectarSerial()
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                    serialPort.DataReceived -= SerialPort_DataReceived;
                    serialPort.Dispose();
                    serialPort = null;
                }

                isConnected = false;
                AtualizarInterfaceConexao();

                TxtStatus.Text = "Status: Desconectado";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                TxtEstatisticas.Text = "Desconectado. Selecione uma porta и conecte.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao desconectar: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AtualizarInterfaceConexao()
        {
            BtnConectar.IsEnabled = !isConnected;
            BtnDesconectar.IsEnabled = isConnected;
            CmbPortasSerial.IsEnabled = !isConnected;
        }

        #endregion

        #region Processamento de Dados

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    string dados = serialPort.ReadLine();
                    Dispatcher.Invoke(() => ProcessarDadosArduino(dados));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show($"Erro na leitura serial: {ex.Message}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void ProcessarDadosArduino(string dados)
        {
            if (string.IsNullOrWhiteSpace(dados)) return;

            try
            {
                PassaporteDigital? passaporte = null;

                if (dados.Contains("EVENTO FSR:"))
                {
                    passaporte = ProcessarEventoFSR(dados);
                }
                else if (dados.Contains("EVENTO IR:"))
                {
                    passaporte = ProcessarEventoIR(dados);
                }
                else if (dados.Contains("EVENTO ULTRASSONICO:"))
                {
                    passaporte = ProcessarEventoUltrassonico(dados);
                }

                if (passaporte != null)
                {
                    passaporte.GerarCodigoVerificacao();
                    Passaportes.Insert(0, passaporte);
                    LimitarLista();
                    AtualizarEstatisticas();

                    // Verificar se temos dados suficientes para gerar um passaporte de produção
                    VerificarPassaporteProducao(passaporte);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao processar dados: {ex.Message}");
            }
        }

        private PassaporteDigital? ProcessarEventoFSR(string dados)
        {
            int startIndex = dados.IndexOf("Valor:") + 6;
            int endIndex = dados.IndexOf(")", startIndex);

            if (startIndex > 0 && endIndex > startIndex)
            {
                string valorStr = dados.Substring(startIndex, endIndex - startIndex).Trim();
                if (int.TryParse(valorStr, out int valorPressao))
                {
                    return new PassaporteDigital
                    {
                        TipoSensor = "FSR",
                        Evento = dados.Contains("APLICADA") ? "Pressão Aplicada" : "Pressão Liberada",
                        Valor = valorPressao,
                        Unidade = "unidades"
                    };
                }
            }

            return null;
        }

        private PassaporteDigital ProcessarEventoIR(string dados)
        {
            return new PassaporteDigital
            {
                TipoSensor = "IR",
                Evento = dados.Contains("DETECTADO") ? "Objeto Detectado" : "Objeto Removido",
                Valor = dados.Contains("DETECTADO") ? 1 : 0,
                Unidade = "estado"
            };
        }

        private PassaporteDigital? ProcessarEventoUltrassonico(string dados)
        {
            if (dados.Contains("Distancia:"))
            {
                int startIndex = dados.IndexOf("Distancia:") + 10;
                int endIndex = dados.IndexOf(" cm", startIndex);

                if (startIndex > 0 && endIndex > startIndex)
                {
                    string distanciaStr = dados.Substring(startIndex, endIndex - startIndex).Trim();
                    if (int.TryParse(distanciaStr, out int distancia))
                    {
                        return new PassaporteDigital
                        {
                            TipoSensor = "Ultrassônico",
                            Evento = dados.Contains("PROXIMO") ? "Objeto Próximo" : "Objeto Afastado",
                            Valor = distancia,
                            Unidade = "cm"
                        };
                    }
                }
            }

            return null;
        }

        private void LimitarLista()
        {
            while (Passaportes.Count > 50)
            {
                Passaportes.RemoveAt(Passaportes.Count - 1);
            }
        }

        private void VerificarPassaporteProducao(PassaporteDigital ultimoPassaporte)
        {
            // Verificar se temos eventos suficientes para um processo de produção
            var eventosRecentes = Passaportes
                .Where(p => (DateTime.Now - p.Timestamp).TotalMinutes < 5)
                .ToList();

            // Procurar um processo que corresponda aos sensores ativados
            foreach (var processo in Processos)
            {
                var sensoresAtivados = eventosRecentes
                    .Select(p => p.TipoSensor)
                    .Distinct()
                    .ToArray();

                if (processo.SensoresRequeridos.All(s => sensoresAtivados.Contains(s)))
                {
                    // Gerar passaporte de produção
                    processo.TimestampProducao = DateTime.Now;
                    processo.EventosAssociados = eventosRecentes.ToList();
                    processo.Status = "Concluído";
            
                    // REMOVA ESTA LINHA que muda a aba automaticamente:
                    // TabControl.SelectedIndex = 1; // Mudar para a aba de passaportes
            
                    // Em vez disso, apenas atualize a interface
                    Dispatcher.Invoke(() => {
                        LvPassaportes.Items.Refresh();
                        // Adicione uma notificação visual em vez de mudar a aba
                        TxtEstatisticas.Text = $"✅ Passaporte {processo.Codigo} gerado! Verifique na aba 'Passaportes'.";
                    });
            
                    break;
                }
            }
        }

        #endregion

        #region Event Handlers

        private void BtnConectar_Click(object sender, RoutedEventArgs e)
        {
            ConectarSerial();
        }

        private void BtnDesconectar_Click(object sender, RoutedEventArgs e)
        {
            DesconectarSerial();
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            ExportarPassaportesCSV();
        }

        private void BtnDetalhes_Click(object sender, RoutedEventArgs e)
        {
            if (TabControl.SelectedIndex == 0)
                VerDetalhesSensor();
            else
                VerDetalhesPassaporte();
        }

        private void BtnValidar_Click(object sender, RoutedEventArgs e)
        {
            ValidarPassaporte();
        }

        private void BtnLimpar_Click(object sender, RoutedEventArgs e)
        {
            LimparDados();
        }

        private void BtnGerarPassaporte_Click(object sender, RoutedEventArgs e)
        {
            if (LvPassaportes.SelectedItem is ProcessoProducao processo)
            {
                // Verificar se o processo está concluído
                if (processo.Status != "Concluído")
                {
                    MessageBox.Show("Este processo ainda não foi concluído. Aguarde a ativação de todos os sensores necessários.", 
                        "Processo Incompleto", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Abrir janela de passaporte completo
                var passaporteWindow = new PassaporteCompletoWindow(processo)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
        
                passaporteWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Selecione um processo para gerar o passaporte.", "Aviso", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Funcionalidades de Passaporte

        private void ExportarPassaportesCSV()
        {
            if (Passaportes.Count == 0)
            {
                MessageBox.Show("Não há passaportes para exportar.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                FileName = $"passaportes_digitais_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("ID,Timestamp,CodigoVerificacao,TipoSensor,Evento,Valor,Unidade,Status");

                    foreach (var passaporte in Passaportes)
                    {
                        csv.AppendLine($"{passaporte.Id},{passaporte.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                                       $"{passaporte.CodigoVerificacao},{passaporte.TipoSensor}," +
                                       $"{passaporte.Evento},{passaporte.Valor},{passaporte.Unidade},{passaporte.Status}");
                    }

                    File.WriteAllText(saveDialog.FileName, csv.ToString());
                    MessageBox.Show("Passaportes exportados com sucesso!", "Sucesso",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao exportar: {ex.Message}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void VerDetalhesSensor()
        {
            if (LvDadosSensores.SelectedItem is PassaporteDigital passaporte)
            {
                var detalhesWindow = new Window
                {
                    Title = $"Detalhes do Sensor - {passaporte.CodigoVerificacao}",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"📋 Dados do Sensor",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = (System.Windows.Media.SolidColorBrush)FindResource("PrimaryDarkBrush")
                });

                string[] labels = { "Código:", "Data/Hora:", "Sensor:", "Evento:", "Valor:", "Status:" };
                string[] values =
                {
                    passaporte.CodigoVerificacao,
                    passaporte.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                    passaporte.TipoSensor,
                    passaporte.Evento,
                    $"{passaporte.Valor} {passaporte.Unidade}",
                    passaporte.Status
                };

                for (int i = 0; i < labels.Length; i++)
                {
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());

                    grid.Children.Add(new TextBlock
                    {
                        Text = labels[i],
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 10, 5)
                    });

                    var valueText = new TextBlock
                    {
                        Text = values[i],
                        Margin = new Thickness(0, 0, 0, 5)
                    };

                    if (labels[i] == "Status:")
                    {
                        valueText.Foreground = passaporte.StatusColor;
                        valueText.FontWeight = FontWeights.Bold;
                    }

                    Grid.SetColumn(valueText, 1);
                    grid.Children.Add(valueText);

                    stackPanel.Children.Add(grid);
                }

                detalhesWindow.Content = new ScrollViewer { Content = stackPanel };
                detalhesWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Selecione um registro de sensor para ver os detalhes.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void VerDetalhesPassaporte()
        {
            if (LvPassaportes.SelectedItem is ProcessoProducao processo)
            {
                var detalhesWindow = new Window
                {
                    Title = $"Detalhes do Passaporte - {processo.Codigo}",
                    Width = 500,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                // Título
                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"📋 Passaporte Digital de Produção",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = (System.Windows.Media.SolidColorBrush)FindResource("PrimaryDarkBrush")
                });

                // Informações básicas
                AddDetailRow(stackPanel, "Processo:", processo.Nome);
                AddDetailRow(stackPanel, "Código:", processo.Codigo);
                AddDetailRow(stackPanel, "Data/Hora:",
                    processo.TimestampProducao?.ToString("dd/MM/yyyy HH:mm:ss") ?? "N/A");
                AddDetailRow(stackPanel, "Status:", processo.Status);

                // Materiais
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Materiais Utilizados:",
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 15, 0, 5)
                });

                foreach (var material in processo.Materiais)
                {
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = $"• {material}",
                        Margin = new Thickness(10, 0, 0, 2)
                    });
                }

                // Métricas de sustentabilidade
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Métricas de Sustentabilidade:",
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 15, 0, 5)
                });

                AddDetailRow(stackPanel, "Tempo de Produção:", $"{processo.TempoProducaoEstimado} minutos");
                AddDetailRow(stackPanel, "Emissão de CO₂:", $"{processo.EmissaoCO2Estimada} kg");
                AddDetailRow(stackPanel, "Energia Consumida:", $"{processo.EnergiaEstimada} kWh");

                // Sensores ativados
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Sensores Ativados:",
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 15, 0, 5)
                });

                foreach (var sensor in processo.SensoresRequeridos)
                {
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = $"• {sensor}",
                        Margin = new Thickness(10, 0, 0, 2)
                    });
                }

                detalhesWindow.Content = new ScrollViewer { Content = stackPanel };
                detalhesWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Selecione um passaporte para ver os detalhes.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddDetailRow(StackPanel panel, string label, string value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            grid.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 5)
            });

            var valueText = new TextBlock
            {
                Text = value,
                Margin = new Thickness(0, 0, 0, 5)
            };

            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);

            panel.Children.Add(grid);
        }

        private void ValidarPassaporte()
        {
            if (LvPassaportes.SelectedItem is ProcessoProducao processo)
            {
                bool isValid = processo.Status == "Concluído";

                MessageBox.Show(
                    isValid
                        ? $"✅ Passaporte VÁLIDO\nProcesso: {processo.Nome}\nCódigo: {processo.Codigo}"
                        : $"❌ Passaporte INVÁLIDO\nProcesso não concluído ou dados incompletos",
                    "Validação de Passaporte",
                    MessageBoxButton.OK,
                    isValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("Selecione um passaporte para validar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LimparDados()
        {
            if (Passaportes.Count == 0) return;

            if (MessageBox.Show("Deseja limpar todos os registros de sensores?", "Confirmar",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Passaportes.Clear();
                AtualizarEstatisticas();
            }
        }

        private void AtualizarEstatisticas()
        {
            if (Passaportes.Count == 0)
            {
                TxtEstatisticas.Text = "Nenhum registro de sensor";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"📊 Total de Registros: {Passaportes.Count}");

            var porSensor = Passaportes.GroupBy(p => p.TipoSensor)
                .Select(g => new { Sensor = g.Key, Count = g.Count() });

            foreach (var grupo in porSensor.OrderByDescending(g => g.Count))
            {
                sb.AppendLine($"• {grupo.Sensor}: {grupo.Count} eventos");
            }

            TxtEstatisticas.Text = sb.ToString();
        }

        private void GerarDocumentoPassaporte(ProcessoProducao processo)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "TXT Files|*.txt",
                FileName = $"passaporte_{processo.Codigo}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    string conteudoPdf = $"PASSAPORTE DIGITAL GREEN ROOTS\n\n" +
                                         $"Processo: {processo.Nome}\n" +
                                         $"Código: {processo.Codigo}\n" +
                                         $"Data: {DateTime.Now:dd/MM/yyyy HH:mm}\n\n" +
                                         $"MATERIAIS:\n{string.Join("\n", processo.Materiais.Select(m => $"• {m}"))}\n\n" +
                                         $"MÉTRICAS AMBIENTAIS:\n" +
                                         $"• Tempo de Produção: {processo.TempoProducaoEstimado} min\n" +
                                         $"• Emissão de CO₂: {processo.EmissaoCO2Estimada} kg\n" +
                                         $"• Energia Consumida: {processo.EnergiaEstimada} kWh\n\n" +
                                         $"SENSORES UTILIZADOS:\n{string.Join("\n", processo.SensoresRequeridos.Select(s => $"• {s}"))}\n\n" +
                                         $"CÓDIGO DE VERIFICAÇÃO: GR-{processo.Codigo}-{DateTime.Now:yyyyMMdd}";

                    File.WriteAllText(saveDialog.FileName, conteudoPdf);

                    MessageBox.Show($"Passaporte gerado com sucesso!\n\nArquivo: {saveDialog.FileName}",
                        "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao gerar passaporte: {ex.Message}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }

    public class PassaporteDigital : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.Now;

        private string _tipoSensor = string.Empty;
        private string _evento = string.Empty;
        private double _valor;
        private string _unidade = string.Empty;
        private string _status = "Pendente";
        private string _codigoVerificacao = string.Empty;

        public string TipoSensor
        {
            get => _tipoSensor;
            set
            {
                _tipoSensor = value;
                OnPropertyChanged(nameof(TipoSensor));
            }
        }

        public string Evento
        {
            get => _evento;
            set
            {
                _evento = value;
                OnPropertyChanged(nameof(Evento));
            }
        }

        public double Valor
        {
            get => _valor;
            set
            {
                _valor = value;
                OnPropertyChanged(nameof(Valor));
            }
        }

        public string Unidade
        {
            get => _unidade;
            set
            {
                _unidade = value;
                OnPropertyChanged(nameof(Unidade));
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string CodigoVerificacao
        {
            get => _codigoVerificacao;
            set
            {
                _codigoVerificacao = value;
                OnPropertyChanged(nameof(CodigoVerificacao));
            }
        }

        public System.Windows.Media.Brush StatusColor
        {
            get
            {
                return Status switch
                {
                    "Aprovado" => System.Windows.Media.Brushes.Green,
                    "Rejeitado" => System.Windows.Media.Brushes.Red,
                    "Pendente" => System.Windows.Media.Brushes.Orange,
                    _ => System.Windows.Media.Brushes.Gray
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void GerarCodigoVerificacao()
        {
            var random = new Random();
            CodigoVerificacao = $"{Id.ToString().Substring(0, 8).ToUpper()}-{random.Next(1000, 9999)}";
            Status = "Aprovado";
        }
    }

    public class ProcessoProducao : INotifyPropertyChanged
    {
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string[] SensoresRequeridos { get; set; } = Array.Empty<string>();
        public string[] Materiais { get; set; } = Array.Empty<string>();
        public double TempoProducaoEstimado { get; set; }
        public double EmissaoCO2Estimada { get; set; }
        public double EnergiaEstimada { get; set; }

        private string _status = "Pendente";

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
        public string CodigoVerificacaoCompleto
        {
            get
            {
                var eventosHash = EventosAssociados?
                    .Select(e => e.GetHashCode())
                    .Sum() ?? 0;
            
                return $"GR-{Codigo}-{TimestampProducao:yyyyMMdd}-{eventosHash:X}";
            }
        }


        public System.Windows.Media.Brush StatusColor
        {
            get
            {
                return Status switch
                {
                    "Concluído" => System.Windows.Media.Brushes.Green,
                    "Pendente" => System.Windows.Media.Brushes.Orange,
                    "Erro" => System.Windows.Media.Brushes.Red,
                    _ => System.Windows.Media.Brushes.Gray
                };
            }
        }

        public DateTime? TimestampProducao { get; set; }
        public List<PassaporteDigital>? EventosAssociados { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }