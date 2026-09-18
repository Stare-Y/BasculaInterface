
import OrdersPage from "../pages/OrdersPage";
import OrderFormPage from "../pages/OrderFormPage";


const DashboardRoutes = {
    path: 'Dashboard',
    children: [
        {
            path: '',
            element: <div>dashboard xd</div>
        },
        {
            path: 'Orders',
            element: <OrdersPage></OrdersPage>
        },
        {
            path: "Orders/:id",
            element: <OrderFormPage />  
        }
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