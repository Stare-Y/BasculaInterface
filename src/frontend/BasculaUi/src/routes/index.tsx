import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'; // Importa Navigate
import DashboardRoutes from "./DashboardRoutes"; 
import DashboardLayout from "../layout/DashboardLayout";

import type { JSX, ReactNode } from "react";

export interface AppRoute {
  path: string;
  element: ReactNode;
  children?: AppRoute[];
}

export const renderRoutes = (routes: AppRoute[]): JSX.Element[] => {
  return routes.map((route) => {
    if (route.children && route.children.length > 0) {
      return (
        <Route key={route.path} path={route.path} element={route.element}>
          {renderRoutes(route.children)}
        </Route>
      );
    }

    return (
      <Route key={route.path} path={route.path} element={route.element} />
    );
  });
};
// ==============================|| ROUTING RENDER ||============================== //

function AppRoutes() {
    return (
        <BrowserRouter>
            <Routes>
                {/* Rutas públicas (LoginRoutes) */}
                <Route
                    path={DashboardRoutes.path}
                    element={
                        <DashboardLayout/>
                    }
                >
                    {renderRoutes(DashboardRoutes.children)}
                </Route>

                {/* Rutas de cliente (CustomerRoutes) */}
                {/* <Route
                    path={CustomerRoutes.path}
                    element={
                        // <GuestGuard>
                            <CustomerLayout />
                        // </GuestGuard>
                    }
                > */}
                    {/* {renderRoutes(CustomerRoutes.children)}
                </Route> */}

                {/* Rutas de operativa (OperativeRoutes) */}
                {/* <Route
                    path={OperativeRoutes.path}
                    element={
                        <AuthGuard>
                            <OperativeLayout />
                        </AuthGuard>
                    }
                >
                    {renderRoutes(OperativeRoutes.children)}
                </Route> */}

                {/* Redirigir rutas no válidas a la página por defecto */}
                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </BrowserRouter>
    );
}

export default AppRoutes;
