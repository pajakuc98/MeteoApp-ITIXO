MeteoApp - Aplikace pro sběr dat z meteostanice
Tato konzolová aplikace je navržena pro automatické stahování dat z meteostanice, jejich transformaci do formátu JSON a následné uložení do SQL databáze. Aplikace je robustní vůči nedostupnosti meteostanice a umožňuje interaktivní ovládání přes menu, včetně spuštění automatického hodinového stahování.

Klíčové vlastnosti
Konfigurovatelná URL: URL meteostanice je snadno nastavitelná v souboru appsettings.json.

Transformace dat: Stahuje XML data, parsuje je a převádí do JSON formátu.

Ukládání do SQL databáze: Data jsou ukládána do SQL Server databáze (s podporou LocalDB) pomocí Entity Framework Core.

Ošetření nedostupnosti: Při nedostupnosti meteostanice uloží prázdný záznam s informací o nedostupnosti.

Interaktivní menu: Umožňuje ruční stažení dat (z výchozí nebo vlastní URL), zobrazení posledních záznamů a ovládání automatického timeru.

Hodinový timer: Volitelný timer pro automatické stahování dat každou hodinu.

Použité technologie
Jazyk: C#

.NET Runtime: .NET 8

Databáze: SQL Server (testováno s LocalDB)

ORM: Entity Framework Core

HTTP Klient: HttpClient (součást .NET)

XML parsování: LINQ to XML (System.Xml.Linq)

JSON serializace: System.Text.Json

Konfigurace: Microsoft.Extensions.Configuration

Dependency Injection: Microsoft.Extensions.DependencyInjection a Microsoft.Extensions.Hosting

Timer: System.Timers.Timer

Nastavení a spuštění
Pro spuštění aplikace postupujte podle následujících kroků:

Klonujte repozitář:

git clone <URL_VAŠEHO_REPOZITÁŘE>
cd MeteoApp

Nainstalujte NuGet balíčky:
Ujistěte se, že máte nainstalovány všechny potřebné NuGet balíčky. Můžete je nainstalovat pomocí Visual Studia (Manage NuGet Packages) nebo z příkazové řádky:

dotnet restore

Potřebné balíčky zahrnují:

Microsoft.Extensions.Configuration

Microsoft.Extensions.Configuration.Json

Microsoft.Extensions.Hosting

Microsoft.EntityFrameworkCore.SqlServer

Microsoft.EntityFrameworkCore.Tools

Microsoft.Extensions.Http

Konfigurace appsettings.json:
V kořenovém adresáři projektu MeteoApp by měl být soubor appsettings.json. Upravte v něm URL meteostanice a připojovací řetězec k vaší SQL databázi. Pro vývoj je doporučeno použít SQL Server LocalDB.

{
  "WeatherStationUrl": "https://pastebin.com/raw/PMQueqDV",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MeteoDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}

Ujistěte se, že v jeho vlastnostech (v Solution Exploreru) je nastaveno "Copy to Output Directory" na "Copy if newer".

Databázové migrace:
Pro vytvoření a aktualizaci databázového schématu spusťte migrace Entity Framework Core. Otevřete Package Manager Console (v Visual Studiu: Tools -> NuGet Package Manager -> Package Manager Console) a ujistěte se, že je vybrán projekt MeteoApp. Poté spusťte:

Add-Migration InitialCreate
Update-Database

Pokud používáte dotnet CLI, ujistěte se, že máte nainstalovaný dotnet-ef nástroj (dotnet tool install --global dotnet-ef), a poté spusťte:

dotnet ef migrations add InitialCreate
dotnet ef database update

Spuštění aplikace:
Aplikaci můžete spustit z Visual Studia (F5) nebo z příkazové řádky v kořenovém adresáři projektu:

dotnet run

Aplikace se spustí a zobrazí interaktivní menu, kde můžete vybrat požadovanou akci. Pro pravidelné hodinové stahování je nutné aplikaci naplánovat (např. pomocí Windows Task Scheduleru na Windows nebo cronu na Linuxu) a spouštět ji s výchozí volbou (např. dotnet run -- --auto-download pokud byste chtěli přidat argument pro automatické spuštění stahování bez menu).

Odhad času implementace
Celková přibližná doba implementace mi zabrala 4-5 hodin. Tento čas zahrnuje nastavení projektu, instalaci a konfiguraci balíčků, implementaci logiky pro stahování, parsování a ukládání dat, ošetření chybových stavů, implementaci menu a timeru, a také ladění a přípravu dokumentace.