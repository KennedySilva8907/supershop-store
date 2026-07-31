import { createBrowserRouter } from "react-router";
import { RequireAuth } from "../features/auth/RequireAuth";
import { AccountPage } from "../pages/account/AccountPage";
import { AddressesPage } from "../pages/account/AddressesPage";
import { ConfirmEmailPage } from "../pages/auth/ConfirmEmailPage";
import { ForgotPasswordPage } from "../pages/auth/ForgotPasswordPage";
import { RegisterPage } from "../pages/auth/RegisterPage";
import { ResetPasswordPage } from "../pages/auth/ResetPasswordPage";
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
      { path: "recuperar-password", element: <ForgotPasswordPage /> },
      { path: "nova-password", element: <ResetPasswordPage /> },

      {
        element: <RequireAuth />,
        children: [
          { path: "conta", element: <AccountPage /> },
          { path: "conta/moradas", element: <AddressesPage /> },
        ],
      },
    ],
  },
]);
