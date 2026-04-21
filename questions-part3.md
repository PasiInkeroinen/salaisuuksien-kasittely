# Kysymykset — Osa 3: Key Vault ja Infrastructure as Code

Vastaa kysymyksiin omin sanoin. Lyhyet, selkeät vastaukset riittävät.

---

## Key Vault

**1.** Miksi `ModerationService:ApiKey` tallennettiin Key Vaultiin eikä Application Settingsiin? Mitä lisäarvoa Key Vault tuo Application Settingsiin verrattuna?

> Application Settings näkyvät selväkielisinä Azuren portaalissa, joten ne eivät sovi salaisuuksille. Key Vault tallentaa arvot salattuina, kirjaa käyttötapahtumat lokiin ja pääsyoikeudet hallitaan RBAC:lla erikseen.

---

**2.** Key Vault -salaisuuden nimi on `ModerationService--ApiKey` (kaksi väliviivaa), mutta koodissa se luetaan `configuration["ModerationService:ApiKey"]` (kaksoispiste). Miksi käytetään `--`?

> Key Vault ei salli kaksoispistettä salaisuuksien nimissä. ASP.NET Core Key Vault -laajennus muuntaa automaattisesti `--` kaksoispistteeksi konfiguraatioavaimessa, joten sovelluksen koodi pysyy muuttumattomana.

---

**3.** `Program.cs`:ssä Key Vault lisätään konfiguraatiolähteeksi `if (!string.IsNullOrEmpty(keyVaultUrl))`-ehdolla. Miksi tämä ehto on tärkeä? Mitä tapahtuisi ilman sitä?

> Ilman ehtoa sovellus yrittäisi yhdistää Key Vaultiin myös kehityskoneella, missä URL:ia ei ole määritelty. Tämä aiheuttaisi käynnistysvirheen. Ehto tekee Key Vaultista vapaaehtoisen: paikalliset User Secrets toimivat kehityksessä, Key Vault aktivoituu vain Azuressa.

---

**4.** Kun sovellus on käynnissä Azuressa, konfiguraation prioriteettijärjestys on: Key Vault → Application Settings → `appsettings.json`. Selitä millä arvolla `ModerationService:ApiKey` lopulta ladataan — ja käy läpi jokainen askel siitä, miten arvo päätyy sovelluksen `IOptions<ModerationServiceOptions>`:iin.

> Arvo ladataan Key Vaultista, koska sillä on korkein prioriteetti. Vaiheet: (1) `Program.cs` lisää `AddAzureKeyVault` konfiguraatiolähteeksi. (2) Key Vault hakee salaisuuden `ModerationService--ApiKey` ja muuntaa sen avaimeksi `ModerationService:ApiKey`. (3) `services.Configure<ModerationServiceOptions>()` sitoo konfiguraation POCO-luokkaan. (4) `ModerationServiceClient` saa arvon `IOptions<ModerationServiceOptions>`:n kautta.

---

**5.** Mitä eroa on `Key Vault Secrets User` ja `Key Vault Secrets Officer` -roolien välillä? Miksi annettiin nimenomaan `Secrets User`?

> `Secrets User` voi lukea salaisuuksia. `Secrets Officer` voi myös luoda, päivittää ja poistaa niitä. Sovellus tarvitsee vain lukuoikeuden, joten `Secrets User` riittää — pienimmän oikeuden periaatteen mukaisesti.

---

## Infrastructure as Code (Bicep)

**6.** Bicep-templatessa RBAC-roolimääritykset tehdään suoraan (`storageBlobRole`, `keyVaultSecretsRole`). Mitä etua tällä on verrattuna siihen, että ajat erilliset `az role assignment create` -komennot käsin?

> Roolit luodaan automaattisesti joka kerta kun deployment ajetaan — ei tarvitse muistaa ajaa erillisiä komentoja. Infrastruktuuri on myös toistettava: uuden ympäristön roolimääritykset ovat aina oikein ilman manuaalisia vaiheita.

---

**7.** Bicep-parametritiedostossa `main.bicepparam` on `param moderationApiKey = ''` — arvo jätetään tyhjäksi. Miksi? Miten oikea arvo annetaan?

> Salaisuutta ei tallenneta tiedostoon eikä versionhallintaan. Oikea arvo annetaan deployment-komennon yhteydessä: `az deployment group create ... --parameters moderationApiKey="oikea-arvo"`.

---

**8.** Bicep-templatessa `webApp`-resurssin `identity`-lohkossa on `type: 'SystemAssigned'`. Mitä tämä tekee, ja mitä manuaalista komentoa se korvaa?

> Se luo App Servicelle System-assigned Managed Identityn automaattisesti deploymentin aikana. Korvaa manuaalisen komennon `az webapp identity assign --name ... --resource-group ...`.

---

**9.** RBAC-roolimäärityksen nimi generoidaan `guid()`-funktiolla:

```bicep
name: guid(storageAccount.id, webApp.identity.principalId, 'StorageBlobDataContributor')
```

Miksi nimi generoidaan näin eikä esimerkiksi kovakoodatulla merkkijonolla? Mitä tapahtuisi jos nimi olisi sama kaikissa deploymenteissa?

> Roolimäärityksen nimen täytyy olla globaalisti uniikki GUID. Jos käytettäisiin kovakoodattua nimeä, se törmäisi eri resursseille tai ympäristöille luotuihin määrityksiin. `guid()` generoi deterministisen, uniikkin GUID:n syötteiden perusteella, joten sama deployment ei luo duplikaatteja mutta eri resurssit saavat eri nimen.

---

**10.** Olet nyt rakentanut saman infrastruktuurin kahdella tavalla: manuaalisesti (Osat 2 & 3) ja Bicepillä (Osa 3). Kuvaile konkreettisesti yksi tilanne, jossa IaC-lähestymistapa on selvästi manuaalista parempi. Kuvaile myös tilanne, jossa manuaalinen tapa riittää.

> IaC on selkeästi parempi kun pitää luoda sama infrastruktuuri uudelleen — esimerkiksi uusi opiskelija kloonaa repon ja ajaa yhden komennon, kaikki resurssit ja roolit syntyvt oikein. Manuaalinen tapa riittää kun haluaa nopeasti kokeilla jotain kertaluonteisesti portaalissa, kuten tarkistaa että jokin asetus toimii ennen kuin kirjoittaa sen Bicepiin.
