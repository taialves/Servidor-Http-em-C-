using System.Net;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Collections.Generic;

class ServidorHttp
{
    private TcpListener Controlador { get; set; }
    private int porta { get; set; }
    private int QtdeRequests { get; set; }
    private string HtmlExemplo { get; set; }
    private SortedList<string, string> TiposMime { get; set; }
    private SortedList<string, string> DiretoriosHosts { get; set; }
    public ServidorHttp(int porta = 8080)
    {
        this.porta = porta;
        this.CriarHtmlExemplo();
        this.PopularTiposMime();
        this.PopularDiretoriosHosts();
        try
        {
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.porta);
            this.Controlador.Start();
            Console.WriteLine($"Servidor HTTP está rodando na porta {this.porta}.");
            Console.WriteLine($"Para acessar, digite no navegador: http://localhost:{porta}.");
            Task servidorHttpTask = Task.Run(() => WaitRequests());
            servidorHttpTask.GetAwaiter().GetResult();
            /*  como nao tem mais nada p ser executado depois de servidorHttpTask
                chamo GetAwaiter() para o programa ficar aguardando o termino de WaitRequests
            */
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao iniciar o servidor na porta {this.porta}:\n{e.Message}");
        }
    }

    private async Task WaitRequests()
    {
        while (true)
        {
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdeRequests++;
            Task task = Task.Run(() => ProcessarRequest(conexao, QtdeRequests));
        }
    }
    private void ProcessarRequest(Socket conexao, int numRequest)
    {
        Console.WriteLine($"Processando request #{numRequest}...\n");
        if (conexao.Connected)
        {
            byte[] bytesRequest = new byte[1024];
            conexao.Receive(bytesRequest, bytesRequest.Length, 0);
            string textRequest = Encoding.UTF8.GetString(bytesRequest)
                .Replace((char)0, ' ').Trim();

            if (textRequest.Length > 0)
            {
                Console.WriteLine($"\n{textRequest}\n");

                //capturando o Host, metodo http, o recurso e a versao http da requisicao
                string[] linhas = textRequest.Split("\r\n");
                string metodoHttp = linhas[0].Split(" ")[0];
                string recursoBuscado = linhas[0].Split(" ")[1];
                recursoBuscado = recursoBuscado.Equals("/") ? 
                    "/index.html" : recursoBuscado;

                string textoParametros = recursoBuscado.Contains("?") ?
                    recursoBuscado.Split("?")[1] : "";

                SortedList<string, string> parametros = ProcessarParametros(textoParametros);

                recursoBuscado = recursoBuscado.Split("?")[0];
                string versaoHttp = linhas[0].Split(" ")[2];
                string nomeHost = linhas[1].Split(" ")[1];

                byte[] bytesCabecalho;
                byte[] bytesConteudo;
                FileInfo fiArquivo = new FileInfo(ObterCaminhoFisicoArquivo(nomeHost,recursoBuscado));
                if (fiArquivo.Exists)
                {
                    if (TiposMime.ContainsKey(fiArquivo.Extension.ToLower()))
                    {
                        if(fiArquivo.Extension.ToLower() == ".dhtml")
                            bytesConteudo = GerarHTMLDinamico(fiArquivo.FullName, parametros);
                        else
                            bytesConteudo = File.ReadAllBytes(fiArquivo.FullName);

                        
                        string tipoMime = TiposMime[fiArquivo.Extension.ToLower()];
                        bytesCabecalho = GenerateHeader(
                            versaoHttp, tipoMime, "200", bytesConteudo.Length);
                    }
                    else
                    {
                        bytesConteudo = Encoding.UTF8.GetBytes(
                            "<h1>Erro 415 - Tipo de arquivo não suportado. </h1>");
                        bytesCabecalho = GenerateHeader(
                            versaoHttp, "text/html;charset=utf-8","415" ,bytesConteudo.Length);
                    }
                } 
                else
                {
                    bytesConteudo = Encoding.UTF8.GetBytes(
                        "<h1> Erro 404 - Arquivo não encontrado</h1>");
                    bytesCabecalho = GenerateHeader(
                        versaoHttp, "text/html;charset=utf-8", "404", bytesConteudo.Length);
                }



                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);
                bytesEnviados += conexao.Send(bytesConteudo, bytesConteudo.Length, 0);

                conexao.Close();

                Console.WriteLine($"\n{bytesEnviados} bytes enviados em resposta � requisi��o #{numRequest}.");
            }
        }
        Console.WriteLine($"\nRequest {numRequest} finished");
    }

    

    public byte[] GenerateHeader(string HttpVersion, string MimeType, string httpCode, int qtdeBytes = 0)
    {
        StringBuilder text = new StringBuilder();
        text.Append($"{HttpVersion} {httpCode}{Environment.NewLine}");
        text.Append($"Server: Servidor Http Simples 1.0{Environment.NewLine}");
        text.Append($"Content-Type: {MimeType}{Environment.NewLine}");
        text.Append($"Content-Length: {qtdeBytes}{Environment.NewLine}{Environment.NewLine}");

        return Encoding.UTF8.GetBytes(text.ToString());
    }
    private void CriarHtmlExemplo()
    {
        StringBuilder html = new StringBuilder();
        html.Append("<!DOCTYPE html>" +
            "<html lang=\"pt-br\">" +
            "<head>" +
                "<meta charset=\"UTF-8\">");
        html.Append("<meta name\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.Append("<title> P�gina Est�tica</title>" +
            "</head>" +
            "<body>");
        html.Append("<h1>P�gina Est�tica </h1>" +
            "</body>" +
            "</html>");
        this.HtmlExemplo = html.ToString();

    }
    private void PopularTiposMime()
    {
        this.TiposMime = new SortedList<string, string>();
        this.TiposMime.Add(".html", "text/html;charset=utf-8");
        this.TiposMime.Add(".htm", "text/html;charset=utf-8");
        this.TiposMime.Add(".css", "text/css");
        this.TiposMime.Add(".js", "text/javascript");
        this.TiposMime.Add(".png", "image/png");
        this.TiposMime.Add(".jpg", "image/jpeg");
        this.TiposMime.Add(".gif", "image/gif");
        this.TiposMime.Add(".svg", "image/svg+xml");
        this.TiposMime.Add(".webp", "image/webp");
        this.TiposMime.Add(".ico", "image/ico");
        this.TiposMime.Add(".woff", "font/woff");
        this.TiposMime.Add(".woff2", "font/woff2");
        this.TiposMime.Add(".dhtml", "text/html;charset=utf-8");
    }
    private void PopularDiretoriosHosts()
    {
        this.DiretoriosHosts = new SortedList<string, string>();
        this.DiretoriosHosts.Add("localhost", "C:\\Users\\tails\\OneDrive\\Área de Trabalho\\Desenvolvimento\\Apps Console\\ServidorHttp_csharp\\www\\localhost");
        this.DiretoriosHosts.Add("taialves.com", "C:\\Users\\tails\\OneDrive\\Área de Trabalho\\Desenvolvimento\\Apps Console\\ServidorHttp_csharp\\www\\taialves.com");
        this.DiretoriosHosts.Add("quitandaonline.com.br", "C:\\YouTube\\QuitandaOnline");
    }
    public string ObterCaminhoFisicoArquivo(string host, string arquivo)
    {
        string diretorio = this.DiretoriosHosts[host.Split(":")[0]];
        string caminhoArquivo = diretorio + arquivo.Replace("/", "\\");
        return caminhoArquivo;
    }

    public byte[] GerarHTMLDinamico(string caminhoArquivo, SortedList<string,string> parametros)
    {
        string coringa = "{{HtmlGerado}}";
        string htmlModelo = File.ReadAllText(caminhoArquivo);
        StringBuilder htmlGerado = new StringBuilder();
        //htmlGerado.Append("<ul>");
        //foreach (var tipo in this.TiposMime.Keys)
        //{
        //    htmlGerado.Append($"<li>Arquivos com extensão {tipo}</li>");
        //}
        //htmlGerado.Append("</ul>");

        if(parametros.Count > 0)
        {
            htmlGerado.Append("<ul>");
            foreach (var p in parametros)
            {
                htmlGerado.Append($"<li>{p.Key} = {p.Value}</li>");
            }
            htmlGerado.Append("</ul>");
        }
        else
        {
            htmlGerado.Append("<p> Nenhum parâmetro foi passado </p>");
        }


        string textoHtmlGerado = htmlModelo.Replace(coringa, htmlGerado.ToString());

        return Encoding.UTF8.GetBytes(textoHtmlGerado,0,textoHtmlGerado.Length);
    }

    private SortedList<string, string> ProcessarParametros(string textoParametros)
    {
        SortedList<string, string> parametros = new SortedList<string, string>();

        if (!string.IsNullOrEmpty(textoParametros.Trim()))
        {
            string[] paresChaveValor = textoParametros.Split("&");

            foreach(var par in paresChaveValor)
            {
                parametros.Add(par.Split("=")[0].ToLower(), par.Split("=")[1]);
            }
        }
        return parametros; 
    }

}