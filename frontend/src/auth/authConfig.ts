import { Configuration, LogLevel } from '@azure/msal-browser';

// Azure AD configuration for ExpenseFlow
export const msalConfig: Configuration = {
  auth: {
    clientId: '00435dee-8aff-429b-bab6-762973c091c4',
    authority: 'https://login.microsoftonline.com/953922e6-5370-4a01-a3d5-773a30df726b',
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            break;
          case LogLevel.Warning:
            console.warn(message);
            break;
          case LogLevel.Info:
            console.info(message);
            break;
          case LogLevel.Verbose:
            console.debug(message);
            break;
        }
      },
      logLevel: LogLevel.Warning,
    },
  },
};

// API client ID for requesting access tokens
const API_CLIENT_ID = import.meta.env.VITE_API_CLIENT_ID || '00435dee-8aff-429b-bab6-762973c091c4';

// For API calls, we request an access token with the API scope
// The backend validates the token audience matches api://{client-id}
export const apiScopes = {
  all: [`api://${API_CLIENT_ID}/access_as_user`],
};

// Login request configuration - includes API scope for access token
export const loginRequest = {
  scopes: ['openid', 'profile', 'email', `api://${API_CLIENT_ID}/access_as_user`],
};
