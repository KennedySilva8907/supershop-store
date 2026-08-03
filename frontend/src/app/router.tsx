import { createBrowserRouter } from "react-router";
import { RequireAuth } from "../features/auth/RequireAuth";
import { AccountPage } from "../pages/account/AccountPage";
import { AddressesPage } from "../pages/account/AddressesPage";
import { ChangePasswordPage } from "../pages/account/ChangePasswordPage";
import { OrdersPage } from "../pages/account/OrdersPage";
import { ConfirmEmailPage } from "../pages/auth/ConfirmEmailPage";
import { ForgotPasswordPage } from "../pages/auth/ForgotPasswordPage";
import { RegisterPage } from "../pages/auth/RegisterPage";
import { ResetPasswordPage } from "../pages/auth/ResetPasswordPage";
import { SignInPage } from "../pages/auth/SignInPage";
import { CartPage } from "../pages/cart/CartPage";
import { CatalogPage } from "../pages/catalog/CatalogPage";
import { CheckoutPage } from "../pages/checkout/CheckoutPage";
import { HomePage } from "../pages/home/HomePage";
import { OrderPage } from "../pages/order/OrderPage";
import { ProductPage } from "../pages/product/ProductPage";
import { AdminLayout } from "./AdminLayout";
import { AdminOrderPage } from "../pages/admin/AdminOrderPage";
import { AdminOrdersPage } from "../pages/admin/AdminOrdersPage";
import { AdminProductsPage } from "../pages/admin/AdminProductsPage";
import { DashboardPage } from "../pages/admin/DashboardPage";
import { StockGridPage } from "../pages/admin/StockGridPage";
import { Layout } from "./Layout";

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: "catalogo", element: <CatalogPage /> },
      { path: "catalogo/:categorySlug", element: <CatalogPage /> },
      { path: "produto/:slug", element: <ProductPage /> },
      { path: "carrinho", element: <CartPage /> },

      { path: "entrar", element: <SignInPage /> },
      { path: "registar", element: <RegisterPage /> },
      { path: "confirmar-email", element: <ConfirmEmailPage /> },
      { path: "recuperar-password", element: <ForgotPasswordPage /> },
      { path: "nova-password", element: <ResetPasswordPage /> },

      {
        element: <RequireAuth />,
        children: [
          { path: "checkout", element: <CheckoutPage /> },
          { path: "encomenda/:orderNumber", element: <OrderPage /> },
          { path: "conta", element: <AccountPage /> },
          { path: "conta/moradas", element: <AddressesPage /> },
          { path: "conta/password", element: <ChangePasswordPage /> },
          { path: "conta/encomendas", element: <OrdersPage /> },
        ],
      },
    ],
  },
  {
    element: <RequireAuth adminOnly />,
    children: [
      {
        element: <AdminLayout />,
        children: [
          { path: "admin", element: <DashboardPage /> },
          { path: "admin/produtos", element: <AdminProductsPage /> },
          { path: "admin/produtos/:id/stock", element: <StockGridPage /> },
          { path: "admin/encomendas", element: <AdminOrdersPage /> },
          { path: "admin/encomendas/:id", element: <AdminOrderPage /> },
        ],
      },
    ],
  },
]);
