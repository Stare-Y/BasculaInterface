import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'; // Importa Navigate
import DashboardRoutes from "./DashboardRoutes"; 
import DashboardLayout from "../layout/DashboardLayout";

// import AuthLayout from 'layout/Auth';
// import CustomerLayout from 'layout/Customer';
// import OperativeLayout from 'layout/Operative';

// import AuthGuard from 'guards/AuthGuard';
// import GuestGuard from 'guards/GuestGuard';

// // project import
// import LoginRoutes from './LoginRoutes';
// import CustomerRoutes from './CustomerRoutes';
// import OperativeRoutes from './OperativeRoutes';

// Función recursiva para renderizar rutas anidadas
const renderRoutes = routes => {
    return routes.map(route => {
        if (route.children) {
            return (
                <Route key={route.path} path={route.path} element={route.element}>
                    {renderRoutes(route.children)}
                </Route>
            );
        } else {
            return <Route key={route.path} path={route.path} element={route.element} />;
        }
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
