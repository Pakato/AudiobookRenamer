/*
 * ============================================================================
 * GOODREADS SCRAPER - EXEMPLO DE CONFIGURAÇÃO E USO
 * ============================================================================
 * 
 * Este arquivo demonstra como configurar e usar o GoodreadsScraperService
 * em diferentes cenários. NÃO é necessário incluí-lo no projeto de produção.
 * 
 * ============================================================================
 */

#region Configuração via código (Program.cs ou Startup.cs)

/*
 * OPÇÃO 1: Configuração direta via código
 * ========================================
 * 
 * using Goodreads.Scraper.Extensions;
 * 
 * var builder = Host.CreateApplicationBuilder(args);
 * 
 * // Adiciona o Goodreads Scraper com configurações padrão
 * builder.Services.AddGoodreadsScraper();
 * 
 * // OU com configurações personalizadas
 * builder.Services.AddGoodreadsScraper(settings =>
 * {
 *     settings.RequestDelayMs = 3000;           // 3 segundos entre requisições
 *     settings.MaxRetries = 5;                  // 5 tentativas
 *     settings.TimeoutSeconds = 60;             // Timeout de 60 segundos
 *     settings.MaxSearchResults = 20;           // Máximo de 20 resultados por busca
 *     
 *     // Configuração de proxy (opcional)
 *     settings.UseProxy = true;
 *     settings.Proxy = new ProxySettings
 *     {
 *         Address = "http://proxy.example.com:8080",
 *         Username = "user",
 *         Password = "password",
 *         BypassOnLocal = true,
 *         
 *         // Proxies rotativos (opcional)
 *         RotatingProxies =
 *         [
 *             new ProxyEndpoint { Address = "http://proxy1.example.com:8080" },
 *             new ProxyEndpoint { Address = "http://proxy2.example.com:8080", Username = "user", Password = "pass" }
 *         ]
 *     };
 *     
 *     // User-Agents personalizados (opcional)
 *     settings.CustomUserAgents =
 *     [
 *         "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/131.0.0.0 Safari/537.36",
 *         "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_5) AppleWebKit/537.36 Chrome/130.0.0.0 Safari/537.36"
 *     ];
 * });
 */

#endregion

#region Configuração via appsettings.json

/*
 * OPÇÃO 2: Configuração via arquivo appsettings.json
 * ==================================================
 * 
 * Adicione a seguinte seção ao seu appsettings.json:
 * 
 * {
 *   "GoodreadsScraper": {
 *     "BaseUrl": "https://www.goodreads.com",
 *     "RequestDelayMs": 2000,
 *     "MaxRetries": 3,
 *     "RetryBaseDelaySeconds": 2,
 *     "TimeoutSeconds": 30,
 *     "MaxSearchResults": 10,
 *     "UseProxy": false,
 *     "Proxy": {
 *       "Address": "http://proxy.example.com:8080",
 *       "Username": "user",
 *       "Password": "password",
 *       "BypassOnLocal": true,
 *       "RotatingProxies": [
 *         {
 *           "Address": "http://proxy1.example.com:8080"
 *         },
 *         {
 *           "Address": "http://proxy2.example.com:8080",
 *           "Username": "user2",
 *           "Password": "password2"
 *         }
 *       ]
 *     },
 *     "CustomUserAgents": []
 *   }
 * }
 * 
 * E no código:
 * 
 * using Goodreads.Scraper.Extensions;
 * 
 * var builder = Host.CreateApplicationBuilder(args);
 * builder.Services.AddGoodreadsScraper(builder.Configuration);
 */

#endregion

#region Exemplo de uso do serviço

