import { Box, Toolbar } from "@mui/material";
import Sidebar from "../components/dashboard/Sidebar";
import Topbar from "../components/dashboard/Topbar";
import { Outlet } from 'react-router-dom';

export default function DashboardLayout({ children }) {
  return (
    <Box sx={{ display: "flex" }}>
      <Topbar />
      <Sidebar />

      <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
}