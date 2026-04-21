# Kysymykset — Osa 1: Lokaali kehitys

Vastaa kysymyksiin omin sanoin. Lyhyet, selkeät vastaukset riittävät — tarkoitus on osoittaa, että olet ymmärtänyt konseptit.

---

## Clean Architecture

**1.** Selitä omin sanoin: mitä tarkoittaa, että `UploadPhotoUseCase` "ei tiedä" tallennetaanko kuva paikalliselle levylle vai Azureen? Näytä koodirivit, jotka osoittavat tämän.

> Käyttötapaus saa konstruktorissa vain `IStorageService`-rajapinnan — ei tiedä onko se `LocalStorageService` vai `AzureBlobStorageService`. Tallennuslogiikka on aina sama yksi rivi:
> ```csharp
> imageUrl = await _storageService.UploadAsync(
>     request.FileStream, request.FileName, request.ContentType, request.AlbumId);
> ```

---

**2.** Miksi `IStorageService`-rajapinta on määritelty `GalleryApi.Domain`-kerroksessa, mutta `LocalStorageService` on `GalleryApi.Infrastructure`-kerroksessa? Mitä hyötyä tästä jaosta on?

> Clean Architecturessa riippuvuudet kulkevat sisäänpäin — Domain ei saa tietää Infrastructure-toteutuksista. Rajapinta on Domainissa jotta Application-kerros voi käyttää sitä ilman sitoumusta mihinkään konkreettiseen luokkaan. Näin tallennustapa voidaan vaihtaa muuttamatta Application-koodia lainkaan.

---

**3.** Testit käyttävät `Mock<IAlbumRepository>`. Mitä mock-objekti tarkoittaa, ja miksi Clean Architecture tekee tämän testaustavan mahdolliseksi?

> Mock on testitoteutus rajapinnasta: se ei käy oikeasti tietokannassa, vaan palautat testissä haluamasi arvon. Clean Architecture mahdollistaa tämän koska käyttötapaukset riippuvat vain rajapinnoista, eivät konkreettisista luokista — joten mock voidaan injektoida suoraan ilman oikeaa tietokantaa.

---

## Salaisuuksien hallinta

**4.** Kovakoodattu API-avain on ongelma, vaikka repositorio olisi yksityinen. Selitä kaksi eri syytä miksi.

> 1. **Git-historia**: vaikka avaimen poistaa myöhemmässä commitissa, se jää historiaan ja löytyy `git log -p`:llä.
> 2. **Jaettu avain**: kaikki jotka kloonaavat repon saavat saman avaimen — ei voida tietää kuka on käyttänyt sitä tai rajata pääsyä yksittäiseltä henkilöltä.

---

**5.** Riittääkö se, että poistat kovakoodatun avaimen myöhemmässä commitissa? Perustele vastauksesi.

> Ei riitä. Git säilyttää kaiken historian, joten avain löytyy edelleen vanhasta commitista. Ainoa oikea toimenpide on vaihtaa avain välittömästi — pelkkä poistaminen koodista ei mitätöi jo vuotanutta avainta.

---

**6.** Minne User Secrets tallennetaan käyttöjärjestelmässä? (Mainitse sekä Windows- että Linux/macOS-polut.) Miksi tämä sijainti on turvallinen?

> - **Windows**: `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json`
> - **macOS/Linux**: `~/.microsoft/usersecrets/<id>/secrets.json`
>
> Sijainti on projektin hakemiston ulkopuolella, joten tiedosto ei koskaan päädy versionhallintaan.

---

## Options Pattern ja konfiguraatio

**7.** Mitä hyötyä on `IOptions<ModerationServiceOptions>`:n käyttämisestä verrattuna siihen, että luetaan arvo suoraan `IConfiguration`-rajapinnalta (`configuration["ModerationService:ApiKey"]`)?

> Options Pattern on tyyppiturvalline: kääntäjä huomaa kirjoitusvirheet ja IntelliSense toimii, toisin kuin merkkijonoavaimilla. Myös testaaminen on helpompaa — testissä voi antaa `Options.Create(new ModerationServiceOptions { ApiKey = "test" })` suoraan.

---

**8.** ASP.NET Core lukee konfiguraation useista lähteistä prioriteettijärjestyksessä. Listaa lähteet korkeimmasta matalimpaan ja selitä, mikä arvo lopulta käytetään, kun sama avain on sekä `appsettings.json`:ssa että User Secretsissä.

> Korkeimmasta matalimpaan: User Secrets → ympäristömuuttujat → `appsettings.Development.json` → `appsettings.json`. Kun sama avain on kahdessa lähteessä, korkeampi prioriteetti voittaa. Tässä tapauksessa User Secretsin arvo ylikirjoittaa `appsettings.json`n tyhjän arvon.

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

> `IsDevelopment()` sitoo tallennustavan ympäristönimeen — et voi esimerkiksi testata Azure-tallennusta kehityskoneella ilman koodimuutosta. Konfiguraatioarvo on joustavampi: vaihdat tallennustavan yhdellä asetuksella ilman koodimuutoksia.

---

## Tiedostotallennus

**10.** Kun lataat kuvan, `imageUrl`-kentän arvo on `/uploads/abc123-..../photo.jpg`. Miten tähän URL:iin pääsee selaimella? Mihin koodiin tämä perustuu?

> Lisäämällä URL suoraan selaimeen: `https://localhost:PORT/uploads/abc123.../photo.jpg`. Tämä toimii koska `Program.cs`:ssä on `app.UseStaticFiles()`, joka tarjoilee `wwwroot/`-kansion sisällön. `LocalStorageService` tallentaa tiedoston sinne ja palauttaa vastaavan polun.

---

**11.** Mitä tapahtuu jos yrität ladata tiedoston jonka MIME-tyyppi on `application/pdf`? Missä tiedostossa ja millä koodirivillä tämä käyttäytyminen on määritelty?

> Pyyntö hylätään `400 Bad Request` -vastauksella. Sallitut tyypit on määritelty `UploadPhotoUseCase.cs`:ssä taulukossa `AllowedContentTypes` ja tarkistetaan ennen tallennusta.

---

**12.** `DeletePhotoUseCase` poistaa tiedoston kutsumalla `_storageService.DeleteAsync(photo.FileName, photo.AlbumId)` — ei `photo.ImageUrl`:lla. Miksi?

> `ImageUrl` on eri muodossa lokaalissa (`/uploads/...`) ja Azuressa (koko HTTPS-URL), joten se ei kelpaa poistoon. `FileName` ja `AlbumId` ovat aina samat yksinkertaiset arvot, ja kukin `IStorageService`-toteutus osaa muodostaa niistä oikean polun tai blob-nimen.
