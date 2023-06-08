using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Synthesis;
using Microsoft.Speech.Recognition;
using System.Globalization;
using Newtonsoft.Json;
using System.Net.Http;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace voiceRecognition
{
    public partial class Form1 : Form
    {
        //Variaveis globais
        // variaveis para voz
        static CultureInfo ci = new CultureInfo("pt-BR");// linguagem utilizada
        static SpeechRecognitionEngine reconhecedor; // reconhecedor de voz
        SpeechSynthesizer resposta = new SpeechSynthesizer();// sintetizador de voz
        private const int WM_NCHITTEST = 0x84;
        private const int HT_CAPTION = 0x2;
        //private const int HTLEFT = 10;
        //private const int HTRIGHT = 11;
        //private const int HTTOP = 12;
        //private const int HTTOPLEFT = 13;
        //private const int HTTOPRIGHT = 14;
        //private const int HTBOTTOM = 15;
        //private const int HTBOTTOMLEFT = 16;
        //private const int HTBOTTOMRIGHT = 17;

        private bool isDragging = false;
        private Point dragStartPoint;
        private bool isFullScreen = false;
        private Size originalSize;
        private Bitmap bitmap;
        private Graphics graphics;

        
        private string outputPath;

        // Palavras aceitas
        public string[] listaPalavras = {
            "diga cinco países da Europa",
            "conte uma piada",
            "conte uma história",
            "diga cinco países",
            "qual é a capital do Bostil?",
            "quem foi o primeiro presidente dos Estados Unidos?",
            "quantos planetas existem no sistema solar?",
            "qual é a fórmula química da água?",
            "quem escreveu o livro 'Dom Quixote'?",
            "me recomende livros",
            "qual é o maior oceano do mundo?",
            "Conhece sociedade primitiva podcast?",
            "faça uma sujestão de banda de rock",
            "teste de audio",
            "alguns judeus estão ligados a Nova Ordem Mundial?",
            "como você sabe?",
            "eu não perguntei isso",
            "me conte a história de Napoleão",
            "conte a história da cidade de Canudos",
            "Conte a história do Sacro Império Romano Germanico",
            "Conte a história da origem da igreja católica",
            "Qual a argumentação católica contra a doutrina protestante de não venerar santos",
            "Qual a composição étnica, genética e religiosa da atual Bulgária?",
            "Quem é Jair Messias Bolsonaro?",
            "liste os 5 melhores atletas da história",
            "Resuma a história da guerra de canudos em um parágrafo",
            "explique resumidamente como é a economia na venezuela",
            "5 principais bancos brasileiros"





        };

        private const string apiKey = "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";
        private const string gptEndpoint = "https://api.openai.com/v1/completions";

        public Form1()
        {
            InitializeComponent();
            Init();
            this.Size = new Size(884, 525); // Definir o tamanho do formulário para 1100 por 600
            //txt1.KeyDown += Txt1_KeyDown;
            this.originalSize = this.Size;
            richTextBox1.KeyDown += richTextBox1_KeyDown;
            btnParar.Click += btnParar_Click;

        }

        private async Task SearchRecipesAsync(string query)
        {
            string searchText = richTextBox1.Text;
            if (!string.IsNullOrEmpty(searchText))
            {
                try
                {
                    string recipes = await GetRecipes(searchText);
                    UpdateRichTextBox(recipes);
                    SpeakText(recipes);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ocorreu um erro: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private async Task<string> GetRecipes(string searchText)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

                var jsonContent = new
                {
                    prompt = searchText,
                    model = "text-davinci-003",
                    max_tokens = 1000
                };

                var content = new StringContent(JsonConvert.SerializeObject(jsonContent), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(gptEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                dynamic data = JsonConvert.DeserializeObject(responseContent);
                return data.choices[0].text;
            }
        }


        private void UpdateRichTextBox(string recipes)
        {
            richTextBoxEdit1.Text = recipes;
        }


        private void SpeakText(string text)
        {
            resposta.SpeakAsync(text);

        }

        public void Gramatica()
        {
            try
            {
                //reconhecedor = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-us"));
                reconhecedor = new SpeechRecognitionEngine(ci);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERRO ao integrar lingua escolhida:" + ex.Message);
            }

            // criacao da gramatica simples que o programa vai entender
            // usando um objeto Choices
            var gramatica = new Choices();
            gramatica.Add(listaPalavras); // inclui a gramatica criada

            // cria o construtor gramatical
            // e passa o objeto criado com as palavras
            var gb = new GrammarBuilder();
            gb.Append(gramatica);

            // cria a instancia e carrega a engine de reconhecimento
            // passando a gramatica construida anteriomente
            try
            {
                var g = new Grammar(gb);

                try
                {
                    // carrega o arquivo de gramatica
                    reconhecedor.RequestRecognizerUpdate();
                    reconhecedor.LoadGrammarAsync(g);

                    // registra a voz como mecanismo de entrada para o evento de reconhecimento
                    reconhecedor.SpeechRecognized += Sre_Reconhecimento;

                    reconhecedor.SetInputToDefaultAudioDevice(); // microfone padrao
                    resposta.SetOutputToDefaultAudioDevice(); // auto falante padrao
                    reconhecedor.RecognizeAsync(RecognizeMode.Multiple); // multiplo reconhecimento
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERRO ao criar reconhecedor: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERRO ao criar a gramática: " + ex.Message);
            }
        }

        public void Init()
        {
            resposta.Volume = 100; // controla volume de saida
            resposta.Rate = 1; // velocidade de fala

            Gramatica(); // inicialização da gramatica
        }

        // funcao para reconhecimento de voz
        async void Sre_Reconhecimento(object sender, SpeechRecognizedEventArgs e)
        {
            string frase = e.Result.Text;
            float confidence = e.Result.Confidence;

            //if (frase.Equals("abrir edge"))
            //{
                //resposta.SpeakAsync("Abrindo o edge");
                //Process.Start("microsoft-edge:"); // Abre o Microsoft Edge
                
            //}


            if (confidence < 0.60) return;

            richTextBox1.Clear();
            richTextBox1.AppendText(frase + Environment.NewLine); // Adiciona a frase reconhecida ao richTextBox1


            string[] lines = richTextBox1.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            string ultimaLinha = lines[lines.Length - 1]; // Obtém apenas a última linha do richTextBox1
          


            await SearchRecipesAsync(ultimaLinha); // Chama a função SearchRecipesAsync() e aguarda o resultado, passando a última linha como parâmetro
            {
               
            }
        }
        private void richTextBox1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Evita que o caractere de quebra de linha seja inserido no RichTextBox
                string searchText = richTextBox1.Text.Trim();
                if (!string.IsNullOrEmpty(searchText))
                {
                    SearchRecipesAsync(searchText);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
        private void iconButton2_Click(object sender, EventArgs e)
        {
            if (isFullScreen)
            {
                isFullScreen = false;
                this.WindowState = FormWindowState.Normal;
                this.Size = new Size((int)(originalSize.Width * 1), (int)(originalSize.Height * 1));
            }
            else
            {
                isFullScreen = true;
                this.WindowState = FormWindowState.Maximized;
            }
        }

        private void iconButton3_Click(object sender, EventArgs e)
        {
            // Minimizar a janela
            this.WindowState = FormWindowState.Minimized;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST && !isFullScreen)
            {
                int x = (int)(m.LParam.ToInt64() & 0xFFFF);
                int y = (int)((m.LParam.ToInt64() & 0xFFFF0000) >> 16);
                Point pt = PointToClient(new Point(x, y));

                if (pt.Y >= 0 && pt.Y < 50)
                {
                    if (pt.X >= 0 && pt.X < ClientSize.Width)
                        m.Result = (IntPtr)HT_CAPTION;
                }
                else if (pt.Y >= 50 && pt.Y <= ClientSize.Height)
                {
                    if (pt.X >= 0 && pt.X <= ClientSize.Width)
                        m.Result = (IntPtr)HT_CAPTION;
                }
            }
        }
        private void btnParar_Click(object sender, EventArgs e)
        {
            resposta.SpeakAsyncCancelAll(); // Para a fala atual
            richTextBoxEdit1.Text = "";
        }

        private void iconButton1_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}