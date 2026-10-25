using System.Net;
using System.Net.Http.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

public class ProfileAndErrorsTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    [Fact]
    public async Task Perfil_e_criado_no_cadastro_e_pode_ser_editado()
    {
        var s = await TestApi.RegisterAsync(factory.CreateClient());
        var c = factory.Authed(s.AccessToken);
        var put = await c.PutAsJsonAsync("/api/v1/profile", new { fullName = "Ana Paula Souza", phone = "(44) 99999-2222", workModel = "Hybrid", salaryExpectation = 2500.5, birthDate = "1998-03-14" });
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var p = await (await c.GetAsync("/api/v1/profile")).JsonAsync();
        p.GetProperty("fullName").GetString().Should().Be("Ana Paula Souza");
        p.GetProperty("workModel").GetString().Should().Be("Hybrid");
        p.GetProperty("birthDate").GetString().Should().Be("1998-03-14");
    }

    [Fact]
    public async Task CRUD_de_experiencia_aceita_mes_AAAA_MM()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var add = await c.PostAsJsonAsync("/api/v1/profile/experiences", new { company = "Empresa Y", role = "Assistente", startDate = "2023-07", isCurrent = true });
        add.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await add.JsonAsync()).GetProperty("id").GetGuid();

        var upd = await c.PutAsJsonAsync($"/api/v1/profile/experiences/{id}", new { company = "Empresa Y", role = "Analista", startDate = "2023-07-01", endDate = "2024-01", isCurrent = false });
        (await upd.JsonAsync()).GetProperty("role").GetString().Should().Be("Analista");

        var list = await (await c.GetAsync("/api/v1/profile/experiences")).JsonAsync();
        list.GetArrayLength().Should().Be(1);
        (await c.DeleteAsync($"/api/v1/profile/experiences/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await c.DeleteAsync($"/api/v1/profile/experiences/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Regra_de_negocio_violada_e_422_com_code()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var res = await c.PostAsJsonAsync("/api/v1/profile/experiences", new { company = "Y", role = "A", startDate = "2024-05", endDate = "2023-01" });
        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var j = await res.JsonAsync();
        j.GetProperty("code").GetString().Should().Be("experience.invalid_period");
        j.GetProperty("detail").GetString().Should().Be("A data de término não pode ser anterior à data de início.");

        await c.PostAsJsonAsync("/api/v1/profile/skills", new { name = "Excel" });
        (await c.PostAsJsonAsync("/api/v1/profile/skills", new { name = "excel" })).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Data_em_formato_invalido_e_400()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var res = await c.PostAsJsonAsync("/api/v1/profile/experiences", new { company = "Y", role = "A", startDate = "ontem" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await res.JsonAsync()).GetProperty("errors").GetProperty("startDate")[0].GetString().Should().Contain("Data inválida");
    }

    [Fact]
    public async Task Campo_obrigatorio_ausente_e_400_com_mensagem_pt_BR()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var res = await c.PostAsJsonAsync("/api/v1/profile/educations", new { institution = "UEM", course = "ADM" });
        var errors = (await res.JsonAsync()).GetProperty("errors");
        errors.GetProperty("level")[0].GetString().Should().Be("Informe o nível.");
        errors.GetProperty("startDate")[0].GetString().Should().Be("Informe a data de início.");
    }

    [Fact]
    public async Task Recurso_de_outra_pessoa_e_404()
    {
        var a = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var b = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var created = await (await a.PostAsJsonAsync("/api/v1/forms/completions", new { portal = "vagas.exemplo.com", jobTitle = "Assistente", fieldsFilled = 3 })).JsonAsync();
        var id = created.GetProperty("id").GetGuid();
        (await b.GetAsync($"/api/v1/applications/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await b.DeleteAsync($"/api/v1/applications/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await a.GetAsync($"/api/v1/applications/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_lista_so_o_que_falta()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var before = await (await c.GetAsync("/api/v1/profile/missing")).JsonAsync();
        before.EnumerateArray().Select(x => x.GetProperty("key").GetString()).Should().Contain("personal.phone").And.NotContain("personal.fullName");
        await c.PutAsJsonAsync("/api/v1/profile", new { fullName = "Pessoa Teste", phone = "(44) 9999-0000" });
        var after = await (await c.GetAsync("/api/v1/profile/missing")).JsonAsync();
        after.EnumerateArray().Select(x => x.GetProperty("key").GetString()).Should().NotContain("personal.phone");
    }

    [Fact]
    public async Task Rota_inexistente_da_API_e_404_ProblemDetails()
    {
        var res = await factory.CreateClient().GetAsync("/api/v1/nao-existe");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await res.JsonAsync()).GetProperty("detail").GetString().Should().Be("Rota não encontrada.");
    }

    [Fact]
    public async Task Respostas_aprovadas_CRUD()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var a = await (await c.PostAsJsonAsync("/api/v1/answers", new { question = "Por que nós?", text = "Porque sim." })).JsonAsync();
        var id = a.GetProperty("id").GetGuid();
        (await (await c.PutAsJsonAsync($"/api/v1/answers/{id}", new { question = "Por que nós?", text = "Outro texto." })).JsonAsync())
            .GetProperty("text").GetString().Should().Be("Outro texto.");
        (await c.DeleteAsync($"/api/v1/answers/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Sites_autorizados_normalizam_origem_e_recusam_duplicata()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var r = await c.PostAsJsonAsync("/api/v1/authorized-sites", new { origin = "https://vagas.exemplo.com/candidatura/1" });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        (await r.JsonAsync()).GetProperty("origin").GetString().Should().Be("https://vagas.exemplo.com");
        (await c.PostAsJsonAsync("/api/v1/authorized-sites", new { origin = "https://vagas.exemplo.com" })).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
