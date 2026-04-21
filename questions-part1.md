# Kysymykset — Osa 1: Lokaali kehitys

Vastaa kysymyksiin omin sanoin. Lyhyet, selkeät vastaukset riittävät — tarkoitus on osoittaa, että olet ymmärtänyt konseptit.

---

## Clean Architecture

**1.** Selitä omin sanoin: mitä tarkoittaa, että `UploadPhotoUseCase` "ei tiedä" tallennetaanko kuva paikalliselle levylle vai Azureen? Näytä koodirivit, jotka osoittavat tämän.

> `UploadPhotoUseCase` käyttää ainoastaan `IStorageService`-rajapintaa — se ei tunne `LocalStorageService`- tai `AzureBlobStorageService`-luokkia lainkaan. DI-säiliö päättää ajon aikana kumpi toteutus injektoidaan. Tämä näkyy konstruktorissa ja latauslogiikassa:
> ```csharp
> // Konstruktori — vain rajapinta, ei konkreettista luokkaa
> public UploadPhotoUseCase(
>     IPhotoRepository photoRepository,
>     IAlbumRepository albumRepository,
>     IStorageService storageService)   // ← rajapinta, ei LocalStorageService tai AzureBlobStorageService
> 
> // Käyttö — täsmälleen sama rivi riippumatta tallennuspaikasta
> imageUrl = await _storageService.UploadAsync(
>     request.FileStream, request.FileName, request.ContentType, request.AlbumId);
> ```

---

**2.** Miksi `IStorageService`-rajapinta on määritelty `GalleryApi.Domain`-kerroksessa, mutta `LocalStorageService` on `GalleryApi.Infrastructure`-kerroksessa? Mitä hyötyä tästä jaosta on?

> Clean Architecturessa riippuvuudet kulkevat vain sisäänpäin: Domain ei saa riippua Infrastructure-kerroksesta. Rajapinta määritellään Domainissa, jotta Application-kerros voi käyttää sitä tietämättä mitään konkreettisesta toteutuksesta. Hyöty: tallennustapa voidaan vaihtaa (lokaali → Azure Blob Storage) muuttamatta yhtäkään riviä Domain- tai Application-kerroksessa — ainoastaan `DependencyInjection.cs`:n yksi rivi vaihtuu.

---

**3.** Testit käyttävät `Mock<IAlbumRepository>`. Mitä mock-objekti tarkoittaa, ja miksi Clean Architecture tekee tämän testaustavan mahdolliseksi?

> Mock-objekti on väärennös oikeasta toteutuksesta: se toteuttaa rajapinnan, mutta ei oikeasti käy tietokannassa — voit ohjelmoida sen palauttamaan mitä tahansa. Clean Architecture tekee tämän mahdolliseksi koska käyttötapaukset riippuvat pelkästään rajapinnoista (`IAlbumRepository`), ei konkreettisista EF Core -luokista. Testi voi siis luoda `Mock<IAlbumRepository>` ja injektoida sen suoraan käyttötapaukselle ilman tietokantaa.

---

## Salaisuuksien hallinta

**4.** Kovakoodattu API-avain on ongelma, vaikka repositorio olisi yksityinen. Selitä kaksi eri syytä miksi.

> 1. **Git-historia ei unohda**: vaikka avain poistetaan myöhemmässä commitissa, se jää git-historiaan ikuisesti. Kuka tahansa jolla on pääsy repositorioon voi kaivaa sen esiin `git log -p`-komennolla.
> 2. **Kaikki kehittäjät jakavat saman avaimen**: kun tiimin kaikki jäsenet kloonaavat projektin, he saavat saman avaimen. Ei voida tietää kuka on käyttänyt avainta, eikä yksittäisen kehittäjän pääsyä voi sulkea ilman että vaihdetaan avain kaikilta.

---

**5.** Riittääkö se, että poistat kovakoodatun avaimen myöhemmässä commitissa? Perustele vastauksesi.

> Ei riitä. Git tallentaa jokaisen commitin pysyvästi, mukaan lukien sen muutoksen jossa avain lisättiin. Vaikka uusin versio tiedostosta ei sisällä avainta, se löytyy edelleen `git log --all -p`-komennolla. Oikea toimenpide on avain**vaihtaa** välittömästi vanhan mitätöimiseksi — sen poistaminen koodista on välttämätöntä, mutta ei yksinään riittävää.

---

**6.** Minne User Secrets tallennetaan käyttöjärjestelmässä? (Mainitse sekä Windows- että Linux/macOS-polut.) Miksi tämä sijainti on turvallinen?

> - **Windows**: `%APPDATA%\Microsoft\UserSecrets\gallery-api-dev-secrets\secrets.json`
> - **macOS/Linux**: `~/.microsoft/usersecrets/gallery-api-dev-secrets/secrets.json`
>
> Sijainti on turvallinen koska se on projektin hakemiston **ulkopuolella** — tiedosto ei koskaan päädy versionhallintaan, vaikka kehittäjä unohtaisi tarkistaa `.gitignore`n. Jokainen kehittäjä hallinnoi omia salaisuuksiaan omalla koneellaan.

---

## Options Pattern ja konfiguraatio

