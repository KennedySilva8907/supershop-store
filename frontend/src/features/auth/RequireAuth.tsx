import { Navigate, Outlet, useLocation } from "react-router";
import { useAuth } from "./AuthContext";

export function RequireAuth({ adminOnly = false }: { adminOnly?: boolean }) {
  const { user, status, isAdmin } = useAuth();
  const location = useLocation();

  if (status === "starting") {
    return (
      <div className="mx-auto max-w-7xl px-6 py-24" aria-busy="true">
        <div className="h-8 w-52 animate-pulse bg-surface" />
      </div>
    );
  }

  if (user === null) {
    return <Navigate to="/entrar" replace state={{ from: location.pathname + location.search }} />;
  }

  if (adminOnly && !isAdmin) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
