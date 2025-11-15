// Frontend note: keep this base URL in sync with the host allowed in the HR API's AllowFrontend CORS policy.
// Switch it to https://localhost:7162/api when you run the .NET project locally.
export const environment = {
  apiBaseUrl: 'https://commpany-api.runasp.net/api'
} as const;
