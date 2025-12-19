import { useMsal } from '@azure/msal-react';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { apiScopes } from './authConfig';

/**
 * Hook to get an access token for API calls.
 * Handles silent token acquisition and prompts for login if needed.
 */
export function useApiToken() {
  const { instance, accounts } = useMsal();

  const getToken = async (): Promise<string | null> => {
    if (accounts.length === 0) {
      return null;
    }

    const request = {
      scopes: apiScopes.all,
      account: accounts[0],
    };

    try {
      const response = await instance.acquireTokenSilent(request);
      // Use idToken since we're using OIDC scopes (openid, profile, email)
      return response.idToken;
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        // Token expired or consent required - redirect to login
        try {
          const response = await instance.acquireTokenPopup(request);
          return response.idToken;
        } catch (popupError) {
          console.error('Failed to acquire token via popup:', popupError);
          return null;
        }
      }
      console.error('Failed to acquire token silently:', error);
      return null;
    }
  };

  return { getToken };
}
