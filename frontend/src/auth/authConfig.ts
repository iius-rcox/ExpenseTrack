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

// Scopes for authentication - request ID token only
// The backend uses AllowWebApiToBeAuthorizedByACL=true to accept ID tokens
// This avoids needing a properly exposed API scope in Azure AD
export const apiScopes = {
  all: ['openid', 'profile', 'email'],
};

// Login request configuration - use OIDC scopes to get ID token
export const loginRequest = {
  scopes: ['openid', 'profile', 'email'],
};
