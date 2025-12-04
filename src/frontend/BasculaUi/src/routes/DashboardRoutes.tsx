
import OrdersPage from "../pages/OrdersPage";

const DashboardRoutes = {
    path: 'Dashboard',
    children: [
        {
            path: 'Orders',
            element: <OrdersPage></OrdersPage>
        },
        // {
        //     path: 'contracts',
        //     children: [
        //         {
        //             path: 'Sales',
        //             children: [
        //                 {
        //                     path: '',
        //                     element: <DashboardSalesContracts />
        //                 },
        //                 {
        //                     path: '',
        //                     element: <DashboardMainSalesContracts />,
        //                     children: [
        //                         {
        //                             path: 'principal',
        //                             element: <DashboardFormSalesContracts />
        //                         },
        //                         {
        //                             path: 'history',
        //                             element: <DashboardFormSalesHistory />
        //                         }
        //                     ]
        //                 }
        //             ]
        //         }
        // }
    
    ]
};

export default DashboardRoutes;