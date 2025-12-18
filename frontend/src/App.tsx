import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { loginRequest } from './auth/authConfig';
import StatementImportPage from './pages/StatementImportPage';

function App() {
  const isAuthenticated = useIsAuthenticated();
  const { instance, accounts } = useMsal();

  const handleLogin = () => {
    instance.loginRedirect(loginRequest).catch((error) => {
      console.error('Login failed:', error);
    });
  };

  const handleLogout = () => {
    instance.logoutRedirect().catch((error) => {
      console.error('Logout failed:', error);
    });
  };

  if (!isAuthenticated) {
    return (
      <div className="login-container">
        <div className="login-card">
          <h1>ExpenseFlow</h1>
          <p>Sign in to manage your expenses</p>
          <button onClick={handleLogin} className="login-button">
            Sign in with Microsoft
          </button>
        </div>
        <style>{`
          .login-container {
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
          }
          .login-card {
            background: white;
            padding: 48px;
            border-radius: 12px;
            box-shadow: 0 4px 24px rgba(0, 0, 0, 0.15);
            text-align: center;
            max-width: 400px;
            width: 90%;
          }
          .login-card h1 {
            margin: 0 0 8px;
            color: #1f2937;
            font-size: 28px;
          }
          .login-card p {
            margin: 0 0 32px;
            color: #6b7280;
          }
          .login-button {
            background: #0078d4;
            color: white;
            border: none;
            padding: 14px 32px;
            font-size: 16px;
            border-radius: 6px;
            cursor: pointer;
            transition: background 0.2s;
          }
          .login-button:hover {
            background: #106ebe;
          }
        `}</style>
      </div>
    );
  }

  return (
    <div className="app">
      <header className="app-header">
        <h1>ExpenseFlow</h1>
        <div className="user-info">
          <span>{accounts[0]?.name || accounts[0]?.username}</span>
          <button onClick={handleLogout} className="logout-button">
            Sign out
          </button>
        </div>
      </header>
      <main>
        <StatementImportPage />
      </main>
      <style>{`
        .app {
          min-height: 100vh;
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        }
        .app-header {
          background: #1f2937;
          color: white;
          padding: 16px 24px;
          display: flex;
          justify-content: space-between;
          align-items: center;
        }
        .app-header h1 {
          margin: 0;
          font-size: 20px;
        }
        .user-info {
          display: flex;
          align-items: center;
          gap: 16px;
        }
        .user-info span {
          color: #d1d5db;
        }
        .logout-button {
          background: transparent;
          border: 1px solid #6b7280;
          color: #d1d5db;
          padding: 8px 16px;
          border-radius: 4px;
          cursor: pointer;
          transition: all 0.2s;
        }
        .logout-button:hover {
          background: #374151;
          border-color: #9ca3af;
        }
        main {
          padding: 24px;
          max-width: 1200px;
          margin: 0 auto;
        }
      `}</style>
    </div>
  );
}

export default App;
