using './main.bicep'

// App Service nimi (maailmanlaajuisesti uniikki)
param appName = 'gallery-api-pasi'

// ModerationService API-avain -- EI tallenneta tähän, annetaan deploy-komennossa --parameters
param moderationApiKey = ''
