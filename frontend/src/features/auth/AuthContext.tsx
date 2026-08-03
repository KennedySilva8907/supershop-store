import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { ApiError, apiSend, configureAuth, postRaw } from "../../lib/apiClient";
import type {
  AuthResponse,
  ChangePasswordPayload,
  LoginPayload,
  RegisterPayload,
  UpdateProfilePayload,
  User,
} from "../../types/auth";

interface AuthState {
  user: User | null;
  status: "starting" | "ready";
  isAdmin: boolean;
  signIn: (payload: LoginPayload) => Promise<void>;
  register: (payload: RegisterPayload) => Promise<User>;
  updateProfile: (payload: UpdateProfilePayload) => Promise<void>;
  changePassword: (payload: ChangePasswordPayload) => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [status, setStatus] = useState<AuthState["status"]>("starting");
  const accessToken = useRef<string | null>(null);

  const apply = useCallback((response: AuthResponse) => {
    accessToken.current = response.accessToken;
    setUser(response.user);
  }, []);

  const clear = useCallback(() => {
    accessToken.current = null;
    setUser(null);
  }, []);

  const refreshSession = useCallback(async () => {
    try {
      apply(await postRaw<AuthResponse>("/auth/refresh"));
      return true;
    } catch {
      clear();
      return false;
    }
  }, [apply, clear]);

  configureAuth({
    readAccessToken: () => accessToken.current,
    refreshSession,
    onSessionExpired: clear,
  });

  useEffect(() => {
    let cancelled = false;

    refreshSession().finally(() => {
      if (!cancelled) setStatus("ready");
    });

    return () => {
      cancelled = true;
    };
  }, [refreshSession]);

  const signIn = useCallback(
    async (payload: LoginPayload) => apply(await apiSend<AuthResponse>("POST", "/auth/login", payload)),
    [apply],
  );

  const register = useCallback(
    (payload: RegisterPayload) => apiSend<User>("POST", "/auth/register", payload),
    [],
  );

  const updateProfile = useCallback(async (payload: UpdateProfilePayload) => {
    setUser(await apiSend<User>("PUT", "/me", payload));
  }, []);

  const changePassword = useCallback(
    async (payload: ChangePasswordPayload) =>
      apply(await apiSend<AuthResponse>("PUT", "/me/password", payload)),
    [apply],
  );

  const signOut = useCallback(async () => {
    try {
      await apiSend("POST", "/auth/logout");
    } catch (error) {
      if (!(error instanceof ApiError)) throw error;
    } finally {
      clear();
    }
  }, [clear]);

  const value = useMemo<AuthState>(
    () => ({
      user,
      status,
      isAdmin: user?.roles.includes("Admin") ?? false,
      signIn,
      register,
      updateProfile,
      changePassword,
      signOut,
    }),
    [user, status, signIn, register, updateProfile, changePassword, signOut],
  );

  return <AuthContext value={value}>{children}</AuthContext>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);

  if (context === null) {
    throw new Error("useAuth tem de ser usado dentro de AuthProvider.");
  }

  return context;
}
