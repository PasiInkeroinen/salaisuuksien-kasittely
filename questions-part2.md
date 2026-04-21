# Kysymykset — Osa 2: Azure-julkaisu

Vastaa kysymyksiin omin sanoin. Lyhyet, selkeät vastaukset riittävät.

---

## Azure Blob Storage

**1.** Mitä eroa on `LocalStorageService.UploadAsync`:n ja `AzureBlobStorageService.UploadAsync`:n palauttamilla URL-arvoilla? Miksi ne eroavat?

> Lokaali palauttaa suhteellisen polun kuten `/uploads/{albumId}/kuva.jpg`, Azure palauttaa täydellisen HTTPS-URL:n kuten `https://stgallerygalleryapipasi.blob.core.windows.net/photos/{albumId}/kuva.jpg`. Ero johtuu siitä että tiedostot sijaitsevat eri paikoissa ja niille pääsee eri tavoin.

---

**2.** `AzureBlobStorageService` luo `BlobServiceClient`:n käyttäen `DefaultAzureCredential()` eikä yhteysmerkkijonoa. Mitä etua tästä on? Mitä `DefaultAzureCredential` tekee eri ympäristöissä?

> Ei tarvita salaisuuksia koodissa tai konfiguraatiossa. `DefaultAzureCredential` kokeilee tunnistautumistapoja järjestyksessä: kehityskoneella se käyttää Azure CLI:n kirjautumista, Azuressa se käyttää App Servicen Managed Identityä automaattisesti.

---

**3.** Blob Container luodaan `--public-access blob` -asetuksella. Mitä tämä tarkoittaa: mitä pystyy tekemään ilman tunnistautumista, ja mikä vaatii Managed Identityn?

> Kuka tahansa voi lukea (ladata) yksittäisiä blobeja suoraan URL:lla — tämä mahdollistaa kuvien näyttämisen selaimessa ilman kirjautumista. Kirjoitus, poisto ja uusien blobien lataaminen vaativat Managed Identityn kautta myönnetyn `Storage Blob Data Contributor` -roolin.

---

## Application Settings

**4.** Application Settings ylikirjoittavat `appsettings.json`:n arvot. Selitä tämä mekanismi: miten se toimii ja miksi se on hyödyllistä eri ympäristöjä varten?

> Azure App Servicen Application Settings välittyvät sovellukselle ympäristömuuttujina, joilla on korkeampi prioriteetti kuin `appsettings.json`:lla. Näin samaa julkaistua pakettia voidaan käyttää eri ympäristöissä (dev, staging, prod) ilman koodimuutoksia — vain asetukset vaihtuvat.

---

**5.** Application Settingsissa käytetään `Storage__Provider` (kaksi alaviivaa), mutta koodissa luetaan `configuration["Storage:Provider"]` (kaksoispiste). Miksi?

> Ympäristömuuttujien nimet eivät voi sisältää kaksoispistettä kaikilla alustoilla, joten ASP.NET Core käyttää `__` (kaksi alaviivaa) hierarkian erottimena ympäristömuuttujissa. Se muuntaa `Storage__Provider` automaattisesti avaimeksi `Storage:Provider`.

---

**6.** Mitkä konfiguraatioarvot soveltuvat Application Settingsiin, ja mitkä eivät? Anna esimerkki kummastakin tässä tehtävässä.

> Application Settingsiin sopii ei-salaiset asetukset kuten `Storage__Provider=azure` tai `Storage__AccountName=stgallerygalleryapipasi`. Ei sovi salaisuudet kuten API-avaimet — ne kuuluvat Key Vaultiin, koska Application Settings näkyvät selväkielisinä Azuren portaalissa.

---

## Managed Identity ja RBAC

**7.** Selitä omin sanoin: mitä tarkoittaa "System-assigned Managed Identity"? Mitä tapahtuu tälle identiteetille, jos App Service poistetaan?

> Se on Azure AD:n automaattisesti luoma identiteetti App Servicelle — sovellus voi sen avulla tunnistautua muihin Azure-palveluihin ilman salasanoja tai avaimia. Identiteetti on sidottu App Serviceen: kun App Service poistetaan, myös identiteetti poistetaan automaattisesti.

---

**8.** App Servicelle annettiin `Storage Blob Data Contributor` -rooli Storage Accountin tasolle — ei koko subscriptionin tasolle. Miksi tämä on parempi tapa? Mikä periaate tähän liittyy?

> Jos sovellus murtuisi, hyökkääjä pääsisi käsiksi vain tähän yhteen Storage Accountiin — ei kaikkiin resursseihin subscriptionissa. Tämä noudattaa pienimmän oikeuden periaatetta (Least Privilege): annetaan vain ne oikeudet jotka ovat välttämättömiä.

---