**7.** Mitä hyötyä on `IOptions<ModerationServiceOptions>`:n käyttämisestä verrattuna siihen, että luetaan arvo suoraan `IConfiguration`-rajapinnalta (`configuration["ModerationService:ApiKey"]`)?

> Options Pattern tarjoaa kolme keskeistä etua: (1) **Tyyppiturvallisuus** — kääntäjä huomaa kirjoitusvirheet, `IConfiguration`-avaimet ovat pelkkiä merkkijonoja joiden kirjoitusvirhe huomataan vasta ajonaikana. (2) **IntelliSense** — `_options.ApiKey` saa automaattitäydennyksen, merkkijonoavain ei. (3) **Testattavuus** — testissä voidaan antaa `Options.Create(new ModerationServiceOptions { ApiKey = "test" })` suoraan, sen sijaan että pitäisi mockata `IConfiguration`.

---

**8.** ASP.NET Core lukee konfiguraation useista lähteistä prioriteettijärjestyksessä. Listaa lähteet korkeimmasta matalimpaan ja selitä, mikä arvo lopulta käytetään, kun sama avain on sekä `appsettings.json`:ssa että User Secretsissä.

> Prioriteettijärjestys (korkein ensin):
> 1. User Secrets (vain kehitysmoodissa)
> 2. Ympäristömuuttujat
> 3. `appsettings.Development.json`
> 4. `appsettings.json`
>
> Kun `ModerationService:ApiKey` on sekä `appsettings.json`:ssa (tyhjä `""`) että User Secretsissä (`"sk-moderation-local-dev-key"`), käytetään User Secretsin arvoa, koska se on korkeammalla prioriteetilla ja ylikirjoittaa alemman lähteen arvon.

---

**9.** `DependencyInjection.cs`:ssä valitaan tallennustoteutus näin:

```csharp
var provider = configuration["Storage:Provider"] ?? "local";
if (provider == "azure")
    services.AddScoped<IStorageService, AzureBlobStorageService>();
else
    services.AddScoped<IStorageService, LocalStorageService>();
```

Miksi käytetään konfiguraatioarvoa `env.IsDevelopment()`-tarkistuksen sijaan? Mitä haittaa olisi `if (env.IsDevelopment()) { käytä lokaalia }`-lähestymistavassa?

> `env.IsDevelopment()` sitoo tallennustavan ympäristöön, ei konfiguraatioon. Haitat: (1) Jos halutaan testata Azure Blob Storagea kehityskoneella, se on mahdotonta ilman koodimuutosta. (2) Jos staging-ympäristö ei ole `Development`, se käyttäisi silti lokaalia — tämä ei ole tahattoman konfiguraatiomuutoksen ehkäisyä vaan kovakoodausta. Konfiguraatioarvo on eksplisiittinen: tiedät aina tarkalleen mitä käytetään katsomalla yhtä arvoa.

---

## Tiedostotallennus

**10.** Kun lataat kuvan, `imageUrl`-kentän arvo on `/uploads/abc123-..../photo.jpg`. Miten tähän URL:iin pääsee selaimella? Mihin koodiin tämä perustuu?

> URL:iin pääsee suoraan selaimella lisäämällä se palvelimen osoitteen perään, esim. `https://localhost:PORT/uploads/abc123.../photo.jpg`. Tämä perustuu `Program.cs`:n riviin `app.UseStaticFiles()`, joka käskee ASP.NET Coren tarjoilla `wwwroot/`-kansion sisällön HTTP:nä. `LocalStorageService` tallentaa tiedoston `wwwroot/uploads/{albumId}/{fileName}` ja palauttaa polun `/uploads/{albumId}/{fileName}` — juuri sen osuuden jonka `UseStaticFiles()` tarjoilee.

---

**11.** Mitä tapahtuu jos yrität ladata tiedoston jonka MIME-tyyppi on `application/pdf`? Missä tiedostossa ja millä koodirivillä tämä käyttäytyminen on määritelty?

> Pyyntö hylätään `400 Bad Request` -vastauksella ja virheviestiä `"Tiedostotyyppi 'application/pdf' ei ole sallittu."`. Tämä on määritelty tiedostossa `GalleryApi.Application/UseCases/Photos/UploadPhotoUseCase.cs`, rivit:
> ```csharp
> private static readonly string[] AllowedContentTypes =
>     ["image/jpeg", "image/png", "image/webp", "image/gif"];
> ...
> if (!AllowedContentTypes.Contains(request.ContentType))
>     return Result<PhotoDto>.Failure(...);
> ```

---

**12.** `DeletePhotoUseCase` poistaa tiedoston kutsumalla `_storageService.DeleteAsync(photo.FileName, photo.AlbumId)` — ei `photo.ImageUrl`:lla. Miksi?

> `ImageUrl` on eri formaateissa eri tallennustavoissa: lokaalisti se on `/uploads/{albumId}/photo.jpg` ja Azure Blob Storagessa se on täydellinen HTTPS-URL kuten `https://stgallery....blob.core.windows.net/photos/{albumId}/photo.jpg`. `FileName` ja `AlbumId` sen sijaan ovat aina samat yksinkertaiset arvot — `IStorageService`-toteutus itse tietää miten niistä muodostetaan oikea polku tai blob-nimi. Näin poistologiikka toimii oikein molemmilla tallennustavoilla ilman muutoksia.
