import { useMsal } from "@azure/msal-react";
import { createContext, FC, useCallback, useState, useEffect, useRef } from "react";
import { AuthContextInterface, AuthProviderProps, User } from "./AuthInterfaces";

const authDefault = {
  user: undefined,
  login: () => {},
  logout: () => {},
};

export const AuthContext = createContext<AuthContextInterface>(authDefault);

export const AuthProvider: FC<AuthProviderProps> = ({ children, authScopes, onLoginError }) => {
  const { instance } = useMsal();

  const [user, setUser] = useState<User | undefined>();
  const loginSilentIntervalRef = useRef<NodeJS.Timeout>();

  const fetchUserInfo = useCallback(async () => {
    const userResult = await fetch("/api/v1/user");
    if (!userResult.ok) throw new Error(userResult.statusText);

    const userJson = await userResult.json();
    setUser({ id: userJson.id, fullName: userJson.fullName, isAdmin: userJson.isAdmin, email: userJson.email });
  }, [setUser]);

  const loginCompleted = useCallback(
    async (idToken: string) => {
      document.cookie = `geopilot.auth=${idToken};Path=/;Secure`;
      await fetchUserInfo();
    },
    [fetchUserInfo],
  );

  const logoutCompleted = useCallback(async () => {
    instance.setActiveAccount(null);
    await instance.clearCache();
    clearInterval(loginSilentIntervalRef.current);
    document.cookie = "geopilot.auth=;expires=Thu, 01 Jan 1970 00:00:00 GMT;Path=/;Secure";
    setUser(undefined);
  }, [setUser, instance]);

  const loginSilent = useCallback(async () => {
    try {
      await instance.initialize();
      const result = await instance.acquireTokenSilent({
        scopes: authScopes,
      });
      await loginCompleted(result.idToken);
    } catch (error) {
      console.warn("Failed to refresh authentication.", error);
      await logoutCompleted();
    }
  }, [instance, authScopes, loginCompleted, logoutCompleted]);

  const setRefreshTokenInterval = useCallback(() => {
    clearInterval(loginSilentIntervalRef.current);
    loginSilentIntervalRef.current = setInterval(loginSilent, 1000 * 60 * 5);
  }, [loginSilent]);

  // Fetch user info after reload
  const activeAccount = instance.getActiveAccount();
  const hasActiveAccount = activeAccount !== null;
  useEffect(() => {
    if (hasActiveAccount && !user) {
      loginSilent();
      setRefreshTokenInterval();
    }
  }, [hasActiveAccount, user, loginSilent, setRefreshTokenInterval]);

  async function login() {
    try {
      const result = await instance.loginPopup({
        clientId: "geopilot-client",
        authority: "http://localhost:4011/realms/geopilot",
        scopes: authScopes,
      });
      try {
        await loginCompleted(result.idToken);
        instance.setActiveAccount(result.account);
        setRefreshTokenInterval();
      } catch (error) {
        onLoginError?.("Dieser Account ist nicht berechtigt zur Anmeldung.");
        await logoutCompleted();
      }
    } catch (error) {
      console.warn("Login failed.", error);
      await logoutCompleted();
    }
  }

  async function logout() {
    try {
      await instance.logoutPopup();
      await logoutCompleted();
    } catch (error) {
      console.warn(error);
    }
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        login,
        logout,
      }}>
      {children}
    </AuthContext.Provider>
  );
};
