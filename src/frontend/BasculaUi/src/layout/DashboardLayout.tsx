import { Box, Toolbar } from "@mui/material";
import Sidebar from "../components/dashboard/Sidebar";
import Topbar from "../components/dashboard/Topbar";
import { Outlet } from "react-router-dom";
import { useState } from "react";

export default function DashboardLayout() {
  // Estado del sidebar en mobile
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <>
      {/* Topbar con botón de menú */}
      <Topbar
        onToggleSidebar={() => setSidebarOpen(true)}
        onToggleTheme={() => console.log("Cambiar tema (opcional)")}
      />

      {/* Layout principal */}
      <Box sx={{ display: "flex" }}>
        <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />

        <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
          {/* Empuja el contenido debajo del Topbar */}
          <Toolbar />
          <Outlet />
        </Box>
      </Box>
    </>
  );
}
