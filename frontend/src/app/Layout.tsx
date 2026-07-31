import { Outlet, ScrollRestoration } from "react-router";
import { Footer } from "../components/layout/Footer";
import { Header } from "../components/layout/Header";

export function Layout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <main className="flex-1">
        <Outlet />
      </main>
      <Footer />
      <ScrollRestoration />
    </div>
  );
}
