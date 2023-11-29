import { PublicClientApplication } from "@azure/msal-browser";
import { MsalProvider } from "@azure/msal-react";
import { createContext, useCallback, useState, useMemo, useEffect, useRef } from "react";

const authDefault = {
  user: undefined,
  login: () => {},
  logout: () => {},
};

export const AuthContext = createContext(authDefault);

export const AuthProvider = ({ children, authScopes, oauth }) => {
  const msalInstance = useMemo(() => {
    return new PublicClientApplication(oauth ?? {});
  }, [oauth]);

  const [user, setUser] = useState();
  const loginSilentIntervalRef = useRef();

  const fetchUserInfo = useCallback(async () => {
    const userResult = await fetch("/api/v1/user");
    if (!userResult.ok) throw new Error(userResult.statusText);

    const userJson = await userResult.json();
    setUser({ name: userJson.fullName, isAdmin: userJson.isAdmin });
  }, [setUser]);

  const loginCompleted = useCallback(
    async (idToken) => {
      document.cookie = `geocop.auth=${idToken};Path=/;Secure`;
      await fetchUserInfo();
    },
    [fetchUserInfo],
  );

  const logoutCompleted = useCallback(() => {
    msalInstance.setActiveAccount(null);
    clearInterval(loginSilentIntervalRef.current);
    document.cookie = "geocop.auth=;expires=Thu, 01 Jan 1970 00:00:00 GMT;Path=/;Secure";
    setUser(undefined);
  }, [setUser, msalInstance]);

  const loginSilent = useCallback(async () => {
    try {
      await msalInstance.initialize();
      const result = await msalInstance.acquireTokenSilent({
        scopes: authScopes,
      });
      loginCompleted(result.idToken);
    } catch (error) {
      console.warn("Failed to refresh authentication.", error);
      logoutCompleted();
    }
  }, [msalInstance, authScopes, loginCompleted, logoutCompleted]);

  const setRefreshTokenInterval = useCallback(() => {
    clearInterval(loginSilentIntervalRef.current);
    loginSilentIntervalRef.current = setInterval(loginSilent, 1000 * 60 * 5);
  }, [loginSilent]);

  // Fetch user info after reload
  const activeAccount = msalInstance.getActiveAccount();
  const hasActiveAccount = activeAccount !== null;
  useEffect(() => {
    if (hasActiveAccount && !user) {
      loginSilent();
      setRefreshTokenInterval();
    }
  }, [hasActiveAccount, user, loginSilent, setRefreshTokenInterval]);

  async function login() {
    try {
      const result = await msalInstance.loginPopup({
        scopes: authScopes,
      });
      msalInstance.setActiveAccount(result.account);
      loginCompleted(result.idToken);
      setRefreshTokenInterval();
    } catch (error) {
      console.warn(error);
    }
  }

  async function logout() {
    try {
      await msalInstance.logoutPopup();
      logoutCompleted();
    } catch (error) {
      console.warn(error);
    }
  }

  return (
    <MsalProvider instance={msalInstance}>
      <AuthContext.Provider
        value={{
          user,
          login,
          logout,
        }}
      >
        {children}
      </AuthContext.Provider>
    </MsalProvider>
  );
};