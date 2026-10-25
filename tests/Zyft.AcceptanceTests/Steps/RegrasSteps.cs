using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using Zyft.AcceptanceTests.Support;

namespace Zyft.AcceptanceTests.Steps;

[Binding]
public sealed class RegrasSteps : IDisposable
{
    private readonly AcceptanceFactory _factory = new();
    private HttpClient _web = default!;
    private HttpClient? _extension;
    private HttpResponseMessage? _response;
    private JsonElement _field;
    private JsonElement _body;

    private static HttpClient Bearer(HttpClient c, string token)
    {
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    [Given("que sou uma pessoa cadastrada com o perfil")]
    public async Task DadoPessoaComPerfil(Table table)
    {
        var anon = _factory.CreateClient();
        var reg = await anon.PostAsJsonAsync("/api/v1/auth/register", new { name = "Ana", email = $"ana{Guid.NewGuid():N}@exemplo.com", password = "Senha123" });
        reg.EnsureSuccessStatusCode();
        var token = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        _web = Bearer(_factory.CreateClient(), token);
        var profile = table.Rows.ToDictionary(r => r["campo"], r => (object)r["valor"]);
        (await _web.PutAsJsonAsync("/api/v1/profile", profile)).EnsureSuccessStatusCode();
    }

    [Given("que autorizei o uso de dados sensíveis")]
    public Task DadoConsentimentoSensivel() => Grant("SensitiveData");

    [Given("que autorizei a análise de vaga")]
    public Task DadoConsentimentoVaga() => Grant("JobAnalysis");

    private async Task Grant(string purpose) =>
        (await _web.PutAsJsonAsync($"/api/v1/consents/{purpose}", new { granted = true })).EnsureSuccessStatusCode();

    [Given("cadastrei o dado sensível {string} com o valor {string}")]
    public async Task DadoSensivel(string kind, string value) =>
        (await _web.PostAsJsonAsync("/api/v1/sensitive-data", new { kind, value })).StatusCode.Should().Be(HttpStatusCode.Created);

    [Given("que a IA responde ao de-para apontando {string} e escrevendo o valor {string}")]
    public void DadoIaMaliciosa(string key, string value) =>
        _factory.Llm.Respond = r => JsonSerializer.Serialize(new
        {
            mappings = new[] { new { fieldId = "campo", action = "fill", profileKey = key, value } },
        });

    [Given("que a IA diz que o requisito {string} é atendido pela linha {int}")]
    public void DadoIaInventaEvidencia(string requirement, int line) =>
        _factory.Llm.Respond = r => r.Prompt.StartsWith("Extraia")
            ? JsonSerializer.Serialize(new
            {
                title = "Vaga", company = "", location = "", contractType = "", schedule = "", salary = "", workModel = "", summary = "",
                requirements = new[] { new { text = requirement, kind = "obrigatório" } },
            })
            : JsonSerializer.Serialize(new { analysis = "confia", status = "atende", line });

    [Given("que pareei a extensão com a conta")]
    public async Task DadoExtensaoPareada()
    {
        var code = (await (await _web.PostAsync("/api/v1/extension/pairing-codes", null)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
        var pair = await _factory.CreateClient().PostAsJsonAsync("/api/v1/extension/pair", new { code, deviceName = "Chrome" });
        pair.EnsureSuccessStatusCode();
        var token = (await pair.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tokens").GetProperty("accessToken").GetString()!;
        _extension = Bearer(_factory.CreateClient(), token);
    }

    [When("a extensão pede sugestões para um campo {string} com rótulo {string}")]
    public Task QuandoMapeia(string type, string label) =>
        Map(new { id = "campo", tag = "input", type, label });

    [When("a extensão pede sugestões para um select {string} com a opção {string}")]
    public Task QuandoMapeiaSelect(string label, string option) =>
        Map(new { id = "campo", tag = "select", type = "select", label, options = new[] { new { value = "", text = "Selecione" }, new { value = "opt", text = option } } });

    private async Task Map(object field)
    {
        var res = await _web.PostAsJsonAsync("/api/v1/forms/mappings", new { pageUrl = "https://vagas.exemplo.com/form", fields = new[] { field } });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _field = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mappings")[0];
    }

    [When("peço a análise da vaga {string}")]
    public async Task QuandoAnalisa(string text)
    {
        _response = await _web.PostAsJsonAsync("/api/v1/jobs/analyze", new { text = text.Replace("\\n", "\n") });
        if (_response.IsSuccessStatusCode) _body = await _response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [When("a extensão tenta exportar os dados da conta")]
    public async Task QuandoExtensaoExporta() => _response = await _extension!.GetAsync("/api/v1/me/export");

    [Then("o campo vem bloqueado")]
    public void EntaoBloqueado() => _field.GetProperty("state").GetString().Should().Be("Blocked");

    [Then("o campo não traz nenhum valor")]
    public void EntaoSemValor() => _field.TryGetProperty("value", out _).Should().BeFalse();

    [Then("o campo traz um valor")]
    public void EntaoComValor() => _field.TryGetProperty("value", out _).Should().BeTrue();

    [Then("o campo não vem marcado para preencher")]
    public void EntaoDesmarcado() => _field.GetProperty("selected").GetBoolean().Should().BeFalse();

    [Then("o campo traz o valor {string}")]
    public void EntaoValor(string value) => _field.GetProperty("value").GetString().Should().Be(value);

    [Then("a origem do valor é {string}")]
    public void EntaoOrigem(string source) => _field.GetProperty("source").GetString().Should().Be(source);

    [Then("a resposta é {int} com o código {string}")]
    public async Task EntaoStatus(int status, string code)
    {
        ((int)_response!.StatusCode).Should().Be(status);
        (await _response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be(code);
    }

    [Then("o requisito {string} fica com status {string}")]
    public void EntaoRequisito(string text, string status)
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var req = _body.GetProperty("requirements").EnumerateArray().Single(r => r.GetProperty("text").GetString() == text);
        req.GetProperty("status").GetString().Should().Be(status);
        req.GetProperty("evidence").GetString().Should().BeEmpty();
    }

    [Then("a resposta não tem porcentagem nem nota")]
    public void EntaoSemNota() => _body.GetRawText().Should().NotContain("%").And.NotContainAny("score", "\"nota\"", "percent");

    public void Dispose() => _factory.Dispose();
}
