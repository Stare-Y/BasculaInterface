import { Drawer, List, ListItemButton, ListItemIcon, ListItemText } from "@mui/material";
import DashboardIcon from "@mui/icons-material/Dashboard";
import PeopleIcon from "@mui/icons-material/People";
import { Link } from "react-router-dom";

const drawerWidth = 240;

export default function Sidebar() {
  return (
    <Drawer
      variant="permanent"
      sx={{
        width: drawerWidth,
        "& .MuiDrawer-paper": {
          width: drawerWidth,
          boxSizing: "border-box"
        }
      }}
    >
      <List>
        <ListItemButton component={Link} to="/">
          <ListItemIcon><DashboardIcon /></ListItemIcon>
          <ListItemText primary="Dashboard" />
        </ListItemButton>

        <ListItemButton component={Link} to="/dashboard/orders">
          <ListItemIcon><PeopleIcon /></ListItemIcon>
          <ListItemText primary="Ordenes" />
        </ListItemButton>
      </List>
    </Drawer>
  );
}