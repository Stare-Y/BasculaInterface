import {
  AppBar,
  Toolbar,
  Typography,
  IconButton,
  Box,
  Avatar,
  Menu,
  MenuItem,
  Tooltip,
  useTheme,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import Brightness4Icon from "@mui/icons-material/Brightness4";
import Brightness7Icon from "@mui/icons-material/Brightness7";
import { useState } from "react";


interface TopbarProps {
  onToggleSidebar: () => void;
  onToggleTheme?: () => void; 
}

export default function Topbar({ onToggleSidebar, onToggleTheme } :TopbarProps) {
  const theme = useTheme();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  const openMenu = (e: React.MouseEvent<HTMLButtonElement>) => {
      setAnchorEl(e.currentTarget);
    };
  const closeMenu = () => setAnchorEl(null);

  return (
    <AppBar
      position="fixed"
      elevation={0}
      sx={{
        backdropFilter: "blur(8px)",
        backgroundColor: "rgba(0,0,0,0.7)",
        zIndex: (theme) => theme.zIndex.drawer + 1,
      }}
    >
      <Toolbar sx={{ display: "flex", justifyContent: "space-between" }}>
        
        {/* Botón menú  en mobile */}
        <IconButton
          sx={{ display: { xs: "flex", md: "none" }, mr: 1 }}
          color="inherit"
          onClick={onToggleSidebar}
        >
          <MenuIcon />
        </IconButton>

        {/* Título */}
        <Typography
          variant="h6"
          sx={{
            fontWeight: 600,
            letterSpacing: 1,
            textTransform: "uppercase",
          }}
        >
          Bascula XD
        </Typography>

        <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
          
          {/* Dark/Light Mode */}
          <IconButton color="inherit" onClick={onToggleTheme}>
            {theme.palette.mode === "dark" ? <Brightness7Icon /> : <Brightness4Icon />}
          </IconButton>

          {/* Avatar usuario */}
          <Tooltip title="Perfil">
            <IconButton onClick={openMenu} sx={{ p: 0 }}>
              <Avatar sx={{ bgcolor: "primary.main" }}>D</Avatar>
            </IconButton>
          </Tooltip>

          {/* Menú del avatar */}
          <Menu
            anchorEl={anchorEl}
            open={Boolean(anchorEl)}
            onClose={closeMenu}
            transformOrigin={{ horizontal: "right", vertical: "top" }}
            anchorOrigin={{ horizontal: "right", vertical: "bottom" }}
          >
            <MenuItem onClick={closeMenu}>Mi Perfil</MenuItem>
            <MenuItem onClick={closeMenu}>Configuración</MenuItem>
            <MenuItem onClick={closeMenu}>Cerrar Sesión</MenuItem>
          </Menu>

        </Box>
      </Toolbar>
    </AppBar>
  );
}
