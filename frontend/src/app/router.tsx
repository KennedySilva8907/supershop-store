import { createBrowserRouter } from "react-router";
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
    ],
  },
]);
