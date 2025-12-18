import ReactDOM from 'react-dom/client'
import { PublicClientApplication, EventType, EventMessage, AuthenticationResult } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import { msalConfig } from './auth/authConfig'
import App from './App'

// Create MSAL instance
const msalInstance = new PublicClientApplication(msalConfig);

// Handle redirect promise on page load
msalInstance.initialize().then(() => {
  // Handle redirect response if coming back from login
  msalInstance.handleRedirectPromise().then((response) => {
    if (response) {
      msalInstance.setActiveAccount(response.account);
    }
  }).catch((error) => {
    console.error('Redirect error:', error);
  });

  // Set active account on login success
  msalInstance.addEventCallback((event: EventMessage) => {
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
      const payload = event.payload as AuthenticationResult;
      msalInstance.setActiveAccount(payload.account);
    }
  });

  // Check if there's already an active account
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length > 0) {
    msalInstance.setActiveAccount(accounts[0]);
  }

  // Render app
  ReactDOM.createRoot(document.getElementById('root')!).render(
    <MsalProvider instance={msalInstance}>
      <App />
    </MsalProvider>
  );
});
