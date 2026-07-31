import { createBrowserRouter } from "react-router";
import { RequireAuth } from "../features/auth/RequireAuth";
import { AccountPage } from "../pages/account/AccountPage";
import { ConfirmEmailPage } from "../pages/auth/ConfirmEmailPage";
import { RegisterPage } from "../pages/auth/RegisterPage";
import { SignInPage } from "../pages/auth/SignInPage";
import { CatalogPage } from "../pages/catalog/CatalogPage";
import { HomePage } from "../pages/home/HomePage";
import { ProductPage } from "../pages/product/ProductPage";
import { Layout } from "./Layout";

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: "catalogo", element: <CatalogPage /> },
      { path: "catalogo/:categorySlug", element: <CatalogPage /> },
      { path: "produto/:slug", element: <ProductPage /> },
      { path: "entrar", element: <SignInPage /> },
      { path: "registar", element: <RegisterPage /> },
      { path: "confirmar-email", element: <ConfirmEmailPage /> },
      {
        element: <RequireAuth />,
        children: [{ path: "conta", element: <AccountPage /> }],
      },
    ],
  },
]);