/*
 * EXEMPLO DE USO
 * ==============
 * 
 * public class AudiobookProcessor
 * {
 *     private readonly IGoodreadsScraperService _scraper;
 *     private readonly ILogger<AudiobookProcessor> _logger;
 * 
 *     public AudiobookProcessor(
 *         IGoodreadsScraperService scraper,
 *         ILogger<AudiobookProcessor> logger)
 *     {
 *         _scraper = scraper;
 *         _logger = logger;
 *     }
 * 
 *     public async Task ProcessAudiobookAsync(string fileName)
 *     {
 *         // Extrai o nome do livro do arquivo
 *         var bookName = Path.GetFileNameWithoutExtension(fileName);
 *         
 *         // Busca e obtém metadados em uma única chamada
 *         var metadata = await _scraper.SearchAndGetMetadataAsync(bookName);
 *         
 *         if (metadata == null)
 *         {
 *             _logger.LogWarning("Nenhum resultado encontrado para: {BookName}", bookName);
 *             return;
 *         }
 *         
 *         // Baixa a capa do livro
 *         if (!string.IsNullOrEmpty(metadata.CoverImageUrl))
 *         {
 *             metadata.CoverImageData = await _scraper.DownloadCoverImageAsync(metadata.CoverImageUrl);
 *         }
 *         
 *         // Exibe os metadados encontrados
 *         _logger.LogInformation("""
 *             Metadados encontrados:
 *             - Título: {Title}
 *             - Autor: {Author}
 *             - Série: {Series}
 *             - Ano: {Year}
 *             - Gênero: {Genre}
 *             - Rating: {Rating}
 *             - ISBN-13: {Isbn13}
 *             - Capa: {HasCover}
 *             """,
 *             metadata.Title,
 *             metadata.PrimaryAuthor,
 *             metadata.FormattedSeries,
 *             metadata.Year,
 *             metadata.PrimaryGenre,
 *             metadata.Rating,
 *             metadata.Isbn13,
 *             metadata.CoverImageData != null ? "Sim" : "Não");
 *         
 *         // Aplica as tags ID3 ao arquivo de áudio usando TagLib ou ATL
 *         // ... código de aplicação das tags ...
 *     }
 *     
 *     public async Task SearchWithSelectionAsync(string query)
 *     {
 *         // Busca livros
 *         var results = await _scraper.SearchBooksAsync(query);
 *         
 *         if (results.Count == 0)
 *         {
 *             Console.WriteLine("Nenhum resultado encontrado.");
 *             return;
 *         }
 *         
 *         // Mostra opções para o usuário
 *         Console.WriteLine($"Encontrados {results.Count} resultados:");
 *         for (int i = 0; i < results.Count; i++)
 *         {
 *             var r = results[i];
 *             Console.WriteLine($"  {i + 1}. {r.Title} - {string.Join(", ", r.Authors)} ({r.Year})");
 *         }
 *         
 *         // Usuário seleciona
 *         Console.Write("Selecione (1-{0}): ", results.Count);
 *         if (int.TryParse(Console.ReadLine(), out var selection) && 
 *             selection >= 1 && selection <= results.Count)
 *         {
 *             var selected = results[selection - 1];
 *             
 *             // Obtém metadados completos do livro selecionado
 *             var metadata = await _scraper.GetBookMetadataAsync(selected.BookId);
 *             
 *             // ... processa metadados ...
 *         }
 *     }
 * }
 */

#endregion

#region Fallback com PuppeteerSharp (JavaScript dinâmico)

/*
 * FALLBACK: PuppeteerSharp para páginas com JavaScript
 * ====================================================
 * 
 * Se o Goodreads implementar proteções pesadas (CAPTCHAs, conteúdo via JS),
 * use PuppeteerSharp como fallback.
 * 
 * 1. Instale o pacote:
 *    dotnet add package PuppeteerSharp
 * 
 * 2. Código de exemplo:
 * 
 * using PuppeteerSharp;
 * 
 * public class PuppeteerFallbackScraper
 * {
 *     public async Task<string> FetchPageWithBrowserAsync(string url)
 *     {
 *         // Download do browser na primeira execução
 *         using var browserFetcher = new BrowserFetcher();
 *         await browserFetcher.DownloadAsync();
 *         
 *         // Lança browser headless
 *         await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
 *         {
 *             Headless = true,
 *             Args = new[]
 *             {
 *                 "--no-sandbox",
 *                 "--disable-setuid-sandbox",
 *                 "--disable-dev-shm-usage"
 *             }
 *         });
 *         
 *         await using var page = await browser.NewPageAsync();
 *         
 *         // Configura User-Agent realista
 *         await page.SetUserAgentAsync(
 *             "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
 *             "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
 *         
 *         // Navega para a página
 *         await page.GoToAsync(url, new NavigationOptions
 *         {
 *             WaitUntil = new[] { WaitUntilNavigation.NetworkIdle2 },
 *             Timeout = 60000
 *         });
 *         
 *         // Aguarda elementos específicos carregarem
 *         await page.WaitForSelectorAsync("h1[data-testid='bookTitle']", 
 *             new WaitForSelectorOptions { Timeout = 10000 });
 *         
 *         // Obtém o HTML renderizado
 *         return await page.GetContentAsync();
 *     }
 *     
 *     public async Task SolveAndContinueAsync(IPage page)
 *     {
 *         // Detecta CAPTCHA
 *         var captchaElement = await page.QuerySelectorAsync("#captcha");
 *         if (captchaElement != null)
 *         {
 *             // Opções:
 *             // 1. Usar serviço de resolução de CAPTCHA (2captcha, Anti-Captcha)
 *             // 2. Pausar e aguardar intervenção manual
 *             // 3. Tentar novamente com IP diferente (proxy rotation)
 *             
 *             throw new Exception("CAPTCHA detected. Manual intervention required.");
 *         }
 *     }
 * }
 */

#endregion
