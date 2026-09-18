import {
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  useMediaQuery,
  Toolbar,
} from "@mui/material";
import DashboardIcon from "@mui/icons-material/Dashboard";
import PeopleIcon from "@mui/icons-material/People";
import { Link } from "react-router-dom";

const drawerWidth = 240;

interface SidebarProps {
  open: boolean;              // Estado del sidebar (solo para mobile)
  onClose: () => void;        // Función para cerrar drawer en mobile
}

export default function Sidebar({ open, onClose }: SidebarProps) {
  const isMobile = useMediaQuery("(max-width:900px)");

  const drawer = (
    <>
      {/* Empuja el contenido debajo del Topbar */}
      <Toolbar />

      <List>
        <ListItemButton component={Link} to="/dashboard/">
          <ListItemIcon><DashboardIcon /></ListItemIcon>
          <ListItemText primary="Dashboard" />
        </ListItemButton>

        <ListItemButton component={Link} to="/dashboard/orders">
          <ListItemIcon><PeopleIcon /></ListItemIcon>
          <ListItemText primary="Órdenes" />
        </ListItemButton>
      </List>
    </>
  );

  return (
    <Drawer
      variant={isMobile ? "temporary" : "permanent"}
      open={isMobile ? open : true}
      onClose={onClose}
      sx={{
        width: drawerWidth,
        flexShrink: 0,
        "& .MuiDrawer-paper": {
          width: drawerWidth,
          boxSizing: "border-box",
        },
      }}
    >
      {drawer}
    </Drawer>
  );
}
